// Magica Cloth 2.
// Copyright (c) 2025 MagicaSoft.
// https://magicasoft.jp
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace MagicaCloth2
{
    /// <summary>
    /// Normal
    /// セルフコリジョンなし、かつプロキシメッシュの頂点数が一定値未満のジョブ
    /// １つの巨大なジョブ内ですべてを完結させる
    /// </summary>
    public partial class SimulationManager
    {
        [BurstCompile]
        unsafe struct SimulationNormalJob : IJobParallelFor
        {
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchNormalTeamList;
            public float4 simulationPower;
            public float simulationDeltaTime;
            public int mappingCount;

            // team
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<TeamWindData> teamWindArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // wind
            public int windZoneCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<WindManager.WindData> windDataArray;

            // transform
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> transformPositionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> transformLocalToWorldMatrixArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> transformLocalPositionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> transformLocalRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformLocalScaleArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> localTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<VirtualMeshBoneWeight> boneWeights;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> skinBoneTransformIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> skinBoneBindPoses;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexBindPoseRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRootIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineStartDataIndices;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineDataCounts;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> baseLineData;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> vertexLocalPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexLocalRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> vertexChildIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> vertexChildDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag8> baseLineFlags;
            [Unity.Collections.ReadOnly]
            public NativeArray<int3> triangles;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> triangleNormals;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> triangleTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<float2> uvs;
            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> normalAdjustmentRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexToTransformRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> oldPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> oldRotArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> basePosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> baseRotArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> oldPositionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> oldRotationArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> velocityPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> dispPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> velocityArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> realVelocityArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float> staticFrictionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> collisionNormalArray;

            // collider
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<ExBitFlag16> colliderFlagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderCenterArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderSizeArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> colliderFramePositions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> colliderFrameRotations;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> colliderFrameScales;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> colliderOldFramePositions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> colliderOldFrameRotations;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> colliderNowPositions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> colliderNowRotations;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> colliderOldPositions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> colliderOldRotations;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<ColliderManager.WorkData> colliderWorkDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> colliderMainColliderIndices;

            // inertia
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> fixedArray;

            // distance
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> distanceIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> distanceDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> distanceDistanceArray;

            // triangleBending
            [Unity.Collections.ReadOnly]
            public NativeArray<ulong> bendingTrianglePairArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> bendingRestAngleOrVolumeArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<sbyte> bendingSignOrVolumeArray;

            // buffer
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> stepBasicPositionBuffer;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> stepBasicRotationBuffer;

            // buffer2
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> tempVectorBufferA;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> tempVectorBufferB;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> tempCountBuffer;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float> tempFloatBufferA;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> tempRotationBufferA;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> tempRotationBufferB;

            // バッチチームのローカルインデックスごと
            public void Execute(int localIndex)
            {
                //Debug.Log(localIndex);
                // チームID
                int teamId = batchNormalTeamList[localIndex];

                // !通常タイプではセルフコリジョンが無いため、相互参照を考えずに該当チームを処理すれば良い

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafePtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafePtr();
                TeamWindData* windPt = (TeamWindData*)teamWindArray.GetUnsafePtr();

                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);
                ref var param = ref *(paramPt + teamId);
                ref var wdata = ref *(windPt + teamId);

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // ■プロキシメッシュをスキニングし基本姿勢を求める
                VirtualMeshManager.SimulationPreProxyMeshUpdate(
                    new DataChunk(0, tdata.proxyCommonChunk.dataLength),
                    // team
                    teamId,
                    ref tdata,
                    // vmesh
                    attributes,
                    localPositions,
                    localNormals,
                    localTangents,
                    boneWeights,
                    skinBoneTransformIndices,
                    skinBoneBindPoses,
                    ref positions,
                    ref rotations,
                    // transform
                    transformLocalToWorldMatrixArray
                    );

                // チームのセンター姿勢の決定とテレポート判定および慣性用の移動量計算
                TeamManager.SimulationCalcCenterAndInertiaAndWind(
                    simulationDeltaTime,
                    // team
                    teamId,
                    ref tdata,
                    ref cdata,
                    ref wdata,
                    ref param,
                    // vmesh
                    positions,
                    rotations,
                    vertexBindPoseRotations,
                    // inertia
                    fixedArray,
                    // transform
                    transformPositionArray,
                    transformRotationArray,
                    transformScaleArray,
                    // wind
                    windZoneCount,
                    windDataArray
                    );

                // パーティクルの全体慣性およびリセットの適用
                SimulationPreTeamUpdate(
                    new DataChunk(0, tdata.particleChunk.dataLength),
                    // team
                    ref tdata,
                    param,
                    cdata,
                    // vmesh
                    positions,
                    rotations,
                    vertexDepths,
                    // particle
                    ref nextPosArray,
                    ref oldPosArray,
                    ref oldRotArray,
                    ref basePosArray,
                    ref baseRotArray,
                    ref oldPositionArray,
                    ref oldRotationArray,
                    ref velocityPosArray,
                    ref dispPosArray,
                    ref velocityArray,
                    ref realVelocityArray,
                    ref frictionArray,
                    ref staticFrictionArray,
                    ref collisionNormalArray
                    );

                // ■コライダーのローカル姿勢を求める、および全体慣性とリセットの適用
                if (tdata.colliderCount > 0)
                {
                    ColliderManager.SimulationPreUpdate(
                        new DataChunk(0, tdata.colliderChunk.dataLength),
                        // team
                        ref tdata,
                        ref cdata,
                        // collider
                        ref colliderFlagArray,
                        ref colliderCenterArray,
                        ref colliderFramePositions,
                        ref colliderFrameRotations,
                        ref colliderFrameScales,
                        ref colliderOldFramePositions,
                        ref colliderOldFrameRotations,
                        ref colliderNowPositions,
                        ref colliderNowRotations,
                        ref colliderOldPositions,
                        ref colliderOldRotations,
                        ref colliderMainColliderIndices,
                        // transform
                        ref transformPositionArray,
                        ref transformRotationArray,
                        ref transformScaleArray,
                        ref transformLocalPositionArray,
                        ref transformLocalRotationArray,
                        ref transformLocalScaleArray
                        );
                }

                // ステップ実行
                for (int updateIndex = 0; updateIndex < tdata.updateCount; updateIndex++)
                {
                    // ステップごとのチーム更新
                    TeamManager.SimulationStepTeamUpdate(
                        updateIndex,
                        simulationDeltaTime,
                        // team
                        teamId,
                        ref tdata,
                        ref param,
                        ref cdata,
                        ref wdata
                    );

                    // コライダーの更新
                    if (tdata.colliderCount > 0)
                    {
                        ColliderManager.SimulationStartStep(
                        // team
                        ref tdata,
                        ref cdata,
                        // collider
                        ref colliderFlagArray,
                        ref colliderSizeArray,
                        ref colliderFramePositions,
                        ref colliderFrameRotations,
                        ref colliderFrameScales,
                        ref colliderOldFramePositions,
                        ref colliderOldFrameRotations,
                        ref colliderNowPositions,
                        ref colliderNowRotations,
                        ref colliderOldPositions,
                        ref colliderOldRotations,
                        ref colliderWorkDataArray
                        );
                    }

                    // 速度更新、外力の影響、慣性シフト
                    SimulationStepUpdateParticles(
                        new DataChunk(0, tdata.particleChunk.dataLength),
                        simulationPower,
                        simulationDeltaTime,
                        // team
                        teamId,
                        ref tdata,
                        ref cdata,
                        ref param,
                        ref wdata,
                        // wind
                        ref windDataArray,
                        // vmesh
                        ref attributes,
                        ref depthArray,
                        ref positions,
                        ref rotations,
                        ref vertexRootIndices,
                        // particle
                        ref nextPosArray,
                        ref oldPosArray,
                        ref basePosArray,
                        ref baseRotArray,
                        ref oldPositionArray,
                        ref oldRotationArray,
                        ref velocityPosArray,
                        ref velocityArray,
                        ref frictionArray,
                        // buffer
                        ref stepBasicPositionBuffer,
                        ref stepBasicRotationBuffer,
                        // temp
                        ref tempVectorBufferA,
                        ref tempVectorBufferB,
                        ref tempCountBuffer,
                        ref tempFloatBufferA
                        );

                    // 制約解決のためのステップごとの基準姿勢を計算
                    // ベースラインごと
                    // アニメーションポーズ使用の有無
                    // 初期姿勢の計算が不要なら抜ける
                    SimulationStepUpdateBaseLinePose(
                        new DataChunk(0, tdata.baseLineChunk.dataLength),
                        // team
                        ref tdata,
                        // vmesh
                        ref attributes,
                        ref vertexParentIndices,
                        ref baseLineStartDataIndices,
                        ref baseLineDataCounts,
                        ref baseLineData,
                        ref vertexLocalPositions,
                        ref vertexLocalRotations,
                        // particle
                        ref basePosArray,
                        ref baseRotArray,
                        // buffer
                        ref stepBasicPositionBuffer,
                        ref stepBasicRotationBuffer
                        );

                    // 制約の解決 ================================================================
                    TetherConstraint.SolverConstraint(
                        new DataChunk(0, tdata.particleChunk.dataLength),
                        // team
                        ref tdata,
                        ref param,
                        ref cdata,
                        // vmesh
                        ref attributes,
                        ref depthArray,
                        ref vertexRootIndices,
                        // particle
                        ref nextPosArray,
                        ref velocityPosArray,
                        ref frictionArray,
                        // buffer
                        ref stepBasicPositionBuffer
                        );

                    DistanceConstraint.SolverConstraint(
                        new DataChunk(0, tdata.particleChunk.dataLength),
                        simulationPower,
                        // team
                        ref tdata,
                        ref param,
                        // vmesh
                        ref attributes,
                        ref depthArray,
                        // particle
                        ref nextPosArray,
                        ref basePosArray,
                        ref velocityPosArray,
                        ref frictionArray,
                        // constraint
                        ref distanceIndexArray,
                        ref distanceDataArray,
                        ref distanceDistanceArray
                        );

                    AngleConstraint.SolverConstraint(
                        new DataChunk(0, tdata.baseLineChunk.dataLength),
                        simulationPower,
                        // team
                        ref tdata,
                        ref param,
                        // vmesh
                        ref attributes,
                        ref depthArray,
                        ref vertexParentIndices,
                        ref baseLineStartDataIndices,
                        ref baseLineDataCounts,
                        ref baseLineData,
                        // particle
                        ref nextPosArray,
                        ref velocityPosArray,
                        ref frictionArray,
                        // buffer
                        ref stepBasicPositionBuffer,
                        ref stepBasicRotationBuffer,
                        // buffer2
                        ref tempFloatBufferA,
                        ref tempVectorBufferA,
                        ref tempRotationBufferA,
                        ref tempRotationBufferB,
                        ref tempVectorBufferB
                        );

                    // バッファクリア
                    SimulationClearTempBuffer(
                        new DataChunk(0, tdata.particleChunk.dataLength),
                        // team
                        ref tdata,
                        // buffer2
                        ref tempVectorBufferA,
                        ref tempVectorBufferB,
                        ref tempCountBuffer,
                        ref tempFloatBufferA
                        );

                    TriangleBendingConstraint.SolverConstraint(
                        new DataChunk(0, tdata.bendingPairChunk.dataLength),
                        simulationPower,
                        // team
                        ref tdata,
                        ref param,
                        // vmesh
                        ref attributes,
                        ref depthArray,
                        // particle
                        ref nextPosArray,
                        ref frictionArray,
                        // constraints
                        ref bendingTrianglePairArray,
                        ref bendingRestAngleOrVolumeArray,
                        ref bendingSignOrVolumeArray,
                        // buffer2
                        ref tempVectorBufferA,
                        ref tempCountBuffer
                        );

                    TriangleBendingConstraint.SumConstraint(
                        new DataChunk(0, tdata.particleChunk.dataLength),
                        // team
                        ref tdata,
                        ref param,
                        // vmesh
                        ref attributes,
                        // particle
                        ref nextPosArray,
                        // buffer2
                        ref tempVectorBufferA,
                        ref tempCountBuffer
                        );

                    // コライダーコリジョン
                    if (param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Point)
                    {
                        ColliderCollisionConstraint.SolverPointConstraint(
                            new DataChunk(0, tdata.particleChunk.dataLength),
                            // team
                            ref tdata,
                            ref param,
                            // vmesh
                            ref attributes,
                            ref depthArray,
                            // particle
                            ref nextPosArray,
                            ref frictionArray,
                            ref collisionNormalArray,
                            ref velocityPosArray,
                            ref basePosArray,
                            // collider
                            ref colliderFlagArray,
                            ref colliderWorkDataArray
                            );
                    }
                    else if (param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge)
                    {
                        ColliderCollisionConstraint.SolverEdgeConstraint(
                            new DataChunk(0, tdata.proxyEdgeChunk.dataLength),
                            // team
                            ref tdata,
                            ref param,
                            // vmesh
                            ref attributes,
                            ref depthArray,
                            ref edges,
                            // particle
                            ref nextPosArray,
                            // collider
                            ref colliderFlagArray,
                            ref colliderWorkDataArray,
                            // buffer2
                            ref tempVectorBufferA,
                            ref tempVectorBufferB,
                            ref tempCountBuffer,
                            ref tempFloatBufferA
                            );
                        ColliderCollisionConstraint.SumEdgeConstraint(
                            new DataChunk(0, tdata.particleChunk.dataLength),
                            // team
                            ref tdata,
                            ref param,
                            // particle
                            ref nextPosArray,
                            ref frictionArray,
                            ref collisionNormalArray,
                            // buffer2
                            ref tempVectorBufferA,
                            ref tempVectorBufferB,
                            ref tempCountBuffer,
                            ref tempFloatBufferA
                            );
                    }

                    // コライダー衝突後はパーティクルが乱れる可能性があるためもう一度距離制約で整える。
                    // これは裏返り防止などに効果大。
                    DistanceConstraint.SolverConstraint(
                        new DataChunk(0, tdata.particleChunk.dataLength),
                        simulationPower,
                        // team
                        ref tdata,
                        ref param,
                        // vmesh
                        ref attributes,
                        ref depthArray,
                        // particle
                        ref nextPosArray,
                        ref basePosArray,
                        ref velocityPosArray,
                        ref frictionArray,
                        // constraint
                        ref distanceIndexArray,
                        ref distanceDataArray,
                        ref distanceDistanceArray
                        );

                    // モーション制約はコライダーより優先
                    MotionConstraint.SolverConstraint(
                        new DataChunk(0, tdata.particleChunk.dataLength),
                        // team
                        ref tdata,
                        ref param,
                        // vmesh
                        ref attributes,
                        ref depthArray,
                        // particle
                        ref basePosArray,
                        ref baseRotArray,
                        ref nextPosArray,
                        ref velocityPosArray,
                        ref frictionArray,
                        ref collisionNormalArray
                        );

                    // 座標確定
                    SimulationStepPostTeam(
                        new DataChunk(0, tdata.particleChunk.dataLength),
                        simulationDeltaTime,
                        // team
                        teamId,
                        ref tdata,
                        ref cdata,
                        ref param,
                        // vmesh
                        ref attributes,
                        ref depthArray,
                        // particle
                        ref oldPosArray,
                        ref velocityArray,
                        ref nextPosArray,
                        ref velocityPosArray,
                        ref frictionArray,
                        ref staticFrictionArray,
                        ref collisionNormalArray,
                        ref realVelocityArray
                        );

                    // コライダーの後更新
                    ColliderManager.SimulationEndStep(
                        new DataChunk(0, tdata.colliderChunk.dataLength),
                        // team
                        ref tdata,
                        // collider
                        ref colliderNowPositions,
                        ref colliderNowRotations,
                        ref colliderOldPositions,
                        ref colliderOldRotations
                        );
                }

                // ■表示位置の決定
                SimulationCalcDisplayPosition(
                    new DataChunk(0, tdata.particleChunk.dataLength),
                    simulationDeltaTime,
                    // team
                    ref tdata,
                    // particle
                    ref oldPosArray,
                    ref realVelocityArray,
                    ref oldPositionArray,
                    ref oldRotationArray,
                    ref dispPosArray,
                    // vmesh
                    ref attributes,
                    ref positions,
                    ref rotations,
                    ref vertexRootIndices,
                    // temp
                    ref tempVectorBufferA,
                    ref tempRotationBufferA
                );

                // ■クロスシミュレーション後の頂点姿勢計算
                // プロキシメッシュの頂点から法線接線を求め姿勢を確定させる
                // ラインがある場合はベースラインごとに姿勢を整える
                // BoneClothの場合は頂点姿勢を連動するトランスフォームデータにコピーする
                VirtualMeshManager.SimulationPostProxyMeshUpdateLine(
                    new DataChunk(0, tdata.baseLineChunk.dataLength),
                    // team
                    ref tdata,
                    ref param,
                    // vmesh
                    ref attributes,
                    ref positions,
                    ref rotations,
                    ref vertexLocalPositions,
                    ref vertexLocalRotations,
                    ref vertexChildIndexArray,
                    ref vertexChildDataArray,
                    ref baseLineFlags,
                    ref baseLineStartDataIndices,
                    ref baseLineDataCounts,
                    ref baseLineData,
                    // temp
                    ref tempVectorBufferA,
                    ref tempRotationBufferA
                    );
                VirtualMeshManager.SimulationPostProxyMeshUpdateTriangle(
                    new DataChunk(0, tdata.proxyTriangleChunk.dataLength),
                    // team
                    ref tdata,
                    // vmesh
                    ref positions,
                    ref triangles,
                    ref triangleNormals,
                    ref triangleTangents,
                    ref uvs
                    );
                VirtualMeshManager.SimulationPostProxyMeshUpdateTriangleSum(
                    new DataChunk(0, tdata.proxyCommonChunk.dataLength),
                    // team
                    ref tdata,
                    // vmesh
                    ref rotations,
                    ref triangleNormals,
                    ref triangleTangents,
                    ref vertexToTriangles,
                    ref normalAdjustmentRotations
                    );
                VirtualMeshManager.SimulationPostProxyMeshUpdateWorldTransform(
                    new DataChunk(0, tdata.proxyCommonChunk.dataLength),
                    // team
                    ref tdata,
                    // vmesh
                    ref positions,
                    ref rotations,
                    ref vertexToTransformRotations,
                    // transform
                    ref transformPositionArray,
                    ref transformRotationArray
                    );
                VirtualMeshManager.SimulationPostProxyMeshUpdateLocalTransform(
                    // team
                    ref tdata,
                    // vmesh
                    ref attributes,
                    ref vertexParentIndices,
                    // transform
                    ref transformPositionArray,
                    ref transformRotationArray,
                    ref transformScaleArray,
                    ref transformLocalPositionArray,
                    ref transformLocalRotationArray
                    );

                // ■コライダー更新後処理
                ColliderManager.SimulationPostUpdate(
                    // team
                    ref tdata,
                    // collider
                    ref colliderFramePositions,
                    ref colliderFrameRotations,
                    ref colliderOldFramePositions,
                    ref colliderOldFrameRotations
                    );

                // ■チーム更新後処理
                TeamManager.SimulationPostTeamUpdate(
                    // team
                    ref tdata,
                    ref cdata
                    );
            }
        }

        //=========================================================================================
        /// <summary>
        /// シミュレーション実行前処理
        /// -リセット
        /// -移動影響
        /// </summary>
        static void SimulationPreTeamUpdate(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            in ClothParameters param,
            in InertiaConstraint.CenterData cdata,
            // vmesh
            in NativeArray<float3> positions,
            in NativeArray<quaternion> rotations,
            in NativeArray<float> vertexDepths,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float3> oldPosArray,
            ref NativeArray<quaternion> oldRotArray,
            ref NativeArray<float3> basePosArray,
            ref NativeArray<quaternion> baseRotArray,
            ref NativeArray<float3> oldPositionArray,
            ref NativeArray<quaternion> oldRotationArray,
            ref NativeArray<float3> velocityPosArray,
            ref NativeArray<float3> dispPosArray,
            ref NativeArray<float3> velocityArray,
            ref NativeArray<float3> realVelocityArray,
            ref NativeArray<float> frictionArray,
            ref NativeArray<float> staticFrictionArray,
            ref NativeArray<float3> collisionNormalArray
            )
        {
            // パーティクルごと
            var pc = tdata.particleChunk;
            //int pindex = pc.startIndex;
            int pindex = pc.startIndex + chunk.startIndex;
            //int vindex = tdata.proxyCommonChunk.startIndex;
            int vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
            if (tdata.IsReset)
            {
                // リセット
                //for (int i = 0; i < pc.dataLength; i++, pindex++, vindex++)
                for (int i = 0; i < chunk.dataLength; i++, pindex++, vindex++)
                {
                    var pos = positions[vindex];
                    var rot = rotations[vindex];

                    nextPosArray[pindex] = pos;
                    oldPosArray[pindex] = pos;
                    oldRotArray[pindex] = rot;
                    basePosArray[pindex] = pos;
                    baseRotArray[pindex] = rot;
                    oldPositionArray[pindex] = pos;
                    oldRotationArray[pindex] = rot;
                    velocityPosArray[pindex] = pos;
                    dispPosArray[pindex] = pos;
                    velocityArray[pindex] = 0;
                    realVelocityArray[pindex] = 0;
                    frictionArray[pindex] = 0;
                    staticFrictionArray[pindex] = 0;
                    collisionNormalArray[pindex] = 0;
                }
            }
            else if (tdata.IsInertiaShift || tdata.IsNegativeScaleTeleport)
            {
                // シフト、テレポート
                //for (int i = 0; i < pc.dataLength; i++, pindex++, vindex++)
                for (int i = 0; i < chunk.dataLength; i++, pindex++, vindex++)
                {
                    var oldPos = oldPosArray[pindex];
                    var oldRot = oldRotArray[pindex];
                    var oldPosition = oldPositionArray[pindex];
                    var oldRotation = oldRotationArray[pindex];
                    var dispPos = dispPosArray[pindex];
                    var velocity = velocityArray[pindex];
                    var realVelocity = realVelocityArray[pindex];

                    // ■マイナススケール
                    if (tdata.IsNegativeScaleTeleport)
                    {
                        // 本体のスケール反転に合わせてシミュレーションに影響が出ないように必要な座標系を同様に軸反転させる
                        // パーティクルはセンター空間で軸反転させる
                        // 軸反転用マトリックス
                        float4x4 negativeM = cdata.negativeScaleMatrix;

                        oldPos = MathUtility.TransformPoint(oldPos, negativeM);
                        oldRot = MathUtility.TransformRotation(oldRot, negativeM, 1);

                        oldPosition = MathUtility.TransformPoint(oldPosition, negativeM);
                        oldRotation = MathUtility.TransformRotation(oldRotation, negativeM, 1);

                        dispPos = MathUtility.TransformPoint(dispPos, negativeM);

                        velocity = MathUtility.TransformVector(velocity, negativeM);
                        realVelocity = MathUtility.TransformVector(realVelocity, negativeM);
                    }

                    // ■慣性全体シフト
                    if (tdata.IsInertiaShift)
                    {
                        // cdata.frameComponentShiftVector : 全体シフトベクトル
                        // cdata.frameComponentShiftRotation : 全体シフト回転
                        // cdata.oldComponentWorldPosition : フレーム移動前のコンポーネント中心位置

                        oldPos = MathUtility.ShiftPosition(oldPos, cdata.oldComponentWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                        oldRot = math.mul(cdata.frameComponentShiftRotation, oldRot);

                        oldPosition = MathUtility.ShiftPosition(oldPosition, cdata.oldComponentWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                        oldRotation = math.mul(cdata.frameComponentShiftRotation, oldRotation);

                        dispPos = MathUtility.ShiftPosition(dispPos, cdata.oldComponentWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);

                        velocity = math.mul(cdata.frameComponentShiftRotation, velocity);
                        realVelocity = math.mul(cdata.frameComponentShiftRotation, realVelocity);
                    }

                    oldPosArray[pindex] = oldPos;
                    oldRotArray[pindex] = oldRot;
                    oldPositionArray[pindex] = oldPosition;
                    oldRotationArray[pindex] = oldRotation;
                    dispPosArray[pindex] = dispPos;
                    velocityArray[pindex] = velocity;
                    realVelocityArray[pindex] = realVelocity;
                }
            }
        }

        static float3 WindBatchJob(
            int teamId,
            in WindParams windParams,
            int vindex,
            int pindex,
            float depth,
            ref NativeArray<int> vertexRootIndices,
            ref TeamWindData teamWindData,
            ref NativeArray<WindManager.WindData> windDataArray,
            ref NativeArray<float> frictionArray
            )
        {
            float3 windForce = 0;

            // 基準ルート座標
            // (1)チームごとにずらす
            // (2)同期率によりルートラインごとにずらす
            // (3)チームの座標やパーティクルの座標は計算に入れない
            int rootIndex = vertexRootIndices[vindex];
            float3 windPos = (teamId + 1) * 4.19230645f + (rootIndex * 0.0023963f * (1.0f - windParams.synchronization) * 100);

            // ゾーンごとの風影響計算
            int cnt = teamWindData.ZoneCount;
            for (int i = 0; i < cnt; i++)
            {
                var windInfo = teamWindData.windZoneList[i];
                var windData = windDataArray[windInfo.windId];
                windForce += WindForceBlendBatchJob(windInfo, windParams, windPos, windData.turbulence);
            }

#if true
            // 移動風影響計算
            if (windParams.movingWind > 0.01f)
            {
                windForce += WindForceBlendBatchJob(teamWindData.movingWind, windParams, windPos, 1.0f);
            }
#endif

            //Debug.Log($"windForce:{windForce}");

            // その他影響
            // チーム風影響
            float influence = windParams.influence; // 0.0 ~ 2.0

            // 摩擦による影響
            float friction = frictionArray[pindex];
            influence *= (1.0f - friction);

            // 深さ影響
            float depthScale = depth * depth;
            influence *= math.lerp(1.0f, depthScale, windParams.depthWeight);

            // 最終影響
            windForce *= influence;

            //Debug.Log($"windForce:{windForce}");

            return windForce;
        }

        static void SimulationStepUpdateParticles(
            DataChunk chunk,
            float4 simulationPower,
            float simulationDeltaTime,
            // team
            int teamId,
            ref TeamManager.TeamData tdata,
            ref InertiaConstraint.CenterData cdata,
            ref ClothParameters param,
            ref TeamWindData wdata,
            // wind
            ref NativeArray<WindManager.WindData> windDataArray,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float> depthArray,
            ref NativeArray<float3> positions,
            ref NativeArray<quaternion> rotations,
            ref NativeArray<int> vertexRootIndices,
            // particle
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float3> oldPosArray,
            ref NativeArray<float3> basePosArray,
            ref NativeArray<quaternion> baseRotArray,
            ref NativeArray<float3> oldPositionArray,
            ref NativeArray<quaternion> oldRotationArray,
            ref NativeArray<float3> velocityPosArray,
            ref NativeArray<float3> velocityArray,
            ref NativeArray<float> frictionArray,
            // buffer
            ref NativeArray<float3> stepBasicPositionBuffer,
            ref NativeArray<quaternion> stepBasicRotationBuffer,
            // temp buffer
            ref NativeArray<float3> tempVectorBufferA,
            ref NativeArray<float3> tempVectorBufferB,
            ref NativeArray<int> tempCountBuffer,
            ref NativeArray<float> tempFloatBufferA
            )
        {
            // 速度更新、外力の影響、慣性シフト
            //int pindex = tdata.particleChunk.startIndex;
            //int vindex = tdata.proxyCommonChunk.startIndex;
            int pindex = tdata.particleChunk.startIndex + chunk.startIndex;
            int vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
            //for (int i = 0; i < tdata.particleChunk.dataLength; i++, pindex++, vindex++)
            for (int i = 0; i < chunk.dataLength; i++, pindex++, vindex++)
            {
                // nextPosSwap
                var attr = attributes[vindex];
                float depth = depthArray[vindex];
                var oldPos = oldPosArray[pindex];

                var nextPos = oldPos;
                var velocityPos = oldPos;

                // 基準姿勢のステップ補間
                var oldPosition = oldPositionArray[pindex];
                var oldRotation = oldRotationArray[pindex];
                var position = positions[vindex];
                var rotation = rotations[vindex];

                // ベース位置補間
                float3 basePos = math.lerp(oldPosition, position, tdata.frameInterpolation);
                quaternion baseRot = math.slerp(oldRotation, rotation, tdata.frameInterpolation);
                baseRot = math.normalize(baseRot); // 必要
                basePosArray[pindex] = basePos;
                baseRotArray[pindex] = baseRot;

                // ステップ基本位置
                stepBasicPositionBuffer[pindex] = basePos;
                stepBasicRotationBuffer[pindex] = baseRot;

                // 移動パーティクル
                if (attr.IsMove() || tdata.IsSpring)
                {
                    // 重量
                    //float mass = MathUtility.CalcMass(depth);

                    // 速度
                    var velocity = velocityArray[pindex];

#if true
                    // ■ローカル慣性シフト
                    // シフト量
                    float3 inertiaVector = cdata.inertiaVector;
                    quaternion inertiaRotation = cdata.inertiaRotation;

                    // 慣性の深さ影響
                    float inertiaDepth = param.inertiaConstraint.depthInertia * (1.0f - depth * depth); // 二次曲線
                                                                                                        //Debug.Log($"[{pindex}] inertiaDepth:{inertiaDepth}");
                    inertiaVector = math.lerp(inertiaVector, cdata.stepVector, inertiaDepth);
                    inertiaRotation = math.slerp(inertiaRotation, cdata.stepRotation, inertiaDepth);

                    // たぶんこっちが正しい
                    float3 lpos = oldPos - cdata.oldWorldPosition;
                    lpos = math.mul(inertiaRotation, lpos);
                    lpos += inertiaVector;
                    float3 wpos = cdata.oldWorldPosition + lpos;
                    var inertiaOffset = wpos - nextPos;

                    // nextPos
                    nextPos = wpos;

                    // 速度位置も調整
                    velocityPos += inertiaOffset;

                    // 速度に慣性回転を加える
                    velocity = math.mul(inertiaRotation, velocity);
#endif

                    // 安定化用の速度割合
                    velocity *= tdata.velocityWeight;

                    // 抵抗
                    // 重力に影響させたくないので先に計算する（※通常はforce適用後に行うのが一般的）
                    float damping = param.dampingCurveData.MC2EvaluateCurveClamp01(depth);
                    velocity *= math.saturate(1.0f - damping * simulationPower.z);

                    // 外力
                    float3 force = 0;

                    // 重力
                    float3 gforce = param.worldGravityDirection * (param.gravity * tdata.gravityRatio);
                    force += gforce;

                    // 外力
                    float3 exForce = 0;
                    float mass = MathUtility.CalcMass(depth);
                    switch (tdata.forceMode)
                    {
                        case ClothForceMode.VelocityAdd:
                            exForce = tdata.impactForce / mass;
                            break;
                        case ClothForceMode.VelocityAddWithoutDepth:
                            exForce = tdata.impactForce;
                            break;
                        case ClothForceMode.VelocityChange:
                            exForce = tdata.impactForce / mass;
                            velocity = 0;
                            break;
                        case ClothForceMode.VelocityChangeWithoutDepth:
                            exForce = tdata.impactForce;
                            velocity = 0;
                            break;
                    }
                    force += exForce;

                    // 風力
                    force += WindBatchJob(
                        teamId,
                        //tdata,
                        param.wind,
                        //cdata,
                        vindex,
                        pindex,
                        depth,
                        ref vertexRootIndices,
                        ref wdata,
                        ref windDataArray,
                        ref frictionArray
                        );

                    // 外力チームスケール倍率
                    force *= tdata.scaleRatio;

                    // 速度更新
                    velocity += force * simulationDeltaTime;

                    // 予測位置更新
                    nextPos += velocity * simulationDeltaTime;
                }
                else
                {
                    // 固定パーティクル
                    nextPos = basePos;
                    velocityPos = basePos;
                }

                // スプリング（固定パーティクルのみ）
                if (tdata.IsSpring && attr.IsFixed())
                {
                    // ノイズ用に時間を不規則にずらす
                    //Spring(param.springConstraint, param.normalAxis, ref nextPos, basePos, baseRot, (tdata.time + index * 49.6198f) * 2.4512f + math.csum(nextPos), tdata.scaleRatio);
                    SpringBatchJob(param.springConstraint, param.normalAxis, ref nextPos, basePos, baseRot, (tdata.time + pindex * 49.6198f) * 2.4512f + math.csum(nextPos), tdata.scaleRatio);
                }

                // 速度計算用の移動前の位置
                velocityPosArray[pindex] = velocityPos;

                // 予測位置格納
                nextPosArray[pindex] = nextPos;

                // 作業バッファクリア
                tempVectorBufferA[pindex] = 0;
                tempVectorBufferB[pindex] = 0;
                tempCountBuffer[pindex] = 0;
                tempFloatBufferA[pindex] = 0;
            }
        }

        static void SpringBatchJob(
            in SpringConstraint.SpringConstraintParams springParams,
            ClothNormalAxis normalAxis,
            ref float3 nextPos,
            in float3 basePos,
            in quaternion baseRot,
            float noiseTime,
            float scaleRatio
            )
        {
            // clamp distance
            var v = nextPos - basePos;
            float3 dir = math.up();
            switch (normalAxis)
            {
                case ClothNormalAxis.Right:
                    dir = math.right();
                    break;
                case ClothNormalAxis.Up:
                    dir = math.up();
                    break;
                case ClothNormalAxis.Forward:
                    dir = math.forward();
                    break;
                case ClothNormalAxis.InverseRight:
                    dir = -math.right();
                    break;
                case ClothNormalAxis.InverseUp:
                    dir = -math.up();
                    break;
                case ClothNormalAxis.InverseForward:
                    dir = -math.forward();
                    break;
            }
            dir = math.mul(baseRot, dir);
            float limitDistance = springParams.limitDistance * scaleRatio; // スケール倍率

            if (limitDistance > 1e-08f)
            {
                // 球クランプ
                var len = math.length(v);
                if (len > limitDistance)
                {
                    v *= (limitDistance / len);
                }

                // 楕円クランプ
                if (springParams.normalLimitRatio < 1.0f)
                {
                    // もっとスマートにならないか..
                    float ylen = math.dot(dir, v);
                    float3 vx = v - dir * ylen;
                    float xlen = math.length(vx);
                    float t = xlen / limitDistance;
                    float y = math.cos(math.asin(t));
                    y *= limitDistance * springParams.normalLimitRatio;

                    if (math.abs(ylen) > y)
                    {
                        v -= (math.abs(ylen) - y) * math.sign(ylen) * dir;
                    }
                }
            }
            else
            {
                v = float3.zero;
            }

            // スプリング力
            float power = springParams.springPower;

            // ノイズ
            if (springParams.springNoise > 0.0f)
            {
                float noise = math.sin(noiseTime); // -1.0~+1.0
                                                   //Debug.Log(noise);
                noise *= springParams.springNoise * 0.6f; // スケーリング
                power = math.max(power + power * noise, 0.0f);
            }

            // スプリング適用
            v -= v * power;
            nextPos = basePos + v;
        }

        static void SimulationStepUpdateBaseLinePose(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<int> vertexParentIndices,
            ref NativeArray<ushort> baseLineStartDataIndices,
            ref NativeArray<ushort> baseLineDataCounts,
            ref NativeArray<ushort> baseLineData,
            ref NativeArray<float3> vertexLocalPositions,
            ref NativeArray<quaternion> vertexLocalRotations,
            // particle
            ref NativeArray<float3> basePosArray,
            ref NativeArray<quaternion> baseRotArray,
            // buffer
            ref NativeArray<float3> stepBasicPositionBuffer,
            ref NativeArray<quaternion> stepBasicRotationBuffer
            )
        {
            // 制約解決のためのステップごとの基準姿勢を計算
            // ベースラインごと
            // アニメーションポーズ使用の有無
            // 初期姿勢の計算が不要なら抜ける
            float blendRatio = tdata.animationPoseRatio;
            if (blendRatio <= 0.99f)
            {
                int b_datastart = tdata.baseLineDataChunk.startIndex;
                int p_start = tdata.particleChunk.startIndex;
                int v_start = tdata.proxyCommonChunk.startIndex;

                // チームスケール
                float3 scl = tdata.initScale * tdata.scaleRatio;

                //int bindex = tdata.baseLineChunk.startIndex;
                int bindex = tdata.baseLineChunk.startIndex + chunk.startIndex;
                //for (int k = 0; k < tdata.baseLineChunk.dataLength; k++, bindex++)
                for (int k = 0; k < chunk.dataLength; k++, bindex++)
                {
                    int b_start = baseLineStartDataIndices[bindex];
                    int b_cnt = baseLineDataCounts[bindex];
                    int b_dataindex = b_start + b_datastart;
                    {
                        for (int i = 0; i < b_cnt; i++, b_dataindex++)
                        {
                            int l_index = baseLineData[b_dataindex];
                            int pindex = p_start + l_index;
                            int vindex = v_start + l_index;

                            // 親
                            int p_index = vertexParentIndices[vindex];
                            int p_pindex = p_index + p_start;

                            var attr = attributes[vindex];
                            if (attr.IsMove() && p_index >= 0)
                            {
                                // 移動
                                // 親から姿勢を算出する
                                var lpos = vertexLocalPositions[vindex];
                                var lrot = vertexLocalRotations[vindex];
                                var ppos = stepBasicPositionBuffer[p_pindex];
                                var prot = stepBasicRotationBuffer[p_pindex];

                                // マイナススケール
                                lpos *= tdata.negativeScaleDirection;
                                lrot = lrot.value * tdata.negativeScaleQuaternionValue;

                                stepBasicPositionBuffer[pindex] = math.mul(prot, lpos * scl) + ppos;
                                stepBasicRotationBuffer[pindex] = math.mul(prot, lrot);
                            }
                            else
                            {
                                // マイナススケール
                                var prot = stepBasicRotationBuffer[pindex];
                                var lw = float4x4.TRS(0, prot, tdata.negativeScaleDirection);
                                quaternion rot = MathUtility.ToRotation(lw.c1.xyz, lw.c2.xyz);
                                stepBasicRotationBuffer[pindex] = rot;
                            }
                        }
                    }

                    // アニメーション姿勢とブレンド
                    if (blendRatio > Define.System.Epsilon)
                    {
                        b_dataindex = b_start + b_datastart;
                        for (int i = 0; i < b_cnt; i++, b_dataindex++)
                        {
                            int l_index = baseLineData[b_dataindex];
                            int pindex = p_start + l_index;

                            var bpos = basePosArray[pindex];
                            var brot = baseRotArray[pindex];

                            stepBasicPositionBuffer[pindex] = math.lerp(stepBasicPositionBuffer[pindex], bpos, blendRatio);
                            stepBasicRotationBuffer[pindex] = math.slerp(stepBasicRotationBuffer[pindex], brot, blendRatio);
                        }
                    }
                }
            }
        }

        static float3 WindForceBlendBatchJob(
            in TeamWindInfo windInfo,
            in WindParams windParams,
            in float3 windPos,
            float windTurbulence
            )
        {
            float windMain = windInfo.main;
            if (windMain < 0.01f)
                return 0;

            //Debug.Log($"windMain:{windMain}");

            // 風速係数
            float mainRatio = windMain / Define.System.WindBaseSpeed; // 0.0 ~ 

            // Sin波形
            var sinPos = windPos + windInfo.time * 10.0f;
            float2 sinXY = math.sin(sinPos.xy);

            // Noise波形
            var noisePos = windPos + windInfo.time * 2.3132f; // Sin波形との調整用
            float2 noiseXY = new float2(noise.cnoise(noisePos.xy), noise.cnoise(noisePos.yx));
            noiseXY *= 2.3f; // cnoiseは弱いので補強 2.0?

            // 波形ブレンド
            float2 waveXY = math.lerp(sinXY, noiseXY, windParams.blend);

            // 基本乱流率
            windTurbulence *= windParams.turbulence; // 0.0 ~ 2.0

            // 風向き
            const float rangAng = 45.0f; // 乱流角度
            var ang = math.radians(waveXY * rangAng);
            ang.y *= math.lerp(0.1f, 0.5f, windParams.blend); // 横方向は抑える。そうしないと円運動になってしまうため。0.3 - 0.5?
            ang *= windTurbulence; // 乱流率
            var rq = quaternion.Euler(ang.x, ang.y, 0.0f); // XY
            var dirq = MathUtility.AxisQuaternion(windInfo.direction);
            float3 wdir = math.forward(math.mul(dirq, rq));

            // 風速
            // 風速が低いと大きくなり、風速が高いと0.0になる
            float mainScale = math.saturate(1.0f - mainRatio * 1.0f);
            float mainWave = math.unlerp(-1.0f, 1.0f, waveXY.x); // 0.0 ~ 1.0
            mainWave *= mainScale * windTurbulence;
            windMain -= windMain * mainWave;

            // 合成
            float3 windForce = wdir * windMain;

            return windForce;
        }

        /// <summary>
        /// 座標確定
        /// </summary>
        static void SimulationStepPostTeam(
            DataChunk chunk,
            float simulationDeltaTime,
            // team
            int teamId,
            ref TeamManager.TeamData tdata,
            ref InertiaConstraint.CenterData cdata,
            ref ClothParameters param,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float> depthArray,
            // particle
            ref NativeArray<float3> oldPosArray,
            ref NativeArray<float3> velocityArray,
            ref NativeArray<float3> nextPosArray,
            ref NativeArray<float3> velocityPosArray,
            ref NativeArray<float> frictionArray,
            ref NativeArray<float> staticFrictionArray,
            ref NativeArray<float3> collisionNormalArray,
            ref NativeArray<float3> realVelocityArray
            )
        {
            // 座標確定
            //int pindex = tdata.particleChunk.startIndex;
            //int vindex = tdata.proxyCommonChunk.startIndex;
            int pindex = tdata.particleChunk.startIndex + chunk.startIndex;
            int vindex = tdata.proxyCommonChunk.startIndex + chunk.startIndex;
            //for (int k = 0; k < tdata.particleChunk.dataLength; k++, pindex++, vindex++)
            for (int k = 0; k < chunk.dataLength; k++, pindex++, vindex++)
            {
                var attr = attributes[vindex];
                var depth = depthArray[vindex];
                var nextPos = nextPosArray[pindex];
                var oldPos = oldPosArray[pindex];

                if (attr.IsMove() || tdata.IsSpring)
                {
                    // 移動パーティクル
                    var velocityOldPos = velocityPosArray[pindex];

#if true
                    // ■摩擦
                    float friction = frictionArray[pindex];
                    float3 cn = collisionNormalArray[pindex];
                    bool isCollision = math.lengthsq(cn) > Define.System.Epsilon && friction > Define.System.Epsilon; // 接触の有無
                    float staticFrictionParam = param.colliderCollisionConstraint.staticFriction * tdata.scaleRatio;
                    float dynamicFrictionParam = param.colliderCollisionConstraint.dynamicFriction;
#endif

#if true
                    // ■静止摩擦
                    float staticFriction = staticFrictionArray[pindex];
                    if (isCollision && staticFrictionParam > 0.0f)
                    {
                        // 接線方向の移動速度から計算する
                        var v = nextPos - oldPos;
                        var tanv = v - MathUtility.Project(v, cn); // 接線方向の移動ベクトル
                        float tangentVelocity = math.length(tanv) / simulationDeltaTime; // 接線方向の移動速度

                        // 静止速度以下ならば係数を上げる
                        if (tangentVelocity < staticFrictionParam)
                        {
                            staticFriction = math.saturate(staticFriction + 0.04f); // 係数増加(0.02?)
                        }
                        else
                        {
                            // 接線速度に応じて係数を減少
                            var vel = tangentVelocity - staticFrictionParam;
                            var value = math.max(vel / 0.2f, 0.05f);
                            staticFriction = math.saturate(staticFriction - value);
                        }

                        // 接線方向に位置を巻き戻す
                        tanv *= staticFriction;
                        nextPos -= tanv;
                        velocityOldPos -= tanv;
                    }
                    else
                    {
                        // 減衰
                        staticFriction = math.saturate(staticFriction - 0.05f);
                    }
                    staticFrictionArray[pindex] = staticFriction;
#endif

                    // ■速度更新(m/s) ------------------------------------------
                    // 速度計算用の位置から割り出す（制約ごとの速度調整用）
                    float3 velocity = (nextPos - velocityOldPos) / simulationDeltaTime;
                    float sqVel = math.lengthsq(velocity);
                    float3 normalVelocity = sqVel > Define.System.Epsilon ? math.normalize(velocity) : 0;

#if true
                    // ■動摩擦
                    // 衝突面との角度が大きいほど減衰が強くなる(MC1)
                    if (isCollision && dynamicFrictionParam > 0.0f && sqVel >= Define.System.Epsilon)
                    {
                        //float dot = math.dot(cn, math.normalize(velocity));
                        float dot = math.dot(cn, normalVelocity);
                        dot = 0.5f + 0.5f * dot; // 1.0(front) - 0.5(side) - 0.0(back)
                        dot *= dot; // サイドを強めに
                        dot = 1.0f - dot; // 0.0(front) - 0.75(side) - 1.0(back)
                        velocity -= velocity * (dot * math.saturate(friction * dynamicFrictionParam));
                    }

                    // 摩擦減衰
                    friction *= Define.System.FrictionDampingRate;
                    frictionArray[pindex] = friction;
#endif

#if true
                    // 最大速度
                    // 最大速度はある程度制限したほうが動きが良くなるので入れるべき。
                    // 特に回転時の髪などの動きが柔らかくなる。
                    // しかし制限しすぎるとコライダーの押し出し制度がさがるので注意。
                    if (param.inertiaConstraint.particleSpeedLimit >= 0.0f)
                    {
                        velocity = MathUtility.ClampVector(velocity, param.inertiaConstraint.particleSpeedLimit * tdata.scaleRatio);
                    }
#endif
#if true
                    // ■遠心力加速 ---------------------------------------------
                    if (cdata.angularVelocity > Define.System.Epsilon && param.inertiaConstraint.centrifualAcceleration > Define.System.Epsilon && sqVel >= Define.System.Epsilon)
                    {
                        // 回転中心のローカル座標
                        var lpos = nextPos - cdata.nowWorldPosition;

                        // 回転軸平面に投影
                        var v = MathUtility.ProjectOnPlane(lpos, cdata.rotationAxis);
                        var r = math.length(v);
                        if (r > Define.System.Epsilon)
                        {
                            float3 n = v / r;

                            // 角速度(rad/s)
                            float w = cdata.angularVelocity;

                            // 重量（重いほど遠心力は強くなる）
                            // ここでは末端に行くほど軽くする
                            //float m = (1.0f - depth) * 3.0f;
                            //float m = 1.0f + (1.0f - depth) * 2.0f;
                            float m = 1.0f + (1.0f - depth); // fix
                                                             //float m = 1.0f + depth * 3.0f;
                                                             //const float m = 1;

                            // 遠心力
                            var f = m * w * w * r;

                            // 回転方向uと速度方向が同じ場合のみ力を加える（内積による乗算）
                            // 実際の物理では遠心力は紐が張った状態でなければ発生しないがこの状態を判別する方法は簡単ではない
                            // そのためこのような近似で代用する
                            // 回転と速度が逆方向の場合は紐が緩んでいると判断し遠心力の増強を適用しない
                            float3 u = math.normalize(math.cross(cdata.rotationAxis, n));
                            f *= math.saturate(math.dot(normalVelocity, u));

                            // 遠心力を速度に加算する
                            velocity += n * (f * param.inertiaConstraint.centrifualAcceleration * 0.02f);
                        }
                    }
#endif
                    // 安定化用の速度割合
                    velocity *= tdata.velocityWeight;

                    // 書き戻し
                    velocityArray[pindex] = velocity;
                }

                // 実速度
                float3 realVelocity = (nextPos - oldPos) / simulationDeltaTime;
                realVelocityArray[pindex] = realVelocity;

                // 今回の予測位置を記録
                oldPosArray[pindex] = nextPos;
            }
        }

        /// <summary>
        /// シミュレーション完了後の表示位置の計算
        /// - 未来予測
        /// </summary>
        static void SimulationCalcDisplayPosition(
            DataChunk chunk,
            float simulationDeltaTime,
            // team
            ref TeamManager.TeamData tdata,
            // particle
            ref NativeArray<float3> oldPosArray,
            ref NativeArray<float3> realVelocityArray,
            ref NativeArray<float3> oldPositionArray,
            ref NativeArray<quaternion> oldRotationArray,
            ref NativeArray<float3> dispPosArray,
            // vmesh
            ref NativeArray<VertexAttribute> attributes,
            ref NativeArray<float3> positions,
            ref NativeArray<quaternion> rotations,
            ref NativeArray<int> vertexRootIndices,
            // temp
            ref NativeArray<float3> tempVectorBufferA,
            ref NativeArray<quaternion> tempRotationBufferA
        )
        {
            //int pindex = tdata.particleChunk.startIndex;
            int pindex = tdata.particleChunk.startIndex + chunk.startIndex;
            int v_start = tdata.proxyCommonChunk.startIndex;
            //int vindex = v_start;
            int vindex = v_start + chunk.startIndex;
            //for (int k = 0; k < tdata.particleChunk.dataLength; k++, pindex++, vindex++)
            for (int k = 0; k < chunk.dataLength; k++, pindex++, vindex++)
            {
                var attr = attributes[vindex];

                var pos = positions[vindex];
                var rot = rotations[vindex];
                //Debug.Log($"DispRot [{vindex}] nor:{MathUtility.ToNormal(rot)}, tan:{MathUtility.ToTangent(rot)}");

                if (attr.IsMove() || tdata.IsSpring)
                {
                    // 移動パーティクル
                    var dpos = oldPosArray[pindex];

#if !MC2_DISABLE_FUTURE
                    // 未来予測
                    // 最終計算位置と実速度から次のステップ位置を予測し、その間のフレーム時間位置を表示位置とする
                    float3 velocity = realVelocityArray[pindex] * simulationDeltaTime;
                    float3 fpos = dpos + velocity;
                    float interval = (tdata.nowUpdateTime + simulationDeltaTime) - tdata.oldTime;
                    float t = interval > 0.0f ? (tdata.time - tdata.oldTime) / interval : 0.0f;
                    fpos = math.lerp(dispPosArray[pindex], fpos, t);
                    // ルートからの距離クランプ（安全対策）
                    int rootIndex = vertexRootIndices[vindex];
                    if (rootIndex >= 0)
                    {
                        var rootPos = positions[v_start + rootIndex];
                        float originalDist = math.distance(rootPos, pos);
                        float clampDist = originalDist * Define.System.MaxDistanceRatioFutuerPrediction; // 許容限界距離
                        var v = fpos - rootPos;
                        v = MathUtility.ClampVector(v, clampDist);
                        fpos = rootPos + v;
                    }

                    dpos = fpos;
#endif

                    // 表示位置
                    var dispPos = dpos;

                    // 表示位置を記録
                    dispPosArray[pindex] = dispPos;

                    // ブレンドウエイト
                    var vpos = math.lerp(positions[vindex], dispPos, tdata.blendWeight);

                    // vmeshに反映
                    positions[vindex] = vpos;
                }
                else
                {
                    // 固定パーティクル
                    // 表示位置は常にオリジナル位置
                    var dispPos = positions[vindex];
                    dispPosArray[pindex] = dispPos;
                }

                // １つ前の原点位置を記録
                if (tdata.IsRunning)
                {
                    oldPositionArray[pindex] = pos;
                    oldRotationArray[pindex] = rot;
                }

                // マイナススケール
                // 回転をマイナススケールを適用した表示計算用の回転に変換する
                if (tdata.IsNegativeScale)
                {
                    var lw = float4x4.TRS(0, rot, tdata.negativeScaleDirection);
                    rot = MathUtility.ToRotation(lw.c1.xyz, lw.c2.xyz);
                    rotations[vindex] = rot;
                }

                // BoneClothのベースライン姿勢制御のために基本姿勢をtempに保存
                tempVectorBufferA[pindex] = pos;
                tempRotationBufferA[pindex] = rot;
            }
        }

        unsafe static void SimulationClearTempBuffer(
            DataChunk chunk,
            // team
            ref TeamManager.TeamData tdata,
            // buffer2
            ref NativeArray<float3> tempVectorBufferA,
            ref NativeArray<float3> tempVectorBufferB,
            ref NativeArray<int> tempCountBuffer,
            ref NativeArray<float> tempFloatBufferA
            //ref NativeArray<quaternion> tempRotationBufferA,
            //ref NativeArray<quaternion> tempRotationBufferB,
            )
        {
            int pindex = tdata.particleChunk.startIndex + chunk.startIndex;
            for (int i = 0; i < chunk.dataLength; i++, pindex++)
            {
                tempVectorBufferA[pindex] = 0;
                tempVectorBufferB[pindex] = 0;
                tempCountBuffer[pindex] = 0;
                tempFloatBufferA[pindex] = 0;
            }
        }
    }
}

