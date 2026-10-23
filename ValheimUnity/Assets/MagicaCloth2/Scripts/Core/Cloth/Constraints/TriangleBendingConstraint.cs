// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// トライアングル曲げ制約
    /// </summary>
    public class TriangleBendingConstraint : IDisposable
    {
        /// <summary>
        /// ボリュームとして処理する判定フラグ
        /// </summary>
        const sbyte VOLUME_SIGN = 100;

        public enum Method
        {
            None = 0,

            /// <summary>
            /// ２面角による曲げ制御
            /// ２面は初期の角度を保つように移動する。ただし角度のみなので±の近い方に曲る。
            /// </summary>
            DihedralAngle = 1,

            /// <summary>
            /// 方向性ありの２面角曲げ制約
            /// 初期姿勢を保つように復元する
            /// </summary>
            DirectionDihedralAngle = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Restoring force (0.0 ~ 1.0)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float stiffness;

            public SerializeData()
            {
                //method = Method.DirectionDihedralAngle;
                stiffness = 1.0f;
            }

            public void DataValidate()
            {
                stiffness = Mathf.Clamp01(stiffness);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    //method = method,
                    stiffness = stiffness,
                };
            }
        }

        public struct TriangleBendingConstraintParams
        {
            public Method method;
            public float stiffness;

            public void Convert(SerializeData sdata)
            {
                // モードはDirectionDihedralAngleに固定する
                method = sdata.stiffness > Define.System.Epsilon ? Method.DirectionDihedralAngle : Method.None;
                //method = sdata.stiffness > Define.System.Epsilon ? Method.DihedralAngle : Method.None;

                stiffness = sdata.stiffness;
            }
        }

        //=========================================================================================
        [System.Serializable]
        public class ConstraintData : IValid
        {
            public ResultCode result;
            public ulong[] trianglePairArray;
            public float[] restAngleOrVolumeArray;
            public sbyte[] signOrVolumeArray;

            public int writeBufferCount;
            public uint[] writeDataArray;
            public uint[] writeIndexArray;

            public bool IsValid()
            {
                return trianglePairArray != null && trianglePairArray.Length > 0;
            }
        }

        /// <summary>
        /// トライアングルペアの４頂点をushortとしてulongにパッキングしたもの
        ///   v2 +
        ///     /|\
        /// v0 + | + v1
        ///     \|/
        ///   v3 +
        /// 上位ビットからv0-v1-v2-v3の順で並んでいる
        /// </summary>
        public ExNativeArray<ulong> trianglePairArray;

        /// <summary>
        /// トライアングルペアごとの復元角度もしくはボリューム値
        /// </summary>
        public ExNativeArray<float> restAngleOrVolumeArray;

        /// <summary>
        /// トライアングルペアごとの復元方向もしくはボリューム判定（VOLUME_SIGN(100)=このペアはボリュームである）
        /// </summary>
        public ExNativeArray<sbyte> signOrVolumeArray;

        //public int DataCount => trianglePairArray?.Count ?? 0;

        /// <summary>
        /// ボリューム計算の浮動小数点誤差を回避するための倍数
        /// </summary>
        const float VolumeScale = 1000.0f;

        //=========================================================================================
        public TriangleBendingConstraint()
        {
            trianglePairArray = new ExNativeArray<ulong>(0, true);
            restAngleOrVolumeArray = new ExNativeArray<float>(0, true);
            signOrVolumeArray = new ExNativeArray<sbyte>(0, true);
        }

        public void Dispose()
        {
            trianglePairArray?.Dispose();
            restAngleOrVolumeArray?.Dispose();
            signOrVolumeArray?.Dispose();

            trianglePairArray = null;
            restAngleOrVolumeArray = null;
            signOrVolumeArray = null;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[TriangleBendConstraint]");
            sb.AppendLine($"  -trianglePairArray:{trianglePairArray.ToSummary()}");
            sb.AppendLine($"  -restAngleOrVolumeArray:{restAngleOrVolumeArray.ToSummary()}");
            sb.AppendLine($"  -signOrVolumeArray:{signOrVolumeArray.ToSummary()}");

            return sb.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// 制約データの作成
        /// </summary>
        /// <param name="proxyMesh"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static ConstraintData CreateData(VirtualMesh proxyMesh, in ClothParameters parameters)
        {
            var constraintData = new ConstraintData();

            try
            {
                // アルゴリズムの有無にかかわらずトライアングルがあるなら生成する
                //var method = parameters.triangleBendingConstraint.method;
                //if (method == Method.None)
                //    return null;

                if (proxyMesh.TriangleCount == 0)
                    return null;

                int ecnt = proxyMesh.EdgeCount;
                if (ecnt == 0)
                    return null;

                var trianglePairList = new List<ulong>(ecnt * 2);
                var restAngleOrVolumeList = new List<float>(ecnt * 2);
                var signOrVolumeList = new List<sbyte>(ecnt * 2);
                var writeDataList = new List<uint>(ecnt * 2);
                var volumeSet = new HashSet<int4>();

                int bendingCount = 0;
                int volumeCount = 0;

                int vcnt = proxyMesh.VertexCount;
                using var multiBuilder = new MultiDataBuilder<byte>(vcnt, vcnt * 2);

                // エッジごとの接続トライアングルをループ
                for (int k = 0; k < ecnt; k++)
                {
                    int2 edge = proxyMesh.edges[k];
                    if (proxyMesh.edgeToTriangles.ContainsKey(edge) == false)
                        continue;

                    var triangles = proxyMesh.edgeToTriangles.MC2ToFixedList128Bytes(edge);
                    int tcnt = triangles.Length;

                    // トライアングルの組み合わせ
                    for (int i = 0; i < tcnt - 1; i++)
                    {
                        // 0
                        int tindex0 = triangles[i];
                        int3 tri0 = proxyMesh.triangles[tindex0];

                        for (int j = i + 1; j < tcnt; j++)
                        {
                            // 1
                            int tindex1 = triangles[j];
                            int3 tri1 = proxyMesh.triangles[tindex1];

                            // 対角点
                            int2 dp = MathUtility.GetRestTriangleVertex(tri0, tri1, edge);

                            // 4点の構成
                            // 頂点インデックス形成
                            //   v2 +
                            //     /|\
                            // v0 + | + v1
                            //     \|/
                            //   v3 +
                            // 2/3が共通の辺, 0/1が対角点
                            int4 vtx = new int4(dp.x, dp.y, edge.x, edge.y);

                            // ４点がすべて固定ならば除外する
                            var attr0 = proxyMesh.attributes[vtx.x];
                            var attr1 = proxyMesh.attributes[vtx.y];
                            var attr2 = proxyMesh.attributes[vtx.z];
                            var attr3 = proxyMesh.attributes[vtx.w];
                            if (attr0.IsDontMove() && attr1.IsDontMove() && attr2.IsDontMove() && attr3.IsDontMove())
                                continue;

                            // １点でも無効なら除外する
                            if (attr0.IsInvalid() || attr1.IsInvalid() || attr2.IsInvalid() || attr3.IsInvalid())
                                continue;

                            ulong pair = DataUtility.Pack64(vtx);

                            // (1)TriangleBendingとして登録判定
                            float restData;
                            sbyte signFlag;
                            InitDihedralAngle(proxyMesh, vtx.x, vtx.y, vtx.z, vtx.w, out restData, out signFlag);
                            var degAngle = math.abs(math.degrees(restData));
                            if (degAngle < Define.System.TriangleBendingMaxAngle) // 90度以上で登録すると不安定になる
                            {
                                trianglePairList.Add(pair);
                                restAngleOrVolumeList.Add(restData);
                                signOrVolumeList.Add(signFlag);
                                //Debug.Log($"rest angle:{math.degrees(restData)}, signFlag:{signFlag}");

                                uint writeData = DataUtility.Pack32(
                                    multiBuilder.CountValuesForKey(vtx.x),
                                    multiBuilder.CountValuesForKey(vtx.y),
                                    multiBuilder.CountValuesForKey(vtx.z),
                                    multiBuilder.CountValuesForKey(vtx.w)
                                    );
                                writeDataList.Add(writeData);

                                multiBuilder.Add(vtx.x, 0);
                                multiBuilder.Add(vtx.y, 0);
                                multiBuilder.Add(vtx.z, 0);
                                multiBuilder.Add(vtx.w, 0);

                                bendingCount++;
                            }
                            // (2)Volumeとして登録判定
                            if (degAngle >= Define.System.VolumeMinAngle && degAngle <= 179.0f)
                            {
                                var sortPack = DataUtility.PackInt4(vtx);
                                if (volumeSet.Contains(sortPack) == false)
                                {

                                    InitVolume(proxyMesh, vtx.x, vtx.y, vtx.z, vtx.w, out restData, out signFlag);
                                    trianglePairList.Add(pair);
                                    restAngleOrVolumeList.Add(restData);
                                    signOrVolumeList.Add(signFlag);
                                    volumeSet.Add(sortPack);

                                    uint writeData = DataUtility.Pack32(
                                        multiBuilder.CountValuesForKey(vtx.x),
                                        multiBuilder.CountValuesForKey(vtx.y),
                                        multiBuilder.CountValuesForKey(vtx.z),
                                        multiBuilder.CountValuesForKey(vtx.w)
                                        );
                                    writeDataList.Add(writeData);

                                    multiBuilder.Add(vtx.x, 0);
                                    multiBuilder.Add(vtx.y, 0);
                                    multiBuilder.Add(vtx.z, 0);
                                    multiBuilder.Add(vtx.w, 0);

                                    volumeCount++;

                                    //Develop.DebugLog($"Volume Pair. edge:{edge}, tri:({tri0},{tri1}) restAngle:{degAngle}, restData:{restData}, signFlag:{signFlag}");
                                }
                            }
                            //if (math.all(tri0 - 243) == false || math.all(tri1 - 243) == false)
                            //    Debug.Log($"Bend Triangle Pair. edge:{edge}, tri:({tri0},{tri1})");
                        }
                    }
                }

                // データ格納
                constraintData.trianglePairArray = trianglePairList.Count > 0 ? trianglePairList.ToArray() : null;
                constraintData.restAngleOrVolumeArray = restAngleOrVolumeList.Count > 0 ? restAngleOrVolumeList.ToArray() : null;
                constraintData.signOrVolumeArray = signOrVolumeList.Count > 0 ? signOrVolumeList.ToArray() : null;
                constraintData.writeDataArray = writeDataList.Count > 0 ? writeDataList.ToArray() : null;
                constraintData.writeBufferCount = multiBuilder.Count();
                constraintData.writeIndexArray = multiBuilder.ToIndexArray();

                constraintData.result.SetSuccess();

                //Develop.DebugLog($"TriangleBending:{bendingCount}, Volume:{volumeCount}");
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                constraintData.result.SetError(Define.Result.Constraint_CreateTriangleBendingException);
                throw;
            }
            finally
            {
            }

            return constraintData;
        }

        static void InitVolume(VirtualMesh proxyMesh, int v0, int v1, int v2, int v3, out float volumeRest, out sbyte signFlag)
        {
            // 0/1が対角点,2/3が共通辺
            // ここは実行時とボリューム値を合わせるためワールド座標で計算する必要がある。
            float3 pos0 = MathUtility.TransformPoint(proxyMesh.localPositions[v0], proxyMesh.initLocalToWorld);
            float3 pos1 = MathUtility.TransformPoint(proxyMesh.localPositions[v1], proxyMesh.initLocalToWorld);
            float3 pos2 = MathUtility.TransformPoint(proxyMesh.localPositions[v2], proxyMesh.initLocalToWorld);
            float3 pos3 = MathUtility.TransformPoint(proxyMesh.localPositions[v3], proxyMesh.initLocalToWorld);

            volumeRest = (1.0f / 6.0f) * math.dot(math.cross(pos1 - pos0, pos2 - pos0), pos3 - pos0);
            volumeRest *= VolumeScale; // 浮動小数点演算誤差回避
            signFlag = VOLUME_SIGN; // Volume
        }

        static void InitDihedralAngle(VirtualMesh proxyMesh, int v0, int v1, int v2, int v3, out float restAngle, out sbyte signFlag)
        {
            // 0/1が対角点,2/3が共通辺
            float3 pos0 = proxyMesh.localPositions[v0];
            float3 pos1 = proxyMesh.localPositions[v1];
            float3 pos2 = proxyMesh.localPositions[v2];
            float3 pos3 = proxyMesh.localPositions[v3];

            float3 n1 = math.cross(pos2 - pos0, pos3 - pos0);
            float3 n2 = math.cross(pos3 - pos1, pos2 - pos1);
            n1 = math.normalize(n1);
            n2 = math.normalize(n2);
            float dot = math.dot(n1, n2);
            dot = MathUtility.Clamp1(dot);

            restAngle = math.acos(dot);

            // 方向性の算出
            // 復元方向を正負のフラグとして格納する
            float3 e = pos3 - pos2;
            float dir = math.dot(math.cross(n1, n2), e);
            float sign = math.sign(dir);
            //Debug.Assert(sign != 0);
            //dihedralRestAngle *= sign;

            signFlag = sign < 0 ? (sbyte)-1 : (sbyte)1;
        }

        //=========================================================================================
        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Register(ClothProcess cprocess)
        {
            if (cprocess?.bendingConstraintData?.IsValid() ?? false)
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);

                var cdata = cprocess.bendingConstraintData;
                tdata.bendingPairChunk = trianglePairArray.AddRange(cdata.trianglePairArray);
                restAngleOrVolumeArray.AddRange(cdata.restAngleOrVolumeArray);
                signOrVolumeArray.AddRange(cdata.signOrVolumeArray);
            }
        }

        /// <summary>
        /// 制約データを解除する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Exit(ClothProcess cprocess)
        {
            if (cprocess != null && cprocess.TeamId > 0)
            {
                ref var tdata = ref MagicaManager.Team.GetTeamDataRef(cprocess.TeamId);

                trianglePairArray.Remove(tdata.bendingPairChunk);
                restAngleOrVolumeArray.Remove(tdata.bendingPairChunk);
                signOrVolumeArray.Remove(tdata.bendingPairChunk);

                tdata.bendingPairChunk.Clear();
            }
        }

        //=========================================================================================
        // Solver
        //=========================================================================================
        internal unsafe static void SolverConstraint(
            DataChunk chunk,
            in float4 simulationPower,
            // team
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float> depthArray,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float> frictionArray,
            // constraints
            ref NativeArray<ulong> trianglePairArray,
            ref NativeArray<float> restAngleOrVolumeArray,
            ref NativeArray<sbyte> signOrVolumeArray,
            // buffer2
            ref NativeArray<float3> tempVectorBufferA,
            ref NativeArray<int> tempCountBuffer
            )
        {
            if (param.triangleBendingConstraint.method == Method.None)
                return;

            if (tdata.bendingPairChunk.IsValid == false)
                return;

            // 剛性
            float stiffness = param.triangleBendingConstraint.stiffness;
            if (stiffness < 1e-06f)
                return;
            stiffness = math.saturate(stiffness * simulationPower.y);

            int p_start = tdata.particleChunk.startIndex;
            int v_start = tdata.proxyCommonChunk.startIndex;
            int pindex;
            int vindex;

            int* sumPt = (int*)tempVectorBufferA.GetUnsafePtr();
            int* cntPt = (int*)tempCountBuffer.GetUnsafePtr();

            // ■計算
            // ベンドペアごと
            //int pairIndex = tdata.bendingPairChunk.startIndex;
            int pairIndex = tdata.bendingPairChunk.startIndex + chunk.startIndex;
            //for (int k = 0; k < tdata.bendingPairChunk.dataLength; k++, pairIndex++)
            for (int k = 0; k < chunk.dataLength; k++, pairIndex++)
            {
                // トライアングルペア
                var pairData = trianglePairArray[pairIndex];
                int4 vertices = DataUtility.Unpack64(pairData);
                //Debug.Log(vertices);

                int4 pindex4 = vertices + p_start;
                int4 vindex4 = vertices + v_start;

                // 状態
                float3x4 nextPosBuffer = 0;
                float3x4 addPosBuffer = 0;
                float4 invMassBuffer = 1;
                for (int i = 0; i < 4; i++)
                {
                    pindex = pindex4[i];
                    vindex = vindex4[i];
                    nextPosBuffer[i] = nextPosArray[pindex];
                    float friction = frictionArray[pindex];
                    float depth = depthArray[vindex];
                    bool fix = attributes[vindex].IsDontMove();
                    invMassBuffer[i] = fix ? 0.01f : MathUtility.CalcInverseMass(friction, depth);
                }

                // データ
                int l_index = pairIndex - tdata.bendingPairChunk.startIndex;
                int dataIndex = tdata.bendingPairChunk.startIndex + l_index;
                float restAngle = restAngleOrVolumeArray[dataIndex];
                sbyte signOrVolume = signOrVolumeArray[dataIndex];

                // メソッドごとの解決
                bool result = false;
                if (signOrVolume == VOLUME_SIGN)
                {
                    // Volume
                    float volumeRest = restAngle * tdata.scaleRatio; // スケール倍率

                    // マイナススケール
                    volumeRest *= tdata.negativeScaleSign;

                    result = CalcVolume(nextPosBuffer, invMassBuffer, volumeRest, stiffness, ref addPosBuffer);
                }
                else
                {
                    // Triangle Bending
                    if (param.triangleBendingConstraint.method == Method.DihedralAngle)
                    {
                        // 方向性なし二面角
                        result = CalcDihedralAngle(0, nextPosBuffer, invMassBuffer, restAngle, stiffness, ref addPosBuffer);
                    }
                    else if (param.triangleBendingConstraint.method == Method.DirectionDihedralAngle)
                    {
                        // 方向性あり二面角
                        float sign = signOrVolume < 0 ? -1 : 1;
                        restAngle *= sign;

                        // マイナススケール
                        restAngle *= tdata.negativeScaleSign;

                        result = CalcDihedralAngle(sign, nextPosBuffer, invMassBuffer, restAngle, stiffness, ref addPosBuffer);
                    }
                }

                // 集計バッファへ格納
                if (result)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        pindex = pindex4[i];
                        InterlockUtility.AddFloat3(pindex, addPosBuffer[i], cntPt, sumPt);
                    }
                }
            }
        }

        internal unsafe static void SumConstraint(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            // particle
            ref NativeArray<float3> nextPosArray,
            // buffer2
            ref NativeArray<float3> tempVectorBufferA,
            ref NativeArray<int> tempCountBuffer
            )
        {
            if (param.triangleBendingConstraint.method == Method.None)
                return;

            if (tdata.bendingPairChunk.IsValid == false)
                return;

            // 剛性
            float stiffness = param.triangleBendingConstraint.stiffness;
            if (stiffness < 1e-06f)
                return;

            int p_start = tdata.particleChunk.startIndex;
            int v_start = tdata.proxyCommonChunk.startIndex;

            int* sumPt = (int*)tempVectorBufferA.GetUnsafePtr();
            int* cntPt = (int*)tempCountBuffer.GetUnsafePtr();

            // ■集計
            // パーティクルごと
            int pindex = p_start + chunk.startIndex;
            int vindex = v_start + chunk.startIndex;
            //for (int k = 0; k < tdata.particleChunk.dataLength; k++, pindex++, vindex++)
            for (int k = 0; k < chunk.dataLength; k++, pindex++, vindex++)
            {
                // 移動のみ
                if (attributes[vindex].IsDontMove() == false)
                {
                    int cnt = cntPt[pindex];
                    if (cnt > 0)
                    {
                        int pindex2 = pindex * 3;
                        float3 add = new float3(sumPt[pindex2], sumPt[pindex2 + 1], sumPt[pindex2 + 2]);
                        add /= cnt;
                        // データは固定小数点なので戻す
                        add *= InterlockUtility.ToFloat;

                        nextPosArray[pindex] = nextPosArray[pindex] + add;
                    }
                }

                // バッファクリア
                tempCountBuffer[pindex] = 0;
                tempVectorBufferA[pindex] = 0;
            }
        }

        static bool CalcVolume(
            in float3x4 nextPosBuffer,
            in float4 invMassBuffer,
            float volumeRest,
            float stiffness,
            ref float3x4 addPosBuffer
            )
        {
            float3 nextPos0 = nextPosBuffer[0];
            float3 nextPos1 = nextPosBuffer[1];
            float3 nextPos2 = nextPosBuffer[2];
            float3 nextPos3 = nextPosBuffer[3];

            float invMass0 = invMassBuffer[0];
            float invMass1 = invMassBuffer[1];
            float invMass2 = invMassBuffer[2];
            float invMass3 = invMassBuffer[3];

            float volume = (1.0f / 6.0f) * math.dot(math.cross(nextPos1 - nextPos0, nextPos2 - nextPos0), nextPos3 - nextPos0);
            volume *= VolumeScale; // 浮動小数点演算誤差回避

            float3 grad0 = math.cross(nextPos1 - nextPos2, nextPos3 - nextPos2);
            float3 grad1 = math.cross(nextPos2 - nextPos0, nextPos3 - nextPos0);
            float3 grad2 = math.cross(nextPos0 - nextPos1, nextPos3 - nextPos1);
            float3 grad3 = math.cross(nextPos1 - nextPos0, nextPos2 - nextPos0);

            float lambda =
                invMass0 * math.lengthsq(grad0) +
                invMass1 * math.lengthsq(grad1) +
                invMass2 * math.lengthsq(grad2) +
                invMass3 * math.lengthsq(grad3);
            lambda *= VolumeScale; // 浮動小数点演算誤差回避

            if (math.abs(lambda) < 1e-06f)
                return false;

            lambda = stiffness * (volumeRest - volume) / lambda;

            addPosBuffer[0] = lambda * invMass0 * grad0;
            addPosBuffer[1] = lambda * invMass1 * grad1;
            addPosBuffer[2] = lambda * invMass2 * grad2;
            addPosBuffer[3] = lambda * invMass3 * grad3;

            return true;
        }

        static bool CalcDihedralAngle(
            float sign,
            in float3x4 nextPosBuffer,
            in float4 invMassBuffer,
            float restAngle,
            float stiffness,
            ref float3x4 addPosBuffer
            )
        {
            float3 nextPos0 = nextPosBuffer[0];
            float3 nextPos1 = nextPosBuffer[1];
            float3 nextPos2 = nextPosBuffer[2];
            float3 nextPos3 = nextPosBuffer[3];

            float invMass0 = invMassBuffer[0];
            float invMass1 = invMassBuffer[1];
            float invMass2 = invMassBuffer[2];
            float invMass3 = invMassBuffer[3];

            float3 e = nextPos3 - nextPos2;
            float elen = math.length(e);
            if (elen < 1e-08f)
                return false;

            float invElen = 1.0f / elen;

            float3 n1 = math.cross(nextPos2 - nextPos0, nextPos3 - nextPos0);
            float3 n2 = math.cross(nextPos3 - nextPos1, nextPos2 - nextPos1);

            float n1_lengsq = math.lengthsq(n1);
            float n2_lengsq = math.lengthsq(n2);

            // 稀に発生する長さ０に対処
            if (n1_lengsq == 0.0f || n2_lengsq == 0.0f)
                return false;
            //Develop.Assert(n1_lengsq > 0.0f);
            //Develop.Assert(n2_lengsq > 0.0f);
            n1 /= n1_lengsq;
            n2 /= n2_lengsq;

            float3 d0 = elen * n1;
            float3 d1 = elen * n2;
            float3 d2 = math.dot(nextPos0 - nextPos3, e) * invElen * n1 + math.dot(nextPos1 - nextPos3, e) * invElen * n2;
            float3 d3 = math.dot(nextPos2 - nextPos0, e) * invElen * n1 + math.dot(nextPos2 - nextPos1, e) * invElen * n2;

            n1 = math.normalize(n1);
            n2 = math.normalize(n2);
            float dot = math.dot(n1, n2);
            dot = MathUtility.Clamp1(dot);
            float phi = math.acos(dot);

            float lambda =
                invMass0 * math.lengthsq(d0) +
                invMass1 * math.lengthsq(d1) +
                invMass2 * math.lengthsq(d2) +
                invMass3 * math.lengthsq(d3);

            if (lambda == 0.0f)
                return false;

            // 方向性
            float dirSign = math.sign(math.dot(math.cross(n1, n2), e));
            if (sign != 0)
            {
                // 方向性あり(DirectionDihedralAngle)
                phi *= dirSign;
            }
            else
            {
                // 方向性なし(DihedralAngle)
                lambda *= dirSign;
            }

            lambda = (restAngle - phi) / lambda * stiffness;

            float3 corr0 = -invMass0 * lambda * d0;
            float3 corr1 = -invMass1 * lambda * d1;
            float3 corr2 = -invMass2 * lambda * d2;
            float3 corr3 = -invMass3 * lambda * d3;

            addPosBuffer[0] = corr0;
            addPosBuffer[1] = corr1;
            addPosBuffer[2] = corr2;
            addPosBuffer[3] = corr3;

            return true;
        }
    }
}
