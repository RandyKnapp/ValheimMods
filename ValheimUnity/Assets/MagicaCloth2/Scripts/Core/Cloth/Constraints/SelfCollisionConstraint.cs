// Magica Cloth 2.
// Copyright (c) 2025 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class SelfCollisionConstraint : IDisposable
    {
        public enum SelfCollisionMode
        {
            None = 0,

            /// <summary>
            /// PointPoint
            /// </summary>
            //Point = 1, // omit!

            /// <summary>
            /// PointTriangle + EdgeEdge + Intersect
            /// </summary>
            FullMesh = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// self-collision mode
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public SelfCollisionMode selfMode;

            /// <summary>
            /// primitive thickness.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData surfaceThickness = new CurveSerializeData(0.005f, 0.5f, 1.0f, false);

            /// <summary>
            /// mutual collision mode.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public SelfCollisionMode syncMode;

            /// <summary>
            /// Mutual Collision Opponent.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public MagicaCloth syncPartner;

            /// <summary>
            /// cloth weight.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float clothMass = 0.0f;

            public SerializeData()
            {
                selfMode = SelfCollisionMode.None;
                syncMode = SelfCollisionMode.None;
            }

            public void DataValidate()
            {
                surfaceThickness.DataValidate(Define.System.SelfCollisionThicknessMin, Define.System.SelfCollisionThicknessMax);
                clothMass = Mathf.Clamp01(clothMass);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    selfMode = selfMode,
                    surfaceThickness = surfaceThickness.Clone(),
                    syncMode = syncMode,
                    syncPartner = syncPartner,
                    clothMass = clothMass,
                };
            }

            public MagicaCloth GetSyncPartner()
            {
                return syncMode != SelfCollisionMode.None ? syncPartner : null;
            }
        }

        public struct SelfCollisionConstraintParams
        {
            public SelfCollisionMode selfMode;
            public float4x4 surfaceThicknessCurveData;
            public SelfCollisionMode syncMode;
            public float clothMass;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
                selfMode = clothType == ClothProcess.ClothType.BoneSpring ? SelfCollisionMode.None : sdata.selfMode;
                surfaceThicknessCurveData = sdata.surfaceThickness.ConvertFloatArray();
                syncMode = clothType == ClothProcess.ClothType.BoneSpring ? SelfCollisionMode.None : sdata.syncMode;
                clothMass = sdata.clothMass;
            }
        }

        //=========================================================================================
        /// <summary>
        /// プリミティブ
        /// Point/Edge/Triangleの管理
        /// </summary>
        public const uint KindPoint = 0;
        public const uint KindEdge = 1;
        public const uint KindTriangle = 2;

        public const uint Flag_KindMask = 0x03000000; // 24~25bit
        public const uint Flag_Fix0 = 0x04000000;
        public const uint Flag_Fix1 = 0x08000000;
        public const uint Flag_Fix2 = 0x10000000;
        public const uint Flag_AllFix = 0x20000000;
        public const uint Flag_Ignore = 0x40000000; // 無効もしくは無視頂点が含まれる
        public const uint Flag_Enable = 0x80000000; // 接触判定有効
        public const uint Flag_Intersect0 = 0x00000001;
        public const uint Flag_Intersect1 = 0x00000002;
        public const uint Flag_Intersect2 = 0x00000004;

        public const uint Flag_FixIntersect0 = (Flag_Fix0 | Flag_Intersect0);
        public const uint Flag_FixIntersect1 = (Flag_Fix1 | Flag_Intersect1);
        public const uint Flag_FixIntersect2 = (Flag_Fix2 | Flag_Intersect2);

        unsafe internal struct Primitive : IComparable<Primitive>
        {
            /// <summary>
            /// フラグ
            /// </summary>
            public uint flag;

            /// <summary>
            /// プリミティブを構成するパーティクルインデックス
            /// 不要な軸は(-1)が設定されている
            /// </summary>
            public int3 particleIndices;

            public float3 invMass;

            /// <summary>
            /// プリミティブAABB
            /// </summary>
            public AABB aabb;

            /// <summary>
            /// UniformGird座標
            /// </summary>
            public int3 grid;

            public float depth;

            /// <summary>
            /// 厚み
            /// </summary>
            public float thickness;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsIgnore()
            {
                return (flag & Flag_Ignore) != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsAllFix()
            {
                return (flag & Flag_AllFix) != 0;
            }

            /// <summary>
            /// パーティクルインデックスが１つ以上重複しているか判定する
            /// </summary>
            /// <param name="pri"></param>
            /// <returns></returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool AnyParticle(ref Primitive pri)
            {
                uint kind = ((flag & Flag_KindMask) >> 24) + 1;

                for (int i = 0; i < kind; i++)
                {
                    int p = particleIndices[i];

                    // 入力すべてが非０ならtrue
                    if (math.all(pri.particleIndices - p) == false)
                        return true;
                }

                return false;
            }

            /// <summary>
            /// ソート用
            /// グリッドX->Y->Zの順でソート
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public int CompareTo(Primitive other)
            {
                if (grid.x != other.grid.x)
                    return grid.x - other.grid.x;
                if (grid.y != other.grid.y)
                    return grid.y - other.grid.y;
                return grid.z - other.grid.z;
            }
        }
        internal ExNativeArray<Primitive> primitiveArrayB;

        /// <summary>
        /// グリッド
        /// プリミティブ検出用のグリッド情報
        /// </summary>
        internal struct GridInfo : IComparable<GridInfo>
        {
            // このグリッドのハッシュ値
            public int hash;

            // このグリッドの開始プリミティブインデックス
            public int start;

            // このグリッドに格納されているプリミティブ数
            public int count;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int CompareTo(GridInfo other)
            {
                if (hash < other.hash)
                    return -1;
                else if (hash > other.hash)
                    return 1;
                else
                    return 0;
            }
        }
        internal ExNativeArray<GridInfo> uniformGridStartCountBuffer;

        /// <summary>
        /// コンタクト
        /// 衝突プリミティブペアの管理
        /// </summary>
        internal const byte ContactType_EdgeEdge = 0;
        internal const byte ContactType_PointTriangle = 1;
        internal const byte ContactType_TrianglePoint = 2;

        internal struct ContactInfo
        {
            public int primitiveIndex0;
            public int primitiveIndex1;
            public byte contactType;
            public byte enable;
            public half thickness;
            public half s;
            public half t;
            public half3 n;
        }

        internal NativeQueue<ContactInfo> contactQueue;
        internal NativeList<ContactInfo> contactList;

        /// <summary>
        /// インターセクト
        /// 絡まり防止のEdgeTriangleペアの管理
        /// </summary>
        internal struct IntersectInfo
        {
            public int2 edgeParticeIndices;
            public int3 triangleParticleIndices;
        }

        internal NativeQueue<IntersectInfo> intersectQueue;
        internal NativeList<IntersectInfo> intersectList;

        //=========================================================================================
        /// <summary>
        /// ポイントプリミティブ総数
        /// </summary>
        public int PointPrimitiveCount { get; private set; } = 0;

        /// <summary>
        /// エッジプリミティブ総数
        /// </summary>
        public int EdgePrimitiveCount { get; private set; } = 0;

        /// <summary>
        /// トライアングルプリミティブ総数
        /// </summary>
        public int TrianglePrimitiveCount { get; private set; } = 0;

        //=========================================================================================
        /// <summary>
        /// 交差解決フラグ(パーティクルと連動)
        /// </summary>
        internal NativeArray<byte> intersectFlagArray;

        internal int IntersectCount { get; private set; } = 0;

        //=========================================================================================
        public SelfCollisionConstraint()
        {
            intersectFlagArray = new NativeArray<byte>(0, Allocator.Persistent);

            primitiveArrayB = new ExNativeArray<Primitive>(0, true);
            uniformGridStartCountBuffer = new ExNativeArray<GridInfo>(0, true);
            contactQueue = new NativeQueue<ContactInfo>(Allocator.Persistent);
            contactList = new NativeList<ContactInfo>(Allocator.Persistent);
            intersectQueue = new NativeQueue<IntersectInfo>(Allocator.Persistent);
            intersectList = new NativeList<IntersectInfo>(Allocator.Persistent);

            //Develop.DebugLog($"UseQueueCount:{UseQueueCount}");
        }

        public void Dispose()
        {
            PointPrimitiveCount = 0;
            EdgePrimitiveCount = 0;
            TrianglePrimitiveCount = 0;

            intersectFlagArray.MC2DisposeSafe();

            primitiveArrayB?.Dispose();
            primitiveArrayB = null;
            uniformGridStartCountBuffer?.Dispose();
            uniformGridStartCountBuffer = null;

            if (contactQueue.IsCreated)
                contactQueue.Dispose();
            if (contactList.IsCreated)
                contactList.Dispose();
            if (intersectQueue.IsCreated)
                intersectQueue.Dispose();
            if (intersectList.IsCreated)
                intersectList.Dispose();

            IntersectCount = 0;
        }

        /// <summary>
        /// データの有無を返す
        /// </summary>
        /// <returns></returns>
        public bool HasPrimitive()
        {
            return (PointPrimitiveCount + EdgePrimitiveCount + TrianglePrimitiveCount) > 0;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[SelfCollisionConstraint]");
            sb.AppendLine($"  -intersectFlagArray:{(intersectFlagArray.IsCreated ? intersectFlagArray.Length : 0)}");

            return sb.ToString();
        }

        //=========================================================================================
        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Register(ClothProcess cprocess)
        {
            UpdateTeam(cprocess.TeamId);
        }

        /// <summary>
        /// 制約データを解除する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void Exit(ClothProcess cprocess)
        {
            if (cprocess != null && cprocess.TeamId > 0)
            {
                // Exitフラグを見て自動的にすべて解放される
                // また同期相手のフラグ更新も行う
                UpdateTeam(cprocess.TeamId);
            }
        }

        /// <summary>
        /// フラグおよびバッファの更新
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="tdata"></param>
        internal void UpdateTeam(int teamId)
        {
            var tm = MagicaManager.Team;

            if (tm.ContainsTeamData(teamId) == false)
                return;
            ref var tdata = ref tm.GetTeamDataRef(teamId);
            var oldFlag = tdata.flag;

            // チームが消滅中かどうか
            bool exit = tdata.flag.IsSet(TeamManager.Flag_Exit);

            // sync解除
            if (exit && tdata.syncTeamId != 0 && tm.ContainsTeamData(tdata.syncTeamId))
            {
                ref var stdata = ref tm.GetTeamDataRef(tdata.syncTeamId);
                tm.RemoveSyncParent(ref stdata, teamId);
            }

            // 自身の状況を判定する
            ref var parameter = ref tm.GetParametersRef(teamId);
            var selfMode = exit ? SelfCollisionMode.None : parameter.selfCollisionConstraint.selfMode;
            var syncMode = exit ? SelfCollisionMode.None : parameter.selfCollisionConstraint.syncMode;

            bool usePointPrimitive = false;
            bool useEdgePrimitive = false;
            bool useTrianglePrimitive = false;

            bool selfEdgeEdge = false;
            bool selfPointTriangle = false;
            bool selfTrianglePoint = false;
            bool selfEdgeTriangleIntersect = false;
            bool selfTriangleEdgeIntersect = false;

            bool syncEdgeEdge = false;
            bool syncPointTriangle = false;
            bool syncTrianglePoint = false;
            bool syncEdgeTriangleIntersect = false;
            bool syncTriangleEdgeIntersect = false;

            bool PsyncEdgeEdge = false;
            bool PsyncPointTriangle = false;
            bool PsyncTrianglePoint = false;
            bool PsyncEdgeTriangleIntersect = false;
            bool PsyncTriangleEdgeIntersect = false;

            if (selfMode == SelfCollisionMode.FullMesh)
            {
                if (tdata.EdgeCount > 0)
                {
                    useEdgePrimitive = true;
                    selfEdgeEdge = true;
                }
                if (tdata.TriangleCount > 0)
                {
                    usePointPrimitive = true;
                    useTrianglePrimitive = true;
                    selfPointTriangle = true;
                    selfTrianglePoint = true;
                }
                if (tdata.EdgeCount > 0 && tdata.TriangleCount > 0)
                {
                    selfEdgeTriangleIntersect = true;
                    selfTriangleEdgeIntersect = true;
                }
            }

            // sync
            if (syncMode != SelfCollisionMode.None && tm.ContainsTeamData(tdata.syncTeamId))
            {
                ref var stdata = ref tm.GetTeamDataRef(tdata.syncTeamId);
                if (syncMode == SelfCollisionMode.FullMesh)
                {
                    if (tdata.EdgeCount > 0 && stdata.EdgeCount > 0)
                    {
                        useEdgePrimitive = true;
                        syncEdgeEdge = true;
                    }
                    if (tdata.TriangleCount > 0)
                    {
                        useTrianglePrimitive = true;
                        syncTrianglePoint = true;
                    }
                    if (stdata.TriangleCount > 0)
                    {
                        usePointPrimitive = true;
                        syncPointTriangle = true;
                    }
                    if (tdata.EdgeCount > 0 && stdata.TriangleCount > 0)
                    {
                        syncEdgeTriangleIntersect = true;
                    }
                    if (tdata.TriangleCount > 0 && stdata.EdgeCount > 0)
                    {
                        syncTriangleEdgeIntersect = true;
                    }
                }
            }

            // sync parent
            if (tdata.syncParentTeamId.Length > 0 && exit == false)
            {
                for (int i = 0; i < tdata.syncParentTeamId.Length; i++)
                {
                    int parentTeamId = tdata.syncParentTeamId[i];
                    ref var ptdata = ref tm.GetTeamDataRef(parentTeamId);
                    if (ptdata.IsValid)
                    {
                        ref var parentParameter = ref tm.GetParametersRef(parentTeamId);
                        var parentSyncMode = parentParameter.selfCollisionConstraint.syncMode;
                        if (parentSyncMode == SelfCollisionMode.FullMesh)
                        {
                            if (ptdata.EdgeCount > 0 && tdata.EdgeCount > 0)
                            {
                                useEdgePrimitive = true;
                                PsyncEdgeEdge = true;
                            }
                            if (ptdata.TriangleCount > 0)
                            {
                                usePointPrimitive = true;
                                PsyncPointTriangle = true;
                            }
                            if (tdata.TriangleCount > 0)
                            {
                                useTrianglePrimitive = true;
                                PsyncTrianglePoint = true;
                            }
                            if (tdata.EdgeCount > 0 && ptdata.TriangleCount > 0)
                            {
                                PsyncEdgeTriangleIntersect = true;
                            }
                            if (tdata.TriangleCount > 0 && ptdata.EdgeCount > 0)
                            {
                                PsyncTriangleEdgeIntersect = true;
                            }
                        }
                    }
                }
            }

            // フラグ
            tdata.flag.SetBits(TeamManager.Flag_Self_PointPrimitive, usePointPrimitive);
            tdata.flag.SetBits(TeamManager.Flag_Self_EdgePrimitive, useEdgePrimitive);
            tdata.flag.SetBits(TeamManager.Flag_Self_TrianglePrimitive, useTrianglePrimitive);

            tdata.flag.SetBits(TeamManager.Flag_Self_EdgeEdge, selfEdgeEdge);
            tdata.flag.SetBits(TeamManager.Flag_Self_PointTriangle, selfPointTriangle);
            tdata.flag.SetBits(TeamManager.Flag_Self_TrianglePoint, selfTrianglePoint);
            tdata.flag.SetBits(TeamManager.Flag_Self_EdgeTriangleIntersect, selfEdgeTriangleIntersect);
            tdata.flag.SetBits(TeamManager.Flag_Self_TriangleEdgeIntersect, selfTriangleEdgeIntersect);

            tdata.flag.SetBits(TeamManager.Flag_Sync_EdgeEdge, syncEdgeEdge);
            tdata.flag.SetBits(TeamManager.Flag_Sync_PointTriangle, syncPointTriangle);
            tdata.flag.SetBits(TeamManager.Flag_Sync_TrianglePoint, syncTrianglePoint);
            tdata.flag.SetBits(TeamManager.Flag_Sync_EdgeTriangleIntersect, syncEdgeTriangleIntersect);
            tdata.flag.SetBits(TeamManager.Flag_Sync_TriangleEdgeIntersect, syncTriangleEdgeIntersect);

            tdata.flag.SetBits(TeamManager.Flag_PSync_EdgeEdge, PsyncEdgeEdge);
            tdata.flag.SetBits(TeamManager.Flag_PSync_PointTriangle, PsyncPointTriangle);
            tdata.flag.SetBits(TeamManager.Flag_PSync_TrianglePoint, PsyncTrianglePoint);
            tdata.flag.SetBits(TeamManager.Flag_PSync_EdgeTriangleIntersect, PsyncEdgeTriangleIntersect);
            tdata.flag.SetBits(TeamManager.Flag_PSync_TriangleEdgeIntersect, PsyncTriangleEdgeIntersect);

            // point buffer
            if (usePointPrimitive && tdata.selfPointChunk.IsValid == false)
            {
                // init
                int pointCount = tdata.ParticleCount;
                tdata.selfPointChunk = primitiveArrayB.AddRange(pointCount);
                uniformGridStartCountBuffer.AddRange(pointCount);
                int start = tdata.selfPointChunk.startIndex;
                InitPrimitive(teamId, tdata, KindPoint, start, pointCount);
                PointPrimitiveCount += pointCount;
            }
            else if (usePointPrimitive == false && tdata.selfPointChunk.IsValid)
            {
                // remove
                primitiveArrayB.Remove(tdata.selfPointChunk);
                uniformGridStartCountBuffer.Remove(tdata.selfPointChunk);
                PointPrimitiveCount -= tdata.selfPointChunk.dataLength;
                tdata.selfPointChunk.Clear();
            }

            // edge buffer
            if (useEdgePrimitive && tdata.selfEdgeChunk.IsValid == false)
            {
                // init
                int edgeCount = tdata.EdgeCount;
                tdata.selfEdgeChunk = primitiveArrayB.AddRange(edgeCount);
                uniformGridStartCountBuffer.AddRange(edgeCount);
                int start = tdata.selfEdgeChunk.startIndex;
                InitPrimitive(teamId, tdata, KindEdge, start, edgeCount);
                EdgePrimitiveCount += edgeCount;
            }
            else if (useEdgePrimitive == false && tdata.selfEdgeChunk.IsValid)
            {
                // remove
                primitiveArrayB.Remove(tdata.selfEdgeChunk);
                uniformGridStartCountBuffer.Remove(tdata.selfEdgeChunk);
                EdgePrimitiveCount -= tdata.selfEdgeChunk.dataLength;
                tdata.selfEdgeChunk.Clear();
            }

            // triangle buffer
            if (useTrianglePrimitive && tdata.selfTriangleChunk.IsValid == false)
            {
                // init
                int triangleCount = tdata.TriangleCount;
                tdata.selfTriangleChunk = primitiveArrayB.AddRange(triangleCount);
                uniformGridStartCountBuffer.AddRange(triangleCount);
                int start = tdata.selfTriangleChunk.startIndex;
                InitPrimitive(teamId, tdata, KindTriangle, start, triangleCount);
                TrianglePrimitiveCount += triangleCount;
            }
            else if (useTrianglePrimitive == false && tdata.selfTriangleChunk.IsValid)
            {
                // remove
                primitiveArrayB.Remove(tdata.selfTriangleChunk);
                uniformGridStartCountBuffer.Remove(tdata.selfTriangleChunk);
                TrianglePrimitiveCount -= tdata.selfTriangleChunk.dataLength;
                tdata.selfTriangleChunk.Clear();
            }

            // Intersect
            bool useIntersect = tdata.flag.TestAny(TeamManager.Flag_Self_EdgeTriangleIntersect, 6);
            bool oldIntersect = oldFlag.TestAny(TeamManager.Flag_Self_EdgeTriangleIntersect, 6);
            if (useIntersect && oldIntersect == false)
            {
                // init
                IntersectCount++; // チーム利用カウント
            }
            else if (useIntersect == false && oldIntersect)
            {
                // remove
                IntersectCount--;
            }

            // 同期対象に対して再帰する
            if (tdata.syncTeamId != 0 && tm.ContainsTeamData(tdata.syncTeamId))
            {
                UpdateTeam(tdata.syncTeamId);
            }
        }

        /// <summary>
        /// プリミティブ初期化
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="tdata"></param>
        /// <param name="kind"></param>
        /// <param name="startPrimitive"></param>
        /// <param name="length"></param>
        void InitPrimitive(int teamId, TeamManager.TeamData tdata, uint kind, int startPrimitive, int length)
        {
            var vm = MagicaManager.VMesh;

            var job = new InitPrimitiveJob()
            {
                teamId = teamId,
                tdata = tdata,

                kind = kind,
                startPrimitive = startPrimitive,

                edges = vm.edges.GetNativeArray(),
                triangles = vm.triangles.GetNativeArray(),
                attributes = vm.attributes.GetNativeArray(),
                vertexDepths = vm.vertexDepths.GetNativeArray(),

                primitiveArrayB = primitiveArrayB.GetNativeArray(),
            };
            job.Run(length); // ここではRun()で実行する
        }

        [BurstCompile]
        struct InitPrimitiveJob : IJobParallelFor
        {
            public int teamId;
            public TeamManager.TeamData tdata;

            public uint kind;
            public int startPrimitive;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;

            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArrayB;

            public void Execute(int index)
            {
                int pri_index = startPrimitive + index;

                var p = primitiveArrayB[pri_index];
                int pstart = tdata.particleChunk.startIndex;

                // プリミティブを構成するパーティクルインデックス
                int3 particleIndices = -1;
                if (kind == KindPoint)
                {
                    particleIndices[0] = pstart + index;
                }
                else if (kind == KindEdge)
                {
                    int estart = tdata.proxyEdgeChunk.startIndex;
                    particleIndices.xy = edges[estart + index] + pstart;
                }
                else if (kind == KindTriangle)
                {
                    int tstart = tdata.proxyTriangleChunk.startIndex;
                    particleIndices.xyz = triangles[tstart + index] + pstart;
                }

                // フラグなど
                uint flag = 0;
                uint fix_flag = Flag_Fix0;
                bool ignore = false;
                int fixcnt = 0;
                float depth = 0;
                int ac = (int)kind + 1; // 軸の数
                for (int i = 0; i < ac; i++)
                {
                    int pindex = particleIndices[i];
                    int vindex = tdata.proxyCommonChunk.startIndex + pindex - pstart;
                    var attr = attributes[vindex];
                    if (attr.IsMove())
                        flag &= ~fix_flag;
                    else
                    {
                        flag |= fix_flag;
                        fixcnt++;
                    }
                    fix_flag <<= 1;
                    if (attr.IsInvalid())
                        ignore = true;
                    depth += vertexDepths[vindex];
                }
                if (fixcnt == ac)
                    flag |= Flag_AllFix;
                else
                    flag &= ~Flag_AllFix;
                if (ignore)
                    flag |= Flag_Ignore;
                depth /= ac;

                p.flag = (kind << 24) | flag;
                p.particleIndices = particleIndices;
                p.depth = depth;
                p.grid = Define.System.SelfCollisionIgnoreGrid; // 無効グリッド

                primitiveArrayB[pri_index] = p;

                //Debug.Log($"pri[{pri_index}] p:{p.particleIndices}, {p.GetKind()}");
            }
        }

        /// <summary>
        /// 作業バッファ更新
        /// </summary>
        internal void WorkBufferUpdate()
        {
            // 交差フラグバッファ
            if (IntersectCount > 0)
            {
                int pcnt = MagicaManager.Simulation.ParticleCount;
                intersectFlagArray.MC2Resize(pcnt, options: NativeArrayOptions.ClearMemory);
            }
        }

        //=========================================================================================
        // Primitive
        //=========================================================================================
        [BurstCompile]
        unsafe internal struct SelfStep_UpdatePrimitiveJob : IJobParallelFor
        {
            public int workerCount;
            public int updateIndex;
            public float4 simulationPower;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // self collision
            public bool useIntersect;
            [NativeDisableParallelForRestriction]
            public NativeArray<Primitive> primitiveArrayB;
            [Unity.Collections.ReadOnly]
            public NativeArray<byte> intersectFlagArray;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割（３固定）
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var param = ref *(paramPt + teamId);

                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // セルフコリジョン
                // ■プリミティブとグリッドの更新
                // 範囲
                //var chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                //if (chunk.IsValid)
                {
                    UpdatePrimitive(
                        workerIndex,
                        // team
                        teamId,
                        ref tdata,
                        ref param,
                        // particle
                        ref nextPosArray,
                        ref oldPosArray,
                        ref frictionArray,
                        // self collisiotn
                        useIntersect,
                        ref primitiveArrayB,
                        ref intersectFlagArray
                        );
                }
            }
        }

        /// <summary>
        /// ブロードフェーズ
        /// プリミティブ更新
        /// </summary>
        unsafe static void UpdatePrimitive(
            int k,
            // team
            int teamId,
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float3> oldPosArray,
            ref NativeArray<float> frictionArray,
            // self collision
            bool useIntersect,
            ref NativeArray<Primitive> primitiveArrayB,
            ref NativeArray<byte> intersectFlagArray
            )
        {
            // チームの種類ごと(0:Point,1:Edge,2:Triangle)

            // ■プリミティブ更新 ===================================================================
            Primitive* pt = (Primitive*)primitiveArrayB.GetUnsafePtr();

            int pstart = tdata.particleChunk.startIndex;

            // (0)Point/(1)Edge/(2)Triangle
            float maxPrimitiveSize = 0;
            //for (int k = 0; k < 3; k++)
            {
                DataChunk pc = k == KindPoint ? tdata.selfPointChunk : (k == KindEdge ? tdata.selfEdgeChunk : tdata.selfTriangleChunk);
                //Debug.Log($"[{teamId}] kind:{k} pc:{pc.ToString()}");

                if (pc.IsValid)
                {
                    uint kind = (uint)k;

                    float3x3 nextPos = 0;
                    float3x3 oldPos = 0;

                    int priIndex = pc.startIndex;
                    for (int j = 0; j < pc.dataLength; j++, priIndex++)
                    {
                        ref var p = ref *(pt + priIndex);

                        if (p.IsIgnore())
                            continue;

                        // プリミティブ更新
                        int ac = k + 1; // 軸の数
                        uint fix_flag = Flag_Fix0;
                        uint intersect_flag = 0;
                        for (int i = 0; i < ac; i++)
                        {
                            int pindex = p.particleIndices[i];
                            nextPos[i] = nextPosArray[pindex];
                            oldPos[i] = oldPosArray[pindex];
                            bool fix = (p.flag & fix_flag) != 0;
                            p.invMass[i] = MathUtility.CalcSelfCollisionInverseMass(frictionArray[pindex], fix, param.selfCollisionConstraint.clothMass);
                            fix_flag <<= 1;

                            if (useIntersect && intersectFlagArray[pindex] != 0)
                                intersect_flag |= (Flag_Intersect0 << i);
                        }
                        float thickness = param.selfCollisionConstraint.surfaceThicknessCurveData.MC2EvaluateCurve(p.depth);
                        thickness *= tdata.scaleRatio; // team scale
                        p.thickness = thickness;

                        // プリミティブAABB
                        var aabb = new AABB(math.min(nextPos[0], oldPos[0]), math.max(nextPos[0], oldPos[0]));
                        for (int i = 1; i < ac; i++)
                        {
                            aabb.Encapsulate(nextPos[i]);
                            aabb.Encapsulate(oldPos[i]);
                        }
                        float aabbSize = aabb.MaxSideLength;
                        maxPrimitiveSize = math.max(maxPrimitiveSize, aabbSize);
                        aabb.Expand(thickness); // 厚み
                        p.aabb = aabb;

                        // インターセクトフラグ更新
                        p.flag = (p.flag & 0xfffffff8) | intersect_flag;
                    }
                }
            }

            // ■UniformGridサイズ決定 ==============================================================
            // 種類がEdgeの場合のみ設定
            if (k == KindEdge)
            {
                const float uniformGridScale = 3.0f; // 一旦これで 3.0?
                float gridSize = maxPrimitiveSize * uniformGridScale;
                tdata.selfGridSize = gridSize;
                tdata.selfMaxPrimitiveSize = maxPrimitiveSize; // 最大プリミティブサイズ
                //Debug.Log($"[{teamId}] maxPrimitiveSize:{maxPrimitiveSize} gridSize:{gridSize}");
            }
            //Debug.Log($"[{teamId}] kind:{k}, maxPrimitiveSize:{maxPrimitiveSize}");
        }

        [BurstCompile]
        unsafe internal struct SelfStep_UpdateGridJob : IJobParallelFor
        {
            public int kindCount;
            public int updateIndex;
            public float4 simulationPower;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // self collision
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArrayB;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<GridInfo> uniformGridStartCountBuffer;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割（３固定）
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / kindCount;
                int kindIndex = index % kindCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);

                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // セルフコリジョン
                // ■プリミティブとグリッドの更新（初回ステップのみ）
                if (updateIndex == 0)
                {
                    UpdateGrid(
                        kindIndex,
                        // team
                        teamId,
                        ref tdata,
                        // self collisiotn
                        ref primitiveArrayB,
                        ref uniformGridStartCountBuffer
                        );
                }
            }
        }

        unsafe static void UpdateGrid(
            int k,
            // team
            int teamId,
            ref TeamManager.TeamData tdata,
            // self collision
            ref NativeArray<Primitive> primitiveArrayB,
            ref NativeArray<GridInfo> uniformGridStartCountBuffer
            )
        {
            // チームの種類ごと(0:Point,1:Edge,2:Triangle)

            // ■プリミティブ更新 ===================================================================
            Primitive* pt = (Primitive*)primitiveArrayB.GetUnsafeReadOnlyPtr();
            GridInfo* gt = (GridInfo*)uniformGridStartCountBuffer.GetUnsafePtr();

            //int pstart = tdata.particleChunk.startIndex;

            // ■ここからは自身のグリッドが参照される場合のみで良い
            // つまり自身のセルフコリジョンか親からの相互コリジョン
            if (tdata.flag.IsSet(TeamManager.Flag_Self_EdgeEdge)
                || tdata.flag.IsSet(TeamManager.Flag_Self_PointTriangle)
                || tdata.flag.IsSet(TeamManager.Flag_Self_TrianglePoint)
                || tdata.flag.IsSet(TeamManager.Flag_PSync_EdgeEdge)
                || tdata.flag.IsSet(TeamManager.Flag_PSync_PointTriangle)
                || tdata.flag.IsSet(TeamManager.Flag_PSync_TrianglePoint)
                )
            {
                {
                    DataChunk pc = k == KindPoint ? tdata.selfPointChunk : (k == KindEdge ? tdata.selfEdgeChunk : tdata.selfTriangleChunk);
                    //Debug.Log($"[{teamId}] calc grid. kind:{k} pstart:{pc.startIndex}, pcount:{pc.dataLength}");
                    if (pc.IsValid)
                    {
                        // ■プリミティブのグリッド座標算出 =======================================================
                        int priIndex = pc.startIndex;
                        for (int j = 0; j < pc.dataLength; j++, priIndex++)
                        {
                            ref var p = ref *(pt + priIndex);
                            if (p.IsIgnore())
                                p.grid = Define.System.SelfCollisionIgnoreGrid; // 無効グリッド
                            else
                                p.grid = GetGrid(p.aabb.Center, tdata.selfGridSize);
                        }

                        // ■プリミティブをグリッド順にソートする =================================================
                        NativeSortExtension.Sort(pt + pc.startIndex, pc.dataLength);

                        // ■グリッドの開始位置と数を摘出する =====================================================
                        int3 nowGrid = 0;
                        int nowGridStart = 0;
                        int nowGridCount = 0;
                        int gridBufferStart = pc.startIndex;
                        int gridBufferIndex = gridBufferStart;
                        int gridBufferCount = 0;
                        priIndex = pc.startIndex;
                        for (int i = 0; i < pc.dataLength; i++, priIndex++)
                        {
                            ref var p = ref *(pt + priIndex);

                            if (i == 0)
                            {
                                // 初回グリッド
                                nowGrid = p.grid;
                                nowGridStart = priIndex;
                                nowGridCount = 0;
                            }
                            else if (p.grid.Equals(nowGrid.xyz) == false)
                            {
                                // 現在のグリッドを保存
                                uniformGridStartCountBuffer[gridBufferIndex] = new GridInfo() { hash = nowGrid.GetHashCode(), start = nowGridStart, count = nowGridCount };
                                gridBufferIndex++;
                                gridBufferCount++;
                                //Debug.Log(nowGrid.GetHashCode());

                                // 次のグリッドの開始
                                nowGrid = p.grid;
                                nowGridStart = priIndex;
                                nowGridCount = 0;
                            }
                            nowGridCount++;
                        }
                        // 最後のグリッドを記録
                        if (nowGridCount > 0)
                        {
                            uniformGridStartCountBuffer[gridBufferIndex] = new GridInfo() { hash = nowGrid.GetHashCode(), start = nowGridStart, count = nowGridCount };
                            gridBufferIndex++;
                            gridBufferCount++;
                            //Debug.Log(nowGrid.GetHashCode());
                        }

                        // グリッド数を記録
                        switch (k)
                        {
                            case 0:
                                tdata.selfPointGridCount = gridBufferCount;
                                break;
                            case 1:
                                tdata.selfEdgeGridCount = gridBufferCount;
                                break;
                            case 2:
                                tdata.selfTriangleGridCount = gridBufferCount;
                                break;
                        }

                        // グリッド情報をハッシュでソート
                        // ２分探索のため
                        NativeSortExtension.Sort(gt + gridBufferStart, gridBufferCount);

                        //Debug.Log($"[{teamId}] calc grid. kind:{k} pstart:{pc.startIndex}, pcount:{pc.dataLength}, grid count:{gridBufferCount}");
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int3 GetGrid(float3 pos, float gridSize)
        {
            //Debug.Assert(gridSize > 0.0f);
            return new int3(math.floor(pos / gridSize));
        }

        //=========================================================================================
        // Contact
        //=========================================================================================
        [BurstCompile]
        unsafe internal struct SelfStep_DetectionContactJob : IJobParallelFor
        {
            public int updateIndex;
            public int workerCount;
            public int teamCount;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;

            // self collision
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArrayB;
            [Unity.Collections.ReadOnly]
            public NativeArray<GridInfo> uniformGridStartCountBuffer;

            // buffer
            [NativeDisableParallelForRestriction]
            public NativeQueue<ContactInfo>.ParallelWriter contactQueue;

            // １チーム６インデックス分割 x ワーカー数
            public void Execute(int index)
            {
                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();

                // チームインデックス
                int teamIndex = index / (6 * workerCount);
                index = index % (6 * workerCount);
                int teamId = batchSelfTeamList[teamIndex];
                ref var tdata = ref *(teamPt + teamId);
                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // ワーカー番号
                int workerIndex = index / 6;
                index = index % 6;

                // セルフコリジョン or 相互コリジョン
                int collisionMode = index / 3;
                index = index % 3;

                // コリジョン種類
                int contactKind = index;

                uint myKind = 0, tarKind = 0;
                switch (contactKind)
                {
                    case ContactType_EdgeEdge:
                        myKind = KindEdge;
                        tarKind = KindEdge;
                        break;
                    case ContactType_PointTriangle:
                        myKind = KindPoint;
                        tarKind = KindTriangle;
                        break;
                    case ContactType_TrianglePoint:
                        myKind = KindTriangle;
                        tarKind = KindPoint;
                        break;
                }

                // ■コンタクトバッファ作成（ステップ初回のみ）

                if (collisionMode == 0)
                {
                    // セルフコリジョン
                    // セルフコリジョンではTrianglePointは不要
                    if (contactKind == ContactType_TrianglePoint)
                        return;

                    int selfFlag = TeamManager.Flag_Self_EdgeEdge + (contactKind * 3);
                    if (tdata.flag.IsSet(selfFlag))
                    {
                        //Debug.Log($"Self detection contact. myTeam:{teamId}, myKind:{myKind}, tarTeam:{teamId}, tarKind:{tarKind}");

                        DetectionContacts(
                            workerCount,
                            workerIndex,
                            teamId,
                            ref tdata,
                            myKind,
                            teamId,
                            ref tdata,
                            tarKind,
                            ref nextPosArray,
                            ref oldPosArray,
                            ref primitiveArrayB,
                            ref uniformGridStartCountBuffer,
                            ref contactQueue
                            );
                    }
                }
                else if (collisionMode == 1 && tdata.syncTeamId > 0)
                {
                    // 相互コリジョン
                    ref var stdata = ref *(teamPt + tdata.syncTeamId);
                    int syncFlag = TeamManager.Flag_Sync_EdgeEdge + (contactKind * 3);
                    if (tdata.flag.IsSet(syncFlag) && stdata.IsProcess && stdata.ParticleCount > 0)
                    {
                        //Debug.Log($"Sync detection contact. myTeam:{teamId}, myKind:{myKind}, tarTeam:{teamId}, tarKind:{tarKind}");

                        DetectionContacts(
                            workerCount,
                            workerIndex,
                            teamId,
                            ref tdata,
                            myKind,
                            tdata.syncTeamId,
                            ref stdata,
                            tarKind,
                            ref nextPosArray,
                            ref oldPosArray,
                            ref primitiveArrayB,
                            ref uniformGridStartCountBuffer,
                            ref contactQueue
                            );
                    }
                }

                //Debug.Log($"Detection contact. team:{teamId}, collisionMode:{collisionMode}, contactKind:{contactKind}, writeCount:{writeCount}");
            }
        }

        unsafe static void DetectionContacts(
            int workerCount,
            int workerIndex,
            // my
            int myTeamId,
            ref TeamManager.TeamData myTeam,
            uint myKind,
            // target
            int targetTeamId,
            ref TeamManager.TeamData targetTeam,
            uint targetKind,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float3> oldPosArray,
            // self collision
            ref NativeArray<Primitive> primitiveArrayB,
            ref NativeArray<GridInfo> uniformGridStartCountBuffer,
            // contact buffer
            ref NativeQueue<ContactInfo>.ParallelWriter contactQueue
            )
        {
            Primitive* pt = (Primitive*)primitiveArrayB.GetUnsafeReadOnlyPtr();
            GridInfo* gt = (GridInfo*)uniformGridStartCountBuffer.GetUnsafeReadOnlyPtr();

            // 参照元
            DataChunk myPriChunk = myKind == KindPoint ? myTeam.selfPointChunk : (myKind == KindEdge ? myTeam.selfEdgeChunk : myTeam.selfTriangleChunk);
            if (myPriChunk.IsValid == false)
                return;

            // ワーカー分散による範囲
            int2 range = MathUtility.CalcSplitRange(myPriChunk.dataLength, workerCount, workerIndex);
            myPriChunk.startIndex += range.x;
            myPriChunk.dataLength = range.y - range.x;

            // 対象
            DataChunk tarPriChunk = targetKind == KindPoint ? targetTeam.selfPointChunk : (targetKind == KindEdge ? targetTeam.selfEdgeChunk : targetTeam.selfTriangleChunk);
            if (tarPriChunk.IsValid == false)
                return;
            int gridBufferStart = tarPriChunk.startIndex;
            int gridBufferIndex = gridBufferStart;
            int gridBufferCount = 0;
            switch (targetKind)
            {
                case KindPoint:
                    gridBufferCount = targetTeam.selfPointGridCount;
                    break;
                case KindEdge:
                    gridBufferCount = targetTeam.selfEdgeGridCount;
                    break;
                case KindTriangle:
                    gridBufferCount = targetTeam.selfTriangleGridCount;
                    break;
            }
            float maxPrimitiveSize = targetTeam.selfMaxPrimitiveSize;
            float gridSize = targetTeam.selfGridSize;

            // 重複判定の有無
            bool duplicateDetection = (myTeamId == targetTeamId && myKind == targetKind);

            // プリミティブの接続判定
            bool connectionCheck = myTeamId == targetTeamId;

            // 接触タイプ
            bool primitiveFlip = false;
            byte contactType = ContactType_EdgeEdge;
            if (myKind == KindPoint && targetKind == KindTriangle)
                contactType = ContactType_PointTriangle;
            else if (myKind == KindTriangle && targetKind == KindPoint)
            {
                contactType = ContactType_PointTriangle;
                primitiveFlip = true;
            }

            // プリミティブごと
            GridInfo searchGridInfo = new GridInfo();
            int priIndex = myPriChunk.startIndex;
            for (int i = 0; i < myPriChunk.dataLength; i++, priIndex++)
            {
                ref var p = ref *(pt + priIndex);

                // 無効判定
                if (p.IsIgnore())
                    continue;

                bool pFix = (p.flag & Flag_AllFix) != 0;

                // このプリミティブの検索範囲
                float3 areaMin = p.aabb.Min - maxPrimitiveSize * 0.5f;
                float3 areaMax = p.aabb.Max + maxPrimitiveSize * 0.5f;

                // グリッド範囲に変換する
                int3 startGrid = GetGrid(areaMin, gridSize);
                int3 endGrid = GetGrid(areaMax, gridSize);

                // グリッド範囲を調べる
                int3 currentGrid = startGrid;
                bool finish = false;
                while (finish == false)
                {
                    // グリッド情報検索（ハッシュ値による２分探索）
                    int currentHash = currentGrid.GetHashCode();
                    searchGridInfo.hash = currentHash;
                    int infoIndex = NativeSortExtension.BinarySearch(gt + gridBufferStart, gridBufferCount, searchGridInfo);
                    if (infoIndex >= 0)
                    {
                        // このグリッドにはプリミティブが存在する
                        ref var gridInfo = ref *(gt + gridBufferStart + infoIndex);
                        int startPriIndex2 = gridInfo.start;
                        int endPriIndex2 = startPriIndex2 + gridInfo.count;

                        // 重複判定：グリッド全体が現在のpriIndexより下ならば検索不要
                        if (duplicateDetection == false || endPriIndex2 >= priIndex)
                        {
                            // 重複判定：現在のpindexより上のものだけを調べる
                            int priIndex2 = duplicateDetection ? math.max(startPriIndex2, priIndex) : startPriIndex2;
                            for (; priIndex2 < endPriIndex2; priIndex2++)
                            {
                                if (duplicateDetection && priIndex == priIndex2)
                                    continue;

                                ref var p2 = ref *(pt + priIndex2);

                                // AABB判定
                                if (p.aabb.Overlaps(p2.aabb) == false)
                                    continue;

                                // 無効判定
                                if (p2.IsIgnore())
                                    continue;

                                // 両方のプリミティブが完全固定ならば無効
                                if (pFix && ((p2.flag & Flag_AllFix) != 0))
                                    continue;

                                // プリミティブ同士が接続している場合は無効
                                if (connectionCheck && p.AnyParticle(ref p2))
                                    continue;

                                // !衝突検出！
                                // コンタクトバッファ生成
                                var contact = new ContactInfo()
                                {
                                    primitiveIndex0 = primitiveFlip == false ? priIndex : priIndex2,
                                    primitiveIndex1 = primitiveFlip == false ? priIndex2 : priIndex,
                                    contactType = contactType,
                                    thickness = (half)(p.thickness + p2.thickness),
                                };

                                // コンタクト変位判定
                                UpdateContactInfo(
                                    ref contact,
                                    pt,
                                    ref nextPosArray,
                                    ref oldPosArray,
                                    Define.System.SelfCollisionSCR,
                                    true
                                    );
                                if (contact.enable == 0)
                                    continue;

                                contactQueue.Enqueue(contact);
                                //Debug.Log($"Contact! myTeamId:{myTeamId}, myKind:{myKind}, targetTeamId:{targetTeamId}, targetKind:{targetKind}, ({priIndex}->{priIndex2})");
                            }
                        }
                    }

                    // next
                    currentGrid.x++;
                    if (currentGrid.x > endGrid.x)
                    {
                        currentGrid.x = startGrid.x;
                        currentGrid.y++;
                        if (currentGrid.y > endGrid.y)
                        {
                            currentGrid.y = startGrid.y;
                            currentGrid.z++;
                            if (currentGrid.z > endGrid.z)
                            {
                                finish = true;
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        unsafe internal struct SelfStep_ConvertContactListJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeQueue<ContactInfo> contactQueue;

            [NativeDisableParallelForRestriction]
            public NativeList<ContactInfo> contactList;

            public void Execute()
            {
                contactList.Clear();
                if (contactQueue.Count > 0)
                    contactList.AddRange(contactQueue.ToArray(Allocator.Temp));

                //Debug.Log($"contact count:{contactList.Length}");
            }
        }

        [BurstCompile]
        unsafe internal struct SelfStep_UpdateContactJob : IJobParallelForDefer
        {
            public bool first;
            [NativeDisableParallelForRestriction]
            public NativeList<ContactInfo> contactList;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            // self collision
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArrayB;

            // コンタクトごと
            public void Execute(int index)
            {
                Primitive* pt = (Primitive*)primitiveArrayB.GetUnsafeReadOnlyPtr();
                ContactInfo* ct = (ContactInfo*)contactList.GetUnsafePtr();

                ref ContactInfo contact = ref *(ct + index);

                // 反復計算用にコンタクト情報を更新する
                UpdateContactInfo(
                    ref contact,
                    pt,
                    ref nextPosArray,
                    ref oldPosArray,
                    Define.System.SelfCollisionSCR,
                    first
                    );
            }
        }

        unsafe static void UpdateContactInfo(
            ref ContactInfo contact,
            Primitive* pt,
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float3> oldPosArray,
            float scrScale,
            bool first
            )
        {
            ref var p0 = ref *(pt + contact.primitiveIndex0);
            ref var p1 = ref *(pt + contact.primitiveIndex1);

            float thickness = contact.thickness;
            float scr = thickness * scrScale;

            contact.enable = 0;

            // AABB


            // コンタクト解決
            if (contact.contactType == ContactType_EdgeEdge)
            {
                var nextPosA0 = nextPosArray[p0.particleIndices.x];
                var nextPosA1 = nextPosArray[p0.particleIndices.y];
                var nextPosB0 = nextPosArray[p1.particleIndices.x];
                var nextPosB1 = nextPosArray[p1.particleIndices.y];
                var oldPosA0 = oldPosArray[p0.particleIndices.x];
                var oldPosA1 = oldPosArray[p0.particleIndices.y];
                var oldPosB0 = oldPosArray[p1.particleIndices.x];
                var oldPosB1 = oldPosArray[p1.particleIndices.y];

                // 移動前の２つの線分の最近接点
                float csqlen = MathUtility.ClosestPtSegmentSegment(oldPosA0, oldPosA1, oldPosB0, oldPosB1, out var s, out var t, out var cA, out var cB);
                float clen = math.sqrt(csqlen); // 最近接点の距離
                if (clen < 1e-09f)
                    return;

                // 押出法線
                float3 n = (cA - cB) / clen;

                // 最近接点での変位
                var dA0 = nextPosA0 - oldPosA0;
                var dA1 = nextPosA1 - oldPosA1;
                var dB0 = nextPosB0 - oldPosB0;
                var dB1 = nextPosB1 - oldPosB1;
                float3 da = math.lerp(dA0, dA1, s);
                float3 db = math.lerp(dB0, dB1, t);

                // 変位da,dbをnに投影して距離チェック
                float l0 = math.dot(n, da);
                float l1 = math.dot(n, db);
                float l = clen + l0 - l1;
                if (l > (thickness + scr))
                    return;

                // 有効
                contact.enable = 1;
                contact.s = (half)s;
                contact.t = (half)t;
                contact.n = (half3)n;
            }
            else if (contact.contactType == ContactType_PointTriangle)
            {
                var nextPosA0 = nextPosArray[p0.particleIndices.x];
                var oldPosA0 = oldPosArray[p0.particleIndices.x];

                var nextPosB0 = nextPosArray[p1.particleIndices.x];
                var nextPosB1 = nextPosArray[p1.particleIndices.y];
                var nextPosB2 = nextPosArray[p1.particleIndices.z];
                var oldPosB0 = oldPosArray[p1.particleIndices.x];
                var oldPosB1 = oldPosArray[p1.particleIndices.y];
                var oldPosB2 = oldPosArray[p1.particleIndices.z];

                // 変位
                var dA = nextPosA0 - oldPosA0;
                var dB0 = nextPosB0 - oldPosB0;
                var dB1 = nextPosB1 - oldPosB1;
                var dB2 = nextPosB2 - oldPosB2;

                // 衝突予測と格納
                float3 cp;
                // 移動前ポイントと移動前トライアングルへの最近接点
                cp = MathUtility.ClosestPtPointTriangle(oldPosA0, oldPosB0, oldPosB1, oldPosB2, out var uvw);

                // 最近接点座標の変位を求める
                float3 dt = dB0 * uvw.x + dB1 * uvw.y + dB2 * uvw.z;

                // 最近接点ベクトル
                float3 cv = cp - oldPosA0;
                float cvlen = math.length(cv);
                if (cvlen <= Define.System.Epsilon)
                    return;

                var n = cv / cvlen;

                // 変位dp,dtをnに投影して距離チェック
                float l0 = math.dot(n, dA);
                float l1 = math.dot(n, dt);
                float l = cvlen - l0 + l1;

                // 接続判定
                if (l >= (thickness + scr))
                    return;

                // 方向性判定
                // !signの算出はオリジナルでは登録時に１回しか行っていない。
                float sign = contact.s;
                if (first)
                {
                    // 移動前トライアングル法線
                    float3 otn = MathUtility.TriangleNormal(oldPosB0, oldPosB1, oldPosB2);

                    // 移動前のパーティクル方向性
                    n = math.normalize(oldPosA0 - cp);
                    float dot = math.dot(otn, n);
                    // 移動前にトライアングル面に対してほぼ水平ならば無視する
                    if (math.abs(dot) >= Define.System.SelfCollisionPointTriangleAngleCos)
                        sign = math.sign(dot);
                    else
                        return;
                }
                contact.s = (half)sign;

                // 有効
                contact.enable = 1;
            }
        }

        [BurstCompile]
        unsafe internal struct SelfStep_SolverContactJob : IJobParallelForDefer
        {
            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;

            // self collision
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArrayB;
            [Unity.Collections.ReadOnly]
            public NativeList<ContactInfo> contactList;

            // buffer2
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> tempVectorBufferA;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempCountBuffer;

            // コンタクトごと
            public void Execute(int index)
            {
                ContactInfo* ct = (ContactInfo*)contactList.GetUnsafeReadOnlyPtr();
                Primitive* pt = (Primitive*)primitiveArrayB.GetUnsafeReadOnlyPtr();
                int* cntPt = (int*)tempCountBuffer.GetUnsafePtr();
                int* sumPt = (int*)tempVectorBufferA.GetUnsafePtr();

                ref var contact = ref *(ct + index);

                // 解決
                if (contact.enable == 0)
                    return;

                ref var p0 = ref *(pt + contact.primitiveIndex0);
                ref var p1 = ref *(pt + contact.primitiveIndex1);

                float thickness = contact.thickness;
                float scr = thickness * Define.System.SelfCollisionSCR;

                // コンタクト解決
                if (contact.contactType == ContactType_EdgeEdge)
                {
                    var nextPosA0 = nextPosArray[p0.particleIndices.x];
                    var nextPosA1 = nextPosArray[p0.particleIndices.y];
                    var nextPosB0 = nextPosArray[p1.particleIndices.x];
                    var nextPosB1 = nextPosArray[p1.particleIndices.y];

                    float s = contact.s;
                    float t = contact.t;
                    float3 n = contact.n;

                    // 移動前に接触判定を行った位置の移動後の位置a/bと方向ベクトル
                    float3 a = math.lerp(nextPosA0, nextPosA1, s);
                    float3 b = math.lerp(nextPosB0, nextPosB1, t);
                    float3 v = a - b;

                    // 接触法線に現在の距離を投影させる
                    float l = math.dot(n, v);
                    //Debug.Log($"A({pA0}-{pA1}), B({pB0}-{pB1}) s:{s}, t:{t} l:{l}");
                    if (l > thickness)
                        return;

                    float invMassA0 = p0.invMass.x;
                    float invMassA1 = p0.invMass.y;
                    float invMassB0 = p1.invMass.x;
                    float invMassB1 = p1.invMass.y;

                    // 離す距離
                    float C = thickness - l;

                    // お互いを離す
                    float b0 = 1.0f - s;
                    float b1 = s;
                    float b2 = 1.0f - t;
                    float b3 = t;
                    float3 grad0 = n * b0;
                    float3 grad1 = n * b1;
                    float3 grad2 = -n * b2;
                    float3 grad3 = -n * b3;

                    float S = invMassA0 * b0 * b0 + invMassA1 * b1 * b1 + invMassB0 * b2 * b2 + invMassB1 * b3 * b3;
                    if (S == 0.0f)
                        return;

                    S = C / S;

                    float3 _A0 = S * invMassA0 * grad0;
                    float3 _A1 = S * invMassA1 * grad1;
                    float3 _B0 = S * invMassB0 * grad2;
                    float3 _B1 = S * invMassB1 * grad3;

#if true
                    // 書き込み
                    if ((p0.flag & Flag_FixIntersect0) == 0)
                    {
                        InterlockUtility.AddFloat3(p0.particleIndices.x, _A0, cntPt, sumPt);
                    }
                    if ((p0.flag & Flag_FixIntersect1) == 0)
                    {
                        InterlockUtility.AddFloat3(p0.particleIndices.y, _A1, cntPt, sumPt);
                    }
                    if ((p1.flag & Flag_FixIntersect0) == 0)
                    {
                        InterlockUtility.AddFloat3(p1.particleIndices.x, _B0, cntPt, sumPt);
                    }
                    if ((p1.flag & Flag_FixIntersect1) == 0)
                    {
                        InterlockUtility.AddFloat3(p1.particleIndices.y, _B1, cntPt, sumPt);
                    }
#endif
                }
                else if (contact.contactType == ContactType_PointTriangle)
                {
                    // トライアングル情報
                    float3 nextPos0 = nextPosArray[p1.particleIndices.x];
                    float3 nextPos1 = nextPosArray[p1.particleIndices.y];
                    float3 nextPos2 = nextPosArray[p1.particleIndices.z];
                    float invMass0 = p1.invMass.x;
                    float invMass1 = p1.invMass.y;
                    float invMass2 = p1.invMass.z;

                    // 移動後トライアングル法線
                    float3 tn = MathUtility.TriangleNormal(nextPos0, nextPos1, nextPos2);

                    // 対象パーティクル情報
                    float3 nextPos = nextPosArray[p0.particleIndices.x];
                    float invMass = p0.invMass.x;

                    // 衝突の解決
                    // 移動後ポイントと移動後トライアングルへの最近接点
                    float3 uvw;
                    MathUtility.ClosestPtPointTriangle(nextPos, nextPos0, nextPos1, nextPos2, out uvw);

                    // 押し出し方向（移動後のトライアングル法線）
                    // 移動前に裏側ならば反転させる
                    float sign = contact.s;
                    float3 n = tn * sign;

                    // 押し出し法線方向に投影した距離
                    float dist = math.dot(n, nextPos - nextPos0);
                    //Debug.Log($"dist:{dist}");
                    if (dist >= thickness)
                        return;

                    // 引き離す距離
                    float restDist = thickness;

                    // 押し出し
                    float C = dist - restDist;

                    float3 grad = n;
                    float3 grad0 = -n * uvw[0];
                    float3 grad1 = -n * uvw[1];
                    float3 grad2 = -n * uvw[2];

                    float s = invMass + invMass0 * uvw.x * uvw.x + invMass1 * uvw.y * uvw.y + invMass2 * uvw.z * uvw.z;
                    if (s == 0.0f)
                        return;
                    s = C / s;

                    float3 corr = -s * invMass * grad;
                    float3 corr0 = -s * invMass0 * grad0;
                    float3 corr1 = -s * invMass1 * grad1;
                    float3 corr2 = -s * invMass2 * grad2;

#if true
                    // 書き込み
                    if ((p0.flag & Flag_FixIntersect0) == 0)
                    {
                        InterlockUtility.AddFloat3(p0.particleIndices.x, corr, cntPt, sumPt);
                    }
                    if ((p1.flag & Flag_FixIntersect0) == 0)
                    {
                        InterlockUtility.AddFloat3(p1.particleIndices.x, corr0, cntPt, sumPt);
                    }
                    if ((p1.flag & Flag_FixIntersect1) == 0)
                    {
                        InterlockUtility.AddFloat3(p1.particleIndices.y, corr1, cntPt, sumPt);
                    }
                    if ((p1.flag & Flag_FixIntersect2) == 0)
                    {
                        InterlockUtility.AddFloat3(p1.particleIndices.z, corr2, cntPt, sumPt);
                    }
#endif
                }
            }
        }

        [BurstCompile]
        unsafe internal struct SelfStep_SumContactJob : IJobParallelFor
        {
            public int updateIndex;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // particle
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> nextPosArray;

            // buffer2
            [NativeDisableParallelForRestriction]
            public NativeArray<float3> tempVectorBufferA;
            [NativeDisableParallelForRestriction]
            public NativeArray<int> tempCountBuffer;

            // ローカルチームインデックスごと
            public void Execute(int localIndex)
            {
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();
                int* cntPt = (int*)tempCountBuffer.GetUnsafePtr();
                int* sumPt = (int*)tempVectorBufferA.GetUnsafePtr();
                float3* nextPosT = (float3*)nextPosArray.GetUnsafePtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                if (updateIndex < tdata.updateCount)
                {
                    int pindex = tdata.particleChunk.startIndex;
                    int pindex2 = pindex * 3;
                    for (int i = 0; i < tdata.particleChunk.dataLength; i++, pindex++, pindex2 += 3)
                    {
                        int cnt = cntPt[pindex];
                        if (cnt > 0)
                        {
                            float3 add = new float3(sumPt[pindex2], sumPt[pindex2 + 1], sumPt[pindex2 + 2]);
                            add /= cnt;
                            // データは固定小数点なので戻す
                            add *= InterlockUtility.ToFloat;

                            // 反映
                            *(nextPosT + pindex) += add;
                        }
                    }
                }

                // バッファクリア
                int pindexB = tdata.particleChunk.startIndex;
                for (int i = 0; i < tdata.particleChunk.dataLength; i++, pindexB++)
                {
                    tempCountBuffer[pindexB] = 0;
                    tempVectorBufferA[pindexB] = 0;
                }
            }
        }

        //=========================================================================================
        // Intersect
        //=========================================================================================
        [BurstCompile]
        unsafe internal struct SelfDetectionIntersectJob : IJobParallelFor
        {
            public int updateIndex;
            public int workerCount;
            public int frameIndex; // 0 ~ (Define.System.SelfCollisionIntersectDiv-1)

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // self collision
            [Unity.Collections.ReadOnly]
            public NativeArray<Primitive> primitiveArrayB;
            [Unity.Collections.ReadOnly]
            public NativeArray<GridInfo> uniformGridStartCountBuffer;

            // buffer
            [NativeDisableParallelForRestriction]
            public NativeQueue<IntersectInfo>.ParallelWriter intersectQueue;

            // １チーム
            public void Execute(int index)
            {
                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();

                // チームインデックス
                int localIndex = index / workerCount;
                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.updateCount == 0)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                int workerIndex = index % workerCount;

                // ■Edge-Triangle接触バッファ作成
                // セルフコリジョン
                if (tdata.flag.IsSet(TeamManager.Flag_Self_EdgeTriangleIntersect))
                {
                    DetectionIntersect(
                        workerCount,
                        workerIndex,
                        frameIndex,
                        // edge
                        teamId,
                        ref tdata,
                        KindEdge,
                        // triangle
                        teamId,
                        ref tdata,
                        KindTriangle,
                        // self collision
                        ref primitiveArrayB,
                        ref uniformGridStartCountBuffer,
                        // intersect
                        ref intersectQueue
                        );
                }

                // 相互コリジョン
                if (tdata.syncTeamId > 0)
                {
                    ref var stdata = ref *(teamPt + tdata.syncTeamId);
                    if (stdata.IsProcess && stdata.ParticleCount > 0)
                    {
                        if (tdata.flag.IsSet(TeamManager.Flag_Sync_EdgeTriangleIntersect))
                        {
                            DetectionIntersect(
                                workerCount,
                                workerIndex,
                                frameIndex,
                                // edge
                                teamId,
                                ref tdata,
                                KindEdge,
                                // triangle
                                tdata.syncTeamId,
                                ref stdata,
                                KindTriangle,
                                // self collision
                                ref primitiveArrayB,
                                ref uniformGridStartCountBuffer,
                                // intersect
                                ref intersectQueue
                                );
                        }
                        if (tdata.flag.IsSet(TeamManager.Flag_Sync_TriangleEdgeIntersect))
                        {
                            DetectionIntersect(
                                workerCount,
                                workerIndex,
                                frameIndex,
                                // triangle
                                teamId,
                                ref tdata,
                                KindTriangle,
                                // edge
                                tdata.syncTeamId,
                                ref stdata,
                                KindEdge,
                                // self collision
                                ref primitiveArrayB,
                                ref uniformGridStartCountBuffer,
                                // intersect
                                ref intersectQueue
                                );
                        }
                    }
                }
                //Debug.Log($"Detection intersect. team:{teamId}, Count:{writeCount}");
            }
        }

        unsafe static void DetectionIntersect(
            int workerCount,
            int workerIndex,
            // 0 ~ (Define.System.SelfCollisionIntersectDiv-1)
            int frameIndex,
            // my
            int myTeamId,
            ref TeamManager.TeamData myTeam,
            uint myKind,
            // target
            int targetTeamId,
            ref TeamManager.TeamData targetTeam,
            uint targetKind,
            // self collision
            ref NativeArray<Primitive> primitiveArrayB,
            ref NativeArray<GridInfo> uniformGridStartCountBuffer,
            // intersect buffer
            ref NativeQueue<IntersectInfo>.ParallelWriter intersectQueue
            )
        {
            Primitive* pt = (Primitive*)primitiveArrayB.GetUnsafeReadOnlyPtr();
            GridInfo* gt = (GridInfo*)uniformGridStartCountBuffer.GetUnsafeReadOnlyPtr();

            // 参照元
            DataChunk myPriChunk = myKind == KindPoint ? myTeam.selfPointChunk : (myKind == KindEdge ? myTeam.selfEdgeChunk : myTeam.selfTriangleChunk);
            if (myPriChunk.IsValid == false)
                return;

            // 対象
            DataChunk tarPriChunk = targetKind == KindPoint ? targetTeam.selfPointChunk : (targetKind == KindEdge ? targetTeam.selfEdgeChunk : targetTeam.selfTriangleChunk);
            if (tarPriChunk.IsValid == false)
                return;
            int gridBufferStart = tarPriChunk.startIndex;
            int gridBufferIndex = gridBufferStart;
            int gridBufferCount = 0;
            switch (targetKind)
            {
                case KindPoint:
                    gridBufferCount = targetTeam.selfPointGridCount;
                    break;
                case KindEdge:
                    gridBufferCount = targetTeam.selfEdgeGridCount;
                    break;
                case KindTriangle:
                    gridBufferCount = targetTeam.selfTriangleGridCount;
                    break;
            }
            float maxPrimitiveSize = targetTeam.selfMaxPrimitiveSize;
            float gridSize = targetTeam.selfGridSize;

            // 初回の交差判定はグリッドサイズ更新前に到達するので除外する
            if (maxPrimitiveSize <= Define.System.Epsilon || gridSize <= Define.System.Epsilon)
                return;

            //Debug.Log($"edgeTeamId:{edgeTeamId}, triangleTeamId:{triangleTeamId}, gridBufferStart:{gridBufferStart}, gridBufferIndex:{gridBufferIndex}, gridBufferCount:{gridBufferCount}");
            //Debug.Log($"edgeTeamId:{edgeTeamId}, maxPrimitiveSize:{maxPrimitiveSize}, gridSize:{gridSize}");

            // プリミティブの接続判定
            bool connectionCheck = myTeamId == targetTeamId;

            // 格納自の入れ替え
            bool primitiveFlip = myKind != KindEdge;

            // 検索範囲
            var chunk = MathUtility.GetWorkerChunk(myPriChunk.dataLength, workerCount, workerIndex);
            if (chunk.IsValid == false)
                return;

            // プリミティブごと
            GridInfo searchGridInfo = new GridInfo();
            int priIndex = myPriChunk.startIndex + chunk.startIndex;
            for (int i = 0; i < chunk.dataLength; i++, priIndex++)
            {
                if ((priIndex % Define.System.SelfCollisionIntersectDiv) != frameIndex)
                    continue;

                ref var p = ref *(pt + priIndex);

                // 無効判定
                if (p.IsIgnore())
                    continue;

                bool pFix = (p.flag & Flag_AllFix) != 0;

                // このプリミティブの検索範囲
                float3 areaMin = p.aabb.Min - maxPrimitiveSize * 0.5f;
                float3 areaMax = p.aabb.Max + maxPrimitiveSize * 0.5f;

                // グリッド範囲に変換する
                int3 startGrid = GetGrid(areaMin, gridSize);
                int3 endGrid = GetGrid(areaMax, gridSize);

                // グリッド範囲を調べる
                int3 currentGrid = startGrid;
                bool finish = false;
                while (finish == false)
                {
                    // グリッド情報検索（ハッシュ値による２分探索）
                    int currentHash = currentGrid.GetHashCode();
                    searchGridInfo.hash = currentHash;
                    int infoIndex = NativeSortExtension.BinarySearch(gt + gridBufferStart, gridBufferCount, searchGridInfo);
                    if (infoIndex >= 0)
                    {
                        // このグリッドにはプリミティブが存在する
                        ref var gridInfo = ref *(gt + gridBufferStart + infoIndex);
                        int startPriIndex2 = gridInfo.start;
                        int endPriIndex2 = startPriIndex2 + gridInfo.count;

                        for (int priIndex2 = startPriIndex2; priIndex2 < endPriIndex2; priIndex2++)
                        {
                            ref var p2 = ref *(pt + priIndex2);

                            // AABB判定
                            if (p.aabb.Overlaps(p2.aabb) == false)
                                continue;

                            // 無効判定
                            if (p2.IsIgnore())
                                continue;

                            // 両方のプリミティブが完全固定ならば無効
                            if (pFix && ((p2.flag & Flag_AllFix) != 0))
                                continue;

                            // プリミティブ同士が接続している場合は無効
                            if (connectionCheck && p.AnyParticle(ref p2))
                                continue;

                            // !衝突検出！
                            // インターセクトバッファ生成
                            var intersect = new IntersectInfo()
                            {
                                edgeParticeIndices = primitiveFlip == false ? p.particleIndices.xy : p2.particleIndices.xy,
                                triangleParticleIndices = primitiveFlip == false ? p2.particleIndices : p.particleIndices,
                            };
                            intersectQueue.Enqueue(intersect);

                            //Debug.Log($"Intersect0! edge:{p.particleIndices.xyz}, tri:{p2.particleIndices.xyz}");
                        }
                    }

                    // next
                    currentGrid.x++;
                    if (currentGrid.x > endGrid.x)
                    {
                        currentGrid.x = startGrid.x;
                        currentGrid.y++;
                        if (currentGrid.y > endGrid.y)
                        {
                            currentGrid.y = startGrid.y;
                            currentGrid.z++;
                            if (currentGrid.z > endGrid.z)
                            {
                                finish = true;
                            }
                        }
                    }
                }
            }
        }

        [BurstCompile]
        unsafe internal struct SelfConvertIntersectListJob : IJob
        {
            [Unity.Collections.ReadOnly]
            public NativeQueue<IntersectInfo> intersectQueue;

            [NativeDisableParallelForRestriction]
            public NativeList<IntersectInfo> intersectList;

            public void Execute()
            {
                intersectList.Clear();
                if (intersectQueue.Count > 0)
                    intersectList.AddRange(intersectQueue.ToArray(Allocator.Temp));

                //Debug.Log($"intersect count:{intersectList.Length}");
            }
        }

        [BurstCompile]
        unsafe internal struct SelfClearIntersectJob : IJobParallelFor
        {
            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            // buffer
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<byte> intersectFlagArray;

            public void Execute(int index)
            {
                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();

                // チームインデックス
                int teamId = batchSelfTeamList[index];
                ref var tdata = ref *(teamPt + teamId);
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                int pindex = tdata.particleChunk.startIndex;
                for (int i = 0; i < tdata.particleChunk.dataLength; i++, pindex++)
                {
                    intersectFlagArray[pindex] = 0;
                }
            }
        }

        [BurstCompile]
        unsafe internal struct SelfSolverIntersectJob : IJobParallelForDefer
        {
            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;

            // self collision
            [Unity.Collections.ReadOnly]
            public NativeList<IntersectInfo> intersectList;

            // buffer
            [Unity.Collections.WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<byte> intersectFlagArray;


            public void Execute(int index)
            {
                IntersectInfo* it = (IntersectInfo*)intersectList.GetUnsafeReadOnlyPtr();

                ref var intersect = ref *(it + index);

                // edge
                float3 p = nextPosArray[intersect.edgeParticeIndices.x];
                float3 q = nextPosArray[intersect.edgeParticeIndices.y];

                // triangle
                float3 a = nextPosArray[intersect.triangleParticleIndices.x];
                float3 b = nextPosArray[intersect.triangleParticleIndices.y];
                float3 c = nextPosArray[intersect.triangleParticleIndices.z];

                // Intersect test
                // 線分とトライアングルの交差判定
                var qp = p - q;

                var ac = c - a;
                var ab = b - a;
                float3 n = math.cross(ab, ac);

                float d = math.dot(qp, n);

                // 水平は無効
                if (math.abs(d) < Define.System.Epsilon)
                    return;

                // 法線裏側からの侵入に対応
                if (d < 0.0f)
                {
                    //p = epri.nextPos.c1;
                    p = q;
                    qp = -qp;
                    d = -d;
                }

                var ap = p - a;
                var t = math.dot(ap, n);
                if (t < 0.0f)
                    return;
                if (t > d)
                    return;

                float3 e = math.cross(qp, ap);
                var v = math.dot(ac, e);
                if (v < 0.0f || v > d)
                    return;
                var w = -math.dot(ab, e);
                if (w < 0.0f || (v + w) > d)
                    return;

                // !交差!
                // Edgeにフラグを立てる
                intersectFlagArray[intersect.edgeParticeIndices.x] = 1;
                intersectFlagArray[intersect.edgeParticeIndices.y] = 1;

                // Triangleにはフラグは立てない

                //Debug.Log($"Intersect! edge:{p0.particleIndices.xyz}, tri:{p1.particleIndices.xyz}");
            }
        }
    }
}
