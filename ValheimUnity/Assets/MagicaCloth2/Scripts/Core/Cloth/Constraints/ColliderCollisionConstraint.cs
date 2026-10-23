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
    /// コライダーによる衝突判定制約
    /// </summary>
    public class ColliderCollisionConstraint : IDisposable
    {
        /// <summary>
        /// Collision judgment mode.
        /// 衝突判定モード
        /// </summary>
        public enum Mode
        {
            None = 0,
            Point = 1,
            Edge = 2,
        }

        [System.Serializable]
        public class SerializeData : IDataValidate, ITransform
        {
            /// <summary>
            /// Collision judgment mode.
            /// 衝突判定モード
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public Mode mode;

            /// <summary>
            /// Friction (0.0 ~ 1.0).
            /// Dynamic friction/stationary friction combined use.
            /// 摩擦(0.0 ~ 1.0)
            /// 動摩擦／静止摩擦兼用
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            [Range(0.0f, 0.5f)]
            public float friction;

            /// <summary>
            /// Collider list.
            /// コライダーリスト
            /// [OK] Runtime changes.
            /// [NG] Export/Import with Presets
            /// </summary>
            public List<ColliderComponent> colliderList = new List<ColliderComponent>();

            /// <summary>
            /// List of Transforms that perform collision detection with BoneSpring.
            /// BoneSpringで衝突判定を行うTransformのリスト
            /// [OK] Runtime changes.
            /// [NG] Export/Import with Presets
            /// </summary>
            public List<Transform> collisionBones = new List<Transform>();

            /// <summary>
            /// The maximum distance from the origin that a vertex will be pushed by the collider. Currently used only with BoneSpring.
            /// コライダーにより頂点が押し出される原点からの最大距離。現在はBoneSpringのみで利用。
            /// [OK] Runtime changes.
            /// [OK] Export/Import with Presets
            /// </summary>
            public CurveSerializeData limitDistance = new CurveSerializeData(0.05f);


            public SerializeData()
            {
                mode = Mode.Point;
                friction = 0.05f;
            }

            public void DataValidate()
            {
                friction = Mathf.Clamp(friction, 0.0f, 0.5f);
                limitDistance.DataValidate(0.0f, 1.0f);
            }

            public SerializeData Clone()
            {
                return new SerializeData()
                {
                    mode = mode,
                    friction = friction,
                    colliderList = new List<ColliderComponent>(colliderList),
                    collisionBones = new List<Transform>(collisionBones),
                };
            }

            public override int GetHashCode()
            {
                int hash = 0;
                foreach (var t in collisionBones)
                {
                    if (t)
                        hash += t.GetMagicaId().GetHashCode();
                }

                return hash;
            }

            public void GetUsedTransform(HashSet<Transform> transformSet)
            {
                colliderList.ForEach(x =>
                {
                    if (x)
                        x.GetUsedTransform(transformSet);
                });
                foreach (var t in collisionBones)
                {
                    if (t)
                        transformSet.Add(t);
                }
            }

            public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
            {
                colliderList.ForEach(x =>
                {
                    if (x)
                        x.ReplaceTransform(replaceDict);
                });
                for (int i = 0; i < collisionBones.Count; i++)
                {
                    var t = collisionBones[i];
                    if (t && replaceDict.ContainsKey(t.GetMagicaId()))
                    {
                        collisionBones[i] = replaceDict[t.GetMagicaId()];
                    }
                }
            }

            public int ColliderLength => colliderList.Count;
        }

        public struct ColliderCollisionConstraintParams
        {
            /// <summary>
            /// 衝突判定モード
            /// BoneSpringではPointに固定される
            /// </summary>
            public Mode mode;

            /// <summary>
            /// 動摩擦係数(0.0 ~ 1.0)
            /// 摩擦1.0に対するステップごとの接線方向の速度減速率
            /// </summary>
            public float dynamicFriction;

            /// <summary>
            /// 静止摩擦係数(0.0 ~ 1.0)
            /// 静止速度(m/s)
            /// </summary>
            public float staticFriction;

            /// <summary>
            /// コライダーにより頂点が押し出される原点からの最大距離。現在はBoneSpringのみで利用。
            /// </summary>
            public float4x4 limitDistance;

            public void Convert(SerializeData sdata, ClothProcess.ClothType clothType)
            {
                switch (clothType)
                {
                    case ClothProcess.ClothType.BoneCloth:
                    case ClothProcess.ClothType.MeshCloth:
                        mode = sdata.mode;
                        // 動摩擦/静止摩擦は設定摩擦に係数を掛けたものを使用する
                        dynamicFriction = sdata.friction * Define.System.ColliderCollisionDynamicFrictionRatio;
                        staticFriction = sdata.friction * Define.System.ColliderCollisionStaticFrictionRatio;
                        break;
                    case ClothProcess.ClothType.BoneSpring:
                        // BoneSpringは球のみ
                        mode = Mode.Point;
                        // BoneSpringのみ押し出し距離制限を設ける
                        limitDistance = sdata.limitDistance.ConvertFloatArray();
                        // 摩擦は定数
                        dynamicFriction = Define.System.BoneSpringCollisionFriction;
                        staticFriction = Define.System.BoneSpringCollisionFriction;
                        break;
                }
            }
        }

        //=========================================================================================
        public ColliderCollisionConstraint()
        {
        }


        public void Dispose()
        {
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[ColliderCollisionConstraint]");
            return sb.ToString();
        }

        //=========================================================================================
        // Point Solver
        //=========================================================================================
        internal static void SolverPointConstraint(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float> vertexDepths,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float> frictionArray,
            ref NativeArray<float3> collisionNormalArray,
            ref NativeArray<float3> velocityPosArray,
            ref NativeArray<float3> basePosArray,
            // collider
            ref NativeArray<ExBitFlag16> colliderFlagArray,
            ref NativeArray<ColliderManager.WorkData> colliderWorkDataArray
            )
        {
            if (tdata.UseColliderCount == 0)
                return;
            if (param.colliderCollisionConstraint.mode != Mode.Point)
                return;
            if (chunk.IsValid == false)
                return;

            bool isSpring = tdata.IsSpring;

            // ■Point
            // パーティクルごと
            //int pindex = tdata.particleChunk.startIndex;
            //int vindex = tdata.proxyCommonChunk.startIndex;
            int pindex = tdata.particleChunk.startIndex + chunk.startIndex;
            int vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
            //for (int k = 0; k < tdata.particleChunk.dataLength; k++, pindex++, vindex++)
            for (int k = 0; k < chunk.dataLength; k++, pindex++, vindex++)
            {
                // パーティクル情報
                var nextPos = nextPosArray[pindex];
                var attr = attributes[vindex];
                if (attr.IsInvalid() || attr.IsDisableCollision())
                    continue;
                if (attr.IsMove() == false && tdata.IsSpring == false) // スプリング利用時は固定頂点も通す
                    continue;
                float depth = vertexDepths[vindex];

                // BoneSpringでは自動的にソフトコライダーとなる
                var basePos = isSpring ? basePosArray[pindex] : float3.zero; // ソフトコライダーのみbasePosが必要

                // パーティクル半径
                float radius = math.max(param.radiusCurveData.MC2EvaluateCurve(depth), 0.0001f); // safe;

                // チームスケール倍率
                radius *= tdata.scaleRatio;

                // コライダーとの距離
                float mindist = float.MaxValue;

                // 接触コライダー情報
                int collisionColliderId = -1;
                float3 collisionNormal = 0;
                float3 n = 0;

                // パーティクル押し出し情報
                float3 addPos = 0;
                int addCnt = 0;
                float3 addN = 0;

                // 接触判定を行うコライダーからの最大距離(collisionFrictionRange)
                // パーティクルサイズから算出する
                float cfr = radius * 1.0f; // 1.0f?

                // パーティクルAABB
                var aabb = new AABB(nextPos - radius, nextPos + radius);
                aabb.Expand(cfr);

                // BoneSpringでの最大押し出し距離
                float maxLength = isSpring ? math.max(param.colliderCollisionConstraint.limitDistance.MC2EvaluateCurve(depth), 0.0001f) * tdata.scaleRatio : -1; // チームスケール倍率

                // チーム内のコライダーをループ
                int cindex = tdata.colliderChunk.startIndex;
                int ccnt = tdata.colliderCount;
                for (int i = 0; i < ccnt; i++, cindex++)
                {
                    var cflag = colliderFlagArray[cindex];
                    if (cflag.IsSet(ColliderManager.Flag_Valid) == false)
                        continue;
                    if (cflag.IsSet(ColliderManager.Flag_Enable) == false)
                        continue;

                    var ctype = DataUtility.GetColliderType(cflag);
                    var cwork = colliderWorkDataArray[cindex];
                    float dist = 100.0f;
                    float3 _nextPos = nextPos;
                    switch (ctype)
                    {
                        case ColliderManager.ColliderType.Sphere:
                            // ソフトコライダーはSphereのみ
                            dist = PointSphereColliderDetection(ref _nextPos, basePos, radius, aabb, cwork, isSpring, maxLength, out n);
                            break;
                        case ColliderManager.ColliderType.CapsuleX_Center:
                        case ColliderManager.ColliderType.CapsuleY_Center:
                        case ColliderManager.ColliderType.CapsuleZ_Center:
                        case ColliderManager.ColliderType.CapsuleX_Start:
                        case ColliderManager.ColliderType.CapsuleY_Start:
                        case ColliderManager.ColliderType.CapsuleZ_Start:
                            dist = PointCapsuleColliderDetection(ref _nextPos, radius, aabb, cwork, out n);
                            break;
                        case ColliderManager.ColliderType.Plane:
                            dist = PointPlaneColliderDetction(ref _nextPos, radius, cwork, out n);
                            break;
                        default:
                            Debug.LogError($"unknown collider type:{ctype}");
                            break;
                    }

                    // 明確な接触と押し出しあり
                    if (dist <= 0.0f)
                    {
                        // 押し出されたベクトルと接触法線をすべて加算する
                        addPos += (_nextPos - nextPos);
                        addN += n;
                        addCnt++;
                        //Debug.Log($"Collision!");
                    }

                    // コライダーに一定距離近づいている場合（動摩擦／静止摩擦が影響する）
                    if (dist <= cfr)
                    {
                        // 接触法線をすべて加算し、またコライダーまでの最近距離を記録する
                        collisionColliderId = cindex;
                        collisionNormal += n; // すべて加算する
                        mindist = math.min(mindist, dist);
                    }
                }

                // 最終位置
                // 平均化する
                if (addCnt > 0)
                {
                    // 合成された接触法線の長さに比例してパーティクルの移動を制限する
                    // これにより２つのコライダーに挟まれたパーティクルが暴れなくなる
                    addN /= addCnt;
                    float len = math.length(addN);
                    if (len < Define.System.Epsilon)
                    {
                        addPos = 0;
                    }
                    else
                    {
                        float t = math.min(len, 1.0f);
                        addPos /= addCnt;
                        nextPos += addPos * t;
                    }
                }

                // 摩擦係数(friction)計算
                if (collisionColliderId >= 0 && cfr > 0.0f && math.lengthsq(collisionNormal) > 1e-06f)
                {
                    // コライダーからの距離により変化(0.0～接地面1.0)
                    //Develop.Assert(cfr > 0.0f);
                    var friction = 1.0f - math.saturate(mindist / cfr);
                    frictionArray[pindex] = math.max(friction, frictionArray[pindex]); // 大きい方

                    // 摩擦用接触法線平均化
                    //Develop.Assert(math.length(collisionNormal) > 0.0f);
                    collisionNormal = math.normalize(collisionNormal);
                }
                collisionNormalArray[pindex] = collisionNormal;
                // todo:一応コライダーIDを記録しているが現在未使用!
                //colliderIdArray[pindex] = collisionColliderId + 1; // +1するので注意！

                // 書き戻し
                nextPosArray[pindex] = nextPos;

                // 速度影響(BoneSpringのみ)
                if (isSpring && addCnt > 0)
                {
                    // BoneSpringでは速度影響を０にする
                    velocityPosArray[pindex] = velocityPosArray[pindex] + addPos;
                }
            }
        }

        static float PointSphereColliderDetection(
            ref float3 nextpos,
            in float3 basePos,
            float radius,
            in AABB aabb,
            in ColliderManager.WorkData cwork,
            bool isSpring,
            float maxLength,
            out float3 normal
            )
        {
            // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！
            normal = 0;

            //=========================================================
            // AABB判定
            //=========================================================
            if (aabb.Overlaps(cwork.aabb) == false)
                return float.MaxValue;

            var oldpos = nextpos;

            //=========================================================
            // 衝突解決
            //=========================================================
            float3 coldpos = cwork.oldPos.c0;
            float3 cpos = cwork.nextPos.c0;
            float cradius = cwork.radius.x;

            // 移動前のコライダーに対するローカル位置から移動後コライダーの押し出し平面を求める
            float3 c, n, v;
            v = nextpos - coldpos;
            Develop.Assert(math.length(v) > 0.0f);
            n = math.normalize(v);
            c = cpos + n * (cradius + radius);

            // 衝突法線
            normal = n;

            // c = 平面位置
            // n = 平面方向
            // 平面衝突判定と押し出し
            //return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
            float dist = MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);

#if true
            // BoneSpring
            if (maxLength > 0.0f)
            {
                // (1)距離制限
                nextpos = MathUtility.ClampDistance(basePos, nextpos, maxLength);

                // (2)反発力減衰
                float l = math.distance(basePos, nextpos);
                float t = math.saturate(l / radius); // 半径基準
                                                     //float t = math.saturate(l / maxLength);
                t = math.lerp(0.0f, 0.85f, t); // 最低でも少し反発を残す
                nextpos = math.lerp(nextpos, oldpos, t);

                // 衝突平面までの距離は摩擦影響を抑えるためスケールする
                dist *= 3.0f;
            }
#endif

            return dist;
        }

        static float PointPlaneColliderDetction(
            ref float3 nextpos,
            float radius,
            in ColliderManager.WorkData cwork,
            out float3 normal
            )
        {
            // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！

            // コライダー情報
            var cpos = cwork.nextPos.c0;
            var n = cwork.oldPos.c0; // ここに押し出し法線

            // 衝突法線
            normal = n;

            // c = 平面位置（パーティクル半径分オフセット）
            // n = 平面方向
            // 平面衝突判定と押し出し
            // 平面との距離を返す（押し出しの場合は0.0）
            return MathUtility.IntersectPointPlaneDist(cpos + n * radius, n, nextpos, out nextpos);
        }

        static float PointCapsuleColliderDetection(
            ref float3 nextpos,
            float radius,
            in AABB aabb,
            in ColliderManager.WorkData cwork,
            out float3 normal
            )
        {
            // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！
            normal = 0;

            //=========================================================
            // AABB判定
            //=========================================================
            if (aabb.Overlaps(cwork.aabb) == false)
                return float.MaxValue;

            // コライダー情報
            float3 soldpos = cwork.oldPos.c0;
            float3 eoldpos = cwork.oldPos.c1;
            float3 spos = cwork.nextPos.c0;
            float3 epos = cwork.nextPos.c1;
            float sr = cwork.radius.x;
            float er = cwork.radius.y;

            //=========================================================
            // 衝突解決
            //=========================================================
            // 移動前のコライダー位置から押し出し平面を割り出す
            float t = MathUtility.ClosestPtPointSegmentRatio(nextpos, soldpos, eoldpos);
            float r = math.lerp(sr, er, t);
            float3 d = math.lerp(soldpos, eoldpos, t);
            float3 v = nextpos - d;

            // 移動前コライダーのローカルベクトル
            float3 lv = math.mul(cwork.inverseOldRot, v);

            // 移動後コライダーに変換
            d = math.lerp(spos, epos, t);
            v = math.mul(cwork.rot, lv);
            Develop.Assert(math.length(v) > 0.0f);
            float3 n = math.normalize(v);
            float3 c = d + n * (r + radius);

            // 衝突法線
            normal = n;

            // c = 平面位置
            // n = 平面方向
            // 平面衝突判定と押し出し
            return MathUtility.IntersectPointPlaneDist(c, n, nextpos, out nextpos);
        }

        //=========================================================================================
        // Edge Solver
        //=========================================================================================
        internal unsafe static void SolverEdgeConstraint(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float> vertexDepths,
            ref NativeArray<int2> edges,
            // particle
            ref NativeArray<float3> nextPosArray,
            // collider
            ref NativeArray<ExBitFlag16> colliderFlagArray,
            ref NativeArray<ColliderManager.WorkData> colliderWorkDataArray,
            // buffer2
            ref NativeArray<float3> tempVectorBufferA,
            ref NativeArray<float3> tempVectorBufferB,
            ref NativeArray<int> tempCountBuffer,
            ref NativeArray<float> tempFloatBufferA
            )
        {
            if (tdata.UseColliderCount == 0)
                return;
            if (param.colliderCollisionConstraint.mode != Mode.Edge)
                return;
            if (chunk.IsValid == false)
                return;

            // ■Edge
            int* vecAPt = (int*)tempVectorBufferA.GetUnsafePtr();
            int* vecBPt = (int*)tempVectorBufferB.GetUnsafePtr();
            int* cntPt = (int*)tempCountBuffer.GetUnsafePtr();
            int* floatPt = (int*)tempFloatBufferA.GetUnsafePtr();

            // ■計算
            // エッジごと
            int vstart = tdata.proxyCommonChunk.startIndex;
            //int eindex = tdata.proxyEdgeChunk.startIndex;
            int eindex = tdata.proxyEdgeChunk.startIndex + chunk.startIndex;
            //for (int k = 0; k < tdata.proxyEdgeChunk.dataLength; k++, eindex++)
            for (int k = 0; k < chunk.dataLength; k++, eindex++)
            {
                // エッジ情報
                int2 edge = edges[eindex];
                int2 vE = edge + vstart;
                var attrE0 = attributes[vE.x];
                var attrE1 = attributes[vE.y];
                // 両方とも固定なら不要
                bool isMove0 = attrE0.IsMove();
                bool isMove1 = attrE1.IsMove();
                if (isMove0 == false && isMove1 == false)
                    continue;
                int pstart = tdata.particleChunk.startIndex;
                int2 pE = edge + pstart;
                float3x2 nextPosE = new float3x2(nextPosArray[pE.x], nextPosArray[pE.y]);
                float2 depthE = new float2(vertexDepths[vE.x], vertexDepths[vE.y]);
                float2 radiusE = new float2(param.radiusCurveData.MC2EvaluateCurve(depthE.x), param.radiusCurveData.MC2EvaluateCurve(depthE.y));

                // チームスケール倍率
                radiusE *= tdata.scaleRatio;

                // 接触判定を行うコライダーからの最大距離
                // パーティクルサイズから算出する
                float cfr = (radiusE.x + radiusE.y) * 0.5f * 1.0f; // 1.0f?

                // 接触コライダー情報
                float mindist = float.MaxValue;
                int collisionColliderId = -1;
                float3 collisionNormal = 0;
                float3 n = 0;

                // エッジAABB
                var aabbE = new AABB(nextPosE.c0 - radiusE.x, nextPosE.c0 + radiusE.x);
                var aabbE1 = new AABB(nextPosE.c1 - radiusE.y, nextPosE.c1 + radiusE.y);
                aabbE.Encapsulate(aabbE1);
                aabbE.Expand(cfr);

                // パーティクル押し出し情報
                float3x2 addPos = 0;
                int addCnt = 0;
                float3 addN = 0;

                // チーム内のコライダーをループ
                int cindex = tdata.colliderChunk.startIndex;
                int ccnt = tdata.colliderCount;
                for (int i = 0; i < ccnt; i++, cindex++)
                {
                    var cflag = colliderFlagArray[cindex];
                    if (cflag.IsSet(ColliderManager.Flag_Valid) == false)
                        continue;
                    if (cflag.IsSet(ColliderManager.Flag_Enable) == false)
                        continue;

                    var ctype = DataUtility.GetColliderType(cflag);
                    var cwork = colliderWorkDataArray[cindex];
                    float dist = 100.0f;
                    float3x2 _nextPos = nextPosE;
                    switch (ctype)
                    {
                        case ColliderManager.ColliderType.Sphere:
                            dist = EdgeSphereColliderDetection(ref _nextPos, radiusE, aabbE, cfr, cwork, out n);
                            break;
                        case ColliderManager.ColliderType.CapsuleX_Center:
                        case ColliderManager.ColliderType.CapsuleY_Center:
                        case ColliderManager.ColliderType.CapsuleZ_Center:
                        case ColliderManager.ColliderType.CapsuleX_Start:
                        case ColliderManager.ColliderType.CapsuleY_Start:
                        case ColliderManager.ColliderType.CapsuleZ_Start:
                            dist = EdgeCapsuleColliderDetection(ref _nextPos, radiusE, aabbE, cfr, cwork, out n);
                            break;
                        case ColliderManager.ColliderType.Plane:
                            dist = EdgePlaneColliderDetection(ref _nextPos, radiusE, cwork, out n);
                            break;
                        default:
                            Debug.LogError($"Unknown collider type:{ctype}");
                            break;
                    }

                    // 明確な接触と押し出しあり
                    if (dist <= 0.0f)
                    {
                        // 押し出されたベクトルと接触法線をすべて加算する
                        addPos += (_nextPos - nextPosE);
                        addN += n;
                        addCnt++;
                    }

                    // コライダーに一定距離近づいている場合（動摩擦／静止摩擦が影響する）
                    if (dist <= cfr)
                    {
                        // 接触法線をすべて加算し、またコライダーまでの最近距離を記録する
                        collisionColliderId = cindex;
                        collisionNormal += n; // すべて加算する
                        mindist = math.min(mindist, dist);
                    }
                }

                // 最終位置
                // 平均化する
                if (addCnt > 0)
                {
                    // 合成された接触法線の長さに比例してパーティクルの移動を制限する
                    // これにより２つのコライダーに挟まれたパーティクルが暴れなくなる
                    addN /= addCnt;
                    float len = math.length(addN);
                    if (len > Define.System.Epsilon)
                    {
                        float t = math.min(len, 1.0f);
                        addPos /= addCnt;
                        addPos *= t;

                        // 書き戻し
                        InterlockUtility.AddFloat3(pE.x, addPos.c0, cntPt, vecAPt);
                        InterlockUtility.AddFloat3(pE.y, addPos.c1, cntPt, vecAPt);
                    }
                }

                // 摩擦係数(friction)集計
                if (collisionColliderId >= 0 && cfr > 0.0f && math.lengthsq(collisionNormal) > 1e-06f)
                {
                    // コライダーからの距離により変化(0.0～接地面1.0)
                    //Develop.Assert(cfr > 0.0f);
                    var friction = 1.0f - math.saturate(mindist / cfr);

                    // 摩擦用接触法線平均化
                    //Develop.Assert(math.length(collisionNormal) > 0.0f);
                    collisionNormal = math.normalize(collisionNormal);

                    if (isMove0)
                    {
                        InterlockUtility.Max(pE.x, friction, floatPt);
                        InterlockUtility.AddFloat3(pE.x, collisionNormal, vecBPt);
                    }
                    if (isMove1)
                    {
                        InterlockUtility.Max(pE.y, friction, floatPt);
                        InterlockUtility.AddFloat3(pE.y, collisionNormal, vecBPt);
                    }
                }
            }
        }

        internal unsafe static void SumEdgeConstraint(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            ref ClothParameters param,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float> frictionArray,
            ref NativeArray<float3> collisionNormalArray,
            // buffer2
            ref NativeArray<float3> tempVectorBufferA,
            ref NativeArray<float3> tempVectorBufferB,
            ref NativeArray<int> tempCountBuffer,
            ref NativeArray<float> tempFloatBufferA
            )
        {
            if (tdata.UseColliderCount == 0)
                return;
            if (param.colliderCollisionConstraint.mode != Mode.Edge)
                return;
            if (chunk.IsValid == false)
                return;

            // ■Edge
            int* vecAPt = (int*)tempVectorBufferA.GetUnsafePtr();
            int* vecBPt = (int*)tempVectorBufferB.GetUnsafePtr();
            int* cntPt = (int*)tempCountBuffer.GetUnsafePtr();
            int* floatPt = (int*)tempFloatBufferA.GetUnsafePtr();

            // ■集計
            // パーティクルごと
            //int pindex = tdata.particleChunk.startIndex;
            //int vindex = tdata.proxyCommonChunk.startIndex;
            int pindex = tdata.particleChunk.startIndex + chunk.startIndex;
            int vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
            //for (int k = 0; k < tdata.particleChunk.dataLength; k++, pindex++, vindex++)
            for (int k = 0; k < chunk.dataLength; k++, pindex++, vindex++)
            {
                // nextpos
                int count = tempCountBuffer[pindex];
                if (count > 0)
                {
                    float3 add = InterlockUtility.ReadAverageFloat3(pindex, cntPt, vecAPt);

                    // 書き出し
                    nextPosArray[pindex] = nextPosArray[pindex] + add;
                }

                // friction
                float f = InterlockUtility.ReadFloat(pindex, floatPt);
                if (f > 0.0f && f > frictionArray[pindex])
                {
                    frictionArray[pindex] = f;
                }

                // collision normal
                float3 n = InterlockUtility.ReadFloat3(pindex, vecBPt);
                if (math.lengthsq(n) > 0.0f)
                {
                    n = math.normalize(n);
                    collisionNormalArray[pindex] = n;
                }

                // バッファクリア
                tempVectorBufferA[pindex] = 0;
                tempVectorBufferB[pindex] = 0;
                tempCountBuffer[pindex] = 0;
                tempFloatBufferA[pindex] = 0;
            }
        }

        static float EdgeSphereColliderDetection(
            ref float3x2 nextPosE,
            in float2 radiusE,
            in AABB aabbE,
            float cfr,
            in ColliderManager.WorkData cwork,
            out float3 normal
            )
        {
            // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！
            normal = 0;

            //=========================================================
            // AABB判定
            //=========================================================
            if (aabbE.Overlaps(cwork.aabb) == false)
                return float.MaxValue;

            // コライダー情報
            float3 coldpos = cwork.oldPos.c0;
            float3 cpos = cwork.nextPos.c0;
            float cradius = cwork.radius.x;

            //=========================================================
            // 衝突判定
            //=========================================================
            // 移動前球に対する線分の最近接点
            float s;
            s = MathUtility.ClosestPtPointSegmentRatio(coldpos, nextPosE.c0, nextPosE.c1);
            float3 c = math.lerp(nextPosE.c0, nextPosE.c1, s);

            // 最近接点の距離
            var v = c - coldpos;
            float clen = math.length(v);
            if (clen < 1e-09f)
                return float.MaxValue;

            // 押し出し法線
            float3 n = v / clen;
            normal = n;

            // 変位
            float3 db = cpos - coldpos;

            // 変位をnに投影して距離チェック
            float l1 = math.dot(n, db);
            float l = clen - l1;

            // 厚み
            float rA = math.lerp(radiusE.x, radiusE.y, s);
            float rB = cradius;
            float thickness = rA + rB;

            // 接触判定
            if (l > (thickness + cfr))
                return float.MaxValue;

            //=========================================================
            // 衝突解決
            //=========================================================
            // 接触法線に現在の距離を投影させる
            v = c - cpos;
            l = math.dot(n, v);
            if (l > thickness)
            {
                // 接触なし
                // 接触面までの距離を返す
                return l - thickness;
            }

            // 離す距離
            float C = thickness - l;

            // エッジのみを引き離す
            //float b0 = 1.0f - t;
            //float b1 = t;
            float2 b = new float2(1.0f - s, s);

            //float3 grad0 = n * b0;
            //float3 grad1 = n * b1;
            float3x2 grad = new float3x2(n * b.x, n * b.y);

            //float S = b0 * b0 + b1 * b1;
            float S = math.dot(b, b);
            if (S == 0.0f)
                return float.MaxValue;

            S = C / S;

            //float3 corr0 = S * grad0;
            //float3 corr1 = S * grad1;
            float3x2 corr = grad * S;

            //=========================================================
            // 反映
            //=========================================================
            nextPosE += corr;

            // 押し出し距離を返す
            return -C;
        }

        static float EdgeCapsuleColliderDetection(
            ref float3x2 nextPosE,
            in float2 radiusE,
            in AABB aabbE,
            float cfr,
            in ColliderManager.WorkData cwork,
            out float3 normal
            )
        {
            // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！
            normal = 0;

            //=========================================================
            // AABB判定
            //=========================================================
            if (aabbE.Overlaps(cwork.aabb) == false)
                return float.MaxValue;

            // コライダー情報
            float3 soldpos = cwork.oldPos.c0;
            float3 eoldpos = cwork.oldPos.c1;
            float3 spos = cwork.nextPos.c0;
            float3 epos = cwork.nextPos.c1;
            float sr = cwork.radius.x;
            float er = cwork.radius.y;

            //=========================================================
            // 衝突判定
            //=========================================================
            // 移動前の２つの線分の最近接点
            float s, t;
            float3 cA, cB;
            float csqlen = MathUtility.ClosestPtSegmentSegment(nextPosE.c0, nextPosE.c1, soldpos, eoldpos, out s, out t, out cA, out cB);
            float clen = math.sqrt(csqlen); // 最近接点の距離
            if (clen < 1e-09f)
                return float.MaxValue;

            // 押出法線
            var v = cA - cB;
            float3 n = v / clen;
            normal = n;

#if !MC2_DISABLE_EDGE_COLLISION_EXTENSION
            // ★カプセル半径を考慮した補正
            // これまでのエッジ-カプセル判定はカプセルの半径が始点と終点で同じであることが前提となっていた
            // そのためカプセルの始点と終点の半径が異なると間違った衝突判定が行われてしまい、それが原因で大きな振動が発生していた
            // （これはBoneClothのようにカプセルエッジよりメッシュエッジのほうが長い場合に顕著になる）
            // そこで始点と終点の半径が異なる場合は、最初の計算の最近接点方向からカプセルエッジを半径分シフトし、
            // それをもとに再度エッジ-エッジ判定を行うように修正した
            // これは完璧ではないがおおよそ理想的な判定を行うようになり、また振動の問題も大幅に解決できる
            // （ただし小刻みな振動はまだ発生することがある）
            if (sr != er)
            {
                // 押し出し法線方向にカプセル半径を考慮してカプセルの中心線をシフトさせる
                float3 soldpos2 = soldpos + n * sr;
                float3 eoldpos2 = eoldpos + n * er;

                // この線分で再び最近接点(s/t)を計算する
                MathUtility.ClosestPtSegmentSegment2(nextPosE.c0, nextPosE.c1, soldpos2, eoldpos2, out s, out t);

                // 最終的にはこのシフト後のsとtを利用するように結果を書き換える
                cA = math.lerp(nextPosE.c0, nextPosE.c1, s);
                cB = math.lerp(soldpos, eoldpos, t);
                v = cA - cB;
                clen = math.length(v);
                n = v / clen;
                normal = n;
            }
#endif

            // 変位
            float3 dB0 = spos - soldpos;
            float3 dB1 = epos - eoldpos;


            // 最近接点での変位
            float3 db = math.lerp(dB0, dB1, t);

            // 変位da,dbをnに投影して距離チェック
            float l1 = math.dot(n, db);
            float l = clen - l1;

            // 厚み
            float rA = math.lerp(radiusE.x, radiusE.y, s);
            float rB = math.lerp(sr, er, t);
            float thickness = rA + rB;

            // 接触判定
            if (l > (thickness + cfr))
                return float.MaxValue;

            //=========================================================
            // 衝突解決
            //=========================================================
            // 接触法線に現在の距離を投影させる
            var d = math.lerp(spos, epos, t);
            v = cA - d;
            l = math.dot(n, v);
            //Debug.Log($"l:{l}");
            if (l > thickness)
            {
                // 接触なし
                // 接触面までの距離を返す
                return l - thickness;
            }

            // 離す距離
            float C = thickness - l;
            //Debug.Log($"C:{C}");

            // エッジのみを引き離す
            //float b0 = 1.0f - s;
            //float b1 = s;
            float2 b = new float2(1.0f - s, s);

            //float3 grad0 = n * b0;
            //float3 grad1 = n * b1;
            float3x2 grad = new float3x2(n * b.x, n * b.y);

            //float S = invMass0 * b0 * b0 + invMass1 * b1 * b1;
            float S = math.dot(b, b);
            if (S == 0.0f)
                return float.MaxValue;

            S = C / S;

            //float3 corr0 = S * invMass0 * grad0;
            //float3 corr1 = S * invMass1 * grad1;
            float3x2 corr = grad * S;

            //=========================================================
            // 反映
            //=========================================================
            nextPosE += corr;

            // 押し出し距離を返す
            return -C;
        }

        static float EdgePlaneColliderDetection(
            ref float3x2 nextPosE,
            in float2 radiusE,
            in ColliderManager.WorkData cwork,
            out float3 normal
            )
        {
            // ★たとえ接触していなくともコライダーまでの距離と法線を返さなければならない！

            // コライダー情報
            var cpos = cwork.nextPos.c0;
            var n = cwork.oldPos.c0; // ここに押し出し法線

            // 衝突法線
            normal = n;

            // c = 平面位置
            // n = 平面方向
            // 平面衝突判定と押し出し
            // 平面との距離を返す（押し出しの場合は0.0）
            float dist0 = MathUtility.IntersectPointPlaneDist(cpos + n * radiusE.x, n, nextPosE.c0, out nextPosE.c0);
            float dist1 = MathUtility.IntersectPointPlaneDist(cpos + n * radiusE.y, n, nextPosE.c1, out nextPosE.c1);

            return math.min(dist0, dist1);
        }
    }
}
