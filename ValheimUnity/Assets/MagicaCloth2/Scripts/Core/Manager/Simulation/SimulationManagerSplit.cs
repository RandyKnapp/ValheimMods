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
    /// Split
    /// セルフコリジョンあり、もしくはプロキシメッシュの頂点数が一定値以上のジョブ
    /// オリジナルと同様にジョブを分割し同期しながら実行する
    /// ただし最適化を行いオリジナルより軽量化している
    /// </summary>
    public partial class SimulationManager
    {
        /// <summary>
        /// プロキシメッシュをスキニングし基本姿勢を求める
        /// </summary>
        [BurstCompile]
        unsafe struct SplitPre_A_Job : IJobParallelFor
        {
            public int workerCount;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float4x4> transformLocalToWorldMatrixArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
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

            // バッチ内のローカルチームインデックスごと
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                //Debug.Log(localIndex);

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];

                //Debug.Log($"{localIndex}, {teamId}");

                ref var tdata = ref *(teamPt + teamId);

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.proxyCommonChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                // ■プロキシメッシュをスキニングし基本姿勢を求める
                VirtualMeshManager.SimulationPreProxyMeshUpdate(
                    //new DataChunk(0, tdata.proxyCommonChunk.dataLength),
                    chunk,
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
            }
        }

        /// <summary>
        /// チームのセンター姿勢の決定と慣性用の移動量計算
        /// </summary>
        [BurstCompile]
        unsafe struct SplitPre_B_Job : IJobParallelFor
        {
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
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
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexBindPoseRotations;


            // inertia
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> fixedArray;

            // バッチ内のローカルチームインデックスごと
            public void Execute(int localIndex)
            {
                //Debug.Log(localIndex);

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafePtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafePtr();
                TeamWindData* windPt = (TeamWindData*)teamWindArray.GetUnsafePtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);
                ref var param = ref *(paramPt + teamId);
                ref var wdata = ref *(windPt + teamId);

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // チームのセンター姿勢の決定と慣性用の移動量計算
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
            }
        }

        /// <summary>
        /// パーティクルの全体慣性およびリセットの適用
        /// コライダーのローカル姿勢を求める、および全体慣性とリセットの適用
        /// </summary>
        [BurstCompile]
        unsafe struct SplitPre_C_Job : IJobParallelFor
        {
            public int workerCount;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformLocalPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformLocalRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformLocalScaleArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> vertexDepths;

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
            [Unity.Collections.ReadOnly]
            public NativeArray<int> colliderMainColliderIndices;

            // バッチ内のローカルチームインデックスごと
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;
                //Debug.Log(localIndex);

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);
                ref var param = ref *(paramPt + teamId);

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // パーティクルの全体慣性およびリセットの適用
                var p_chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                if (p_chunk.IsValid)
                {
                    SimulationPreTeamUpdate(
                        p_chunk,
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
                }

                // ■コライダーのローカル姿勢を求める、および全体慣性とリセットの適用
                if (tdata.colliderCount > 0)
                {
                    var c_chunk = MathUtility.GetWorkerChunk(tdata.colliderChunk.dataLength, workerCount, workerIndex);
                    if (c_chunk.IsValid)
                    {
                        ColliderManager.SimulationPreUpdate(
                            c_chunk,
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
                }
            }
        }

        /// <summary>
        /// チーム更新
        /// コライダーの更新
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_A_Job : IJobParallelFor
        {
            public int updateIndex;
            public float4 simulationPower;
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
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

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag16> colliderFlagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderSizeArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderFramePositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> colliderFrameRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderFrameScales;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderOldFramePositions;
            [Unity.Collections.ReadOnly]
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

            // バッチ内のローカルチームインデックスごと
            public void Execute(int localIndex)
            {
                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafePtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafePtr();
                TeamWindData* windPt = (TeamWindData*)teamWindArray.GetUnsafePtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);
                ref var param = ref *(paramPt + teamId);
                ref var wdata = ref *(windPt + teamId);

                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // ステップ実行
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
            }
        }

        /// <summary>
        /// 速度更新、外力の影響、慣性シフト
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_B_Job : IJobParallelFor
        {
            public int workerCount;
            public int updateIndex;
            public float4 simulationPower;
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamWindData> teamWindArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // wind
            public int windZoneCount;
            [Unity.Collections.ReadOnly]
            public NativeArray<WindManager.WindData> windDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRootIndices;

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> basePosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> baseRotArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> oldRotationArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // buffer
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> stepBasicPositionBuffer;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> stepBasicRotationBuffer;

            // temp
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

            // バッチ内のローカルチームインデックスごと
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafeReadOnlyPtr();
                TeamWindData* windPt = (TeamWindData*)teamWindArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);
                ref var param = ref *(paramPt + teamId);
                ref var wdata = ref *(windPt + teamId);

                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                // 速度更新、外力の影響、慣性シフト
                SimulationStepUpdateParticles(
                    //new DataChunk(0, tdata.particleChunk.dataLength),
                    chunk,
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
            }
        }

        /// <summary>
        /// ベースラインの基準姿勢を計算
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_C_Job : IJobParallelFor
        {
            public int workerCount;
            public int updateIndex;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
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

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotArray;

            // buffer
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> stepBasicPositionBuffer;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> stepBasicRotationBuffer;

            // バッチ内のローカルチームインデックスごと
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

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

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.baseLineChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                // 制約解決のためのステップごとの基準姿勢を計算
                // ベースラインごと
                // アニメーションポーズ使用の有無
                SimulationStepUpdateBaseLinePose(
                    //new DataChunk(0, tdata.baseLineChunk.dataLength),
                    chunk,
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
            }
        }

        /// <summary>
        /// テザー
        /// 距離
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_D_Job : IJobParallelFor
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
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRootIndices;

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // distance
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> distanceIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> distanceDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> distanceDistanceArray;

            // buffer
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> stepBasicPositionBuffer;

            // バッチ内のローカルチームインデックスごと
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);
                ref var param = ref *(paramPt + teamId);

                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                TetherConstraint.SolverConstraint(
                    //new DataChunk(0, tdata.particleChunk.dataLength),
                    chunk,
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
                    //new DataChunk(0, tdata.particleChunk.dataLength),
                    chunk,
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
            }
        }

        /// <summary>
        /// アングル
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_Angle_Job : IJobParallelFor
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

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;
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

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> velocityPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // distance
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> distanceIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> distanceDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> distanceDistanceArray;

            // buffer
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> stepBasicPositionBuffer;
            [Unity.Collections.ReadOnly]
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
            public NativeArray<float> tempFloatBufferA;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> tempRotationBufferA;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> tempRotationBufferB;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
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

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.baseLineChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                AngleConstraint.SolverConstraint(
                    chunk,
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
            }
        }

        /// <summary>
        /// トライアングルベンド
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_Triangle_Job : IJobParallelFor
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

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> frictionArray;

            // triangle bending
            [Unity.Collections.ReadOnly]
            public NativeArray<ulong> bendingTrianglePairArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> bendingRestAngleOrVolumeArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<sbyte> bendingSignOrVolumeArray;

            // buffer2
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> tempVectorBufferA;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> tempCountBuffer;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
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

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.bendingPairChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                TriangleBendingConstraint.SolverConstraint(
                    chunk,
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
            }
        }

        /// <summary>
        /// トライアングルベンド集計
        /// コライダーコリジョンPoint
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_E_Job : IJobParallelFor
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

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> velocityPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> collisionNormalArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag16> colliderFlagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ColliderManager.WorkData> colliderWorkDataArray;

            // buffer2
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> tempVectorBufferA;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<int> tempCountBuffer;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
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

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                TriangleBendingConstraint.SumConstraint(
                    //new DataChunk(0, tdata.particleChunk.dataLength),
                    chunk,
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
                if (tdata.UseColliderCount > 0 && param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Point)
                {
                    // Pointコリジョン
                    ColliderCollisionConstraint.SolverPointConstraint(
                        //new DataChunk(0, tdata.particleChunk.dataLength),
                        chunk,
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
            }
        }

        /// <summary>
        /// コライダーコリジョンEdge
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_Edge_Job : IJobParallelFor
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

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<int2> edges;

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<ExBitFlag16> colliderFlagArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ColliderManager.WorkData> colliderWorkDataArray;

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

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
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

                // コライダーコリジョン
                if (tdata.UseColliderCount > 0 && param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge)
                {
                    // 範囲
                    var chunk = MathUtility.GetWorkerChunk(tdata.proxyEdgeChunk.dataLength, workerCount, workerIndex);
                    if (chunk.IsValid == false)
                        return;

                    // Edgeコリジョン
                    ColliderCollisionConstraint.SolverEdgeConstraint(
                        //new DataChunk(0, tdata.proxyEdgeChunk.dataLength),
                        chunk,
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
                }
            }
        }

        /// <summary>
        /// ■セルフコリジョンあり
        /// コライダーコリジョンEdge集計
        /// 距離
        /// モーション
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_F_Self_Job : IJobParallelFor
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

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> velocityPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float> frictionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> collisionNormalArray;

            // distance
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> distanceIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> distanceDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> distanceDistanceArray;

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

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
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

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid == false)
                    return;

                // コライダーコリジョン
                if (tdata.UseColliderCount > 0 && param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge)
                {
                    // Edgeコリジョン集計
                    ColliderCollisionConstraint.SumEdgeConstraint(
                        //new DataChunk(0, tdata.particleChunk.dataLength),
                        chunk,
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
                    //new DataChunk(0, tdata.particleChunk.dataLength),
                    chunk,
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
                    //new DataChunk(0, tdata.particleChunk.dataLength),
                    chunk,
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
            }
        }

        /// <summary>
        /// ■セルフコリジョンあり
        /// 座標確定
        /// コライダーの後更新
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_G_Self_Job : IJobParallelFor
        {
            public int workerCount;
            public int updateIndex;
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> velocityPosArray;
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
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> collisionNormalArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderNowPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> colliderNowRotations;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> colliderOldPositions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> colliderOldRotations;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);
                ref var param = ref *(paramPt + teamId);

                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid)
                {
                    // 座標確定
                    SimulationStepPostTeam(
                        chunk,
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
                }

                // コライダーの後更新
                var colChunk = MathUtility.GetWorkerChunk(tdata.colliderChunk.dataLength, workerCount, workerIndex);
                if (colChunk.IsValid)
                {
                    ColliderManager.SimulationEndStep(
                        colChunk,
                        // team
                        ref tdata,
                        // collider
                        ref colliderNowPositions,
                        ref colliderNowRotations,
                        ref colliderOldPositions,
                        ref colliderOldRotations
                        );
                }
            }
        }

        /// <summary>
        /// ■セルフコリジョンなし
        /// コライダーコリジョンEdge集計
        /// 距離
        /// モーション
        /// 座標確定
        /// コライダーの後更新
        /// </summary>
        [BurstCompile]
        unsafe struct SplitStep_FG_NoSelf_Job : IJobParallelFor
        {
            public int workerCount;
            public int updateIndex;
            public float4 simulationPower;
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> depthArray;

            // particle
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> nextPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> oldPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> basePosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> baseRotArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> velocityPosArray;
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
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderNowPositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> colliderNowRotations;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> colliderOldPositions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> colliderOldRotations;

            // distance
            [Unity.Collections.ReadOnly]
            public NativeArray<uint> distanceIndexArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ushort> distanceDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float> distanceDistanceArray;

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

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();
                ClothParameters* paramPt = (ClothParameters*)parameterArray.GetUnsafeReadOnlyPtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);
                ref var param = ref *(paramPt + teamId);

                if (updateIndex >= tdata.updateCount)
                    return;
                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid)
                {
                    // コライダーコリジョン
                    if (tdata.UseColliderCount > 0 && param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge)
                    {
                        // Edgeコリジョン集計
                        ColliderCollisionConstraint.SumEdgeConstraint(
                            //new DataChunk(0, tdata.particleChunk.dataLength),
                            chunk,
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
                        //new DataChunk(0, tdata.particleChunk.dataLength),
                        chunk,
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
                        //new DataChunk(0, tdata.particleChunk.dataLength),
                        chunk,
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
                        chunk,
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
                }

                // コライダーの後更新
                var colChunk = MathUtility.GetWorkerChunk(tdata.colliderChunk.dataLength, workerCount, workerIndex);
                if (colChunk.IsValid)
                {
                    ColliderManager.SimulationEndStep(
                        colChunk,
                        // team
                        ref tdata,
                        // collider
                        ref colliderNowPositions,
                        ref colliderNowRotations,
                        ref colliderOldPositions,
                        ref colliderOldRotations
                        );
                }
            }
        }

        /// <summary>
        /// 表示位置の計算
        /// </summary>
        [BurstCompile]
        unsafe struct SplitPost_DisplayPos_Job : IJobParallelFor
        {
            public int workerCount;
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexRootIndices;

            // particle
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> oldPosArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> oldPositionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> oldRotationArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> dispPosArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> realVelocityArray;

            // temp
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> tempVectorBufferA;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> tempRotationBufferA;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // ■表示位置の決定
                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.particleChunk.dataLength, workerCount, workerIndex);
                //Debug.Log($"localIndex:{localIndex}, workerIndex:{workerIndex}, workerCount:{workerCount}, chun.s:{chunk.startIndex}, chun.l:{chunk.dataLength}");
                if (chunk.IsValid)
                {
                    SimulationCalcDisplayPosition(
                        chunk,
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
                }
            }
        }

        /// <summary>
        /// クロスシミュレーションの結果をProxyMeshへ反映させる
        /// ラインがある場合はベースラインごとに姿勢を整える
        /// </summary>
        [BurstCompile]
        unsafe struct SplitPost_CalcProxy_Job : IJobParallelFor
        {
            public int workerCount;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> rotations;
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

            // temp
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> tempVectorBufferA;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> tempRotationBufferB;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
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

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // ■クロスシミュレーション後の頂点姿勢計算
                // ラインがある場合はベースラインごとに姿勢を整える
                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.baseLineChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid)
                {
                    VirtualMeshManager.SimulationPostProxyMeshUpdateLine(
                        chunk,
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
                        ref tempRotationBufferB
                        );
                }
            }
        }

        /// <summary>
        /// トライアングルの法線接線を求める
        /// </summary>
        [BurstCompile]
        unsafe struct SplitPost_CalcProxyTriangle_Job : IJobParallelFor
        {
            public int workerCount;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
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

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // ■クロスシミュレーション後の頂点姿勢計算
                // トライアングルの法線接線を求める
                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.proxyTriangleChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid)
                {
                    VirtualMeshManager.SimulationPostProxyMeshUpdateTriangle(
                        chunk,
                        // team
                        ref tdata,
                        // vmesh
                        ref positions,
                        ref triangles,
                        ref triangleNormals,
                        ref triangleTangents,
                        ref uvs
                        );
                }
            }
        }

        /// <summary>
        /// トライアングルの法線接線から頂点の姿勢を求める
        /// BoneClothの場合は頂点姿勢から連動するトランスフォームのワールド姿勢を計算する
        /// </summary>
        [BurstCompile]
        unsafe struct SplitPost_SumProxyTriangleAndTransform_Job : IJobParallelFor
        {
            public int workerCount;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [Unity.Collections.ReadOnly]
            public NativeArray<TeamManager.TeamData> teamDataArray;

            // transform
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> transformPositionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> transformRotationArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> positions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> rotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleNormals;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> triangleTangents;
            [Unity.Collections.ReadOnly]
            public NativeArray<FixedList32Bytes<uint>> vertexToTriangles;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> normalAdjustmentRotations;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> vertexToTransformRotations;

            // バッチ内のローカルチームインデックスごと
            // ワーカー分割
            public void Execute(int index)
            {
                // チームIDとワーカーID
                int localIndex = index / workerCount;
                int workerIndex = index % workerCount;

                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafeReadOnlyPtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // 範囲
                var chunk = MathUtility.GetWorkerChunk(tdata.proxyCommonChunk.dataLength, workerCount, workerIndex);
                if (chunk.IsValid)
                {
                    // ■クロスシミュレーション後の頂点姿勢計算
                    // トライアングルの法線接線から頂点の姿勢を求める
                    VirtualMeshManager.SimulationPostProxyMeshUpdateTriangleSum(
                        chunk,
                        // team
                        ref tdata,
                        // vmesh
                        ref rotations,
                        ref triangleNormals,
                        ref triangleTangents,
                        ref vertexToTriangles,
                        ref normalAdjustmentRotations
                        );
                    // BoneClothの場合は頂点姿勢から連動するトランスフォームのワールド姿勢を計算する
                    VirtualMeshManager.SimulationPostProxyMeshUpdateWorldTransform(
                        chunk,
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
                }
            }
        }

        /// <summary>
        /// チーム更新後処理
        /// BoneClothの場合はTransformのローカル姿勢を計算する
        /// コライダー更新後処理
        /// </summary>
        [BurstCompile]
        unsafe struct SplitPost_TeamCollider_Job : IJobParallelFor
        {
            public float simulationDeltaTime;

            // team
            [Unity.Collections.ReadOnly]
            public NativeList<int> batchSelfTeamList;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<TeamManager.TeamData> teamDataArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            // collider
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> colliderFramePositions;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> colliderFrameRotations;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> colliderOldFramePositions;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> colliderOldFrameRotations;

            // transform
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformPositionArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<quaternion> transformRotationArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<float3> transformScaleArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> transformLocalPositionArray;
            [NativeDisableParallelForRestriction]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<quaternion> transformLocalRotationArray;

            // vmesh
            [Unity.Collections.ReadOnly]
            public NativeArray<VertexAttribute> attributes;
            [Unity.Collections.ReadOnly]
            public NativeArray<int> vertexParentIndices;

            // バッチ内のローカルチームインデックスごと
            public void Execute(int localIndex)
            {
                // 各ベースポインタ
                TeamManager.TeamData* teamPt = (TeamManager.TeamData*)teamDataArray.GetUnsafePtr();
                InertiaConstraint.CenterData* centerPt = (InertiaConstraint.CenterData*)centerDataArray.GetUnsafePtr();

                int teamId = batchSelfTeamList[localIndex];
                ref var tdata = ref *(teamPt + teamId);
                ref var cdata = ref *(centerPt + teamId);

                if (tdata.IsProcess == false)
                    return;
                if (tdata.ParticleCount == 0)
                    return;

                // BoneClothの場合はTransformのローカル姿勢を計算する
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
    }
}

