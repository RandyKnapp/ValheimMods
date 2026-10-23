// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    public partial class SimulationManager : IManager, IValid
    {
        /// <summary>
        /// チームID
        /// </summary>
        public ExNativeArray<short> teamIdArray;

        /// <summary>
        /// 現在のシミュレーション座標
        /// </summary>
        public ExNativeArray<float3> nextPosArray;

        /// <summary>
        /// １つ前のシミュレーション座標
        /// </summary>
        public ExNativeArray<float3> oldPosArray;

        /// <summary>
        /// １つ前のシミュレーション回転(todo:現在未使用）
        /// </summary>
        public ExNativeArray<quaternion> oldRotArray;

        /// <summary>
        /// 現在のアニメーション姿勢座標
        /// カスタムスキニングの結果も反映されている
        /// </summary>
        public ExNativeArray<float3> basePosArray;

        /// <summary>
        /// 現在のアニメーション姿勢回転
        /// カスタムスキニングの結果も反映されている
        /// </summary>
        public ExNativeArray<quaternion> baseRotArray;

        /// <summary>
        /// １つ前の原点座標
        /// </summary>
        public ExNativeArray<float3> oldPositionArray;

        /// <summary>
        /// １つ前の原点回転
        /// </summary>
        public ExNativeArray<quaternion> oldRotationArray;

        /// <summary>
        /// 速度計算用座標
        /// </summary>
        public ExNativeArray<float3> velocityPosArray;

        /// <summary>
        /// 表示座標
        /// </summary>
        public ExNativeArray<float3> dispPosArray;

        /// <summary>
        /// 速度
        /// </summary>
        public ExNativeArray<float3> velocityArray;

        /// <summary>
        /// 実速度
        /// </summary>
        public ExNativeArray<float3> realVelocityArray;

        /// <summary>
        /// 摩擦(0.0 ~ 1.0)
        /// </summary>
        public ExNativeArray<float> frictionArray;

        /// <summary>
        /// 静止摩擦係数
        /// </summary>
        public ExNativeArray<float> staticFrictionArray;

        /// <summary>
        /// 接触コライダーの衝突法線
        /// </summary>
        public ExNativeArray<float3> collisionNormalArray;

        public int ParticleCount => nextPosArray?.Count ?? 0;

        //=========================================================================================
        /// <summary>
        /// 制約
        /// </summary>
        public DistanceConstraint distanceConstraint;
        public TriangleBendingConstraint bendingConstraint;
        public TetherConstraint tetherConstraint;
        public AngleConstraint angleConstraint;
        public InertiaConstraint inertiaConstraint;
        public ColliderCollisionConstraint colliderCollisionConstraint;
        public MotionConstraint motionConstraint;
        public SelfCollisionConstraint selfCollisionConstraint;

        //=========================================================================================
        /// <summary>
        /// ステップごとのシミュレーションの基準となる姿勢座標
        /// 初期姿勢とアニメーション姿勢をAnimatinBlendRatioで補間したもの
        /// </summary>
        public NativeArray<float3> stepBasicPositionBuffer;

        /// <summary>
        /// ステップごとのシミュレーションの基準となる姿勢回転
        /// 初期姿勢とアニメーション姿勢をAnimatinBlendRatioで補間したもの
        /// </summary>
        public NativeArray<quaternion> stepBasicRotationBuffer;

        /// <summary>
        /// 作業用バッファ
        /// </summary>
        internal NativeArray<float3> tempVectorBufferA;
        internal NativeArray<float3> tempVectorBufferB;
        internal NativeArray<int> tempCountBuffer;
        internal NativeArray<float> tempFloatBufferA;
        internal NativeArray<quaternion> tempRotationBufferA;
        internal NativeArray<quaternion> tempRotationBufferB;

        /// <summary>
        /// ステップ実行カウンター
        /// </summary>
        internal int SimulationStepCount { get; private set; }

        /// <summary>
        /// 実行環境で利用できるワーカースレッド数
        /// </summary>
        internal int WorkerCount => Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount;

        /// <summary>
        /// 分割ジョブを適用するプロキシメッシュの頂点数
        /// </summary>
        internal int splitProxyMeshVertexCount = Define.System.SplitProxyMeshVertexCount;

        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            teamIdArray?.Dispose();
            nextPosArray?.Dispose();
            oldPosArray?.Dispose();
            oldRotArray?.Dispose();
            basePosArray?.Dispose();
            baseRotArray?.Dispose();
            oldPositionArray?.Dispose();
            oldRotationArray?.Dispose();
            velocityPosArray?.Dispose();
            dispPosArray?.Dispose();
            velocityArray?.Dispose();
            realVelocityArray?.Dispose();
            frictionArray?.Dispose();
            staticFrictionArray?.Dispose();
            collisionNormalArray?.Dispose();

            teamIdArray = null;
            nextPosArray = null;
            oldPosArray = null;
            oldRotArray = null;
            basePosArray = null;
            baseRotArray = null;
            oldPositionArray = null;
            oldRotationArray = null;
            velocityPosArray = null;
            dispPosArray = null;
            velocityArray = null;
            realVelocityArray = null;
            frictionArray = null;
            staticFrictionArray = null;
            collisionNormalArray = null;

            if (stepBasicPositionBuffer.IsCreated)
                stepBasicPositionBuffer.Dispose();
            if (stepBasicRotationBuffer.IsCreated)
                stepBasicRotationBuffer.Dispose();
            if (tempVectorBufferA.IsCreated)
                tempVectorBufferA.Dispose();
            if (tempVectorBufferB.IsCreated)
                tempVectorBufferB.Dispose();
            if (tempCountBuffer.IsCreated)
                tempCountBuffer.Dispose();
            if (tempFloatBufferA.IsCreated)
                tempFloatBufferA.Dispose();
            if (tempRotationBufferA.IsCreated)
                tempRotationBufferA.Dispose();
            if (tempRotationBufferB.IsCreated)
                tempRotationBufferB.Dispose();

            distanceConstraint?.Dispose();
            bendingConstraint?.Dispose();
            tetherConstraint?.Dispose();
            angleConstraint?.Dispose();
            inertiaConstraint?.Dispose();
            colliderCollisionConstraint?.Dispose();
            motionConstraint?.Dispose();
            selfCollisionConstraint?.Dispose();
            distanceConstraint = null;
            bendingConstraint = null;
            tetherConstraint = null;
            angleConstraint = null;
            inertiaConstraint = null;
            colliderCollisionConstraint = null;
            motionConstraint = null;
            selfCollisionConstraint = null;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 0; // 1024?
            teamIdArray = new ExNativeArray<short>(capacity);
            nextPosArray = new ExNativeArray<float3>(capacity);
            oldPosArray = new ExNativeArray<float3>(capacity);
            oldRotArray = new ExNativeArray<quaternion>(capacity);
            basePosArray = new ExNativeArray<float3>(capacity);
            baseRotArray = new ExNativeArray<quaternion>(capacity);
            oldPositionArray = new ExNativeArray<float3>(capacity);
            oldRotationArray = new ExNativeArray<quaternion>(capacity);
            velocityPosArray = new ExNativeArray<float3>(capacity);
            dispPosArray = new ExNativeArray<float3>(capacity);
            velocityArray = new ExNativeArray<float3>(capacity);
            realVelocityArray = new ExNativeArray<float3>(capacity);
            frictionArray = new ExNativeArray<float>(capacity);
            staticFrictionArray = new ExNativeArray<float>(capacity);
            collisionNormalArray = new ExNativeArray<float3>(capacity);

            // 制約
            distanceConstraint = new DistanceConstraint();
            bendingConstraint = new TriangleBendingConstraint();
            tetherConstraint = new TetherConstraint();
            angleConstraint = new AngleConstraint();
            inertiaConstraint = new InertiaConstraint();
            colliderCollisionConstraint = new ColliderCollisionConstraint();
            motionConstraint = new MotionConstraint();
            selfCollisionConstraint = new SelfCollisionConstraint();

            SimulationStepCount = 0;

            isValid = true;

            Develop.DebugLog($"JobWorkerCount:{Unity.Jobs.LowLevel.Unsafe.JobsUtility.JobWorkerCount}");
            //Develop.DebugLog($"MaxJobThreadCount:{Unity.Jobs.LowLevel.Unsafe.JobsUtility.MaxJobThreadCount}");
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// プロキシメッシュをマネージャに登録する
        /// </summary>
        internal void RegisterProxyMesh(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            int teamId = cprocess.TeamId;
            var proxyMesh = cprocess.ProxyMeshContainer.shareVirtualMesh;
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);

            int pcnt = proxyMesh.VertexCount;
            tdata.particleChunk = teamIdArray.AddRange(pcnt, (short)teamId);
            nextPosArray.AddRange(pcnt);
            oldPosArray.AddRange(pcnt);
            oldRotArray.AddRange(pcnt);
            basePosArray.AddRange(pcnt);
            baseRotArray.AddRange(pcnt);
            oldPositionArray.AddRange(pcnt);
            oldRotationArray.AddRange(pcnt);
            velocityPosArray.AddRange(pcnt);
            dispPosArray.AddRange(pcnt);
            velocityArray.AddRange(pcnt);
            realVelocityArray.AddRange(pcnt);
            frictionArray.AddRange(pcnt);
            staticFrictionArray.AddRange(pcnt);
            collisionNormalArray.AddRange(pcnt);
        }

        /// <summary>
        /// 制約データを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        internal void RegisterConstraint(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            int teamId = cprocess.TeamId;

            // 慣性制約データをコピー（すでに領域は確保済みなのでコピーする）
            MagicaManager.Team.centerDataArray[teamId] = cprocess.inertiaConstraintData.centerData;

            // 制約データを登録する
            distanceConstraint.Register(cprocess);
            bendingConstraint.Register(cprocess);
            inertiaConstraint.Register(cprocess);
            selfCollisionConstraint.Register(cprocess);
        }


        /// <summary>
        /// プロキシメッシュをマネージャから解除する
        /// </summary>
        internal void ExitProxyMesh(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            int teamId = cprocess.TeamId;
            ref var tdata = ref MagicaManager.Team.GetTeamDataRef(teamId);
            tdata.flag.SetBits(TeamManager.Flag_Exit, true); // 消滅フラグ

            var c = tdata.particleChunk;
            teamIdArray.RemoveAndFill(c);
            nextPosArray.Remove(c);
            oldPosArray.Remove(c);
            oldRotArray.Remove(c);
            basePosArray.Remove(c);
            baseRotArray.Remove(c);
            oldPositionArray.Remove(c);
            oldRotationArray.Remove(c);
            velocityPosArray.Remove(c);
            dispPosArray.Remove(c);
            velocityArray.Remove(c);
            realVelocityArray.Remove(c);
            frictionArray.Remove(c);
            staticFrictionArray.Remove(c);
            collisionNormalArray.Remove(c);

            tdata.particleChunk.Clear();

            // 制約データを解除する
            distanceConstraint.Exit(cprocess);
            bendingConstraint.Exit(cprocess);
            inertiaConstraint.Exit(cprocess);
            selfCollisionConstraint.Exit(cprocess);
        }

        //=========================================================================================
        /// <summary>
        /// 作業バッファの更新
        /// </summary>
        internal void WorkBufferUpdate()
        {
            int pcnt = ParticleCount;

            // 汎用作業バッファ
            // 拡張時には０クリアされる
            stepBasicPositionBuffer.MC2Resize(pcnt);
            stepBasicRotationBuffer.MC2Resize(pcnt);

            // 汎用バッファ
            // 拡張時には０クリアされる
            tempVectorBufferA.MC2Resize(pcnt);
            tempVectorBufferB.MC2Resize(pcnt);
            tempCountBuffer.MC2Resize(pcnt);
            tempFloatBufferA.MC2Resize(pcnt);
            tempRotationBufferA.MC2Resize(pcnt);
            tempRotationBufferB.MC2Resize(pcnt);

            // 制約
            selfCollisionConstraint.WorkBufferUpdate();
        }

        //=========================================================================================
        // Simulation
        //=========================================================================================
        /// <summary>
        /// シミュレーションメインスケジュール
        /// </summary>
        /// <param name="jobHandle"></param>
        /// <returns></returns>
        internal JobHandle ClothSimulationSchedule(JobHandle jobHandle)
        {
            var tm = MagicaManager.Team;
            var bm = MagicaManager.Bone;
            var vm = MagicaManager.VMesh;
            var wm = MagicaManager.Wind;
            var cm = MagicaManager.Collider;
            var tim = MagicaManager.Time;

            int normalClothTeamCount = tm.batchNormalClothTeamList.Length;
            int splitClothTeamCount = tm.batchSplitClothTeamList.Length;
            bool useNormalClothJob = normalClothTeamCount > 0;
            bool useSplitClothJob = splitClothTeamCount > 0;

            if (useNormalClothJob == false && useSplitClothJob == false)
                return jobHandle;

            // 最大更新回数
            int maxUpdateCount = tm.TeamMaxUpdateCount;
            //Debug.Log($"TeamMaxUpdateCount:{tm.TeamMaxUpdateCount}, UseSelfPointCollision:{tm.UseSelfPointCollision}, UseSelfEdgeCollision:{tm.UseSelfEdgeCollision}");

            // 利用できるワーカースレッド数
            int workerCount = math.max(WorkerCount, 1);
            workerCount *= 5; // 更に分割：テスト結果より
            //Debug.Log($"workerCount:{workerCount}");

            // ジョブ連結用
            JobHandle normalClothJobHandle = new JobHandle();
            JobHandle splitClothJobHandle = new JobHandle();
            JobHandle selfIntersectJobHandle = new JobHandle();
            JobHandle solverIntersectJobHandle = new JobHandle();

            // ■分割シミュレーションジョブ
            // セルフコリジョンあり、もしくはプロキシメッシュの頂点数が一定値以上のジョブ
            // オリジナルと同様にジョブを分割し同期しながら実行する
            // ただし最適化を行いオリジナルより軽量化している
            if (useSplitClothJob)
            {
                // コンタクトキューとリスト
                selfCollisionConstraint.contactQueue.Clear();
                selfCollisionConstraint.contactList.Clear();
                selfCollisionConstraint.intersectQueue.Clear();
                selfCollisionConstraint.intersectList.Clear();

                // 分割ジョブ内での各種コリジョンの有無
                bool useEdgeCollision = tm.teamStatus.Value.z > 0;
                bool useSelfCollision = tm.teamStatus.Value.w > 0;

                // セルフコリジョンのインターセクト解決
                bool useIntersect = selfCollisionConstraint.IntersectCount > 0;

                // プロキシメッシュをスキニングし基本姿勢を求める
                var splitPre_A_Job = new SplitPre_A_Job()
                {
                    workerCount = workerCount,

                    // team
                    batchSelfTeamList = tm.batchSplitClothTeamList,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    // transform
                    transformLocalToWorldMatrixArray = bm.localToWorldMatrixArray.GetNativeArray(),

                    // vmesh
                    attributes = vm.attributes.GetNativeArray(),
                    localPositions = vm.localPositions.GetNativeArray(),
                    localNormals = vm.localNormals.GetNativeArray(),
                    localTangents = vm.localTangents.GetNativeArray(),
                    boneWeights = vm.boneWeights.GetNativeArray(),
                    skinBoneTransformIndices = vm.skinBoneTransformIndices.GetNativeArray(),
                    skinBoneBindPoses = vm.skinBoneBindPoses.GetNativeArray(),
                    positions = vm.positions.GetNativeArray(),
                    rotations = vm.rotations.GetNativeArray(),
                };
                splitClothJobHandle = splitPre_A_Job.Schedule(splitClothTeamCount * workerCount, 1, jobHandle);

                // チームのセンター姿勢の決定と慣性用の移動量計算
                var splitPre_B_Job = new SplitPre_B_Job()
                {
                    simulationDeltaTime = MagicaManager.Time.SimulationDeltaTime,

                    // team
                    batchSelfTeamList = tm.batchSplitClothTeamList,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    centerDataArray = tm.centerDataArray.GetNativeArray(),
                    teamWindArray = tm.teamWindArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    // wind
                    windZoneCount = wm.WindCount,
                    windDataArray = wm.windDataArray.GetNativeArray(),

                    // transform
                    transformPositionArray = bm.positionArray.GetNativeArray(),
                    transformRotationArray = bm.rotationArray.GetNativeArray(),
                    transformScaleArray = bm.scaleArray.GetNativeArray(),

                    // vmesh
                    positions = vm.positions.GetNativeArray(),
                    rotations = vm.rotations.GetNativeArray(),
                    vertexBindPoseRotations = vm.vertexBindPoseRotations.GetNativeArray(),

                    // inertia
                    fixedArray = inertiaConstraint.fixedArray.GetNativeArray(),
                };
                splitClothJobHandle = splitPre_B_Job.Schedule(splitClothTeamCount, 1, splitClothJobHandle);

                // パーティクルの全体慣性およびリセットの適用
                // コライダーのローカル姿勢を求める、および全体慣性とリセットの適用
                var splitPre_C_Job = new SplitPre_C_Job()
                {
                    workerCount = workerCount,

                    // team
                    batchSelfTeamList = tm.batchSplitClothTeamList,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    centerDataArray = tm.centerDataArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    // transform
                    transformPositionArray = bm.positionArray.GetNativeArray(),
                    transformRotationArray = bm.rotationArray.GetNativeArray(),
                    transformScaleArray = bm.scaleArray.GetNativeArray(),
                    transformLocalPositionArray = bm.localPositionArray.GetNativeArray(),
                    transformLocalRotationArray = bm.localRotationArray.GetNativeArray(),
                    transformLocalScaleArray = bm.localScaleArray.GetNativeArray(),

                    // vmesh
                    positions = vm.positions.GetNativeArray(),
                    rotations = vm.rotations.GetNativeArray(),
                    vertexDepths = vm.vertexDepths.GetNativeArray(),

                    // particle
                    nextPosArray = nextPosArray.GetNativeArray(),
                    oldPosArray = oldPosArray.GetNativeArray(),
                    oldRotArray = oldRotArray.GetNativeArray(),
                    basePosArray = basePosArray.GetNativeArray(),
                    baseRotArray = baseRotArray.GetNativeArray(),
                    oldPositionArray = oldPositionArray.GetNativeArray(),
                    oldRotationArray = oldRotationArray.GetNativeArray(),
                    velocityPosArray = velocityPosArray.GetNativeArray(),
                    dispPosArray = dispPosArray.GetNativeArray(),
                    velocityArray = velocityArray.GetNativeArray(),
                    realVelocityArray = realVelocityArray.GetNativeArray(),
                    frictionArray = frictionArray.GetNativeArray(),
                    staticFrictionArray = staticFrictionArray.GetNativeArray(),
                    collisionNormalArray = collisionNormalArray.GetNativeArray(),

                    // collider
                    colliderFlagArray = cm.flagArray.GetNativeArray(),
                    colliderCenterArray = cm.centerArray.GetNativeArray(),
                    colliderFramePositions = cm.framePositions.GetNativeArray(),
                    colliderFrameRotations = cm.frameRotations.GetNativeArray(),
                    colliderFrameScales = cm.frameScales.GetNativeArray(),
                    colliderOldFramePositions = cm.oldFramePositions.GetNativeArray(),
                    colliderOldFrameRotations = cm.oldFrameRotations.GetNativeArray(),
                    colliderNowPositions = cm.nowPositions.GetNativeArray(),
                    colliderNowRotations = cm.nowRotations.GetNativeArray(),
                    colliderOldPositions = cm.oldPositions.GetNativeArray(),
                    colliderOldRotations = cm.oldRotations.GetNativeArray(),
                    colliderMainColliderIndices = cm.mainColliderIndices.GetNativeArray(),
                };
                splitClothJobHandle = splitPre_C_Job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                // セルフコリジョンのインターセクトバッファの生成（開始)
                bool useIntersectJob = false;
                if (useSelfCollision && maxUpdateCount > 0 && useIntersect)
                {
                    // インターセクトバッファの生成
                    // インターセクトパーティクルフラグクリア
                    var selfDetectionIntersect_job = new SelfCollisionConstraint.SelfDetectionIntersectJob()
                    {
                        workerCount = workerCount,
                        // div
                        frameIndex = Time.frameCount % Define.System.SelfCollisionIntersectDiv,
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        // self collision
                        primitiveArrayB = selfCollisionConstraint.primitiveArrayB.GetNativeArray(),
                        uniformGridStartCountBuffer = selfCollisionConstraint.uniformGridStartCountBuffer.GetNativeArray(),
                        // buffer
                        intersectQueue = selfCollisionConstraint.intersectQueue.AsParallelWriter(),
                    };
                    selfIntersectJobHandle = selfDetectionIntersect_job.Schedule(splitClothTeamCount * workerCount, 1, jobHandle);

                    // インターセクトバッファをリストに変換
                    var selfConvertIntersectList_job = new SelfCollisionConstraint.SelfConvertIntersectListJob()
                    {
                        intersectQueue = selfCollisionConstraint.intersectQueue,
                        intersectList = selfCollisionConstraint.intersectList,
                    };
                    selfIntersectJobHandle = selfConvertIntersectList_job.Schedule(selfIntersectJobHandle);
                    useIntersectJob = true;
                }

                // ■ステップループ
                for (int updateIndex = 0; updateIndex < maxUpdateCount; updateIndex++)
                {
                    bool isFirstStep = updateIndex == 0;

                    // チーム更新
                    // コライダーの更新
                    var splitStep_A_job = new SplitStep_A_Job()
                    {
                        updateIndex = updateIndex,
                        simulationPower = tim.SimulationPower,
                        simulationDeltaTime = tim.SimulationDeltaTime,
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        centerDataArray = tm.centerDataArray.GetNativeArray(),
                        teamWindArray = tm.teamWindArray.GetNativeArray(),
                        parameterArray = tm.parameterArray.GetNativeArray(),
                        // collider
                        colliderFlagArray = cm.flagArray.GetNativeArray(),
                        colliderSizeArray = cm.sizeArray.GetNativeArray(),
                        colliderFramePositions = cm.framePositions.GetNativeArray(),
                        colliderFrameRotations = cm.frameRotations.GetNativeArray(),
                        colliderFrameScales = cm.frameScales.GetNativeArray(),
                        colliderOldFramePositions = cm.oldFramePositions.GetNativeArray(),
                        colliderOldFrameRotations = cm.oldFrameRotations.GetNativeArray(),
                        colliderNowPositions = cm.nowPositions.GetNativeArray(),
                        colliderNowRotations = cm.nowRotations.GetNativeArray(),
                        colliderOldPositions = cm.oldPositions.GetNativeArray(),
                        colliderOldRotations = cm.oldRotations.GetNativeArray(),
                        colliderWorkDataArray = cm.workDataArray.GetNativeArray(),
                    };
                    splitClothJobHandle = splitStep_A_job.Schedule(splitClothTeamCount, 1, splitClothJobHandle);

                    // 速度更新、外力の影響、慣性シフト
                    var splitStep_B_job = new SplitStep_B_Job()
                    {
                        workerCount = workerCount,
                        updateIndex = updateIndex,
                        simulationPower = tim.SimulationPower,
                        simulationDeltaTime = tim.SimulationDeltaTime,
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        centerDataArray = tm.centerDataArray.GetNativeArray(),
                        teamWindArray = tm.teamWindArray.GetNativeArray(),
                        parameterArray = tm.parameterArray.GetNativeArray(),
                        // wind
                        windZoneCount = wm.WindCount,
                        windDataArray = wm.windDataArray.GetNativeArray(),
                        // vmesh
                        attributes = vm.attributes.GetNativeArray(),
                        depthArray = vm.vertexDepths.GetNativeArray(),
                        positions = vm.positions.GetNativeArray(),
                        rotations = vm.rotations.GetNativeArray(),
                        vertexRootIndices = vm.vertexRootIndices.GetNativeArray(),
                        // particle
                        nextPosArray = nextPosArray.GetNativeArray(),
                        oldPosArray = oldPosArray.GetNativeArray(),
                        basePosArray = basePosArray.GetNativeArray(),
                        baseRotArray = baseRotArray.GetNativeArray(),
                        oldPositionArray = oldPositionArray.GetNativeArray(),
                        oldRotationArray = oldRotationArray.GetNativeArray(),
                        velocityPosArray = velocityPosArray.GetNativeArray(),
                        velocityArray = velocityArray.GetNativeArray(),
                        frictionArray = frictionArray.GetNativeArray(),
                        // buffer
                        stepBasicPositionBuffer = stepBasicPositionBuffer,
                        stepBasicRotationBuffer = stepBasicRotationBuffer,
                        // temp
                        tempVectorBufferA = tempVectorBufferA,
                        tempVectorBufferB = tempVectorBufferB,
                        tempCountBuffer = tempCountBuffer,
                        tempFloatBufferA = tempFloatBufferA,
                    };
                    splitClothJobHandle = splitStep_B_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                    // ベースラインの基準姿勢を計算
                    var splitStep_C_job = new SplitStep_C_Job()
                    {
                        workerCount = workerCount,
                        updateIndex = updateIndex,
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        // vmesh
                        attributes = vm.attributes.GetNativeArray(),
                        vertexRootIndices = vm.vertexRootIndices.GetNativeArray(),
                        vertexParentIndices = vm.vertexParentIndices.GetNativeArray(),
                        baseLineStartDataIndices = vm.baseLineStartDataIndices.GetNativeArray(),
                        baseLineDataCounts = vm.baseLineDataCounts.GetNativeArray(),
                        baseLineData = vm.baseLineData.GetNativeArray(),
                        vertexLocalPositions = vm.vertexLocalPositions.GetNativeArray(),
                        vertexLocalRotations = vm.vertexLocalRotations.GetNativeArray(),
                        // particle
                        basePosArray = basePosArray.GetNativeArray(),
                        baseRotArray = baseRotArray.GetNativeArray(),
                        // buffer
                        stepBasicPositionBuffer = stepBasicPositionBuffer,
                        stepBasicRotationBuffer = stepBasicRotationBuffer,
                    };
                    splitClothJobHandle = splitStep_C_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                    // テザー
                    // 距離
                    var splitStep_D_job = new SplitStep_D_Job()
                    {
                        workerCount = workerCount,
                        updateIndex = updateIndex,
                        simulationPower = tim.SimulationPower,
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        centerDataArray = tm.centerDataArray.GetNativeArray(),
                        parameterArray = tm.parameterArray.GetNativeArray(),
                        // vmesh
                        attributes = vm.attributes.GetNativeArray(),
                        depthArray = vm.vertexDepths.GetNativeArray(),
                        vertexRootIndices = vm.vertexRootIndices.GetNativeArray(),
                        // particle
                        nextPosArray = nextPosArray.GetNativeArray(),
                        basePosArray = basePosArray.GetNativeArray(),
                        velocityPosArray = velocityPosArray.GetNativeArray(),
                        frictionArray = frictionArray.GetNativeArray(),
                        // distance
                        distanceIndexArray = distanceConstraint.indexArray.GetNativeArray(),
                        distanceDataArray = distanceConstraint.dataArray.GetNativeArray(),
                        distanceDistanceArray = distanceConstraint.distanceArray.GetNativeArray(),
                        // buffer
                        stepBasicPositionBuffer = stepBasicPositionBuffer,
                    };
                    splitClothJobHandle = splitStep_D_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                    // アングル
                    var splitStep_Angle_job = new SplitStep_Angle_Job()
                    {
                        workerCount = workerCount,
                        updateIndex = updateIndex,
                        simulationPower = tim.SimulationPower,
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        parameterArray = tm.parameterArray.GetNativeArray(),
                        // vmesh
                        attributes = vm.attributes.GetNativeArray(),
                        depthArray = vm.vertexDepths.GetNativeArray(),
                        vertexRootIndices = vm.vertexRootIndices.GetNativeArray(),
                        vertexParentIndices = vm.vertexParentIndices.GetNativeArray(),
                        baseLineStartDataIndices = vm.baseLineStartDataIndices.GetNativeArray(),
                        baseLineDataCounts = vm.baseLineDataCounts.GetNativeArray(),
                        baseLineData = vm.baseLineData.GetNativeArray(),
                        // particle
                        nextPosArray = nextPosArray.GetNativeArray(),
                        //basePosArray = basePosArray.GetNativeArray(),
                        velocityPosArray = velocityPosArray.GetNativeArray(),
                        frictionArray = frictionArray.GetNativeArray(),
                        // distance
                        distanceIndexArray = distanceConstraint.indexArray.GetNativeArray(),
                        distanceDataArray = distanceConstraint.dataArray.GetNativeArray(),
                        distanceDistanceArray = distanceConstraint.distanceArray.GetNativeArray(),
                        // buffer
                        stepBasicPositionBuffer = stepBasicPositionBuffer,
                        stepBasicRotationBuffer = stepBasicRotationBuffer,
                        // buffer2
                        tempVectorBufferA = tempVectorBufferA,
                        tempVectorBufferB = tempVectorBufferB,
                        tempFloatBufferA = tempFloatBufferA,
                        tempRotationBufferA = tempRotationBufferA,
                        tempRotationBufferB = tempRotationBufferB,
                    };
                    splitClothJobHandle = splitStep_Angle_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                    // トライアングルベンド
                    var splitStep_Triangle_job = new SplitStep_Triangle_Job()
                    {
                        workerCount = workerCount,
                        updateIndex = updateIndex,
                        simulationPower = tim.SimulationPower,
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        parameterArray = tm.parameterArray.GetNativeArray(),
                        // vmesh
                        attributes = vm.attributes.GetNativeArray(),
                        depthArray = vm.vertexDepths.GetNativeArray(),
                        // particle
                        nextPosArray = nextPosArray.GetNativeArray(),
                        frictionArray = frictionArray.GetNativeArray(),
                        // triangle bending
                        bendingTrianglePairArray = bendingConstraint.trianglePairArray.GetNativeArray(),
                        bendingRestAngleOrVolumeArray = bendingConstraint.restAngleOrVolumeArray.GetNativeArray(),
                        bendingSignOrVolumeArray = bendingConstraint.signOrVolumeArray.GetNativeArray(),
                        // buffer2
                        tempVectorBufferA = tempVectorBufferA,
                        tempCountBuffer = tempCountBuffer,
                    };
                    splitClothJobHandle = splitStep_Triangle_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                    // トライアングルベンド集計
                    // コライダーコリジョンPoint
                    var splitStep_E_job = new SplitStep_E_Job()
                    {
                        workerCount = workerCount,
                        updateIndex = updateIndex,
                        simulationPower = tim.SimulationPower,
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        parameterArray = tm.parameterArray.GetNativeArray(),
                        // vmesh
                        attributes = vm.attributes.GetNativeArray(),
                        depthArray = vm.vertexDepths.GetNativeArray(),
                        // particle
                        nextPosArray = nextPosArray.GetNativeArray(),
                        basePosArray = basePosArray.GetNativeArray(),
                        velocityPosArray = velocityPosArray.GetNativeArray(),
                        frictionArray = frictionArray.GetNativeArray(),
                        collisionNormalArray = collisionNormalArray.GetNativeArray(),
                        // collider
                        colliderFlagArray = cm.flagArray.GetNativeArray(),
                        colliderWorkDataArray = cm.workDataArray.GetNativeArray(),
                        // buffer2
                        tempVectorBufferA = tempVectorBufferA,
                        tempCountBuffer = tempCountBuffer,
                    };
                    splitClothJobHandle = splitStep_E_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                    // コライダーコリジョンEdge
                    if (useEdgeCollision)
                    {
                        var splitStep_Edge_job = new SplitStep_Edge_Job()
                        {
                            workerCount = workerCount,
                            updateIndex = updateIndex,
                            simulationPower = tim.SimulationPower,
                            // team
                            batchSelfTeamList = tm.batchSplitClothTeamList,
                            teamDataArray = tm.teamDataArray.GetNativeArray(),
                            parameterArray = tm.parameterArray.GetNativeArray(),
                            // vmesh
                            attributes = vm.attributes.GetNativeArray(),
                            depthArray = vm.vertexDepths.GetNativeArray(),
                            edges = vm.edges.GetNativeArray(),
                            // particle
                            nextPosArray = nextPosArray.GetNativeArray(),
                            // collider
                            colliderFlagArray = cm.flagArray.GetNativeArray(),
                            colliderWorkDataArray = cm.workDataArray.GetNativeArray(),
                            // buffer2
                            tempVectorBufferA = tempVectorBufferA,
                            tempVectorBufferB = tempVectorBufferB,
                            tempCountBuffer = tempCountBuffer,
                            tempFloatBufferA = tempFloatBufferA,
                        };
                        splitClothJobHandle = splitStep_Edge_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);
                    }

                    if (useSelfCollision)
                    {
                        // ■セルフコリジョンあり
                        // コライダーコリジョンEdge集計
                        // 距離
                        // モーション
                        var splitStep_F_Self_job = new SplitStep_F_Self_Job()
                        {
                            workerCount = workerCount,
                            updateIndex = updateIndex,
                            simulationPower = tim.SimulationPower,
                            // team
                            batchSelfTeamList = tm.batchSplitClothTeamList,
                            teamDataArray = tm.teamDataArray.GetNativeArray(),
                            parameterArray = tm.parameterArray.GetNativeArray(),
                            // vmesh
                            attributes = vm.attributes.GetNativeArray(),
                            depthArray = vm.vertexDepths.GetNativeArray(),
                            // particle
                            nextPosArray = nextPosArray.GetNativeArray(),
                            basePosArray = basePosArray.GetNativeArray(),
                            baseRotArray = baseRotArray.GetNativeArray(),
                            velocityPosArray = velocityPosArray.GetNativeArray(),
                            frictionArray = frictionArray.GetNativeArray(),
                            collisionNormalArray = collisionNormalArray.GetNativeArray(),
                            // distance
                            distanceIndexArray = distanceConstraint.indexArray.GetNativeArray(),
                            distanceDataArray = distanceConstraint.dataArray.GetNativeArray(),
                            distanceDistanceArray = distanceConstraint.distanceArray.GetNativeArray(),
                            // buffer2
                            tempVectorBufferA = tempVectorBufferA,
                            tempVectorBufferB = tempVectorBufferB,
                            tempCountBuffer = tempCountBuffer,
                            tempFloatBufferA = tempFloatBufferA,
                        };
                        splitClothJobHandle = splitStep_F_Self_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                        // セルフコリジョンのインターセクトバッファ更新ジョブを合流させる
                        if (isFirstStep && useIntersectJob)
                        {
                            splitClothJobHandle = JobHandle.CombineDependencies(selfIntersectJobHandle, splitClothJobHandle);
                        }

                        // セルフコリジョン
                        if (isFirstStep)
                        {
                            // ■初回ステップ
                            // （チームごとPoint/Edge/Triangle３分割）
                            // プリミティブ情報更新(nextPos/oldPos,invMass,thickness,aabb)
                            // 最大プリミティブサイズ算出、グリッドサイズ算出
                            // プリミティブ用のインターセクトフラグ更新
                            var selfStep_UpdatePrimitive_job = new SelfCollisionConstraint.SelfStep_UpdatePrimitiveJob()
                            {
                                workerCount = 3,
                                updateIndex = updateIndex,
                                simulationPower = tim.SimulationPower,
                                // team
                                batchSelfTeamList = tm.batchSplitClothTeamList,
                                teamDataArray = tm.teamDataArray.GetNativeArray(),
                                parameterArray = tm.parameterArray.GetNativeArray(),
                                // particle
                                nextPosArray = nextPosArray.GetNativeArray(),
                                oldPosArray = oldPosArray.GetNativeArray(),
                                frictionArray = frictionArray.GetNativeArray(),
                                // self collision
                                useIntersect = useIntersect,
                                primitiveArrayB = selfCollisionConstraint.primitiveArrayB.GetNativeArray(),
                                intersectFlagArray = selfCollisionConstraint.intersectFlagArray,
                            };
                            splitClothJobHandle = selfStep_UpdatePrimitive_job.Schedule(splitClothTeamCount * 3, 1, splitClothJobHandle);

                            // （チームごとPoint/Edge/Triangle３分割）
                            // プリミティブのグリッド座標算出
                            // プリミティブをグリッド座標でソート
                            // グリッド情報の作成
                            var selfStep_UpdateGrid_job = new SelfCollisionConstraint.SelfStep_UpdateGridJob()
                            {
                                kindCount = 3,
                                updateIndex = updateIndex,
                                simulationPower = tim.SimulationPower,
                                // team
                                batchSelfTeamList = tm.batchSplitClothTeamList,
                                teamDataArray = tm.teamDataArray.GetNativeArray(),
                                // self collision
                                primitiveArrayB = selfCollisionConstraint.primitiveArrayB.GetNativeArray(),
                                uniformGridStartCountBuffer = selfCollisionConstraint.uniformGridStartCountBuffer.GetNativeArray(),
                            };
                            splitClothJobHandle = selfStep_UpdateGrid_job.Schedule(splitClothTeamCount * 3, 1, splitClothJobHandle);

                            // コンタクトバッファの生成(EdgeEdge/PointTriangle)
                            // チームごと＋特殊分散
                            var selfStep_DetectionContact_job = new SelfCollisionConstraint.SelfStep_DetectionContactJob()
                            {
                                updateIndex = updateIndex,
                                workerCount = workerCount,
                                teamCount = splitClothTeamCount,
                                // team
                                batchSelfTeamList = tm.batchSplitClothTeamList,
                                teamDataArray = tm.teamDataArray.GetNativeArray(),
                                // particle
                                nextPosArray = nextPosArray.GetNativeArray(),
                                oldPosArray = oldPosArray.GetNativeArray(),
                                // self collision
                                primitiveArrayB = selfCollisionConstraint.primitiveArrayB.GetNativeArray(),
                                uniformGridStartCountBuffer = selfCollisionConstraint.uniformGridStartCountBuffer.GetNativeArray(),
                                // buffer
                                contactQueue = selfCollisionConstraint.contactQueue.AsParallelWriter(),
                            };
                            // Self:(EdgeEdge/PointTriangle/TrianglePoint), Sync:(EdgeEdge/PointTriangle/TrianglePoint) = 6
                            splitClothJobHandle = selfStep_DetectionContact_job.Schedule(splitClothTeamCount * 6 * workerCount, 1, splitClothJobHandle);

                            // コンタクトバッファをリストに変換
                            // ここは並列化できない
                            var selfStep_ConvertContactList_job = new SelfCollisionConstraint.SelfStep_ConvertContactListJob()
                            {
                                contactQueue = selfCollisionConstraint.contactQueue,
                                contactList = selfCollisionConstraint.contactList,
                            };
                            splitClothJobHandle = selfStep_ConvertContactList_job.Schedule(splitClothJobHandle);
                        }
                        else
                        {
                            // ■２ステップ以降
                            // コンタクトバッファごと
                            // 現在のnextPos/oldPosから変位を求め衝突の有無とs/t/nなどを求める
                            // aabbは更新なし
                            var selfStep_UpdateContact_job = new SelfCollisionConstraint.SelfStep_UpdateContactJob()
                            {
                                first = updateIndex == 0,
                                //first = true,
                                contactList = selfCollisionConstraint.contactList,
                                // particle
                                nextPosArray = nextPosArray.GetNativeArray(),
                                oldPosArray = oldPosArray.GetNativeArray(),
                                // self collision
                                primitiveArrayB = selfCollisionConstraint.primitiveArrayB.GetNativeArray(),
                            };
                            splitClothJobHandle = selfStep_UpdateContact_job.Schedule(selfCollisionConstraint.contactList, 256, splitClothJobHandle);
                        }

                        // セルフコリジョン
                        // コンタクトバッファ解決（反復）
                        for (int i = 0; i < Define.System.SelfCollisionSolverIteration; i++)
                        {
                            // コンタクトバッファ解決
                            // コンタクトバッファごと
                            var selfStep_SolverContact_job = new SelfCollisionConstraint.SelfStep_SolverContactJob()
                            {
                                // particle
                                nextPosArray = nextPosArray.GetNativeArray(),
                                // self collision
                                primitiveArrayB = selfCollisionConstraint.primitiveArrayB.GetNativeArray(),
                                contactList = selfCollisionConstraint.contactList,
                                // buffer2
                                tempVectorBufferA = tempVectorBufferA,
                                tempCountBuffer = tempCountBuffer,
                            };
                            splitClothJobHandle = selfStep_SolverContact_job.Schedule(selfCollisionConstraint.contactList, 128, splitClothJobHandle);

                            // 集計
                            // チームごと
                            var selfStep_SumContact_job = new SelfCollisionConstraint.SelfStep_SumContactJob()
                            {
                                updateIndex = updateIndex,
                                // team
                                batchSelfTeamList = tm.batchSplitClothTeamList,
                                teamDataArray = tm.teamDataArray.GetNativeArray(),
                                // particle
                                nextPosArray = nextPosArray.GetNativeArray(),
                                // buffer2
                                tempVectorBufferA = tempVectorBufferA,
                                tempCountBuffer = tempCountBuffer,
                            };
                            splitClothJobHandle = selfStep_SumContact_job.Schedule(splitClothTeamCount, 1, splitClothJobHandle);
                        }

                        // 座標確定
                        // コライダーの後更新
                        var splitStep_G_Self_job = new SplitStep_G_Self_Job()
                        {
                            workerCount = workerCount,
                            updateIndex = updateIndex,
                            simulationDeltaTime = tim.SimulationDeltaTime,
                            // team
                            batchSelfTeamList = tm.batchSplitClothTeamList,
                            teamDataArray = tm.teamDataArray.GetNativeArray(),
                            centerDataArray = tm.centerDataArray.GetNativeArray(),
                            parameterArray = tm.parameterArray.GetNativeArray(),
                            // vmesh
                            attributes = vm.attributes.GetNativeArray(),
                            depthArray = vm.vertexDepths.GetNativeArray(),
                            // particle
                            nextPosArray = nextPosArray.GetNativeArray(),
                            oldPosArray = oldPosArray.GetNativeArray(),
                            velocityPosArray = velocityPosArray.GetNativeArray(),
                            velocityArray = velocityArray.GetNativeArray(),
                            realVelocityArray = realVelocityArray.GetNativeArray(),
                            frictionArray = frictionArray.GetNativeArray(),
                            staticFrictionArray = staticFrictionArray.GetNativeArray(),
                            collisionNormalArray = collisionNormalArray.GetNativeArray(),
                            // collider
                            colliderNowPositions = cm.nowPositions.GetNativeArray(),
                            colliderNowRotations = cm.nowRotations.GetNativeArray(),
                            colliderOldPositions = cm.oldPositions.GetNativeArray(),
                            colliderOldRotations = cm.oldRotations.GetNativeArray(),
                        };
                        splitClothJobHandle = splitStep_G_Self_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);
                    }
                    else
                    {
                        // ■セルフコリジョンなし
                        // コライダーコリジョンEdge集計
                        // 距離
                        // モーション
                        // 座標確定
                        // コライダーの後更新
                        var splitStep_FG_NoSelf_job = new SplitStep_FG_NoSelf_Job()
                        {
                            workerCount = workerCount,
                            updateIndex = updateIndex,
                            simulationPower = tim.SimulationPower,
                            simulationDeltaTime = tim.SimulationDeltaTime,
                            // team
                            batchSelfTeamList = tm.batchSplitClothTeamList,
                            teamDataArray = tm.teamDataArray.GetNativeArray(),
                            centerDataArray = tm.centerDataArray.GetNativeArray(),
                            parameterArray = tm.parameterArray.GetNativeArray(),
                            // vmesh
                            attributes = vm.attributes.GetNativeArray(),
                            depthArray = vm.vertexDepths.GetNativeArray(),
                            // particle
                            nextPosArray = nextPosArray.GetNativeArray(),
                            oldPosArray = oldPosArray.GetNativeArray(),
                            basePosArray = basePosArray.GetNativeArray(),
                            baseRotArray = baseRotArray.GetNativeArray(),
                            velocityPosArray = velocityPosArray.GetNativeArray(),
                            velocityArray = velocityArray.GetNativeArray(),
                            realVelocityArray = realVelocityArray.GetNativeArray(),
                            frictionArray = frictionArray.GetNativeArray(),
                            staticFrictionArray = staticFrictionArray.GetNativeArray(),
                            collisionNormalArray = collisionNormalArray.GetNativeArray(),
                            // collider
                            colliderNowPositions = cm.nowPositions.GetNativeArray(),
                            colliderNowRotations = cm.nowRotations.GetNativeArray(),
                            colliderOldPositions = cm.oldPositions.GetNativeArray(),
                            colliderOldRotations = cm.oldRotations.GetNativeArray(),
                            // distance
                            distanceIndexArray = distanceConstraint.indexArray.GetNativeArray(),
                            distanceDataArray = distanceConstraint.dataArray.GetNativeArray(),
                            distanceDistanceArray = distanceConstraint.distanceArray.GetNativeArray(),
                            // buffer2
                            tempVectorBufferA = tempVectorBufferA,
                            tempVectorBufferB = tempVectorBufferB,
                            tempCountBuffer = tempCountBuffer,
                            tempFloatBufferA = tempFloatBufferA,
                        };
                        splitClothJobHandle = splitStep_FG_NoSelf_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);
                    }
                }

                // シミュレーション後処理
                // 表示位置の計算
                var splitPost_DisplayPos_job = new SplitPost_DisplayPos_Job()
                {
                    workerCount = workerCount,
                    simulationDeltaTime = MagicaManager.Time.SimulationDeltaTime,

                    // team
                    batchSelfTeamList = tm.batchSplitClothTeamList,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    // vmesh
                    attributes = vm.attributes.GetNativeArray(),
                    positions = vm.positions.GetNativeArray(),
                    rotations = vm.rotations.GetNativeArray(),
                    vertexRootIndices = vm.vertexRootIndices.GetNativeArray(),

                    // particle
                    oldPosArray = oldPosArray.GetNativeArray(),
                    oldPositionArray = oldPositionArray.GetNativeArray(),
                    oldRotationArray = oldRotationArray.GetNativeArray(),
                    dispPosArray = dispPosArray.GetNativeArray(),
                    realVelocityArray = realVelocityArray.GetNativeArray(),

                    // temp
                    tempVectorBufferA = tempVectorBufferA,
                    tempRotationBufferA = tempRotationBufferA,
                };
                splitClothJobHandle = splitPost_DisplayPos_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                // 並行でインターセクトの解決を行う
                // インターセクトの結果は次のフレームで利用される
                if (useIntersectJob)
                {
                    // インターセクトバッファクリア
                    var self_ClearIntersect_job = new SelfCollisionConstraint.SelfClearIntersectJob()
                    {
                        // team
                        batchSelfTeamList = tm.batchSplitClothTeamList,
                        teamDataArray = tm.teamDataArray.GetNativeArray(),
                        // self collision
                        intersectFlagArray = selfCollisionConstraint.intersectFlagArray,
                    };
                    solverIntersectJobHandle = self_ClearIntersect_job.Schedule(splitClothTeamCount, 1, splitClothJobHandle);

                    // インターセクトバッファ解決
                    var self_SolverIntersect_job = new SelfCollisionConstraint.SelfSolverIntersectJob()
                    {
                        // particle
                        nextPosArray = nextPosArray.GetNativeArray(),
                        // self collision
                        intersectList = selfCollisionConstraint.intersectList,
                        // buffer
                        intersectFlagArray = selfCollisionConstraint.intersectFlagArray,
                    };
                    solverIntersectJobHandle = self_SolverIntersect_job.Schedule(selfCollisionConstraint.intersectList, 128, solverIntersectJobHandle);
                }

                // クロスシミュレーションの結果をProxyMeshへ反映させる
                // ラインがある場合はベースラインごとに姿勢を整える
                var splitPost_CalcProxy_job = new SplitPost_CalcProxy_Job()
                {
                    workerCount = workerCount,

                    // team
                    batchSelfTeamList = tm.batchSplitClothTeamList,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    // vmesh
                    attributes = vm.attributes.GetNativeArray(),
                    positions = vm.positions.GetNativeArray(),
                    rotations = vm.rotations.GetNativeArray(),
                    baseLineStartDataIndices = vm.baseLineStartDataIndices.GetNativeArray(),
                    baseLineDataCounts = vm.baseLineDataCounts.GetNativeArray(),
                    baseLineData = vm.baseLineData.GetNativeArray(),
                    vertexLocalPositions = vm.vertexLocalPositions.GetNativeArray(),
                    vertexLocalRotations = vm.vertexLocalRotations.GetNativeArray(),
                    vertexChildIndexArray = vm.vertexChildIndexArray.GetNativeArray(),
                    vertexChildDataArray = vm.vertexChildDataArray.GetNativeArray(),
                    baseLineFlags = vm.baseLineFlags.GetNativeArray(),

                    // temp
                    tempVectorBufferA = tempVectorBufferA,
                    tempRotationBufferB = tempRotationBufferB,
                };
                splitClothJobHandle = splitPost_CalcProxy_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                // トライアングルの法線接線を求める
                var splitPost_CalcProxyTriangle_job = new SplitPost_CalcProxyTriangle_Job()
                {
                    workerCount = workerCount,

                    // team
                    batchSelfTeamList = tm.batchSplitClothTeamList,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    // vmesh
                    positions = vm.positions.GetNativeArray(),
                    triangles = vm.triangles.GetNativeArray(),
                    triangleNormals = vm.triangleNormals.GetNativeArray(),
                    triangleTangents = vm.triangleTangents.GetNativeArray(),
                    uvs = vm.uv.GetNativeArray(),
                };
                splitClothJobHandle = splitPost_CalcProxyTriangle_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                // トライアングルの法線接線から頂点の姿勢を求める
                // BoneClothの場合は頂点姿勢から連動するトランスフォームのワールド姿勢を計算する
                var splitPost_SumProxyTriangleAndTransform_job = new SplitPost_SumProxyTriangleAndTransform_Job()
                {
                    workerCount = workerCount,

                    // team
                    batchSelfTeamList = tm.batchSplitClothTeamList,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),

                    // transform
                    transformPositionArray = bm.positionArray.GetNativeArray(),
                    transformRotationArray = bm.rotationArray.GetNativeArray(),

                    // vmesh
                    positions = vm.positions.GetNativeArray(),
                    rotations = vm.rotations.GetNativeArray(),
                    triangleNormals = vm.triangleNormals.GetNativeArray(),
                    triangleTangents = vm.triangleTangents.GetNativeArray(),
                    vertexToTriangles = vm.vertexToTriangles.GetNativeArray(),
                    normalAdjustmentRotations = vm.normalAdjustmentRotations.GetNativeArray(),
                    vertexToTransformRotations = vm.vertexToTransformRotations.GetNativeArray(),
                };
                splitClothJobHandle = splitPost_SumProxyTriangleAndTransform_job.Schedule(splitClothTeamCount * workerCount, 1, splitClothJobHandle);

                // チーム更新後処理
                // BoneClothの場合はTransformのローカル姿勢を計算する
                // コライダー更新後処理
                var splitPost_TeamCollider_job = new SplitPost_TeamCollider_Job()
                {
                    simulationDeltaTime = MagicaManager.Time.SimulationDeltaTime,

                    // team
                    batchSelfTeamList = tm.batchSplitClothTeamList,
                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    centerDataArray = tm.centerDataArray.GetNativeArray(),

                    // collider
                    colliderFramePositions = cm.framePositions.GetNativeArray(),
                    colliderFrameRotations = cm.frameRotations.GetNativeArray(),
                    colliderOldFramePositions = cm.oldFramePositions.GetNativeArray(),
                    colliderOldFrameRotations = cm.oldFrameRotations.GetNativeArray(),

                    // transform
                    transformPositionArray = bm.positionArray.GetNativeArray(),
                    transformRotationArray = bm.rotationArray.GetNativeArray(),
                    transformScaleArray = bm.scaleArray.GetNativeArray(),
                    transformLocalPositionArray = bm.localPositionArray.GetNativeArray(),
                    transformLocalRotationArray = bm.localRotationArray.GetNativeArray(),

                    // vmesh
                    attributes = vm.attributes.GetNativeArray(),
                    vertexParentIndices = vm.vertexParentIndices.GetNativeArray(),
                };
                splitClothJobHandle = splitPost_TeamCollider_job.Schedule(splitClothTeamCount, 1, splitClothJobHandle);

                // 最後にインターセクトの解決ジョブを合流
                if (useIntersectJob)
                {
                    splitClothJobHandle = JobHandle.CombineDependencies(splitClothJobHandle, solverIntersectJobHandle);
                }
            }

            // ■一括シミュレーションジョブ
            // セルフコリジョンなし、かつプロキシメッシュの頂点数が一定値未満のジョブ
            // １つの巨大なジョブ内ですべてを完結させる
            if (useNormalClothJob)
            {
                var normalJob = new SimulationNormalJob()
                {
                    batchNormalTeamList = tm.batchNormalClothTeamList,
                    simulationPower = tim.SimulationPower,
                    simulationDeltaTime = tim.SimulationDeltaTime,
                    mappingCount = tm.MappingCount,

                    // team
                    teamDataArray = tm.teamDataArray.GetNativeArray(),
                    centerDataArray = tm.centerDataArray.GetNativeArray(),
                    teamWindArray = tm.teamWindArray.GetNativeArray(),
                    parameterArray = tm.parameterArray.GetNativeArray(),

                    // wind
                    windZoneCount = wm.WindCount,
                    windDataArray = wm.windDataArray.GetNativeArray(),

                    // transform
                    transformPositionArray = bm.positionArray.GetNativeArray(),
                    transformRotationArray = bm.rotationArray.GetNativeArray(),
                    transformScaleArray = bm.scaleArray.GetNativeArray(),
                    transformLocalToWorldMatrixArray = bm.localToWorldMatrixArray.GetNativeArray(),
                    transformLocalPositionArray = bm.localPositionArray.GetNativeArray(),
                    transformLocalRotationArray = bm.localRotationArray.GetNativeArray(),
                    transformLocalScaleArray = bm.localScaleArray.GetNativeArray(),

                    // vmesh
                    attributes = vm.attributes.GetNativeArray(),
                    depthArray = vm.vertexDepths.GetNativeArray(),
                    localPositions = vm.localPositions.GetNativeArray(),
                    localNormals = vm.localNormals.GetNativeArray(),
                    localTangents = vm.localTangents.GetNativeArray(),
                    boneWeights = vm.boneWeights.GetNativeArray(),
                    skinBoneTransformIndices = vm.skinBoneTransformIndices.GetNativeArray(),
                    skinBoneBindPoses = vm.skinBoneBindPoses.GetNativeArray(),
                    positions = vm.positions.GetNativeArray(),
                    rotations = vm.rotations.GetNativeArray(),
                    vertexBindPoseRotations = vm.vertexBindPoseRotations.GetNativeArray(),
                    vertexDepths = vm.vertexDepths.GetNativeArray(),
                    vertexRootIndices = vm.vertexRootIndices.GetNativeArray(),
                    vertexParentIndices = vm.vertexParentIndices.GetNativeArray(),
                    baseLineStartDataIndices = vm.baseLineStartDataIndices.GetNativeArray(),
                    baseLineDataCounts = vm.baseLineDataCounts.GetNativeArray(),
                    baseLineData = vm.baseLineData.GetNativeArray(),
                    vertexLocalPositions = vm.vertexLocalPositions.GetNativeArray(),
                    vertexLocalRotations = vm.vertexLocalRotations.GetNativeArray(),
                    vertexChildIndexArray = vm.vertexChildIndexArray.GetNativeArray(),
                    vertexChildDataArray = vm.vertexChildDataArray.GetNativeArray(),
                    baseLineFlags = vm.baseLineFlags.GetNativeArray(),
                    triangles = vm.triangles.GetNativeArray(),
                    triangleNormals = vm.triangleNormals.GetNativeArray(),
                    triangleTangents = vm.triangleTangents.GetNativeArray(),
                    uvs = vm.uv.GetNativeArray(),
                    vertexToTriangles = vm.vertexToTriangles.GetNativeArray(),
                    normalAdjustmentRotations = vm.normalAdjustmentRotations.GetNativeArray(),
                    vertexToTransformRotations = vm.vertexToTransformRotations.GetNativeArray(),
                    edges = vm.edges.GetNativeArray(),

                    // particle
                    nextPosArray = nextPosArray.GetNativeArray(),
                    oldPosArray = oldPosArray.GetNativeArray(),
                    oldRotArray = oldRotArray.GetNativeArray(),
                    basePosArray = basePosArray.GetNativeArray(),
                    baseRotArray = baseRotArray.GetNativeArray(),
                    oldPositionArray = oldPositionArray.GetNativeArray(),
                    oldRotationArray = oldRotationArray.GetNativeArray(),
                    velocityPosArray = velocityPosArray.GetNativeArray(),
                    dispPosArray = dispPosArray.GetNativeArray(),
                    velocityArray = velocityArray.GetNativeArray(),
                    realVelocityArray = realVelocityArray.GetNativeArray(),
                    frictionArray = frictionArray.GetNativeArray(),
                    staticFrictionArray = staticFrictionArray.GetNativeArray(),
                    collisionNormalArray = collisionNormalArray.GetNativeArray(),

                    // collider
                    colliderFlagArray = cm.flagArray.GetNativeArray(),
                    colliderCenterArray = cm.centerArray.GetNativeArray(),
                    colliderSizeArray = cm.sizeArray.GetNativeArray(),
                    colliderFramePositions = cm.framePositions.GetNativeArray(),
                    colliderFrameRotations = cm.frameRotations.GetNativeArray(),
                    colliderFrameScales = cm.frameScales.GetNativeArray(),
                    colliderOldFramePositions = cm.oldFramePositions.GetNativeArray(),
                    colliderOldFrameRotations = cm.oldFrameRotations.GetNativeArray(),
                    colliderNowPositions = cm.nowPositions.GetNativeArray(),
                    colliderNowRotations = cm.nowRotations.GetNativeArray(),
                    colliderOldPositions = cm.oldPositions.GetNativeArray(),
                    colliderOldRotations = cm.oldRotations.GetNativeArray(),
                    colliderWorkDataArray = cm.workDataArray.GetNativeArray(),
                    colliderMainColliderIndices = cm.mainColliderIndices.GetNativeArray(),

                    // inertia
                    fixedArray = inertiaConstraint.fixedArray.GetNativeArray(),

                    // distance
                    distanceIndexArray = distanceConstraint.indexArray.GetNativeArray(),
                    distanceDataArray = distanceConstraint.dataArray.GetNativeArray(),
                    distanceDistanceArray = distanceConstraint.distanceArray.GetNativeArray(),

                    // triangleBending
                    bendingTrianglePairArray = bendingConstraint.trianglePairArray.GetNativeArray(),
                    bendingRestAngleOrVolumeArray = bendingConstraint.restAngleOrVolumeArray.GetNativeArray(),
                    bendingSignOrVolumeArray = bendingConstraint.signOrVolumeArray.GetNativeArray(),

                    // buffer
                    stepBasicPositionBuffer = stepBasicPositionBuffer,
                    stepBasicRotationBuffer = stepBasicRotationBuffer,

                    // buffer2
                    tempVectorBufferA = tempVectorBufferA,
                    tempVectorBufferB = tempVectorBufferB,
                    tempCountBuffer = tempCountBuffer,
                    tempFloatBufferA = tempFloatBufferA,
                    tempRotationBufferA = tempRotationBufferA,
                    tempRotationBufferB = tempRotationBufferB,
                };
                normalClothJobHandle = normalJob.Schedule(normalClothTeamCount, 1, jobHandle);
            }

            // ■ジョブ連結
            // !一括ジョブと分割ジョブを並行動作させる
            jobHandle = JobHandle.CombineDependencies(splitClothJobHandle, normalClothJobHandle);

            // マッピングメッシュの有無で分岐
            if (MagicaManager.Team.MappingCount > 0)
            {
                // この２つのジョブは並列動作可能
                // ■マッピングメッシュの頂点姿勢を連動するプロキシメッシュからスキニングして求める
                var job1 = vm.PostMappingMeshUpdateBatchSchedule(jobHandle, workerCount);

                // ■BoneClothのTransformへの書き込み
                var job2 = bm.WriteTransformSchedule(jobHandle);

                jobHandle = JobHandle.CombineDependencies(job1, job2);
            }
            else
            {
                // ■BoneClothのTransformへの書き込み
                jobHandle = bm.WriteTransformSchedule(jobHandle);
            }

            return jobHandle;
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"========== Simulation Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Simulation Manager. Invalid");
            }
            else
            {
                sb.AppendLine($"Simulation Manager. Particle:{ParticleCount}");
                sb.AppendLine($"  -teamIdArray:{teamIdArray.ToSummary()}");
                sb.AppendLine($"  -nextPosArray:{nextPosArray.ToSummary()}");
                sb.AppendLine($"  -oldPosArray:{oldPosArray.ToSummary()}");
                sb.AppendLine($"  -oldRotArray:{oldRotArray.ToSummary()}");
                sb.AppendLine($"  -basePosArray:{basePosArray.ToSummary()}");
                sb.AppendLine($"  -baseRotArray:{baseRotArray.ToSummary()}");
                sb.AppendLine($"  -oldPositionArray:{oldPositionArray.ToSummary()}");
                sb.AppendLine($"  -oldRotationArray:{oldRotationArray.ToSummary()}");
                sb.AppendLine($"  -velocityPosArray:{velocityPosArray.ToSummary()}");
                sb.AppendLine($"  -dispPosArray:{dispPosArray.ToSummary()}");
                sb.AppendLine($"  -velocityArray:{velocityArray.ToSummary()}");
                sb.AppendLine($"  -realVelocityArray:{realVelocityArray.ToSummary()}");
                sb.AppendLine($"  -frictionArray:{frictionArray.ToSummary()}");
                sb.AppendLine($"  -staticFrictionArray:{staticFrictionArray.ToSummary()}");
                sb.AppendLine($"  -collisionNormalArray:{collisionNormalArray.ToSummary()}");

                // 制約
                sb.Append(distanceConstraint.ToString());
                sb.Append(bendingConstraint.ToString());
                sb.Append(angleConstraint.ToString());
                sb.Append(inertiaConstraint.ToString());
                sb.Append(colliderCollisionConstraint.ToString());
                sb.Append(selfCollisionConstraint.ToString());

                // 汎用バッファ
                sb.AppendLine($"[Buffer]");
                sb.AppendLine($"  -stepBasicPositionBuffer:{(stepBasicPositionBuffer.IsCreated ? stepBasicPositionBuffer.Length : 0)}");
                sb.AppendLine($"  -stepBasicRotationBuffer:{(stepBasicRotationBuffer.IsCreated ? stepBasicRotationBuffer.Length : 0)}");
                sb.AppendLine($"  -tempVectorBufferA:{(tempVectorBufferA.IsCreated ? tempVectorBufferA.Length : 0)}");
                sb.AppendLine($"  -tempVectorBufferB:{(tempVectorBufferB.IsCreated ? tempVectorBufferB.Length : 0)}");
                sb.AppendLine($"  -tempCountBuffer:{(tempCountBuffer.IsCreated ? tempCountBuffer.Length : 0)}");
                sb.AppendLine($"  -tempFloatBufferA:{(tempFloatBufferA.IsCreated ? tempFloatBufferA.Length : 0)}");
                sb.AppendLine($"  -tempRotationBufferA:{(tempRotationBufferA.IsCreated ? tempRotationBufferA.Length : 0)}");
                sb.AppendLine($"  -tempRotationBufferB:{(tempRotationBufferB.IsCreated ? tempRotationBufferB.Length : 0)}");

                sb.AppendLine();
            }
            sb.AppendLine();
            Debug.Log(sb.ToString());
            allsb.Append(sb);
        }
    }
}
