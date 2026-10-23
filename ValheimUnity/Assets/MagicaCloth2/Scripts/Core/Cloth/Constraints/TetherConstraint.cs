// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 最大距離制約
    /// 移動パーティクルが移動できる距離を自身のルートパーティクルとの距離から制限する
    /// </summary>
    public class TetherConstraint : IDisposable
    {
        [System.Serializable]
        public class SerializeData : IDataValidate
        {
            /// <summary>
            /// Maximum shrink limit (0.0 ~ 1.0).
            /// 0.0=do not shrink.
            /// 最大縮小限界(0.0 ~ 1.0)
            /// 0.0=縮小しない
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 1.0f)]
            public float distanceCompression;

            public SerializeData()
            {
                distanceCompression = 0.4f;
            }

            public void DataValidate()
            {
                distanceCompression = Mathf.Clamp(distanceCompression, 0.0f, 1.0f);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    distanceCompression = distanceCompression,
                };
            }
        }

        public struct TetherConstraintParams
        {
            /// <summary>
            /// 最大縮小割合(0.0 ~ 1.0)
            /// 0.0=縮小しない
            /// </summary>
            public float compressionLimit;

            /// <summary>
            /// 最大拡大割合(0.0 ~ 1.0)
            /// 0.0=拡大しない
            /// </summary>
            public float stretchLimit;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
                switch (clothType)
                {
                    case ClothProcess.ClothType.BoneCloth:
                    case ClothProcess.ClothType.MeshCloth:
                        compressionLimit = sdata.distanceCompression;
                        break;
                    case ClothProcess.ClothType.BoneSpring:
                        // BoneSpringは定数
                        compressionLimit = Define.System.BoneSpringTetherCompressionLimit;
                        break;
                }
                stretchLimit = Define.System.TetherStretchLimit;
            }
        }

        public void Dispose()
        {
        }

        //=========================================================================================
        // Solver
        //=========================================================================================
        internal static void SolverConstraint(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            ref InertiaConstraint.CenterData cdata,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float> vertexDepths,
            ref NativeArray<int> vertexRootIndices,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float3> velocityPosArray,
            ref NativeArray<float> frictionArray,
            // buffer
            ref NativeArray<float3> stepBasicPositionBuffer
            )
        {
            int p_start = tdata.particleChunk.startIndex;
            //int pindex = p_start;
            int pindex = p_start + chunk.startIndex;
            //int vindex = tdata.proxyCommonChunk.startIndex;
            int vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
            //for (int k = 0; k < tdata.particleChunk.dataLength; k++, pindex++, vindex++)
            for (int k = 0; k < chunk.dataLength; k++, pindex++, vindex++)
            {
                var attr = attributes[vindex];
                if (attr.IsMove() == false)
                    continue;

                int rootIndex = vertexRootIndices[vindex];
                if (rootIndex < 0)
                    continue;

                //Debug.Log($"Tether [{pindex}] root:{rootIndex + p_start}");

                var nextPos = nextPosArray[pindex];
                var rootPos = nextPosArray[rootIndex + p_start];
                float depth = vertexDepths[vindex];
                float friction = frictionArray[pindex];
                //float invMass = MathUtility.CalcInverseMass(friction);

                // 現在のベクトル
                float3 v = rootPos - nextPos;

                // 現在の長さ
                float distance = math.length(v);

                // 距離がほぼ０ならば処理をスキップする（エラーの回避）
                if (distance < Define.System.Epsilon)
                    continue;

                // 復元距離
                // フラグにより初期姿勢かアニメーション後姿勢かを切り替える
                float3 calcPos = stepBasicPositionBuffer[pindex];
                float3 calcRootPos = stepBasicPositionBuffer[rootIndex + p_start];
                float calcDistance = math.distance(calcPos, calcRootPos);

                //Debug.Log($"[{pindex}] calcPos:{calcPos}, calcRootPos:{calcRootPos}, calcDistance:{calcDistance}");

                // 初期位置がまったく同じ状況を考慮
                if (calcDistance == 0.0f)
                    continue;

                // 現在の伸縮割合
                //Develop.Assert(calcDistance > 0.0f);
                float ratio = distance / calcDistance;
#if true
                // 距離が範囲内なら伸縮しない
                float dist = 0;
                float stiffness;
                float attn;
                float compressionLimit = 1.0f - param.tetherConstraint.compressionLimit;
                float stretchLimit = 1.0f + param.tetherConstraint.stretchLimit;
                //float widthRatio = math.max(param.stiffnessWidth, 0.001f); // 0.2?
                //float widthRatio = 0.1f; // 0.2?
                if (ratio < compressionLimit)
                {
                    // 縮んでいる場合は戻りを比較的緩やかにする。これは振動の防止につながる。
                    dist = distance - compressionLimit * calcDistance;
                    float t = math.saturate((compressionLimit - ratio) / Define.System.TetherStiffnessWidth);
                    stiffness = Define.System.TetherCompressionStiffness * t;
                    //stiffness = Define.System.TetherCompressionStiffness;
                    attn = Define.System.TetherCompressionVelocityAttenuation;
                }
                else if (ratio > stretchLimit)
                {
                    // 伸びている場合は戻りを急速に行う。その代わり速度影響は弱くする。
                    dist = distance - stretchLimit * calcDistance;
                    float t = math.saturate((ratio - stretchLimit) / Define.System.TetherStiffnessWidth);
                    stiffness = Define.System.TetherStretchStiffness * t;
                    //stiffness = Define.System.TetherStretchStiffness;
                    attn = Define.System.TetherStretchVelocityAttenuation;
                }
                else
                    continue;

                // 移動量
                float3 add = (v / distance) * (dist * stiffness);
#endif

                // 摩擦による移動減衰
                //add *= invMass;

                // 位置
                var oldPos = nextPos;
                nextPos += add;
                nextPosArray[pindex] = nextPos;

                // 速度影響
                //float attn = param.velocityAttenuation;
                velocityPosArray[pindex] = velocityPosArray[pindex] + add * attn;
            }
        }
    }
}
