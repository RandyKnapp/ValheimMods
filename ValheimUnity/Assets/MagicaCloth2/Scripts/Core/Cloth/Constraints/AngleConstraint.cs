// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Text;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 角度復元/角度制限制約
    /// 内部処理がほぼ同じなため１つに統合
    /// </summary>
    public class AngleConstraint : IDisposable
    {
        /// <summary>
        /// angle restoration.
        /// 角度復元
        /// </summary>
        [System.Serializable]
        public class RestorationSerializeData : IDataValidate
        {
            /// <summary>
            /// Presence or absence of use.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useAngleRestoration;

            /// <summary>
            /// resilience.
            /// 復元力
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData stiffness;

            /// <summary>
            /// Velocity decay during restoration.
            /// 復元時の速度減衰
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float velocityAttenuation;

            /// <summary>
            /// Directional Attenuation of Gravity.
            /// Note that this attenuation occurs even if the gravity is 0!
            /// 復元の重力方向減衰
            /// この減衰は重力が０でも発生するので注意！
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float gravityFalloff;

            public RestorationSerializeData()
            {
                useAngleRestoration = true;
                stiffness = new CurveSerializeData(0.2f, 1.0f, 0.2f, true);
                velocityAttenuation = 0.8f;
                gravityFalloff = 0.0f;
            }

            public void DataValidate()
            {
                stiffness.DataValidate(0.0f, 1.0f);
                velocityAttenuation = Mathf.Clamp01(velocityAttenuation);
                gravityFalloff = Mathf.Clamp01(gravityFalloff);
            }

            public RestorationSerializeData Clone()
            {
                return new RestorationSerializeData()
                {
                    useAngleRestoration = useAngleRestoration,
                    stiffness = stiffness.Clone(),
                    velocityAttenuation = velocityAttenuation,
                    gravityFalloff = gravityFalloff,
                };
            }
        }

        /// <summary>
        /// angle limit.
        /// 角度制限
        /// </summary>
        [System.Serializable]
        public class LimitSerializeData : IDataValidate
        {
            /// <summary>
            /// Presence or absence of use.
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public bool useAngleLimit;

            /// <summary>
            /// Limit angle (deg).
            /// 制限角度(deg)
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData limitAngle;

            /// <summary>
            /// Standard stiffness.
            /// 基準剛性
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float stiffness;

            public LimitSerializeData()
            {
                useAngleLimit = false;
                limitAngle = new CurveSerializeData(60.0f, 0.0f, 1.0f);
                stiffness = 1.0f;
            }

            public void DataValidate()
            {
                limitAngle.DataValidate(0.0f, 180.0f);
                stiffness = Mathf.Clamp01(stiffness);
            }

            public LimitSerializeData Clone()
            {
                return new LimitSerializeData()
                {
                    useAngleLimit = useAngleLimit,
                    limitAngle = limitAngle.Clone(),
                    stiffness = stiffness,
                };
            }
        }

        //=========================================================================================
        public struct AngleConstraintParams
        {
            public bool useAngleRestoration;

            /// <summary>
            /// 角度復元力
            /// </summary>
            public float4x4 restorationStiffness;

            /// <summary>
            /// 角度復元速度減衰
            /// </summary>
            public float restorationVelocityAttenuation;

            /// <summary>
            /// 角度復元の重力方向減衰
            /// </summary>
            public float restorationGravityFalloff;


            public bool useAngleLimit;

            /// <summary>
            /// 制限角度(deg)
            /// </summary>
            public float4x4 limitCurveData;

            /// <summary>
            /// 角度制限剛性
            /// </summary>
            public float limitstiffness;

            public void Convert(RestorationSerializeData restorationData, LimitSerializeData limitData)
            {
                useAngleRestoration = restorationData.useAngleRestoration;
                // Restoration Powerは設定値の20%とする.つまり1.0で旧0.2となる
                restorationStiffness = restorationData.stiffness.ConvertFloatArray() * 0.2f;
                restorationVelocityAttenuation = restorationData.velocityAttenuation;
                restorationGravityFalloff = restorationData.gravityFalloff;

                useAngleLimit = limitData.useAngleLimit;
                limitCurveData = limitData.limitAngle.ConvertFloatArray();
                limitstiffness = limitData.stiffness;
            }
        }

        //=========================================================================================
        public AngleConstraint()
        {
        }

        public void Dispose()
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[AngleConstraint]");
            return sb.ToString();
        }

        //=========================================================================================
        // Solver
        //=========================================================================================
        internal static void SolverConstraint(
            DataChunk chunk,
            in float4 simulationPower,
            // team
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float> vertexDepths,
            ref NativeArray<int> vertexParentIndices,
            ref NativeArray<ushort> baseLineStartDataIndices,
            ref NativeArray<ushort> baseLineDataCounts,
            ref NativeArray<ushort> baseLineData,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float3> velocityPosArray,
            ref NativeArray<float> frictionArray,
            // buffer
            ref NativeArray<float3> stepBasicPositionBuffer,
            ref NativeArray<quaternion> stepBasicRotationBuffer,
            // buffer2
            ref NativeArray<float> lengthBufferArray,
            ref NativeArray<float3> localPosBufferArray,
            ref NativeArray<quaternion> localRotBufferArray,
            ref NativeArray<quaternion> rotationBufferArray,
            ref NativeArray<float3> restorationVectorBufferArray
            )
        {
            var angleParam = param.angleConstraint;
            if (angleParam.useAngleLimit == false && angleParam.useAngleRestoration == false)
                return;

            int d_start = tdata.baseLineDataChunk.startIndex;
            int p_start = tdata.particleChunk.startIndex;
            int v_start = tdata.proxyCommonChunk.startIndex;

            bool useAngleLimit = angleParam.useAngleLimit;
            bool useAngleRestoration = angleParam.useAngleRestoration;

            // 剛性
            float limitStiffness = angleParam.limitstiffness;
            float restorationAttn = angleParam.restorationVelocityAttenuation;

            // 復元の重力減衰
            // !この減衰は重力０でも発生するので注意！
            float gravityFalloff = math.lerp(1.0f - angleParam.restorationGravityFalloff, 1.0f, tdata.gravityDot);
            //Debug.Log($"gravityFalloff:{gravityFalloff}");
            //float gravity = param.gravity;
            //float3 gravityVector = gravity > Define.System.Epsilon ? param.gravityDirection : 0;

            // ベースラインごと
            //int bindex = tdata.baseLineChunk.startIndex;
            int bindex = tdata.baseLineChunk.startIndex + chunk.startIndex;
            //for (int a = 0; a < tdata.baseLineChunk.dataLength; a++, bindex++)
            for (int a = 0; a < chunk.dataLength; a++, bindex++)
            {
                int start = baseLineStartDataIndices[bindex];
                int dcnt = baseLineDataCounts[bindex];

                // バッファリング
                int dataIndex = start + d_start;
                for (int i = 0; i < dcnt; i++, dataIndex++)
                {
                    int l_index = baseLineData[dataIndex];
                    int pindex = p_start + l_index;
                    int vindex = v_start + l_index;

                    // 自身
                    var npos = nextPosArray[pindex];
                    var bpos = stepBasicPositionBuffer[pindex];
                    var brot = stepBasicRotationBuffer[pindex];

                    rotationBufferArray[pindex] = brot;

                    // 親
                    if (i > 0)
                    {
                        int p_l_index = vertexParentIndices[vindex];
                        int p_pindex = p_l_index + p_start;
                        var pnpos = nextPosArray[p_pindex];
                        var pbpos = stepBasicPositionBuffer[p_pindex];
                        var pbrot = stepBasicRotationBuffer[p_pindex];

                        if (useAngleLimit)
                        {
                            // 現在ベクトル長
                            float vlen = math.distance(npos, pnpos);

                            // 親からの基本姿勢
                            var bv = bpos - pbpos;
                            float bvlen = math.length(bv);
                            if (vlen < Define.System.Epsilon || bvlen < Define.System.Epsilon)
                            {
                                // length=0
                                //Debug.Log($"NG1");
                                //エッジ長０対処
                                lengthBufferArray[pindex] = 0;
                                localPosBufferArray[pindex] = 0;
                                localRotBufferArray[pindex] = quaternion.identity;
                            }
                            else
                            {
                                //Develop.Assert(math.length(bv) > 0.0f);
                                //var v = math.normalize(bv);
                                var v = bv / bvlen;
                                var ipq = math.inverse(pbrot);
                                float3 localPos = math.mul(ipq, v);
                                quaternion localRot = math.mul(ipq, brot);

                                lengthBufferArray[pindex] = vlen;
                                localPosBufferArray[pindex] = localPos;
                                localRotBufferArray[pindex] = localRot;
                            }
                        }

                        if (useAngleRestoration)
                        {
                            // 復元ベクトル
                            float3 rv = bpos - pbpos;
                            restorationVectorBufferArray[pindex] = rv;
                            //Debug.Log($"[{pindex}] rv:{rv}");
                        }
                    }
                }

                // 反復
                // 角度制限は親と子の位置を徐々に修正していくため反復は必須。
                // 反復は多いほど堅牢性が増す。
                for (int k = 0; k < Define.System.AngleLimitIteration; k++)
                {
                    float iterationRatio = (float)k / (Define.System.AngleLimitIteration - 1); // 0.0 ~ 1.0

                    // 回転の中心点。
                    // 親に近い(値が小さい)ほど角度制限の効果が増すが酷い振動の温床ともなる。
                    // 中心点がちょうど真ん中(0.5)の場合は堅牢性が最大となり振動が発生しなくなるがそのかわり角度復元/制限の効果が弱くなる。
                    // そのため反復ごとに回転中心を徐々に親(0.0)から中間(0.5)に近づけることにより堅牢性と安定性を確保する
                    // この処理により振動が完全に無くなる訳では無いが許容範囲であるし何よりも角度復元/制限の効果が大幅に向上する
                    //float limitRotRatio = math.min(math.lerp(0.3f, 0.7f, iterationRatio), 0.5f);
                    //float limitRotRatio = math.min(math.lerp(0.4f, 0.7f, iterationRatio), 0.5f);
                    float limitRotRatio = 0.4f;
                    float restorationRotRatio = math.lerp(0.1f, 0.5f, iterationRatio);

                    dataIndex = start + d_start;
                    for (int i = 0; i < dcnt; i++, dataIndex++)
                    {
                        int l_index = baseLineData[dataIndex];
                        int pindex = p_start + l_index;
                        int vindex = v_start + l_index;

                        //Debug.Log($"pindex:{pindex}");

                        // 子
                        float3 cpos = nextPosArray[pindex];
                        float cdepth = vertexDepths[vindex];
                        var cattr = attributes[vindex];
                        var cInvMass = MathUtility.CalcInverseMass(frictionArray[pindex]);

                        // 子が固定ならばスキップ
                        if (cattr.IsMove() == false)
                            continue;

                        // 親
                        int p_pindex = vertexParentIndices[vindex] + p_start;
                        int p_vindex = vertexParentIndices[vindex] + v_start;
                        float3 ppos = nextPosArray[p_pindex];
                        //float pdepth = vertexDepths[p_vindex];
                        var pattr = attributes[p_vindex];
                        var pInvMass = MathUtility.CalcInverseMass(frictionArray[p_pindex]);

                        //=====================================================
                        // Angle Limit
                        //=====================================================
                        if (useAngleLimit)
                        {
                            // 親からの基準姿勢
                            quaternion prot = rotationBufferArray[p_pindex];
                            float3 localPos = localPosBufferArray[pindex];
                            quaternion localRot = localRotBufferArray[pindex];

                            // 現在のベクトル
                            float3 v = cpos - ppos;
                            float vlen = math.length(v);
                            if (vlen < Define.System.Epsilon)
                            {
                                //エッジ長０対処
                                //Debug.Log($"NG2");
                                goto EndAngleLimit;
                            }

                            // 復元すべきベクトル
                            float3 tv = math.mul(prot, localPos);
                            float tvlen = math.length(tv);
                            if (tvlen < Define.System.Epsilon)
                            {
                                //エッジ長０対処
                                //Debug.Log($"NG3");
                                float3 add = ppos - cpos;
                                nextPosArray[pindex] = ppos;
                                velocityPosArray[pindex] = velocityPosArray[pindex] + add;
                                rotationBufferArray[pindex] = math.mul(prot, localRot);
                                goto EndAngleLimit;
                            }

                            v /= vlen;
                            tv /= tvlen;

                            // ベクトル長修正
                            float blen = lengthBufferArray[pindex];
                            vlen = math.lerp(vlen, blen, 0.5f); // 計算前の距離に徐々に近づける
                            if (blen < Define.System.Epsilon || vlen < Define.System.Epsilon)
                            {
                                //エッジ長０対処
                                //Debug.Log($"NG4");
                                goto EndAngleLimit;
                            }
                            //Develop.Assert(vlen > 0.0f);
                            //v = math.normalize(v) * vlen;
                            v = v * vlen;

                            // ベクトル角度クランプ
                            float maxAngleDeg = angleParam.limitCurveData.MC2EvaluateCurve(cdepth);
                            float maxAngleRad = math.radians(maxAngleDeg);
                            float angle = MathUtility.Angle(v, tv);
                            float3 rv = v;
                            if (angle > maxAngleRad)
                            {
                                // stiffness
                                float recoveryAngle = math.lerp(angle, maxAngleRad, limitStiffness);

                                MathUtility.ClampAngle(v, tv, recoveryAngle, out rv);
                            }

                            // 回転中心割合
                            float3 rotPos = ppos + v * limitRotRatio;

                            // 親と子のそれぞれの更新位置
                            float3 pfpos = rotPos - rv * limitRotRatio;
                            float3 cfpos = rotPos + rv * (1.0f - limitRotRatio);

                            // 加算
                            float3 padd = pfpos - ppos;
                            float3 cadd = cfpos - cpos;

                            // 摩擦考慮
                            cadd *= cInvMass;
                            padd *= pInvMass;

                            const float attn = Define.System.AngleLimitAttenuation;

                            // 子の書き込み
                            if (cattr.IsMove())
                            {
                                cpos += cadd;
                                nextPosArray[pindex] = cpos;
                                velocityPosArray[pindex] = velocityPosArray[pindex] + cadd * attn;
                            }

                            // 親の書き込み
                            if (pattr.IsMove())
                            {
                                ppos += padd;
                                nextPosArray[p_pindex] = ppos;
                                velocityPosArray[p_pindex] = velocityPosArray[p_pindex] + padd * attn;
                            }

                            // 回転補正
                            v = cpos - ppos;
                            vlen = math.length(v);
                            if (vlen < Define.System.Epsilon)
                            {
                                //エッジ長０対処
                                //Debug.Log($"NG5");
                                goto EndAngleLimit;
                            }
                            v /= vlen;
                            var nrot = math.mul(prot, localRot);
                            //var q = MathUtility.FromToRotation(tv, v);
                            var q = MathUtility.FromToRotationWithoutNormalize(tv, v);
                            nrot = math.mul(q, nrot);
                            rotationBufferArray[pindex] = nrot;
                        }

                        EndAngleLimit:

                        //=====================================================
                        // Angle Restoration
                        //=====================================================
                        if (useAngleRestoration)
                        {
                            //Debug.Log($"pindex:{pindex}, p_pindex:{p_pindex}");

                            // 復元すべきベクトル
                            float3 tv = restorationVectorBufferArray[pindex];
                            float tvlen = math.length(tv);
                            if (tvlen < Define.System.Epsilon)
                            {
                                //エッジ長０対処
                                //Debug.Log($"NG6");
                                float3 add = ppos - cpos;
                                nextPosArray[pindex] = ppos;
                                velocityPosArray[pindex] = velocityPosArray[pindex] + add;
                                continue;
                            }

                            // 現在のベクトル
                            float3 v = cpos - ppos;
                            float vlen = math.length(v);
                            if (vlen < Define.System.Epsilon)
                            {
                                //エッジ長０対処
                                //Debug.Log($"NG7");
                                continue;
                            }

                            // 復元力
                            float restorationStiffness = angleParam.restorationStiffness.MC2EvaluateCurveClamp01(cdepth);
                            restorationStiffness = math.saturate(restorationStiffness * simulationPower.w);

                            //int _pindex = indexBuffer[i] + p_start;
                            //Debug.Log($"i:{i} [{_pindex}] stiffness:{restorationStiffness} cdepth:{cdepth}");

                            // 重力方向減衰
                            restorationStiffness *= gravityFalloff;

                            // 球面線形補間
                            var q = MathUtility.FromToRotationWithoutNormalize(v / vlen, tv / tvlen, restorationStiffness);
                            float3 rv = math.mul(q, v);

                            // 回転中心割合
                            //float restorationRotRatio = GetRotRatio(tv, gravityVector, gravity, gravityFalloff, iterationRatio);
                            //int _pindex = indexBuffer[i] + p_start;
                            //Debug.Log($"i:{i} [{_pindex}] ratio:{restorationRotRatio} cdepth:{cdepth}");
                            float3 rotPos = ppos + v * restorationRotRatio;

                            // 親と子のそれぞれの更新位置
                            float3 pfpos = rotPos - rv * restorationRotRatio;
                            float3 cfpos = rotPos + rv * (1.0f - restorationRotRatio);

                            // 加算
                            float3 padd = pfpos - ppos;
                            float3 cadd = cfpos - cpos;

                            // 摩擦考慮
                            cadd *= cInvMass;
                            padd *= pInvMass;

                            // 子の書き込み
                            if (cattr.IsMove())
                            {
                                cpos += cadd;
                                nextPosArray[pindex] = cpos;
                                velocityPosArray[pindex] = velocityPosArray[pindex] + cadd * restorationAttn;
                            }

                            // 親の書き込み
                            if (pattr.IsMove())
                            {
                                ppos += padd;
                                nextPosArray[p_pindex] = ppos;
                                velocityPosArray[p_pindex] = velocityPosArray[p_pindex] + padd * restorationAttn;
                            }
                        }
                    }
                }
            }

            // バッファクリア
            bindex = tdata.baseLineChunk.startIndex + chunk.startIndex;
            for (int a = 0; a < chunk.dataLength; a++, bindex++)
            {
                int start = baseLineStartDataIndices[bindex];
                int dcnt = baseLineDataCounts[bindex];

                int dataIndex = start + d_start;
                for (int i = 0; i < dcnt; i++, dataIndex++)
                {
                    int l_index = baseLineData[dataIndex];
                    int pindex = p_start + l_index;

                    lengthBufferArray[pindex] = 0;
                    localPosBufferArray[pindex] = 0;
                    restorationVectorBufferArray[pindex] = 0;
                }
            }
        }
    }
}
