// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    public class TeamManager : IManager, IValid
    {
        /// <summary>
        /// チームフラグ(64bit)
        /// </summary>
        public const int Flag_Valid = 0; // データの有効性
        public const int Flag_Enable = 1; // 動作状態
        public const int Flag_Reset = 2; // 姿勢リセット
        public const int Flag_TimeReset = 3; // 時間リセット
        public const int Flag_SyncSuspend = 4; // 同期待ち一時停止
        public const int Flag_Running = 5; // 今回のフレームでシミュレーションが実行されたかどうか
        public const int Flag_Synchronization = 6; // 同期中
        public const int Flag_StepRunning = 7; // ステップ実行中
        public const int Flag_Exit = 8; // 存在消滅時
        public const int Flag_KeepTeleport = 9; // 姿勢保持テレポート
        public const int Flag_InertiaShift = 10; // 慣性全体シフト
        public const int Flag_CameraCullingInvisible = 11; // カメラカリングによる非表示状態
        public const int Flag_CameraCullingKeep = 12; // カメラカリング時に姿勢を保つ
        public const int Flag_Spring = 13; // Spring利用
        public const int Flag_SkipWriting = 14; // 書き込み停止（ストップモーション用）
        public const int Flag_Anchor = 15; // Inertia anchorを利用中
        public const int Flag_AnchorReset = 16; // Inertia anchorの座標リセット
        public const int Flag_NegativeScale = 17; // マイナススケールの有無
        public const int Flag_NegativeScaleTeleport = 18; // マイナススケールによるテレポート
        public const int Flag_DistanceCullingInvisible = 19; // 距離カリングによる非表示状態
        public const int Flag_RestoreTransformOnlyOnec = 20; // Transform復元を一度のみ実行する(BoneClothのDisable時)
        public const int Flag_Tangent = 21; // 接線を計算する
        public const int Flag_ScaleSuspent = 22; // 極小スケールによる機能停止
        public const int Flag_ProxyMeshLine = 23; // プロキシメッシュにLineデータが存在する

        // 以下セルフコリジョン
        // !これ以降の順番を変えないこと
        public const int Flag_Self_PointPrimitive = 32; // PointPrimitive+Sortを保持し更新する
        public const int Flag_Self_EdgePrimitive = 33; // EdgePrimitive+Sortを保持し更新する
        public const int Flag_Self_TrianglePrimitive = 34; // TrianglePrimitive+Sortを保持し更新する

        public const int Flag_Self_EdgeEdge = 35;
        public const int Flag_Sync_EdgeEdge = 36;
        public const int Flag_PSync_EdgeEdge = 37;

        public const int Flag_Self_PointTriangle = 38;
        public const int Flag_Sync_PointTriangle = 39;
        public const int Flag_PSync_PointTriangle = 40;

        public const int Flag_Self_TrianglePoint = 41;
        public const int Flag_Sync_TrianglePoint = 42;
        public const int Flag_PSync_TrianglePoint = 43;

        public const int Flag_Self_EdgeTriangleIntersect = 44;
        public const int Flag_Sync_EdgeTriangleIntersect = 45;
        public const int Flag_PSync_EdgeTriangleIntersect = 46;
        public const int Flag_Self_TriangleEdgeIntersect = 47;
        public const int Flag_Sync_TriangleEdgeIntersect = 48;
        public const int Flag_PSync_TriangleEdgeIntersect = 49;

        /// <summary>
        /// チーム基本データ
        /// </summary>
        public struct TeamData
        {
            /// <summary>
            /// フラグ
            /// </summary>
            public BitField64 flag;

            /// <summary>
            /// 更新モード(オリジナル)
            /// </summary>
            public ClothUpdateMode originalUpdateMode;

            /// <summary>
            /// 更新モード(最終結果)
            /// </summary>
            public ClothUpdateMode updateMode;

            /// <summary>
            /// １秒間の更新頻度
            /// </summary>
            //public int frequency;

            /// <summary>
            /// 現在フレームの更新時間
            /// </summary>
            public float frameDeltaTime;

            /// <summary>
            /// 更新計算用時間
            /// </summary>
            public float time;

            /// <summary>
            /// 前フレームの更新計算用時間
            /// </summary>
            public float oldTime;

            /// <summary>
            /// 現在のシミュレーション更新時間
            /// </summary>
            public float nowUpdateTime;

            /// <summary>
            /// １つ前の最後のシミュレーション更新時間
            /// </summary>
            public float oldUpdateTime;

            /// <summary>
            /// 更新がある場合のフレーム時間
            /// </summary>
            public float frameUpdateTime;

            /// <summary>
            /// 前回更新のフレーム時間
            /// </summary>
            public float frameOldTime;

            /// <summary>
            /// チーム固有のタイムスケール(0.0-1.0)
            /// </summary>
            public float timeScale;

            /// <summary>
            /// チームの最終計算用タイムスケール(0.0~1.0)
            /// グローバルタイムスケールなどを考慮した値
            /// </summary>
            public float nowTimeScale;

            /// <summary>
            /// 今回のチーム更新回数（０ならばこのフレームは更新なし）
            /// </summary>
            public int updateCount;

            /// <summary>
            /// 今回のチーム更新スキップ回数（１以上ならばシミュレーションスキップが発生）
            /// </summary>
            public int skipCount;

            /// <summary>
            /// ステップごとのフレームに対するnowUpdateTime割合
            /// これは(frameStartTime ~ time)間でのnowUpdateTimeの割合
            /// </summary>
            public float frameInterpolation;

            /// <summary>
            /// 重力の影響力(0.0 ~ 1.0)
            /// 1.0は重力が100%影響する
            /// </summary>
            public float gravityRatio;

            public float gravityDot;

            /// <summary>
            /// センタートランスフォーム(ダイレクト値)
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// 現在の中心ワールド座標（この値はCenterData.nowWorldPositionのコピー）
            /// </summary>
            //public float3 centerWorldPosition;

            /// <summary>
            /// アンカーとして設定されているTransformのインスタンスID(0=なし)
            /// </summary>
            //public int anchorTransformId;

            /// <summary>
            /// 距離カリングの測定オブジェクトID(0=メインカメラ)
            /// </summary>
            public int distanceReferenceObjectId;

            /// <summary>
            /// コンポーネント用のTransformインデックス
            /// </summary>
            public int componentTransformIndex;

            /// <summary>
            /// チームスケール
            /// </summary>
            public float3 initScale;            // データ生成時のセンタートランスフォームスケール
            public float scaleRatio;            // 現在のスケール倍率

            /// <summary>
            /// マイナススケール
            /// </summary>
            public float negativeScaleSign;             // マイナススケールの有無(1:正スケール, -1:マイナススケール)
            public float3 negativeScaleDirection;       // スケール方向(xyz)：(1:正スケール, -1:マイナススケール)
            public float3 negativeScaleChange;          // 今回のフレームで変化したスケール(xyz)：(1:変化なし, -1:反転した)
            public float2 negativeScaleTriangleSign;    // トライアングル法線接線フリップフラグ
            public float4 negativeScaleQuaternionValue; // クォータニオン反転用

            /// <summary>
            /// MagicaClothコンポーネントのインスタンスID
            /// </summary>
            //public int componentId;
            public MagicaObjectId componentId;

            /// <summary>
            /// 同期チームID(0=なし)
            /// </summary>
            public int syncTeamId;

            /// <summary>
            /// 自身を同期している親チームID(0=なし)：最大７つ
            /// </summary>
            public FixedList32Bytes<int> syncParentTeamId;

            /// <summary>
            /// 同期先チームのセンタートランスフォームインデックス（ダイレクト値）
            /// </summary>
            public int syncCenterTransformIndex;

            /// <summary>
            /// 連動するAnimatorのインスタンスID(0=なし)
            /// </summary>
            //public int interlockingAnimatorId;
            public MagicaObjectId interlockingAnimatorId;

            /// <summary>
            /// 初期姿勢とアニメーション姿勢のブレンド率（制約で利用）
            /// </summary>
            public float animationPoseRatio;

            /// <summary>
            /// 速度安定時間(StablizationTime)による速度適用割合(0.0 ~ 1.0)
            /// </summary>
            public float velocityWeight;

            /// <summary>
            /// 距離カリングによるブレンド割合(0.0 ~ 1.0)
            /// </summary>
            public float distanceWeight;

            /// <summary>
            /// 最終シミュレーション結果ブレンド割合(0.0 ~ 1.0)
            /// </summary>
            public float blendWeight;

            /// <summary>
            /// 外力モード
            /// </summary>
            public ClothForceMode forceMode;

            /// <summary>
            /// 外力
            /// </summary>
            public float3 impactForce;

            //-----------------------------------------------------------------
            /// <summary>
            /// ProxyMeshのタイプ
            /// </summary>
            public VirtualMesh.MeshType proxyMeshType;

            /// <summary>
            /// ProxyMeshのTransformデータ
            /// </summary>
            public DataChunk proxyTransformChunk;

            /// <summary>
            /// ProxyMeshの共通部分
            /// -attributes
            /// -vertexToTriangles
            /// -vertexToVertexIndexArray
            /// -vertexDepths
            /// -vertexLocalPositions
            /// -vertexLocalRotations
            /// -vertexRootIndices
            /// -vertexParentIndices
            /// -vertexChildIndexArray
            /// -vertexAngleCalcLocalRotations
            /// -uv
            /// -positions
            /// -rotations
            /// -vertexBindPosePositions
            /// -vertexBindPoseRotations
            /// -normalAdjustmentRotations
            /// </summary>
            public DataChunk proxyCommonChunk;

            /// <summary>
            /// ProxyMeshの頂点接続頂点データ
            /// -vertexToVertexDataArray (-vertexToVertexIndexArrayと対)
            /// </summary>
            //public DataChunk proxyVertexToVertexDataChunk;

            /// <summary>
            /// ProxyMeshの子頂点データ
            /// -vertexChildDataArray (-vertexChildIndexArrayと対)
            /// </summary>
            public DataChunk proxyVertexChildDataChunk;

            /// <summary>
            /// ProxyMeshのTriangle部分
            /// -triangles
            /// -triangleTeamIdArray
            /// -triangleNormals
            /// -triangleTangents
            /// </summary>
            public DataChunk proxyTriangleChunk;

            /// <summary>
            /// ProxyMeshのEdge部分
            /// -edges
            /// -edgeTeamIdArray
            /// </summary>
            public DataChunk proxyEdgeChunk;

            /// <summary>
            /// ProxyMeshのBoneCloth/MeshCloth共通部分
            /// -localPositions
            /// -localNormals
            /// -localTangents
            /// -boneWeights
            /// </summary>
            public DataChunk proxyMeshChunk;

            /// <summary>
            /// ProxyMeshのBoneCloth固有部分
            /// -vertexToTransformRotations
            /// </summary>
            public DataChunk proxyBoneChunk;

            /// <summary>
            /// ProxyMeshのMeshClothのスキニングボーン部分
            /// -skinBoneTransformIndices
            /// -skinBoneBindPoses
            /// </summary>
            public DataChunk proxySkinBoneChunk;

            /// <summary>
            /// ProxyMeshのベースライン部分
            /// -baseLineFlags
            /// -baseLineStartDataIndices
            /// -baseLineDataCounts
            /// </summary>
            public DataChunk baseLineChunk;

            /// <summary>
            /// ProxyMeshのベースラインデータ配列
            /// -baseLineData
            /// </summary>
            public DataChunk baseLineDataChunk;

            /// <summary>
            /// 固定点リスト
            /// </summary>
            public DataChunk fixedDataChunk;

            //-----------------------------------------------------------------
            /// <summary>
            /// 接続しているマッピングメッシュへデータへのインデックスセット(最大15まで)
            /// </summary>
            //public FixedList32Bytes<short> mappingDataIndexSet;

            //-----------------------------------------------------------------
            /// <summary>
            /// パーティクルデータ
            /// </summary>
            public DataChunk particleChunk;

            /// <summary>
            /// コライダーデータ
            /// コライダーが有効の場合は未使用であっても最大数まで確保される
            /// </summary>
            public DataChunk colliderChunk;

            /// <summary>
            /// コライダートランスフォーム
            /// コライダーが有効の場合は未使用であっても最大数まで確保される
            /// </summary>
            public DataChunk colliderTransformChunk;

            /// <summary>
            /// 現在有効なコライダー数
            /// </summary>
            public int colliderCount;

            //-----------------------------------------------------------------
            /// <summary>
            /// 距離制約
            /// </summary>
            public DataChunk distanceStartChunk;
            public DataChunk distanceDataChunk;

            /// <summary>
            /// 曲げ制約
            /// </summary>
            public DataChunk bendingPairChunk;

            /// <summary>
            /// セルフコリジョン制約
            /// </summary>
            public DataChunk selfPointChunk;
            public DataChunk selfEdgeChunk;
            public DataChunk selfTriangleChunk;
            public float selfGridSize;
            public int selfPointGridCount;
            public int selfEdgeGridCount;
            public int selfTriangleGridCount;
            public float selfMaxPrimitiveSize;

            //-----------------------------------------------------------------
            /// <summary>
            /// UnityPhysicsでの更新の必要性
            /// </summary>
            public bool IsFixedUpdate => updateMode == ClothUpdateMode.UnityPhysics;

            /// <summary>
            /// タイムスケールを無視
            /// </summary>
            public bool IsUnscaled => updateMode == ClothUpdateMode.Unscaled;

            /// <summary>
            /// １回の更新間隔
            /// </summary>
            //public float SimulationDeltaTime => 1.0f / frequency;

            /// <summary>
            /// データの有効性
            /// </summary>
            public bool IsValid => flag.IsSet(Flag_Valid);

            /// <summary>
            /// 有効状態
            /// </summary>
            public bool IsEnable => flag.IsSet(Flag_Enable);

            /// <summary>
            /// 処理状態
            /// </summary>
            public bool IsProcess => IsEnable && flag.IsSet(Flag_SyncSuspend) == false && IsCullingInvisible == false && IsScaleSuspend == false;

            /// <summary>
            /// 姿勢リセット有無
            /// </summary>
            public bool IsReset => flag.IsSet(Flag_Reset);

            /// <summary>
            /// 姿勢維持テレポートの有無
            /// </summary>
            public bool IsKeepReset => flag.IsSet(Flag_KeepTeleport);

            /// <summary>
            /// 慣性全体シフトの有無
            /// </summary>
            public bool IsInertiaShift => flag.IsSet(Flag_InertiaShift);

            /// <summary>
            /// 今回のフレームでシミュレーションが実行されたかどうか（１回以上実行された場合）
            /// </summary>
            public bool IsRunning => flag.IsSet(Flag_Running);

            /// <summary>
            /// ステップ実行中かどうか
            /// </summary>
            public bool IsStepRunning => flag.IsSet(Flag_StepRunning);

            public bool IsCameraCullingInvisible => flag.IsSet(Flag_CameraCullingInvisible);
            public bool IsCameraCullingKeep => flag.IsSet(Flag_CameraCullingKeep);
            public bool IsDistanceCullingInvisible => flag.IsSet(Flag_DistanceCullingInvisible);
            public bool IsCullingInvisible => IsCameraCullingInvisible || IsDistanceCullingInvisible;
            public bool IsSpring => flag.IsSet(Flag_Spring);
            public bool IsNegativeScale => flag.IsSet(Flag_NegativeScale);
            public bool IsNegativeScaleTeleport => flag.IsSet(Flag_NegativeScaleTeleport);
            public bool IsTangent => flag.IsSet(Flag_Tangent);
            public bool IsScaleSuspend => flag.IsSet(Flag_ScaleSuspent);
            public int ParticleCount => particleChunk.dataLength;

            /// <summary>
            /// 現在有効なコライダー数
            /// </summary>
            public int UseColliderCount => colliderCount;
            public int BaseLineCount => baseLineChunk.dataLength;
            public int TriangleCount => proxyTriangleChunk.dataLength;
            public int EdgeCount => proxyEdgeChunk.dataLength;

            //public int MappingCount => mappingDataIndexSet.Length;

            /// <summary>
            /// 初期スケール（ｘ軸のみで判定、一様スケールしか認めていない）
            /// </summary>
            public float InitScale => initScale.x;
        }
        public ExNativeArray<TeamData> teamDataArray;

        /// <summary>
        /// チームごとの風の影響情報
        /// </summary>
        public ExNativeArray<TeamWindData> teamWindArray;

        /// <summary>
        /// マッピングメッシュデータフラグ
        /// </summary>
        public const int MappingDataFlag_ChangePositionNormal = 0;
        public const int MappingDataFlag_ChangeTangent = 1;
        public const int MappingDataFlag_ChangeBoneWeight = 2;
        public const int MappingDataFlag_ModifyBoneWeight = 3;

        /// <summary>
        /// マッピングメッシュデータ
        /// </summary>
        public struct MappingData : IValid
        {
            public int teamId;

            /// <summary>
            /// 状態フラグ
            /// </summary>
            public BitField32 flag;

            /// <summary>
            /// Mappingメッシュのセンタートランスフォーム（ダイレクト値）
            /// </summary>
            public int centerTransformIndex;

            /// <summary>
            /// Mappingメッシュの基本
            /// -attributes
            /// -localPositions
            /// -localNormlas
            /// -localTangents
            /// -boneWeights
            /// -positions
            /// -rotations
            /// </summary>
            public DataChunk mappingCommonChunk;

            /// <summary>
            /// 初期状態でのプロキシメッシュへの変換マトリックスと変換回転
            /// この姿勢は初期化時に固定される
            /// </summary>
            public float4x4 toProxyMatrix;
            public quaternion toProxyRotation;

            /// <summary>
            /// プロキシメッシュとマッピングメッシュの座標空間が同じかどうか
            /// </summary>
            public bool sameSpace;

            /// <summary>
            /// プロキシメッシュからマッピングメッシュへの座標空間変換用
            /// ▲ワールド対応：ここはワールド空間からマッピングメッシュへの座標変換となる
            /// </summary>
            public float4x4 toMappingMatrix;
            public quaternion toMappingRotation;

            /// <summary>
            /// Mappingメッシュ用のスケーリング比率
            /// </summary>
            public float scaleRatio;

            /// <summary>
            /// 紐づけられているRenderDataWorkバッファへのインデックス
            /// </summary>
            public int renderDataWorkIndex;

            public bool IsValid()
            {
                return teamId > 0;
            }

            public int VertexCount => mappingCommonChunk.dataLength;
        }
        public ExNativeArray<MappingData> mappingDataArray;

        /// <summary>
        /// チームごとのマッピングメッシュIDリスト（チームごとに最大31まで)
        /// </summary>
        public ExNativeArray<FixedList64Bytes<short>> teamMappingIndexArray;

        /// <summary>
        /// チーム全体の集計データ
        /// x:最大更新回数
        /// y:分割ジョブチームのPointコリジョンの数
        /// z:分割ジョブチームのEdgeコリジョンの数
        /// w:分割ジョブチームのSelfコリジョンの数
        /// </summary>
        public NativeReference<int4> teamStatus;

        /// <summary>
        /// パラメータ（teamDataArrayとインデックス連動）
        /// </summary>
        public ExNativeArray<ClothParameters> parameterArray;

        /// <summary>
        /// センタートランスフォームデータ
        /// </summary>
        public ExNativeArray<InertiaConstraint.CenterData> centerDataArray;

        /// <summary>
        /// 登録されているマッピングメッシュ数
        /// </summary>
        public int MappingCount => mappingDataArray?.Count ?? 0;

        /// <summary>
        /// チームの有効状態を別途記録
        /// NativeArrayはジョブ実行中にアクセスできないため。
        /// </summary>
        HashSet<int> enableTeamSet = new HashSet<int>();

        /// <summary>
        /// チームIDとClothProcessクラスの関連辞書
        /// </summary>
        Dictionary<int, ClothProcess> clothProcessDict = new Dictionary<int, ClothProcess>();

        //=========================================================================================
        /// <summary>
        /// 登録されているチーム数（グローバルチームを含む。そのため０にはならない）
        /// </summary>
        public int TeamCount => teamDataArray?.Count ?? 0;

        /// <summary>
        /// 登録されている有効なチーム数（グローバルチームを含まない）
        /// </summary>
        public int TrueTeamCount => clothProcessDict.Count;

        /// <summary>
        /// 実行状態にあるチーム数
        /// </summary>
        public int ActiveTeamCount => enableTeamSet.Count;

        /// <summary>
        /// 今回フレームでのチーム全体の最大更新回数
        /// </summary>
        public int TeamMaxUpdateCount => teamStatus.Value.x;

        //=========================================================================================
        // ■作業データ
        bool isValid;

        /// <summary>
        /// エッジコライダーコリジョンのエッジ数合計
        /// </summary>
        internal int edgeColliderCollisionCount;

        internal NativeReference<int> edgeColliderCollisionCountBuff;
        internal NativeParallelHashMap<MagicaObjectId, int> comp2SuspendCounterMap;
        internal NativeParallelHashMap<MagicaObjectId, int> comp2TeamIdMap;
        internal NativeParallelHashMap<MagicaObjectId, MagicaObjectId> comp2SyncPartnerCompMap;
        internal NativeParallelHashMap<MagicaObjectId, MagicaObjectId> comp2SyncTopCompMap;

        internal NativeList<int> batchNormalClothTeamList;
        internal NativeList<int> batchSplitClothTeamList;

        internal List<ClothProcess> parameterDirtyList;
        internal List<ClothProcess> skipWritingDirtyList;
        internal NativeList<int> cullingDirtyList;
        internal NativeParallelHashSet<int> selfCollisionUpdateSet;
        internal NativeParallelHashMap<MagicaObjectId, int> animatorUpdateModeMap;

        internal ExSimpleNativeArray<MagicaObjectId> teamAnchorTransformIndexArray;
        internal ExSimpleNativeArray<MagicaObjectId> teamDistanceTransformIndexArray;
        internal NativeParallelHashMap<MagicaObjectId, float3> transformPositionMap;
        internal NativeParallelHashMap<MagicaObjectId, quaternion> transformRotationMap;

        internal HashSet<MagicaCloth> cameraCullingClothSet = new HashSet<MagicaCloth>(256);

        //=========================================================================================
        public void Dispose()
        {
            // 破棄監視リストの強制処理
            MonitoringProcess(true);

            isValid = false;

            teamDataArray?.Dispose();
            teamWindArray?.Dispose();
            mappingDataArray?.Dispose();
            teamMappingIndexArray?.Dispose();
            parameterArray?.Dispose();
            centerDataArray?.Dispose();

            teamDataArray = null;
            teamWindArray = null;
            mappingDataArray = null;
            teamMappingIndexArray = null;
            parameterArray = null;
            centerDataArray = null;

            if (teamStatus.IsCreated)
                teamStatus.Dispose();

            enableTeamSet.Clear();
            clothProcessDict.Clear();

            if (edgeColliderCollisionCountBuff.IsCreated)
                edgeColliderCollisionCountBuff.Dispose();
            comp2SuspendCounterMap.MC2DisposeSafe();
            comp2TeamIdMap.MC2DisposeSafe();
            comp2SyncPartnerCompMap.MC2DisposeSafe();
            comp2SyncTopCompMap.MC2DisposeSafe();

            if (batchNormalClothTeamList.IsCreated)
                batchNormalClothTeamList.Dispose();
            if (batchSplitClothTeamList.IsCreated)
                batchSplitClothTeamList.Dispose();

            parameterDirtyList?.Clear();
            skipWritingDirtyList?.Clear();
            if (cullingDirtyList.IsCreated)
                cullingDirtyList.Dispose();
            if (selfCollisionUpdateSet.IsCreated)
                selfCollisionUpdateSet.Dispose();
            animatorUpdateModeMap.MC2DisposeSafe();

            teamAnchorTransformIndexArray?.Dispose();
            teamDistanceTransformIndexArray?.Dispose();
            transformPositionMap.MC2DisposeSafe();
            transformRotationMap.MC2DisposeSafe();

            cameraCullingClothSet.Clear();

            //globalTimeScale = 1.0f;
            //fixedUpdateCount = 0;

            // 破棄監視更新処理
            MagicaManager.afterUpdateDelegate -= MonitoringProcessUpdate;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            Dispose();

            const int capacity = 32;
            teamDataArray = new ExNativeArray<TeamData>(capacity);
            teamWindArray = new ExNativeArray<TeamWindData>(capacity);
            mappingDataArray = new ExNativeArray<MappingData>(capacity);
            teamMappingIndexArray = new ExNativeArray<FixedList64Bytes<short>>(capacity);
            parameterArray = new ExNativeArray<ClothParameters>(capacity);
            centerDataArray = new ExNativeArray<InertiaConstraint.CenterData>(capacity);

            // グローバルチーム[0]を追加する
            var gteam = new TeamData();
            teamDataArray.Add(gteam);
            teamWindArray.Add(new TeamWindData());
            teamMappingIndexArray.Add(new FixedList64Bytes<short>());
            parameterArray.Add(new ClothParameters());
            centerDataArray.Add(new InertiaConstraint.CenterData());

            teamStatus = new NativeReference<int4>(Allocator.Persistent);

            //globalTimeScale = 1.0f;
            //fixedUpdateCount = 0;

            // 作業用
            edgeColliderCollisionCountBuff = new NativeReference<int>(Allocator.Persistent);
            comp2SuspendCounterMap = new NativeParallelHashMap<MagicaObjectId, int>(256, Allocator.Persistent);
            comp2TeamIdMap = new NativeParallelHashMap<MagicaObjectId, int>(256, Allocator.Persistent);
            comp2SyncPartnerCompMap = new NativeParallelHashMap<MagicaObjectId, MagicaObjectId>(256, Allocator.Persistent);
            comp2SyncTopCompMap = new NativeParallelHashMap<MagicaObjectId, MagicaObjectId>(256, Allocator.Persistent);

            batchNormalClothTeamList = new NativeList<int>(Allocator.Persistent);
            batchSplitClothTeamList = new NativeList<int>(Allocator.Persistent);

            parameterDirtyList = new List<ClothProcess>(128);
            skipWritingDirtyList = new List<ClothProcess>(128);
            cullingDirtyList = new NativeList<int>(128, Allocator.Persistent);
            selfCollisionUpdateSet = new NativeParallelHashSet<int>(256, Allocator.Persistent);
            animatorUpdateModeMap = new NativeParallelHashMap<MagicaObjectId, int>(128, Allocator.Persistent);

            teamAnchorTransformIndexArray = new ExSimpleNativeArray<MagicaObjectId>(256, true);
            teamDistanceTransformIndexArray = new ExSimpleNativeArray<MagicaObjectId>(256, true);
            transformPositionMap = new NativeParallelHashMap<MagicaObjectId, float3>(32, Allocator.Persistent);
            transformRotationMap = new NativeParallelHashMap<MagicaObjectId, quaternion>(32, Allocator.Persistent);

            // 破棄監視更新処理
            MagicaManager.afterUpdateDelegate += MonitoringProcessUpdate;

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        /// <summary>
        /// チームを登録する
        /// </summary>
        /// <param name="cprocess"></param>
        /// <param name="clothParams"></param>
        /// <returns></returns>
        internal int AddTeam(ClothProcess cprocess, ClothParameters clothParams)
        {
            if (isValid == false)
                return 0;

            // この段階でProxyMeshは完成している

            var team = new TeamData();
            team.componentId = cprocess.cloth.GetMagicaId();
            // ★Enableフラグは立てない
            team.flag.SetBits(Flag_Valid, true);
            team.flag.SetBits(Flag_Reset, true);
            team.flag.SetBits(Flag_TimeReset, true);
            team.originalUpdateMode = cprocess.cloth.SerializeData.updateMode;
            team.updateMode = cprocess.cloth.SerializeData.updateMode;
            //team.frequency = clothParams.solverFrequency;
            team.timeScale = 1.0f;
            team.initScale = cprocess.clothTransformRecord.scale; // 初期スケール
            team.scaleRatio = 1.0f;
            team.negativeScaleSign = 1;
            team.negativeScaleDirection = 1;
            team.negativeScaleChange = 1;
            team.negativeScaleQuaternionValue = 1;
            team.negativeScaleTriangleSign = 1;
            //team.centerWorldPosition = cprocess.clothTransformRecord.position;
            team.animationPoseRatio = cprocess.cloth.SerializeData.animationPoseRatio;
            team.distanceWeight = 1;
            team.componentTransformIndex = MagicaManager.Bone.AddComponentTransform(cprocess.cloth.transform); // コンポーネントTransform
            //cprocess.componentTransformIndex = team.componentTransformIndex; // cprocessにもコピー
            var c = teamDataArray.Add(team);
            int teamId = c.startIndex;

            // 最大チーム数チェック
            if (teamId >= Define.System.MaximumTeamCount)
            {
                Develop.LogError($"Cannot create more than {Define.System.MaximumTeamCount} teams.");
                teamDataArray.Remove(c);
                return 0;
            }

            var wind = new TeamWindData();
            wind.movingWind.time = -Define.System.WindMaxTime;
            teamWindArray.Add(wind);

            // マッピングメッシュ
            teamMappingIndexArray.Add(new FixedList64Bytes<short>());

            // パラメータ
            parameterArray.Add(clothParams);

            // 慣性制約
            // 初期化時のセンターローカル位置を初期化
            var cdata = new InertiaConstraint.CenterData();
            cdata.frameLocalPosition = cprocess.ProxyMeshContainer.shareVirtualMesh.localCenterPosition.Value;
            centerDataArray.Add(cdata);

            clothProcessDict.Add(teamId, cprocess);

            return teamId;
        }

        /// <summary>
        /// チームを解除する
        /// </summary>
        /// <param name="teamId"></param>
        internal void RemoveTeam(int teamId)
        {
            if (isValid == false || teamId == 0)
                return;

            // セルフコリジョン同期解除
            ref var tdata = ref GetTeamDataRef(teamId);
            if (tdata.syncTeamId > 0 && ContainsTeamData(tdata.syncTeamId))
            {
                ref var stdata = ref GetTeamDataRef(tdata.syncTeamId);
                RemoveSyncParent(ref stdata, teamId);
            }

            // 制約データなど解除
            MagicaManager.Bone.RemoveComponentTransform(tdata.componentTransformIndex);

            // チームデータを破棄する
            var c = new DataChunk(teamId);
            teamDataArray.RemoveAndFill(c);
            teamWindArray.RemoveAndFill(c);
            teamMappingIndexArray.RemoveAndFill(c, new FixedList64Bytes<short>());
            parameterArray.Remove(c);
            centerDataArray.Remove(c);

            // チーム作業バッファをクリア
            teamAnchorTransformIndexArray[teamId] = MagicaObjectId.Invalid;
            teamDistanceTransformIndexArray[teamId] = MagicaObjectId.Invalid;

            // カリング解除
            var cprocess = GetClothProcess(teamId);
            if (cprocess != null)
                cameraCullingClothSet.Remove(cprocess.cloth);

            clothProcessDict.Remove(teamId);
        }

        /// <summary>
        /// チームの有効化設定
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="sw"></param>
        public void SetEnable(ClothProcess cprocess, int teamId, bool sw)
        {
            //Debug.Log($"Set Enable:{sw} F:{Time.frameCount}");
            if (isValid == false || teamId == 0)
                return;
            ref var team = ref teamDataArray.GetRef(teamId);

            // シミュレーションリセットの有無
            // （同期先を参照）
            ClothProcess syncProcess = cprocess.SyncTopCloth ? cprocess.SyncTopCloth.Process : cprocess;
            bool isReset = syncProcess.cloth.SerializeData.disableMode == ClothDisableMode.Reset;

            team.flag.SetBits(Flag_Enable, sw);
            if (isReset)
                team.flag.SetBits(Flag_Reset, sw);

            //Debug.Log($"Set Enable:{sw}, isReset:{isReset} F:{Time.frameCount}");

            if (sw)
                enableTeamSet.Add(teamId);
            else
                enableTeamSet.Remove(teamId);

            // 非表示（リセット）の挙動
            if (sw == false && isReset)
            {
                // 無効時には一度のみTransform復元フラグを立てる
                team.flag.SetBits(Flag_RestoreTransformOnlyOnec, true);
                // Disable時は非表示設定にする
                team.flag.SetBits(Flag_CameraCullingInvisible, true);
                cprocess.SetState(ClothProcess.State_CameraCullingInvisible, true);
                cprocess.cameraCullingOldInvisible = true;
            }

            // コライダーの有効状態（内部でコライダートランスフォームの有効状態も設定）
            MagicaManager.Collider.EnableTeamCollider(teamId);

            // センタートランスフォーム
            MagicaManager.Bone.EnableTransform(team.centerTransformIndex, sw);

            // プロキシメッシュ
            MagicaManager.Bone.EnableTransform(team.proxyTransformChunk, sw);
        }

        public bool IsEnable(int teamId)
        {
            return enableTeamSet.Contains(teamId);
        }

        internal void SetSkipWriting(int teamId, bool sw)
        {
            if (isValid == false || teamId == 0)
                return;
            ref var team = ref teamDataArray.GetRef(teamId);
            team.flag.SetBits(Flag_SkipWriting, sw);
        }

        public bool ContainsTeamData(int teamId)
        {
            return teamId >= 0 && clothProcessDict.ContainsKey(teamId);
        }

        public ref TeamData GetTeamDataRef(int teamId)
        {
            return ref teamDataArray.GetRef(teamId);
        }

        public ref FixedList64Bytes<short> GetTeamMappingRef(int teamId)
        {
            return ref teamMappingIndexArray.GetRef(teamId);
        }

        public ref ClothParameters GetParametersRef(int teamId)
        {
            return ref parameterArray.GetRef(teamId);
        }

        internal ref InertiaConstraint.CenterData GetCenterDataRef(int teamId)
        {
            return ref centerDataArray.GetRef(teamId);
        }

        internal ref MappingData GetMappingDataRef(int mindex)
        {
            return ref mappingDataArray.GetRef(mindex);
        }

        public ClothProcess GetClothProcess(int teamId)
        {
            if (clothProcessDict.ContainsKey(teamId))
                return clothProcessDict[teamId];
            else
                return null;
        }

        //=========================================================================================
        static readonly ProfilerMarker teamCameraCullingPreProfiler = new ProfilerMarker("CameraCullingPre");
        static readonly ProfilerMarker teamCameraCullingProfiler = new ProfilerMarker("CameraCullingPost");

        /// <summary>
        /// カメラカリング状態更新（前処理）
        /// 次のフレームの開始時に行うレンダラー判定用のデータを収集する
        /// </summary>
        internal void CameraCullingPreProcess()
        {
            teamCameraCullingPreProfiler.Begin();

            var cm = MagicaManager.Cloth;

            // ここはシミュレーション処理中なのでtdataなどにアクセスできないので注意！
            cameraCullingClothSet.Clear();
            foreach (var cprocess in cm.clothSet)
            {
                if (cprocess.IsEnable == false)
                    continue;
                if (cprocess.IsRunning() == false)
                    continue;


                // 判定クロス。同期時は同期先を見る
                MagicaCloth jugeCloth = cprocess.SyncTopCloth != null ? cprocess.SyncTopCloth : cprocess.cloth;
                CullingSettings jugeSettings = jugeCloth.SerializeData.cullingSettings;
                ClothProcess jugeProcess = jugeCloth.Process;

                // 最終的なカリングモード
                var cullingMode = jugeSettings.cameraCullingMode;
                if (cullingMode == CullingSettings.CameraCullingMode.AnimatorLinkage)
                {
                    if (jugeProcess.interlockingAnimator)
                    {
                        switch (jugeProcess.interlockingAnimator.cullingMode)
                        {
                            case AnimatorCullingMode.AlwaysAnimate:
                                cullingMode = CullingSettings.CameraCullingMode.Off;
                                break;
                            case AnimatorCullingMode.CullCompletely:
                                cullingMode = CullingSettings.CameraCullingMode.Keep;
                                break;
                            case AnimatorCullingMode.CullUpdateTransforms:
                                cullingMode = CullingSettings.CameraCullingMode.Reset;
                                break;
                        }
                    }
                    else
                        cullingMode = CullingSettings.CameraCullingMode.Off;
                }

                // カリング判定
                if (jugeCloth == null || cullingMode == CullingSettings.CameraCullingMode.Off)
                {
                    // カリングは行わない
                    cprocess.cameraCullingAnimator = null;
                    cprocess.cameraCullingRenderers = null;
                }
                else
                {
                    // 参照すべきレンダラー確定
                    Animator jugeAnimator = null;
                    List<Renderer> jugeRenderers = jugeSettings.cameraCullingRenderers;
                    if (jugeSettings.cameraCullingMethod == CullingSettings.CameraCullingMethod.AutomaticRenderer)
                    {
                        jugeAnimator = jugeCloth.Process.interlockingAnimator;
                        jugeRenderers = jugeCloth.Process.interlockingAnimatorRenderers;
                    }
                    cprocess.cameraCullingAnimator = jugeAnimator;
                    cprocess.cameraCullingRenderers = jugeRenderers;
                }
                cprocess.cameraCullingMode = cullingMode;

                cameraCullingClothSet.Add(cprocess.cloth);
            }

            teamCameraCullingPreProfiler.End();
        }

        /// <summary>
        /// カメラカリング状態更新（後処理）
        /// カメラカリングは該当するRendereのisVisibleフラグから判定される
        /// このフラグは前フレームのレンダリング時に設定される
        /// そのため現在フレームの位置は反映されないので注意する（１フレーム遅れる）
        /// </summary>
        internal void CameraCullingPostProcess()
        {
            teamCameraCullingProfiler.Begin();

            var cm = MagicaManager.Cloth;
            cm.ClearVisibleDict();

            foreach (var cloth in cameraCullingClothSet)
            {
                if (cloth == null)
                    continue;

                var cprocess = cloth.Process;

                // 現在の状態
                bool oldInvisible = cprocess.cameraCullingOldInvisible;
                bool invisible;

                if (cprocess.cameraCullingAnimator == null && cprocess.cameraCullingRenderers == null)
                {
                    // カリングなし
                    invisible = false;
                }
                else
                {
                    // レンダラー判定
                    invisible = !cm.CheckVisible(cprocess.cameraCullingAnimator, cprocess.cameraCullingRenderers);
                }

                // 状態変更
                if (oldInvisible != invisible)
                {
                    //Debug.Log($"Camera culling change. oldInvisible:{oldInvisible} -> invisible:{invisible} F:{Time.frameCount}");
                    int teamId = cprocess.TeamId;
                    ref var tdata = ref GetTeamDataRef(teamId);

                    tdata.flag.SetBits(Flag_CameraCullingInvisible, invisible);
                    tdata.flag.SetBits(Flag_CameraCullingKeep, false);

                    // cprocessクラスにもコピーする
                    cprocess.SetState(ClothProcess.State_CameraCullingInvisible, invisible);
                    cprocess.SetState(ClothProcess.State_CameraCullingKeep, false);
                    cprocess.cameraCullingOldInvisible = invisible;

                    if (invisible)
                    {
                        // (表示->非表示)時の振る舞い
                        switch (cprocess.cameraCullingMode)
                        {
                            case CullingSettings.CameraCullingMode.Reset:
                            case CullingSettings.CameraCullingMode.Off:
                                tdata.flag.SetBits(Flag_Reset, true);
                                //Debug.Log($"Camera culling invisible. Reset On");
                                break;
                            case CullingSettings.CameraCullingMode.Keep:
                                tdata.flag.SetBits(Flag_CameraCullingKeep, true);
                                cprocess.SetState(ClothProcess.State_CameraCullingKeep, true);
                                //Debug.Log($"Camera culling invisible. Keep On");
                                break;
                        }
                    }

                    // 対応するレンダーデータに更新を指示する
                    cprocess.UpdateRendererUse();
                }
            }
            cameraCullingClothSet.Clear();

            teamCameraCullingProfiler.End();
        }

        //=========================================================================================
        static readonly ProfilerMarker startClothUpdateComponentProfiler = new ProfilerMarker("StartClothUpdate.Component");

        /// <summary>
        /// 毎フレーム常に実行するチーム更新
        /// - パラメータ反映
        /// - 更新モード反映
        /// - アンカー／距離カリングの参照オブジェクト反映
        /// - 時間の更新と実行回数の算出
        /// </summary>
        internal void AlwaysTeamUpdate()
        {
            var cm = MagicaManager.Cloth;
            var tm = MagicaManager.Time;
            var rm = MagicaManager.Render;
            var bm = MagicaManager.Bone;
            var sm = MagicaManager.Simulation;

            // 作業バッファクリア
            edgeColliderCollisionCount = 0;
            edgeColliderCollisionCountBuff.Value = 0;
            cm.ClearVisibleDict(); // レンダラーの表示判定辞書をクリア
            selfCollisionUpdateSet.Clear();
            teamAnchorTransformIndexArray.SetLength(TeamCount);
            teamDistanceTransformIndexArray.SetLength(TeamCount);
            transformPositionMap.Clear();
            transformRotationMap.Clear();
            cullingDirtyList.Clear();
            batchNormalClothTeamList.Clear();
            batchSplitClothTeamList.Clear();

            // (1)パラメータ反映
            for (int i = 0; i < parameterDirtyList.Count;)
            {
                var cprocess = parameterDirtyList[i];
                if (cprocess == null)
                {
                    parameterDirtyList.RemoveAt(i);
                    continue;
                }
                if (cprocess.IsEnable == false)
                {
                    i++;
                    continue;
                }

                //Develop.DebugLog($"Update Parameters {teamId}");
                // コライダー更新(内部でteamData更新)
                MagicaManager.Collider.UpdateColliders(cprocess);

                // カリング用アニメーターとレンダラー更新
                cprocess.UpdateCullingAnimatorAndRenderers();

                int teamId = cprocess.TeamId;
                ref var tdata = ref GetTeamDataRef(teamId);
                var cloth = cprocess.cloth;

                // 連動アニメーターのインスタンスID
                tdata.interlockingAnimatorId = cprocess.interlockingAnimator != null ? cprocess.interlockingAnimator.GetMagicaId() : MagicaObjectId.Invalid;

                // パラメータ変更
                cprocess.SyncParameters();
                parameterArray[teamId] = cprocess.parameters;
                tdata.originalUpdateMode = cloth.SerializeData.updateMode;
                tdata.updateMode = cloth.SerializeData.updateMode;
                tdata.animationPoseRatio = cloth.SerializeData.animationPoseRatio;
                tdata.flag.SetBits(Flag_Spring, cprocess.clothType == ClothProcess.ClothType.BoneSpring && cprocess.parameters.springConstraint.springPower > 0.0f); // Spring利用フラグ

                // セルフコリジョン更新
                selfCollisionUpdateSet.Add(teamId);

                // 接線モード
                tdata.flag.SetBits(Flag_Tangent, cloth.SerializeData.meshWriteMode == ClothMeshWriteMode.PositionAndNormalTangent);
                cprocess.SetState(ClothProcess.State_UpdateTangent, tdata.flag.IsSet(Flag_Tangent));

                parameterDirtyList.RemoveAt(i);
            }

            // (2)書き込み停止反映
            for (int i = 0; i < skipWritingDirtyList.Count;)
            {
                var cprocess = skipWritingDirtyList[i];
                if (cprocess == null)
                {
                    skipWritingDirtyList.RemoveAt(i);
                    continue;
                }
                if (cprocess.IsEnable == false)
                {
                    i++;
                    continue;
                }

                bool skipWriting = cprocess.IsState(ClothProcess.State_SkipWriting);

                int teamId = cprocess.TeamId;
                ref var tdata = ref GetTeamDataRef(teamId);

                // チームへ反映
                tdata.flag.SetBits(Flag_SkipWriting, skipWriting);

                // RenderDataへ反映
                foreach (var rinfo in cprocess.renderMeshInfoList)
                {
                    var renderData = rm.GetRendererData(rinfo.renderHandle);
                    renderData.UpdateSkipWriting();
                }

                skipWritingDirtyList.RemoveAt(i);
            }

#if true
            // (3A)チーム前処理ジョブ
            // このジョブは相互参照があるので並列化できない
            var job1 = new AlwaysTeamUpdatePreJob()
            {
                teamDataArray = teamDataArray.GetNativeArray(),
                parameterArray = parameterArray.GetNativeArray(),

                comp2SuspendCounterMap = comp2SuspendCounterMap,
                comp2TeamIdMap = comp2TeamIdMap,
                comp2SyncPartnerCompMap = comp2SyncPartnerCompMap,
                comp2SyncTopCompMap = comp2SyncTopCompMap,
                selfCollisionUpdateSet = selfCollisionUpdateSet,
                edgeColliderCollisionCountBuff = edgeColliderCollisionCountBuff,
            };
            var jobHandle1 = job1.Schedule();

            // (3B)コンポーネント座標読み込みジョブ
            // 3Aと並列実行
            var jobHandle2 = bm.ReadComponentTransform(default);

            JobHandle.ScheduleBatchedJobs(); // 即時開始
#endif

            // (4)他のコンポーネントを参照する必要がある処理
            startClothUpdateComponentProfiler.Begin();
            animatorUpdateModeMap.Clear();
            foreach (var cprocess in cm.clothSet)
            {
                if (cprocess.TeamId == 0)
                    continue;

                // 連動アニメーターの更新モード取得
                if (cprocess.interlockingAnimator)
                {
                    MagicaObjectId animatorId = cprocess.interlockingAnimator.GetMagicaId();
                    if (animatorUpdateModeMap.ContainsKey(animatorId) == false)
                    {
                        animatorUpdateModeMap.Add(animatorId, (int)cprocess.interlockingAnimator.updateMode);
                    }
                }

                // 同期時は同期先を見る
                var refCloth = cprocess.SyncTopCloth != null ? cprocess.SyncTopCloth : cprocess.cloth;
                var sdata = refCloth.SerializeData;

#if true
                // アンカー参照オブジェクト
                var anchorTransform = sdata.inertiaConstraint.anchor;
                MagicaObjectId anchorTransformId = anchorTransform != null ? anchorTransform.GetMagicaId() : MagicaObjectId.Invalid;
                if (anchorTransformId.IsValid() && transformPositionMap.ContainsKey(anchorTransformId) == false)
                {
                    // 参照オブジェクトの座標取得
                    transformPositionMap.Add(anchorTransformId, anchorTransform.position);
                    transformRotationMap.Add(anchorTransformId, anchorTransform.rotation);
                }
                if (cprocess.anchorTransformId != anchorTransformId)
                {
                    // 変更あり
                    cprocess.anchorTransformId = anchorTransformId;
                    teamAnchorTransformIndexArray[cprocess.TeamId] = anchorTransformId;
                }

                // 距離カリング参照オブジェクト
                MagicaObjectId distanceObjectId = sdata.cullingSettings.distanceCullingReferenceObject != null ? sdata.cullingSettings.distanceCullingReferenceObject.GetMagicaId() : MagicaObjectId.Invalid;
                if (distanceObjectId.IsValid() && transformPositionMap.ContainsKey(distanceObjectId) == false)
                {
                    // 参照オブジェクトの座標取得
                    transformPositionMap.Add(distanceObjectId, sdata.cullingSettings.distanceCullingReferenceObject.transform.position);
                }
                if (cprocess.distanceReferenceObjectId != distanceObjectId)
                {
                    // 変更あり
                    cprocess.distanceReferenceObjectId = distanceObjectId;
                    teamDistanceTransformIndexArray[cprocess.TeamId] = distanceObjectId;
                }
#endif
            }

            // メインカメラ座標
            bool hasMainCamera = Camera.main != null;
            float3 mainCameraPosition = Camera.main ? Camera.main.transform.position : 0;
            //transformPositionMap.Add(0, mainCameraPosition);
            transformPositionMap.Add(MagicaObjectId.Invalid, mainCameraPosition);
            startClothUpdateComponentProfiler.End();

            // チーム前処理ジョブ待ち
            jobHandle1.Complete();
            jobHandle2.Complete();
            edgeColliderCollisionCount = edgeColliderCollisionCountBuff.Value;

            // (5)セルフコリジョンのフラグやバッファ更新
            if (selfCollisionUpdateSet.Count() > 0)
            {
                foreach (var teamId in selfCollisionUpdateSet)
                {
                    sm.selfCollisionConstraint.UpdateTeam(teamId);
                }
                selfCollisionUpdateSet.Clear();
            }

            // (6)チーム後処理ジョブ
#if true
            if (ActiveTeamCount > 0)
            {
                // フレーム更新時間
                float deltaTime = Time.deltaTime;
                float fixedDeltaTime = tm.FixedUpdateCount * Time.fixedDeltaTime;
                float unscaledDeltaTime = Time.unscaledDeltaTime;

                //Debug.Log($"DeltaTime:{deltaTime}, FixedDeltaTime:{fixedDeltaTime}, simulationDeltaTime:{MagicaManager.Time.SimulationDeltaTime}, maxDeltaTime:{MagicaManager.Time.MaxDeltaTime}");
                //Debug.Log($"DeltaTime:{deltaTime}, FixedDeltaTime:{fixedDeltaTime}, simulationDeltaTime:{MagicaManager.Time.SimulationDeltaTime}");

                // このJobは即時実行させる
                var postJob = new AlwaysTeamUpdatePostJob()
                {
                    teamCount = TeamCount,
                    unityFrameDeltaTime = deltaTime,
                    unityFrameFixedDeltaTime = fixedDeltaTime,
                    unityFrameUnscaledDeltaTime = unscaledDeltaTime,
                    globalTimeScale = tm.GlobalTimeScale,
                    simulationDeltaTime = tm.SimulationDeltaTime,
                    //maxDeltaTime = MagicaManager.Time.MaxDeltaTime,
                    maxSimmulationCountPerFrame = tm.maxSimulationCountPerFrame,
                    splitProxyMeshVertexCount = sm.splitProxyMeshVertexCount,

                    //maxUpdateCount = maxUpdateCount,
                    teamStatus = teamStatus,
                    teamDataArray = teamDataArray.GetNativeArray(),
                    parameterArray = parameterArray.GetNativeArray(),
                    centerDataArray = centerDataArray.GetNativeArray(),

                    componentPositionArray = bm.componentPositionArray.GetNativeArray(),
                    componentMinScaleArray = bm.componentMinScaleArray.GetNativeArray(),
                    hasMainCamera = hasMainCamera,

                    comp2TeamIdMap = comp2TeamIdMap,
                    comp2SyncTopCompMap = comp2SyncTopCompMap,
                    animatorUpdateModeMap = animatorUpdateModeMap,
                    teamAnchorTransformIndexArray = teamAnchorTransformIndexArray.GetNativeArray(),
                    teamDistanceTransformIndexArray = teamDistanceTransformIndexArray.GetNativeArray(),
                    transformPositionMap = transformPositionMap,
                    transformRotationMap = transformRotationMap,
                    cullingDirtyList = cullingDirtyList,

                    batchNormalClothTeamList = batchNormalClothTeamList,
                    batchSplitClothTeamList = batchSplitClothTeamList,
                };
                postJob.Run();

                // 距離カリング反映
                if (cullingDirtyList.Length > 0)
                {
                    foreach (int teamId in cullingDirtyList)
                    {
                        ref var tdata = ref GetTeamDataRef(teamId);
                        var cprocess = GetClothProcess(teamId);

                        bool invisible = tdata.IsDistanceCullingInvisible;

                        // cprocessクラスにもコピーする
                        // 距離カリングでは表示/非表示切替時に常にリセットする
                        // またカメラカリングのKeepは強制解除する
                        cprocess.SetState(ClothProcess.State_DistanceCullingInvisible, invisible);
                        cprocess.SetState(ClothProcess.State_CameraCullingKeep, false);

                        // 対応するレンダーデータに更新を指示する
                        cprocess.UpdateRendererUse();
                    }
                }
            }
#endif
        }

        [BurstCompile]
        unsafe struct AlwaysTeamUpdatePreJob : IJob
        {
            public NativeArray<TeamData> teamDataArray;
            public NativeArray<ClothParameters> parameterArray;

            // work
            public NativeParallelHashMap<MagicaObjectId, int> comp2SuspendCounterMap;
            public NativeParallelHashMap<MagicaObjectId, int> comp2TeamIdMap;
            public NativeParallelHashMap<MagicaObjectId, MagicaObjectId> comp2SyncPartnerCompMap;
            public NativeParallelHashMap<MagicaObjectId, MagicaObjectId> comp2SyncTopCompMap;
            public NativeParallelHashSet<int> selfCollisionUpdateSet;
            public NativeReference<int> edgeColliderCollisionCountBuff;

            public void Execute()
            {
                int edgeColliderCollisionCount = 0;
                TeamData* tt = (TeamData*)teamDataArray.GetUnsafePtr();

                foreach (var kv in comp2TeamIdMap)
                {
                    int teamId = kv.Value;
                    if (teamId == 0)
                        continue;

                    MagicaObjectId compId = kv.Key;
                    ref var tdata = ref *(tt + teamId);

                    // 作業フラグをクリア -------------------------------------
                    tdata.flag.SetBits(Flag_RestoreTransformOnlyOnec, false);

                    // 動作判定 ----------------------------------------------------
                    if (tdata.flag.IsSet(Flag_Enable) == false)
                        continue;

                    // 同期まち判定 -------------------------------------------------
                    bool syncSuspend = true;
                    if (comp2SuspendCounterMap.ContainsKey(compId) == false || comp2SuspendCounterMap[compId] == 0)
                        syncSuspend = false;

                    // 同期相手の有効状態
                    if (comp2SyncPartnerCompMap.ContainsKey(compId))
                    {
                        MagicaObjectId syncCompId = comp2SyncPartnerCompMap[compId];
                        if (comp2TeamIdMap.ContainsKey(syncCompId))
                        {
                            int syncTeamId = comp2TeamIdMap[syncCompId];
                            if (syncTeamId > 0)
                            {
                                ref var syncTeamData = ref *(tt + syncTeamId);
                                int syncSuspendCounter = comp2SuspendCounterMap.ContainsKey(syncCompId) ? comp2SuspendCounterMap[syncCompId] : 0;
                                if (syncTeamData.IsEnable && syncSuspendCounter == 0)
                                    syncSuspend = false; // 相手も処理可能状態
                            }
                        }
                    }
                    tdata.flag.SetBits(Flag_SyncSuspend, syncSuspend);

                    // 同期待ちなら実行できない
                    if (tdata.flag.IsSet(Flag_SyncSuspend))
                        continue;

                    // チーム同期 --------------------------------------------------
                    int oldSyncTeamId = tdata.syncTeamId;
                    tdata.syncTeamId = 0;
                    if (comp2SyncPartnerCompMap.ContainsKey(compId))
                    {
                        MagicaObjectId syncCompId = comp2SyncPartnerCompMap[compId];
                        if (comp2TeamIdMap.ContainsKey(syncCompId))
                        {
                            int syncTeamId = comp2TeamIdMap[syncCompId];
                            tdata.syncTeamId = syncTeamId;
                        }
                    }
                    tdata.flag.SetBits(Flag_Synchronization, tdata.syncTeamId != 0);
                    tdata.syncCenterTransformIndex = 0;
                    if (oldSyncTeamId != tdata.syncTeamId)
                    {
                        // 変更あり！
                        // 同期解除
                        if (oldSyncTeamId > 0)
                        {
                            //Debug.Log($"Desynchronization! (1) {teamId}");
                            ref var syncTeamData = ref *(tt + oldSyncTeamId);
                            syncTeamData.syncParentTeamId.MC2RemoveItemAtSwapBack(teamId);
                        }

                        // 同期変更
                        if (tdata.syncTeamId != 0)
                        {
                            ref var syncTeamData = ref *(tt + tdata.syncTeamId);

                            // 相手に自身を登録
                            // 最大７まで
                            if (syncTeamData.syncParentTeamId.Length == syncTeamData.syncParentTeamId.Capacity)
                            {
                                //Debug.LogWarning($"Synchronous team number limit!");
                            }
                            else
                                syncTeamData.syncParentTeamId.Add(teamId);

                            // 時間リセットフラグクリア
                            tdata.flag.SetBits(Flag_TimeReset, false);

                            //Debug.Log($"Synchronization! {teamId}->{tdata.syncTeamId}");
                        }
                        else
                        {
                            // 同期解除
                            //cloth.SerializeData.selfCollisionConstraint.syncPartner = null;
                            //Debug.Log($"Desynchronization! (2) {teamId}");
                        }

                        // セルフコリジョン更新セットに追加
                        selfCollisionUpdateSet.Add(teamId);
                    }

                    // 時間とパラメータの同期
                    var clothParam = parameterArray[teamId];

                    // トップ階層の同期クロスを見る
                    int syncTopTeamId = 0;
                    if (comp2SyncTopCompMap.ContainsKey(compId))
                    {
                        MagicaObjectId syncTopCompId = comp2SyncTopCompMap[compId];
                        if (comp2TeamIdMap.ContainsKey(syncTopCompId))
                        {
                            syncTopTeamId = comp2TeamIdMap[syncTopCompId];
                            ref var syncTeamData = ref *(tt + syncTopTeamId);

                            // 時間同期
                            if (syncTeamData.IsValid)
                            {
                                tdata.originalUpdateMode = syncTeamData.originalUpdateMode;
                                tdata.updateMode = syncTeamData.updateMode;
                                //tdata.frequency = syncTeamData.frequency;
                                tdata.time = syncTeamData.time;
                                tdata.oldTime = syncTeamData.oldTime;
                                tdata.nowUpdateTime = syncTeamData.nowUpdateTime;
                                tdata.oldUpdateTime = syncTeamData.oldUpdateTime;
                                tdata.frameUpdateTime = syncTeamData.frameUpdateTime;
                                tdata.frameOldTime = syncTeamData.frameOldTime;
                                tdata.timeScale = syncTeamData.timeScale;
                                tdata.updateCount = syncTeamData.updateCount;
                                tdata.frameInterpolation = syncTeamData.frameInterpolation;
                                tdata.skipCount = syncTeamData.skipCount;
                                //Develop.DebugLog($"Team time sync:{teamId}->{syncCloth.Process.TeamId}");
                            }

                            // パラメータ同期
                            // 同期中は一部のパラメータを連動させる
                            var syncClothParam = parameterArray[syncTopTeamId];
                            clothParam.inertiaConstraint.anchorInertia = syncClothParam.inertiaConstraint.anchorInertia;
                            clothParam.inertiaConstraint.worldInertia = syncClothParam.inertiaConstraint.worldInertia;
                            clothParam.inertiaConstraint.movementInertiaSmoothing = syncClothParam.inertiaConstraint.movementInertiaSmoothing;
                            clothParam.inertiaConstraint.movementSpeedLimit = syncClothParam.inertiaConstraint.movementSpeedLimit;
                            clothParam.inertiaConstraint.rotationSpeedLimit = syncClothParam.inertiaConstraint.rotationSpeedLimit;
                            clothParam.inertiaConstraint.teleportMode = syncClothParam.inertiaConstraint.teleportMode;
                            clothParam.inertiaConstraint.teleportDistance = syncClothParam.inertiaConstraint.teleportDistance;
                            clothParam.inertiaConstraint.teleportRotation = syncClothParam.inertiaConstraint.teleportRotation;
                            parameterArray[teamId] = clothParam;

                            // 同期先のセンタートランスフォームインデックスを記録
                            tdata.syncCenterTransformIndex = syncTeamData.centerTransformIndex;
                        }
                    }

                    // 集計まわり
                    if (clothParam.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge)
                        edgeColliderCollisionCount += tdata.EdgeCount;
                }

                edgeColliderCollisionCountBuff.Value = edgeColliderCollisionCount;
            }
        }

        [BurstCompile]
        struct AlwaysTeamUpdatePostJob : IJob
        {
            public int teamCount;
            public float unityFrameDeltaTime;
            public float unityFrameFixedDeltaTime;
            public float unityFrameUnscaledDeltaTime;
            public float globalTimeScale;
            public float simulationDeltaTime;
            public int maxSimmulationCountPerFrame;
            public int splitProxyMeshVertexCount;

            public NativeReference<int4> teamStatus;
            public NativeArray<TeamData> teamDataArray;
            [Unity.Collections.ReadOnly]
            public NativeArray<ClothParameters> parameterArray;
            public NativeArray<InertiaConstraint.CenterData> centerDataArray;

            public NativeArray<float3> componentPositionArray;
            public NativeArray<float> componentMinScaleArray;
            public bool hasMainCamera;

            // work
            public NativeParallelHashMap<MagicaObjectId, int> comp2TeamIdMap;
            public NativeParallelHashMap<MagicaObjectId, MagicaObjectId> comp2SyncTopCompMap;
            public NativeParallelHashMap<MagicaObjectId, int> animatorUpdateModeMap;
            public NativeArray<MagicaObjectId> teamAnchorTransformIndexArray;
            public NativeArray<MagicaObjectId> teamDistanceTransformIndexArray;
            public NativeParallelHashMap<MagicaObjectId, float3> transformPositionMap;
            public NativeParallelHashMap<MagicaObjectId, quaternion> transformRotationMap;
            public NativeList<int> cullingDirtyList;

            public NativeList<int> batchNormalClothTeamList;
            public NativeList<int> batchSplitClothTeamList;

            public void Execute()
            {
                int maxCount = 0;
                int splitPointCollisionCount = 0;
                int splitEdgeCollisionCount = 0;
                int splitSelfCollisionCount = 0;

                for (int teamId = 1; teamId < teamCount; teamId++)
                {
                    var tdata = teamDataArray[teamId];
                    MagicaObjectId compId = tdata.componentId;
                    if (tdata.IsEnable == false)
                        continue;
                    if (tdata.flag.IsSet(Flag_SyncSuspend))
                        continue;

                    var param = parameterArray[teamId];

                    // 動作検証
                    // 極小スケールによる機能停止
                    tdata.flag.SetBits(Flag_ScaleSuspent, componentMinScaleArray[tdata.componentTransformIndex] < 1e-06f);
                    if (tdata.flag.IsSet(Flag_ScaleSuspent))
                    {
                        teamDataArray[teamId] = tdata;
                        continue;
                    }

                    // アンカー
                    MagicaObjectId anchorTransformIndex = teamAnchorTransformIndexArray[teamId];
                    bool oldAnchor = tdata.flag.IsSet(Flag_Anchor);
                    bool newAnchor = anchorTransformIndex.IsValid();
                    tdata.flag.SetBits(Flag_Anchor, newAnchor);
                    tdata.flag.SetBits(Flag_AnchorReset, oldAnchor != newAnchor);
                    var cdata = centerDataArray[teamId];
                    cdata.anchorPosition = anchorTransformIndex.IsValid() ? transformPositionMap[anchorTransformIndex] : float3.zero;
                    cdata.anchorRotation = anchorTransformIndex.IsValid() ? transformRotationMap[anchorTransformIndex] : quaternion.identity;
                    centerDataArray[teamId] = cdata;

                    // 距離カリング判定
                    DistanceCullingUpdate(teamId, ref tdata, ref param);

                    if (tdata.IsCullingInvisible)
                    {
                        teamDataArray[teamId] = tdata;
                        continue;
                    }

                    // 更新モード
                    int syncTopTeamId = 0;
                    if (comp2SyncTopCompMap.ContainsKey(compId))
                    {
                        MagicaObjectId syncTopCompId = comp2SyncTopCompMap[compId];
                        if (comp2TeamIdMap.ContainsKey(syncTopCompId))
                        {
                            syncTopTeamId = comp2TeamIdMap[syncTopCompId];
                        }
                    }
                    if (tdata.originalUpdateMode == ClothUpdateMode.AnimatorLinkage || syncTopTeamId > 0)
                    {
                        var originalUpdateMode = tdata.originalUpdateMode;
                        MagicaObjectId animatorId = tdata.interlockingAnimatorId;
                        if (syncTopTeamId > 0)
                        {
                            var syncTeamData = teamDataArray[syncTopTeamId];
                            originalUpdateMode = syncTeamData.originalUpdateMode;
                            animatorId = syncTeamData.interlockingAnimatorId;
                        }

                        switch (originalUpdateMode)
                        {
                            case ClothUpdateMode.Normal:
                            case ClothUpdateMode.UnityPhysics:
                            case ClothUpdateMode.Unscaled:
                                tdata.updateMode = originalUpdateMode;
                                break;
                            case ClothUpdateMode.AnimatorLinkage:
                                if (animatorUpdateModeMap.ContainsKey(animatorId))
                                {
                                    AnimatorUpdateMode aniUpdateMode = (AnimatorUpdateMode)animatorUpdateModeMap[animatorId];
                                    switch (aniUpdateMode)
                                    {
                                        case AnimatorUpdateMode.Normal:
                                            tdata.updateMode = ClothUpdateMode.Normal;
                                            break;
#if UNITY_2023_1_OR_NEWER
                                        case AnimatorUpdateMode.Fixed:
                                            tdata.updateMode = ClothUpdateMode.UnityPhysics;
                                            break;
#else
                                        case AnimatorUpdateMode.AnimatePhysics:
                                            tdata.updateMode = ClothUpdateMode.UnityPhysics;
                                            break;
#endif
                                        case AnimatorUpdateMode.UnscaledTime:
                                            tdata.updateMode = ClothUpdateMode.Unscaled;
                                            break;
                                        default:
                                            //Develop.DebugLogWarning($"[{cloth.name}] Unknown Animator UpdateMode:{interlockingAnimator.updateMode}");
                                            tdata.updateMode = ClothUpdateMode.Normal;
                                            break;
                                    }
                                }
                                else
                                    tdata.updateMode = ClothUpdateMode.Normal;
                                break;
                            default:
                                //Develop.LogError($"[{cloth.name}] Unknown Cloth Update Mode:{cloth.SerializeData.updateMode}");
                                tdata.updateMode = ClothUpdateMode.Normal;
                                break;
                        }
                    }

                    // 時間リセット
                    if (tdata.flag.IsSet(Flag_TimeReset))
                    {
                        tdata.time = 0;
                        tdata.oldTime = 0;
                        tdata.nowUpdateTime = 0;
                        tdata.oldUpdateTime = 0;
                        tdata.frameUpdateTime = 0;
                        tdata.frameOldTime = 0;
                    }

                    // 更新時間
                    //Debug.Log($"Team [{teamId}] updateMode:{(int)tdata.updateMode}");
                    float frameDeltaTime = tdata.IsFixedUpdate ? unityFrameFixedDeltaTime : (tdata.IsUnscaled ? unityFrameUnscaledDeltaTime : unityFrameDeltaTime);
                    tdata.frameDeltaTime = frameDeltaTime;
                    float deltaTime = frameDeltaTime;

                    // タイムスケール
                    float timeScale = tdata.timeScale * (tdata.IsUnscaled ? 1.0f : globalTimeScale);
                    timeScale = tdata.flag.IsSet(Flag_SyncSuspend) ? 0.0f : timeScale;
                    tdata.nowTimeScale = timeScale; // 最終計算用タイムスケール

                    // 加算時間
                    float addTime = deltaTime * timeScale; // 今回の加算時間

                    // 時間を加算
                    float time = tdata.time + addTime;
                    //Debug.Log($"[{i}] time:{time}, addTime:{addTime}, timeScale:{timeScale}, suspend:{tdata.flag.IsSet(Flag_Suspend)}");
                    float interval = time - tdata.nowUpdateTime;

                    // 今回の予定更新回数
                    int updateCount = (int)(interval / simulationDeltaTime);

                    // 今回の更新回数（最大更新回数まで）
                    tdata.updateCount = math.min(updateCount, maxSimmulationCountPerFrame);

                    // 今回のスキップ回数（最大更新回数超過分）
                    tdata.skipCount = updateCount - tdata.updateCount;
                    if (tdata.skipCount > 0)
                    {
                        // スキップ発生時はスキップ時間を無かったものとする
                        time = time - simulationDeltaTime * tdata.skipCount;
                    }

                    if (tdata.updateCount > 0 && addTime == 0.0f)
                    {
                        // SimulationDeltaTime加算の誤差が発生！
                        // ステップ毎のnowUpdateTime += tdata.SimulationDeltaTimeが誤差を蓄積
                        // その結果addTime=0でもintervalが一回分となり処理がまわってしまう
                        // こうなると時間補間関連で0除算が発生して数値が壊れる
                        // 誤差を修正する
                        tdata.updateCount = 0;
                        tdata.skipCount = 0;
                        tdata.nowUpdateTime = time - simulationDeltaTime + 0.0001f;
                    }

                    // 時間まわり更新
                    if (tdata.updateCount > 0)
                    {
                        // 更新時のフレーム開始時間
                        tdata.frameOldTime = tdata.frameUpdateTime;
                        tdata.frameUpdateTime = time;

                        // 前回の更新時間
                        tdata.oldUpdateTime = tdata.nowUpdateTime;

                        //Debug.Log($"TeamUpdate!:{i}");
                    }
                    tdata.oldTime = tdata.time;
                    tdata.time = time;

                    // シミュレーション実行フラグ
                    tdata.flag.SetBits(Flag_Running, tdata.updateCount > 0);

                    teamDataArray[teamId] = tdata;

                    // 全体の最大実行回数
                    maxCount = math.max(maxCount, tdata.updateCount);

                    //Debug.Log($"[{teamId}] updateCount:{tdata.updateCount}, skipCount:{tdata.skipCount}, addtime:{addTime}, t.time:{tdata.time}, t.oldtime:{tdata.oldTime}, timeScale:{tdata.timeScale}");

                    // ジョブ情報の作成
                    bool isSplitTeam = false;
                    bool isSelfCollisionJob = tdata.flag.IsSet(Flag_Self_PointPrimitive)
                        || tdata.flag.IsSet(Flag_Self_EdgePrimitive)
                        || tdata.flag.IsSet(Flag_Self_TrianglePrimitive);
                    if (isSelfCollisionJob)
                    {
                        // セルフコリジョン
                        // Splitジョブ
                        batchSplitClothTeamList.Add(teamId);
                        isSplitTeam = true;
                        splitSelfCollisionCount++;
                    }
                    else
                    {
                        // セルフコリジョンなしではプロキシメッシュの頂点数が一定以上なら分割する
                        if (tdata.ParticleCount >= splitProxyMeshVertexCount)
                        {
                            // Splitジョブ
                            batchSplitClothTeamList.Add(teamId);
                            isSplitTeam = true;
                        }
                        else
                        {
                            // Normalジョブ
                            batchNormalClothTeamList.Add(teamId);
                        }
                    }

                    // SplitチームのPoint/Edgeコリジョンの有無
                    if (isSplitTeam && tdata.UseColliderCount > 0)
                    {
                        if (param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Point)
                            splitPointCollisionCount++;
                        if (param.colliderCollisionConstraint.mode == ColliderCollisionConstraint.Mode.Edge)
                            splitEdgeCollisionCount++;
                    }
                }

                teamStatus.Value = new int4(maxCount, splitPointCollisionCount, splitEdgeCollisionCount, splitSelfCollisionCount);
            }

            void DistanceCullingUpdate(int teamId, ref TeamData tdata, ref ClothParameters param)
            {
                // 現在の状態
                bool oldInvisible = tdata.IsDistanceCullingInvisible;
                bool invisible;

                // 距離カリングの有無
                if (param.culling.useDistanceCulling)
                {
                    // 距離カリング有効
                    // 距離判定
                    MagicaObjectId transformId = teamDistanceTransformIndexArray[teamId];
                    if (transformPositionMap.ContainsKey(transformId))
                    {
                        // 現在コンポーネント位置(pos)は同期を取っていない
                        // これは２つのコンポーネントが同じ階層と同じ位置であることを想定しているためである
                        // そのため２つのコンポーネントの位置がずれていると予期せぬ動作不良を起こす危険性がある
                        float3 pos = componentPositionArray[tdata.componentTransformIndex];

                        // 参照オブジェクト位置(spos)は同期されている
                        float3 spos = transformPositionMap[transformId];

                        //Debug.Log($"[{teamId}] pos:{pos}, spos:{spos}");
                        //Debug.Log($"[{teamId}] componentTransformIndex:{tdata.componentTransformIndex}, objTransformId:{transformId}");

                        float dist = math.distance(pos, spos);
                        float cullingDist = param.culling.distanceCullingLength;
                        if (hasMainCamera == false && tdata.componentTransformIndex == 0)
                            cullingDist = 1000000.0f; // メインカメラnull時の安全対策
                        invisible = dist >= cullingDist;

                        // フェードウエイト
                        float fadeDist = math.saturate(param.culling.distanceCullingFadeRatio) * cullingDist;
                        tdata.distanceWeight = 1.0f - math.saturate(math.unlerp(cullingDist - fadeDist, cullingDist, dist));
                    }
                    else
                    {
                        // 距離カリング無効
                        invisible = false;
                        tdata.distanceWeight = 1.0f;
                    }
                }
                else
                {
                    // 距離カリング無効
                    invisible = false;
                    tdata.distanceWeight = 1.0f;
                }

                // 状態変更
                if (oldInvisible != invisible)
                {
                    tdata.flag.SetBits(Flag_DistanceCullingInvisible, invisible);
                    //Debug.Log($"Change distance culling invisible:({oldInvisible}) -> ({invisible})");

                    // 距離カリングでは表示/非表示切替時に常にリセットする
                    // またカメラカリングのKeepは強制解除する
                    tdata.flag.SetBits(Flag_Reset, true);
                    tdata.flag.SetBits(Flag_CameraCullingKeep, false);

                    // メインスレッド処理用のリストに追加する
                    cullingDirtyList.Add(teamId);
                }
            }
        }

        internal void RemoveSyncParent(ref TeamData tdata, int parentTeamId)
        {
            tdata.syncParentTeamId.MC2RemoveItemAtSwapBack(parentTeamId);
        }

        //=========================================================================================
        /// <summary>
        /// ここに登録されるのはClothコンポーネントがDisable時に初期化されたプロセス
        /// これらのプロセスはマネージャ側で消滅が監視される
        /// </summary>
        HashSet<ClothProcess> monitoringProcessSet = new HashSet<ClothProcess>();
        List<ClothProcess> disposeProcessList = new List<ClothProcess>();

        internal void AddMonitoringProcess(ClothProcess cprocess)
        {
            Develop.Assert(cprocess != null);
            monitoringProcessSet.Add(cprocess);
        }

        internal void RemoveMonitoringProcess(ClothProcess cprocess)
        {
            Develop.Assert(cprocess != null);
            if (monitoringProcessSet.Contains(cprocess))
                monitoringProcessSet.Remove(cprocess);
        }

        /// <summary>
        /// コンポーネントDisable時に初期化されたClothProcessを監視し消滅していたらマネージャ側からメモリを開放する
        /// </summary>
        /// <param name="force"></param>
        void MonitoringProcess(bool force)
        {
            disposeProcessList.Clear();
            foreach (var cprocess in monitoringProcessSet)
            {
                if (cprocess.cloth == null || force)
                {
                    // 消滅を検出
                    Develop.DebugLog($"Detection MagicaCloth destroy!");
                    disposeProcessList.Add(cprocess);
                }
            }

            // 消滅したClothを破棄する
            if (disposeProcessList.Count > 0)
            {
                disposeProcessList.ForEach(cprocess => cprocess.Dispose());
                disposeProcessList.Clear();
            }
            if (force)
            {
                monitoringProcessSet.Clear();
            }
        }

        void MonitoringProcessUpdate() => MonitoringProcess(false);

        //=========================================================================================
        // Simulation
        //=========================================================================================
        /// <summary>
        /// チームごとのセンター姿勢の決定と慣性用の移動量計算
        /// および風の影響を計算
        /// </summary>
        internal static void SimulationCalcCenterAndInertiaAndWind(
            float simulationDeltaTime,

            // team
            int teamId,
            ref TeamData tdata,
            ref InertiaConstraint.CenterData cdata,
            ref TeamWindData windData,
            ref ClothParameters param,

            // vmesh
            in NativeArray<float3> positions,
            in NativeArray<quaternion> rotations,
            in NativeArray<quaternion> vertexBindPoseRotations,

            // inertia
            in NativeArray<ushort> fixedArray,

            // transform
            in NativeArray<float3> transformPositionArray,
            in NativeArray<quaternion> transformRotationArray,
            in NativeArray<float3> transformScaleArray,

            // wind
            int windZoneCount,
            in NativeArray<WindManager.WindData> windDataArray
            )
        {
            // ■コンポーネントトランスフォーム同期
            // 同期中は同期先のコンポーネントトランスフォームからワールド慣性を計算する
            int centerTransformIndex = (tdata.syncTeamId != 0 && tdata.flag.IsSet(Flag_Synchronization)) ? tdata.syncCenterTransformIndex : cdata.centerTransformIndex;

            // ■コンポーネント姿勢
            float3 componentWorldPos = transformPositionArray[centerTransformIndex];
            quaternion componentWorldRot = transformRotationArray[centerTransformIndex];
            float3 componentWorldScl = transformScaleArray[cdata.centerTransformIndex]; // ただしスケールは同期させない！(v2.11.1)
            cdata.componentWorldPosition = componentWorldPos;
            cdata.componentWorldRotation = componentWorldRot;
            cdata.componentWorldScale = componentWorldScl;
            //Debug.Log($"componentWorldPos:{componentWorldPos}, componentWorldRot:{componentWorldRot.value}");

            // コンポーネントスケール倍率
            float componentScaleRatio = math.length(componentWorldScl) / math.length(tdata.initScale);

            // ■マイナススケール
            // マイナススケールの場合は計算に必要なデータを予め作成しておく
            float3 oldInverseScaleDirection = tdata.negativeScaleDirection;
            tdata.negativeScaleDirection = math.sign(componentWorldScl); // 各スケールの方向(1/-1)
            tdata.negativeScaleChange = oldInverseScaleDirection * tdata.negativeScaleDirection; // 今回スケール反転された方向(1:変化なし, -1:反転あり)
                                                                                                 //Debug.Log($"inverseScaleChange:{tdata.inverseScaleChange}");
            if (componentWorldScl.x < 0 || componentWorldScl.y < 0 || componentWorldScl.z < 0)
            {
                tdata.negativeScaleSign = -1; // マイナススケール時は(-1)
                tdata.negativeScaleQuaternionValue = new float4(-math.sign(componentWorldScl), 1); // 回転反転用
                tdata.negativeScaleTriangleSign.x = (componentWorldScl.x < 0 || componentWorldScl.z < 0) ? -1 : 1; // TriangleBendingの法線フリップフラグ
                tdata.negativeScaleTriangleSign.y = componentWorldScl.x < 0 ? -1 : 1; // TriangleBendingの接線フリップフラグ
                tdata.flag.SetBits(Flag_NegativeScale, true);
            }
            else
            {
                tdata.negativeScaleSign = 1;
                tdata.negativeScaleQuaternionValue = 1;
                tdata.negativeScaleTriangleSign = 1;
                tdata.flag.SetBits(Flag_NegativeScale, false);
            }
            // 以前とスケール方向が変わっていたいたら軸反転テレポートを行う
            if (oldInverseScaleDirection.Equals(tdata.negativeScaleDirection) == false)
            {
                // 軸反転テレポート
                // 本体がスケール反転するためシミュレーションに影響が出ないように必要な座標系を同様に反転させる
                // 一旦クロスローカル空間に戻し軸反転してからワールドに書き戻す
                tdata.flag.SetBits(Flag_NegativeScaleTeleport, true);
                //Debug.LogWarning($"Negative Scale detection!");

                // コンポーネント反転用マトリックス
                float4x4 nowComponentLW = float4x4.TRS(componentWorldPos, componentWorldRot, componentWorldScl);
                float4x4 oldComponentLW = float4x4.TRS(cdata.oldComponentWorldPosition, cdata.oldComponentWorldRotation, cdata.oldComponentWorldScale);
                float4x4 componentNegativeM = math.mul(nowComponentLW, math.inverse(oldComponentLW));

                // コンポーネント空間のものを反転させる
                // Transformに関するものは回転を反転させる必要はない
                cdata.oldComponentWorldPosition = MathUtility.TransformPoint(cdata.oldComponentWorldPosition, componentNegativeM);
                cdata.oldComponentWorldScale = componentWorldScl; // スケールはリセット
                cdata.oldAnchorPosition = MathUtility.TransformPoint(cdata.oldAnchorPosition, componentNegativeM);
                cdata.smoothingVelocity = MathUtility.TransformVector(cdata.smoothingVelocity, componentNegativeM);
            }

            float3 oldComponentWorldPosition = cdata.oldComponentWorldPosition;
            quaternion oldComponentWorldRotation = cdata.oldComponentWorldRotation;
            float3 oldComponentWorldScale = cdata.oldComponentWorldScale;

            // ■クロスセンター位置
            var centerWorldPos = componentWorldPos;
            var centerWorldRot = componentWorldRot;

            // 固定点リストがある場合は固定点の姿勢から算出する、ない場合はクロストランスフォームを使用する
            if (tdata.fixedDataChunk.IsValid)
            {
                float3 cen = 0;
                float3 nor = 0;
                float3 tan = 0;

                int v_start = tdata.proxyCommonChunk.startIndex;

                int fcnt = tdata.fixedDataChunk.dataLength;
                int fstart = tdata.fixedDataChunk.startIndex;
                for (int i = 0; i < fcnt; i++)
                {
                    var l_findex = fixedArray[fstart + i];
                    int vindex = l_findex + v_start;

                    cen += positions[vindex];

                    var rot = rotations[vindex];

                    // マイナススケール
                    if (tdata.negativeScaleSign < 0)
                    {
                        MathUtility.ToNormalTangent(rot, out float3 n, out float3 t);
                        rot = MathUtility.ToRotation(-n, -t);
                    }

                    // 頂点バインドポーズを乗算して初期姿勢の一定方向に合わせる
                    rot = math.mul(rot, vertexBindPoseRotations[vindex]);

                    nor += MathUtility.ToNormal(rot);
                    tan += MathUtility.ToTangent(rot);
                }

                // マイナススケール
                nor *= (tdata.negativeScaleDirection.x < 0 || tdata.negativeScaleDirection.z < 0) ? -1 : 1;
                tan *= (tdata.negativeScaleDirection.x < 0 || tdata.negativeScaleDirection.y < 0) ? -1 : 1;

#if MC2_DEBUG
                    Develop.Assert(math.length(nor) > 0.0f);
                    Develop.Assert(math.length(tan) > 0.0f);
#endif
                centerWorldPos = cen / fcnt;
                centerWorldRot = MathUtility.ToRotation(math.normalize(nor), math.normalize(tan)); // 単位化必須
            }
            var wtol = MathUtility.WorldToLocalMatrix(centerWorldPos, centerWorldRot, componentWorldScl);
            //Debug.Log($"centerWorldPos:{centerWorldPos}, centerWorldRot:{centerWorldRot.value}");
            //Debug.Log($"centerWorldRot nor:{math.mul(centerWorldRot, math.up())}, tan:{math.mul(centerWorldRot, math.forward())}, bin:{math.mul(centerWorldRot, math.right())}");

            // ■マイナススケール
            if (tdata.IsNegativeScaleTeleport)
            {
                // センター反転用変換マトリックスを計算(2)
                float4x4 nowLW = float4x4.TRS(centerWorldPos, centerWorldRot, componentWorldScl);
                float4x4 oldLW = float4x4.TRS(cdata.oldFrameWorldPosition, cdata.oldFrameWorldRotation, cdata.oldFrameWorldScale);
                float4x4 negativeM = math.mul(nowLW, math.inverse(oldLW));
                cdata.negativeScaleMatrix = negativeM;
            }

            // ■アンカー
            float3 anchorDeltaVector = 0;
            quaternion anchorDeltaRotation = quaternion.identity;
            if (tdata.flag.IsSet(Flag_AnchorReset) || tdata.IsReset)
            {
                cdata.oldAnchorPosition = cdata.anchorPosition;
                cdata.oldAnchorRotation = cdata.anchorRotation;
                cdata.anchorComponentLocalPosition = MathUtility.InverseTransformPoint(componentWorldPos, cdata.anchorPosition, cdata.anchorRotation, 1);
            }
            if (tdata.flag.IsSet(Flag_Anchor))
            {
                // アンカーの移動回転影響
                float3 anchorCenterPosition = MathUtility.TransformPoint(cdata.anchorComponentLocalPosition, cdata.anchorPosition, cdata.anchorRotation, 1);
                anchorDeltaVector = anchorCenterPosition - oldComponentWorldPosition;
                anchorDeltaRotation = MathUtility.FromToRotation(cdata.oldAnchorRotation, cdata.anchorRotation);

                // アンカーの影響割合
                float anchorRatio = 1.0f - param.inertiaConstraint.anchorInertia;
                anchorDeltaVector = math.lerp(float3.zero, anchorDeltaVector, anchorRatio);
                anchorDeltaRotation = math.slerp(quaternion.identity, anchorDeltaRotation, anchorRatio);

                // 打ち消す
                oldComponentWorldPosition += anchorDeltaVector;
                oldComponentWorldRotation = math.mul(anchorDeltaRotation, oldComponentWorldRotation);

                tdata.flag.SetBits(Flag_InertiaShift, true);
            }

            // フレーム移動量と速度
            float3 frameDeltaVector = componentWorldPos - oldComponentWorldPosition;
            float frameDeltaAngle = MathUtility.Angle(oldComponentWorldRotation, componentWorldRot);
            //Debug.Log($"frameDeltaVector:{frameDeltaVector}, frameDeltaAngle:{frameDeltaAngle}");

            // ■テレポート判定（コンポーネント姿勢から判定する）
            // 同期時は同期先のテレポートモードとパラメータが入っている
            if (param.inertiaConstraint.teleportMode != InertiaConstraint.TeleportMode.None && tdata.IsReset == false)
            {
                // 移動と回転どちらか一方がしきい値を超えたらテレポートと判定
                bool isTeleport = false;
                isTeleport = math.length(frameDeltaVector) >= param.inertiaConstraint.teleportDistance * componentScaleRatio ? true : isTeleport;
                isTeleport = math.degrees(frameDeltaAngle) >= param.inertiaConstraint.teleportRotation ? true : isTeleport;

                if (isTeleport)
                {
                    //Debug.Log($"[{teamId}] Auto Teleport!");
                    switch (param.inertiaConstraint.teleportMode)
                    {
                        case InertiaConstraint.TeleportMode.Reset:
                            tdata.flag.SetBits(Flag_Reset, true);
                            break;
                        case InertiaConstraint.TeleportMode.Keep:
                            tdata.flag.SetBits(Flag_KeepTeleport, true);
                            break;
                    }
                }
            }

            // ■スムージング
            // ワールド慣性の急激な変化および小刻みな変化によりクロスが乱れる問題を解消するために慣性をスムージングする
            // ・慣性の急激な変化（急発進・急停止）によるクロスの乱れの緩和
            // ・慣性の小刻みな変化によるクロスの振動の緩和
            float3 smoothDeltaVector = 0;
#if true
            if (param.inertiaConstraint.movementInertiaSmoothing >= 1e-06f && !(tdata.IsKeepReset || tdata.IsReset))
            {
                // 慣性速度をスムージングする
                // 測定はシミュレーションが実行される場合のみ行う（そうしないと振動が発生する）
                if (tdata.IsRunning)
                {
                    float3 frameDeltaVelocity = tdata.frameDeltaTime > 0.0f ? frameDeltaVector / tdata.frameDeltaTime : 0; // 速度ベクトル(m/s)
                    float movementSpeedLimit = param.inertiaConstraint.movementSpeedLimit * componentScaleRatio; // 同期時は同期先の値が入っている
                    if (movementSpeedLimit >= 0.0f)
                    {
                        // 最大速度制限
                        frameDeltaVelocity = MathUtility.ClampVector(frameDeltaVelocity, movementSpeedLimit);
                    }
                    float averageRatio = math.saturate(math.pow(1.0f - param.inertiaConstraint.movementInertiaSmoothing, 3.0f) * 0.99f + 0.01f);
                    cdata.smoothingVelocity = math.lerp(cdata.smoothingVelocity, frameDeltaVelocity, averageRatio); // 比重により平滑化
                }
                //Debug.Log($"smoothingVelocity:{cdata.smoothingVelocity}");

                // スムージングした慣性速度に基づいて１つ前のコンポーネント位置を補正する
                // 処理的にはアンカーと同じ考え
                float3 smoothPos = componentWorldPos - cdata.smoothingVelocity * tdata.frameDeltaTime;
                smoothDeltaVector = smoothPos - oldComponentWorldPosition;
                oldComponentWorldPosition = smoothPos;
                tdata.flag.SetBits(Flag_InertiaShift, true);
            }
#endif

            // リセットおよび最新のセンター座標として格納
            cdata.frameWorldPosition = centerWorldPos;
            cdata.frameWorldRotation = centerWorldRot;
            cdata.frameWorldScale = componentWorldScl;
            if (tdata.IsReset)
            {
                //Debug.LogWarning($"Team Reset!");
                cdata.oldComponentWorldPosition = componentWorldPos;
                cdata.oldComponentWorldRotation = componentWorldRot;
                cdata.oldComponentWorldScale = componentWorldScl;
                oldComponentWorldPosition = componentWorldPos;
                oldComponentWorldRotation = componentWorldRot;
                oldComponentWorldScale = componentWorldScl;

                cdata.oldFrameWorldPosition = centerWorldPos;
                cdata.oldFrameWorldRotation = centerWorldRot;
                cdata.oldFrameWorldScale = componentWorldScl;
                cdata.nowWorldPosition = centerWorldPos;
                cdata.nowWorldRotation = centerWorldRot;
                //cdata.nowWorldScale = centerWorldScl;
                cdata.oldWorldPosition = centerWorldPos;
                cdata.oldWorldRotation = centerWorldRot;
                //tdata.centerWorldPosition = centerWorldPos;
            }
            else if (tdata.IsNegativeScaleTeleport)
            {
                // マイナススケール
                // センター空間に関するものはリセットする
                //Debug.LogWarning($"Team NegativeScale Reset!");
                cdata.oldFrameWorldPosition = centerWorldPos;
                cdata.oldFrameWorldRotation = centerWorldRot;
                cdata.oldFrameWorldScale = componentWorldScl;
                cdata.nowWorldPosition = centerWorldPos;
                cdata.nowWorldRotation = centerWorldRot;
                //cdata.nowWorldScale = centerWorldScl;
                cdata.oldWorldPosition = centerWorldPos;
                cdata.oldWorldRotation = centerWorldRot;
                //tdata.centerWorldPosition = centerWorldPos;
            }

            // ■ワールド慣性シフト
            float3 workOldComponentPosition = oldComponentWorldPosition;
            quaternion workOldComponentRotation = oldComponentWorldRotation;
            if (tdata.IsReset)
            {
                // リセット（なし）
                cdata.frameComponentShiftVector = 0;
                cdata.frameComponentShiftRotation = quaternion.identity;

                // スムージングリセット
                cdata.smoothingVelocity = 0;
                smoothDeltaVector = 0;
            }
            else
            {
                cdata.frameComponentShiftVector = componentWorldPos - oldComponentWorldPosition;
                cdata.frameComponentShiftRotation = MathUtility.FromToRotation(oldComponentWorldRotation, componentWorldRot);
                //Debug.Log($"frameComponentShiftVector:{cdata.frameComponentShiftVector}, frameComponentShiftRotation:{cdata.frameComponentShiftRotation.value}");
                float moveShiftRatio = 0.0f;
                float rotationShiftRatio = 0.0f;

                // ■全体慣性シフト
                float movementShift = 1.0f - param.inertiaConstraint.worldInertia; // 同期時は同期先の値が入っている
                float rotationShift = 1.0f - param.inertiaConstraint.worldInertia; // 同期時は同期先の値が入っている

                // KeepテレポートもしくはCulling時はシフト量100%で実装
                //bool keep = tdata.IsKeepReset || tdata.IsCameraCullingInvisible;
                bool keep = tdata.IsKeepReset || tdata.IsCullingInvisible;
                movementShift = keep ? 1.0f : movementShift;
                rotationShift = keep ? 1.0f : rotationShift;

                if (movementShift > Define.System.Epsilon || rotationShift > Define.System.Epsilon)
                {
                    // 全体シフトあり
                    tdata.flag.SetBits(Flag_InertiaShift, true);
                    moveShiftRatio = movementShift;
                    rotationShiftRatio = rotationShift;

                    workOldComponentPosition = math.lerp(workOldComponentPosition, componentWorldPos, movementShift);
                    workOldComponentRotation = math.slerp(workOldComponentRotation, componentWorldRot, rotationShift);
                }

                // ■最大移動速度制限（全体シフトの結果から計算する）
                float movementSpeedLimit = param.inertiaConstraint.movementSpeedLimit * componentScaleRatio; // 同期時は同期先の値が入っている
                float rotationSpeedLimit = param.inertiaConstraint.rotationSpeedLimit; // 同期時は同期先の値が入っている
                float3 deltaVector = componentWorldPos - workOldComponentPosition;
                float deltaAngle = MathUtility.Angle(workOldComponentRotation, componentWorldRot);
                float frameSpeed = tdata.frameDeltaTime > 0.0f ? math.length(deltaVector) / tdata.frameDeltaTime : 0.0f;
                float frameRotationSpeed = tdata.frameDeltaTime > 0.0f ? math.degrees(deltaAngle) / tdata.frameDeltaTime : 0.0f;
                if (frameSpeed > movementSpeedLimit && movementSpeedLimit >= 0.0f)
                {
                    tdata.flag.SetBits(Flag_InertiaShift, true);
                    float moveLimitRatio = math.saturate(math.max(frameSpeed - movementSpeedLimit, 0.0f) / frameSpeed);
                    moveShiftRatio = math.lerp(moveShiftRatio, 1.0f, moveLimitRatio);
                    workOldComponentPosition = math.lerp(workOldComponentPosition, componentWorldPos, moveLimitRatio);
                }
                if (frameRotationSpeed > rotationSpeedLimit && rotationSpeedLimit >= 0.0f)
                {
                    tdata.flag.SetBits(Flag_InertiaShift, true);
                    float rotationLimitRatio = math.saturate(math.max(frameRotationSpeed - rotationSpeedLimit, 0.0f) / frameRotationSpeed);
                    rotationShiftRatio = math.lerp(rotationShiftRatio, 1.0f, rotationLimitRatio);
                    workOldComponentRotation = math.slerp(workOldComponentRotation, componentWorldRot, rotationLimitRatio);
                }

                // その他の影響
                float otherShiftRatio = 0.0f;

                // 更新スキップによるシフト
                // 更新スキップ時はスキップ時間分ワールド慣性シフトを行う
                if (tdata.skipCount > 0)
                {
                    otherShiftRatio = math.lerp(otherShiftRatio, 1.0f, math.saturate((tdata.skipCount * simulationDeltaTime) / (tdata.frameDeltaTime * tdata.nowTimeScale)));
                }

                // 安定化時間中は慣性を抑える
                if (tdata.velocityWeight < 1.0f)
                {
                    otherShiftRatio = math.lerp(otherShiftRatio, 1.0f, 1.0f - tdata.velocityWeight);
                }

                // タイムスケール
                // タイムスケールの影響分ワールド慣性シフトを行う
                if (tdata.nowTimeScale < 1.0f)
                {
                    otherShiftRatio = math.lerp(otherShiftRatio, 1.0f, 1.0f - tdata.nowTimeScale);
                }

                if (otherShiftRatio > 0.0f)
                {
                    tdata.flag.SetBits(Flag_InertiaShift, true);
                    moveShiftRatio = math.lerp(moveShiftRatio, 1.0f, otherShiftRatio);
                    workOldComponentPosition = math.lerp(workOldComponentPosition, componentWorldPos, otherShiftRatio);
                    rotationShiftRatio = math.lerp(rotationShiftRatio, 1.0f, otherShiftRatio);
                    workOldComponentRotation = math.slerp(workOldComponentRotation, componentWorldRot, otherShiftRatio);
                }

                // ■慣性シフト最終設定
                if (tdata.IsInertiaShift)
                {
                    //Debug.Log($"moveShiftRatio:{moveShiftRatio}, rotationShiftRatio:{rotationShiftRatio}");

                    cdata.frameComponentShiftVector *= moveShiftRatio;
                    cdata.frameComponentShiftRotation = math.slerp(quaternion.identity, cdata.frameComponentShiftRotation, rotationShiftRatio);

                    // アンカーによる打ち消し
                    cdata.frameComponentShiftVector += anchorDeltaVector;
                    cdata.frameComponentShiftRotation = math.mul(anchorDeltaRotation, cdata.frameComponentShiftRotation);

                    // スムージング影響打ち消し
                    cdata.frameComponentShiftVector += smoothDeltaVector;

                    cdata.oldFrameWorldPosition = MathUtility.ShiftPosition(cdata.oldFrameWorldPosition, cdata.oldComponentWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                    cdata.oldFrameWorldRotation = math.mul(cdata.frameComponentShiftRotation, cdata.oldFrameWorldRotation);

                    cdata.nowWorldPosition = MathUtility.ShiftPosition(cdata.nowWorldPosition, cdata.oldComponentWorldPosition, cdata.frameComponentShiftVector, cdata.frameComponentShiftRotation);
                    cdata.nowWorldRotation = math.mul(cdata.frameComponentShiftRotation, cdata.nowWorldRotation);
                }
            }
            //Debug.Log($"team:[{teamId}] frameComponentShiftVector:{cdata.frameComponentShiftVector}, frameComponentShiftRotation:{cdata.frameComponentShiftRotation}");
            //Debug.Log($"team:[{teamId}] centerTransformIndex:{centerTransformIndex}");
            //Debug.Log($"team:[{teamId}] worldInertia:{param.inertiaConstraint.worldInertia}");
            //Debug.Log($"team:[{teamId}] movementSpeedLimit:{param.inertiaConstraint.movementSpeedLimit}");
            //Debug.Log($"team:[{teamId}] rotationSpeedLimit:{param.inertiaConstraint.rotationSpeedLimit}");

            // ■ワールド移動方向と速度割り出し（慣性シフト後の移動量で計算）
            float3 movingVector = componentWorldPos - workOldComponentPosition;
            float movingLength = math.length(movingVector);
            cdata.frameMovingSpeed = tdata.frameDeltaTime > 0.0f ? movingLength / tdata.frameDeltaTime : 0.0f;
            cdata.frameMovingSpeed *= tdata.nowTimeScale > 1e-06f ? 1.0f / tdata.nowTimeScale : 0.0f; // タイムスケール考慮
            cdata.frameMovingDirection = movingLength > 1e-06f ? movingVector / movingLength : 0;

            //Debug.Log($"frameWorldPosition:{cdata.frameWorldPosition}, framwWorldRotation:{cdata.frameWorldRotation.value}");
            //Debug.Log($"oldFrameWorldPosition:{cdata.oldFrameWorldPosition}, oldFrameWorldRotation:{cdata.oldFrameWorldRotation.value}");
            //Debug.Log($"nowWorldPosition:{cdata.nowWorldPosition}, nowWorldRotation:{cdata.nowWorldRotation.value}");
            //Debug.Log($"oldWorldPosition:{cdata.oldWorldPosition}, oldWorldRotation:{cdata.oldWorldRotation.value}");

            // センターローカル座標
            float3 localCenterPos = MathUtility.InverseTransformPoint(centerWorldPos, wtol);
            cdata.frameLocalPosition = localCenterPos;

            // 速度安定化処理
            if (tdata.flag.IsSet(Flag_Reset) || tdata.flag.IsSet(Flag_TimeReset))
            {
                tdata.velocityWeight = param.stablizationTimeAfterReset > 1e-06f ? 0.0f : 1.0f;
                tdata.blendWeight = tdata.velocityWeight;
            }

            // 風の影響を計算 ========================================================
            //var oldTeamWindData = teamWindArray[teamId];
            var newTeamWindData = new TeamWindData();
            if (windZoneCount > 0 && param.wind.IsValid())
            {
                float minVolume = float.MaxValue;
                int addWindCount = 0;
                int latestWindId = -1;

                for (int windId = 0; windId < windZoneCount; windId++)
                {
                    var wdata = windDataArray[windId];
                    if (wdata.IsValid() == false || wdata.IsEnable() == false)
                        continue;

                    // チームが風エリアに入っているか判定する
                    // 加算風は最大３つまで
                    bool isAdditin = wdata.IsAddition();
                    if (isAdditin && addWindCount >= 3)
                        continue;

                    // 風ゾーンのローカル位置
                    float3 lpos = math.transform(wdata.worldToLocalMatrix, centerWorldPos);
                    float llen = math.length(lpos);

                    // エリア判定
                    switch (wdata.mode)
                    {
                        case MagicaWindZone.Mode.BoxDirection:
                            var lv = math.abs(lpos) * 2;
                            if (lv.x > wdata.size.x || lv.y > wdata.size.y || lv.z > wdata.size.z)
                                continue;
                            break;
                        case MagicaWindZone.Mode.SphereDirection:
                        case MagicaWindZone.Mode.SphereRadial:
                            if (llen > wdata.size.x)
                                continue;
                            break;
                    }

                    // エリア風の場合はボリューム判定（体積が小さいものが優先）
                    if (isAdditin == false && wdata.zoneVolume > minVolume)
                        continue;

                    // 風の方向(world)
                    float3 mainDirection = wdata.worldWindDirection;
                    switch (wdata.mode)
                    {
                        case MagicaWindZone.Mode.SphereRadial:
                            if (llen <= 1e-06f)
                                continue;
                            var v = centerWorldPos - wdata.worldPositin;
                            mainDirection = math.normalize(v);
                            break;
                    }
                    //Debug.Log($"wdir:{mainDirection}");

                    // 風力
                    float windMain = wdata.main;
                    switch (wdata.mode)
                    {
                        case MagicaWindZone.Mode.SphereRadial:
                            // 減衰
                            if (llen <= 1e-06f)
                                continue;
                            float depth = math.saturate(llen / wdata.size.x);
                            float attenuation = wdata.attenuation.MC2EvaluateCurveClamp01(depth);
                            windMain *= attenuation;
                            break;
                    }

                    // 計算する風として登録する
                    var windInfo = new TeamWindInfo()
                    {
                        windId = windId,
                        time = -Define.System.WindMaxTime, // マイナス値からスタート
                        main = windMain,
                        direction = mainDirection
                    };
                    if (isAdditin)
                    {
                        newTeamWindData.AddOrReplaceWindZone(windInfo, windData);
                        addWindCount++;
                    }
                    else
                    {
                        newTeamWindData.RemoveWindZone(latestWindId);
                        newTeamWindData.AddOrReplaceWindZone(windInfo, windData);
                        minVolume = wdata.zoneVolume;
                        latestWindId = windId;
                    }
                }
            }

            // 移動風移植
            newTeamWindData.movingWind = windData.movingWind;
            windData.CopyFrom(newTeamWindData);
        }

        /// <summary>
        /// ステップごとの前処理（ステップの開始に実行される）
        /// </summary>
        internal static void SimulationStepTeamUpdate(
            int updateIndex,
            float simulationDeltaTime,
            // team
            int teamId,
            ref TeamData tdata,
            ref ClothParameters param,
            ref InertiaConstraint.CenterData cdata,
            ref TeamWindData wdata
            )
        {
            // ■ステップ実行時のみ処理する
            bool runStep = updateIndex < tdata.updateCount;
            tdata.flag.SetBits(Flag_StepRunning, runStep);

            //Debug.Log($"team[{teamId}] ({updateIndex}/{tdata.updateCount})");

            // ■時間更新 ---------------------------------------------------
            // nowUpdateTime更新
            tdata.nowUpdateTime += simulationDeltaTime;

            // 今回のフレーム割合を計算する
            // frameStartTimeからtime区間でのnowUpdateTimeの割合
            //tdata.frameInterpolation = (tdata.nowUpdateTime - tdata.frameOldTime) / (tdata.time - tdata.frameOldTime);
            // 念の為、0除算チェックと0-1クランプを入れる
            float b = tdata.time - tdata.frameOldTime;
            tdata.frameInterpolation = b > 0 ? math.saturate((tdata.nowUpdateTime - tdata.frameOldTime) / b) : 1.0f;

            //Debug.Log($"Team[{teamId}] time.{tdata.time}, oldTime:{tdata.oldTime}, frameTime:{tdata.frameUpdateTime}, frameOldTime:{tdata.frameOldTime}, nowUpdateTime:{tdata.nowUpdateTime}, frameInterp:{tdata.frameInterpolation}");

            // ■センター ---------------------------------------------------
            // 現在ステップでのセンタートランスフォーム姿勢を求める
            cdata.oldWorldPosition = cdata.nowWorldPosition;
            cdata.oldWorldRotation = cdata.nowWorldRotation;
            cdata.nowWorldPosition = math.lerp(cdata.oldFrameWorldPosition, cdata.frameWorldPosition, tdata.frameInterpolation);
            cdata.nowWorldRotation = math.slerp(cdata.oldFrameWorldRotation, cdata.frameWorldRotation, tdata.frameInterpolation);
            cdata.nowWorldRotation = math.normalize(cdata.nowWorldRotation); // 必要
            float3 wscl = math.lerp(cdata.oldFrameWorldScale, cdata.frameWorldScale, tdata.frameInterpolation);
            //cdata.nowWorldScale = wscl;

            // ステップごとの移動量
            cdata.stepVector = cdata.nowWorldPosition - cdata.oldWorldPosition;
            cdata.stepRotation = MathUtility.FromToRotation(cdata.oldWorldRotation, cdata.nowWorldRotation);
            float stepAngle = MathUtility.Angle(cdata.oldWorldRotation, cdata.nowWorldRotation);
            //Debug.Log($"Team[{teamId}] stepVector:{cdata.stepVector}, stepRotation:{cdata.stepRotation}, stepAngle:{stepAngle}");
            //Debug.Log($"Team[{teamId}] stepVector:{math.length(cdata.stepVector)}, frameInterpolation:{tdata.frameInterpolation}");

            // ローカル慣性
            float localMovementInertia = 1.0f - param.inertiaConstraint.localInertia;
            float localRotationInertia = 1.0f - param.inertiaConstraint.localInertia;
#if true
            float3 localVector = cdata.stepVector * (1.0f - localMovementInertia);
            float localMovementSpeed = math.length(localVector) / simulationDeltaTime; // ローカル移動速度(m/s)
            if (localMovementSpeed > param.inertiaConstraint.localMovementSpeedLimit && param.inertiaConstraint.localMovementSpeedLimit >= 0.0f)
            {
                float t = param.inertiaConstraint.localMovementSpeedLimit / localMovementSpeed;
                localMovementInertia = math.lerp(1.0f, localMovementInertia, t);
            }
            float localAngle = stepAngle * (1.0f - localRotationInertia);
            float localAngleSpeed = math.degrees(localAngle / simulationDeltaTime); // ローカル回転速度(deg/s)
            if (localAngleSpeed > param.inertiaConstraint.localRotationSpeedLimit && param.inertiaConstraint.localRotationSpeedLimit >= 0.0f)
            {
                float t = param.inertiaConstraint.localRotationSpeedLimit / localAngleSpeed;
                localRotationInertia = math.lerp(1.0f, localRotationInertia, t);
            }
#endif
            cdata.stepMoveInertiaRatio = localMovementInertia;
            cdata.stepRotationInertiaRatio = localRotationInertia;

            // 最終慣性
            cdata.inertiaVector = math.lerp(float3.zero, cdata.stepVector, localMovementInertia);
            cdata.inertiaRotation = math.slerp(quaternion.identity, cdata.stepRotation, localRotationInertia);
            //Debug.Log($"Team[{teamId}] localMovementInertia:{localMovementInertia}, localRotationInertia:{localRotationInertia}, inertiaVector:{cdata.inertiaVector}, inertiaRotation:{cdata.inertiaRotation}");

            // ■遠心力用パラメータ算出
            // 今回ステップでの回転速度と回転軸
            cdata.angularVelocity = stepAngle / simulationDeltaTime; // 回転速度(rad/s)
            if (cdata.angularVelocity > Define.System.Epsilon)
                MathUtility.ToAngleAxis(cdata.stepRotation, out _, out cdata.rotationAxis);
            else
                cdata.rotationAxis = 0;
            //Debug.Log($"Team[{teamId}] angularVelocity:{math.degrees(cdata.angularVelocity)}, axis:{cdata.rotationAxis}, q:{cdata.stepRotation.value}");
            //Debug.Log($"Team[{teamId}] angularVelocity:{math.degrees(cdata.angularVelocity)}, now:{cdata.nowWorldRotation.value}, old:{cdata.oldWorldRotation.value}");

            // チームスケール倍率
            tdata.scaleRatio = math.max(math.length(wscl) / math.length(tdata.initScale), 1e-06f);
            //Debug.Log($"[{teamId}] scaleRatio:{tdata.scaleRatio}");

            // ■重力方向割合 ---------------------------------------------------
            float gravityDot = 1.0f;
            if (math.lengthsq(param.worldGravityDirection) > Define.System.Epsilon)
            {
                // マイナススケール
                float3 initLocalGravityDirection = cdata.initLocalGravityDirection;
                initLocalGravityDirection.y *= tdata.negativeScaleDirection.y; // Yマイナススケール時のみY軸を反転

                var worldFalloffDir = math.mul(cdata.nowWorldRotation, initLocalGravityDirection);
                gravityDot = math.dot(worldFalloffDir, param.worldGravityDirection);
                gravityDot = math.saturate(gravityDot * 0.5f + 0.5f);
            }
            tdata.gravityDot = gravityDot;
            //Develop.DebugLog($"gdot:{gravityDot}");

            // ■重力減衰 ---------------------------------------------------
            float gravityRatio = 1.0f;
            if (param.gravity > 1e-06f && param.gravityFalloff > 1e-06f)
            {
                gravityRatio = math.lerp(math.saturate(1.0f - param.gravityFalloff), 1.0f, math.saturate(1.0f - gravityDot));
            }
            tdata.gravityRatio = gravityRatio;

            // 速度安定化時間の速度割合を更新
            if (tdata.velocityWeight < 1.0f)
            {
                float addw = param.stablizationTimeAfterReset > 1e-06f ? simulationDeltaTime / param.stablizationTimeAfterReset : 1.0f;
                tdata.velocityWeight = math.saturate(tdata.velocityWeight + addw);
            }
            //Debug.Log($"{tdata.velocityWeight}");

            // シミュレーション結果のブレンド割合
            tdata.blendWeight = math.saturate(tdata.velocityWeight * param.blendWeight * tdata.distanceWeight);
            //Debug.Log($"{tdata.blendWeight}");

            // 風の時間更新
            UpdateWind(simulationDeltaTime, teamId, tdata, param.wind, cdata, ref wdata);

            //Debug.Log($"[{updateIndex}/{updateCount}] frameRatio:{data.frameInterpolation}, inertiaPosition:{idata.inertiaPosition}");
        }

        // 各風ゾーンの時間更新
        static void UpdateWind(
            float simulationDeltaTime,
            int teamId,
            in TeamData tdata,
            in WindParams windParams,
            in InertiaConstraint.CenterData cdata,
            ref TeamWindData teamWindData
            )
        {
            if (windParams.IsValid() == false)
                return;

            // ゾーン風
            int cnt = teamWindData.ZoneCount;
            for (int i = 0; i < cnt; i++)
            {
                var windInfo = teamWindData.windZoneList[i];
                UpdateWindTime(ref windInfo, windParams.frequency, simulationDeltaTime);
                teamWindData.windZoneList[i] = windInfo;
            }

            // 移動風
            var movingWindInfo = teamWindData.movingWind;
            movingWindInfo.main = 0;
            if (windParams.movingWind > 0.01f)
            {
                movingWindInfo.main = (cdata.frameMovingSpeed * windParams.movingWind) / tdata.scaleRatio;
                movingWindInfo.direction = -cdata.frameMovingDirection;
                UpdateWindTime(ref movingWindInfo, windParams.frequency, simulationDeltaTime);
            }
            teamWindData.movingWind = movingWindInfo;
        }

        static void UpdateWindTime(ref TeamWindInfo windInfo, float frequency, float simulationDeltaTime)
        {
            // 風速係数
            float mainRatio = windInfo.main / Define.System.WindBaseSpeed; // 0.0 ~ 

            // 基本周期
            float freq = 0.2f + mainRatio * 0.5f;
            freq *= frequency; // 0.0 ~ 2.0f;
            freq = math.min(freq, 1.5f); // max 1.5
            freq *= simulationDeltaTime;

            // 時間加算
            windInfo.time = windInfo.time + freq;

            // timeオーバーフロー対策
            if (windInfo.time > Define.System.WindMaxTime) // 約6時間
                windInfo.time -= Define.System.WindMaxTime * 2; // マイナス側から再スタート
        }

        /// <summary>
        /// クロスシミュレーション更新後処理
        /// </summary>
        internal static void SimulationPostTeamUpdate(
            // team
            ref TeamData tdata,
            ref InertiaConstraint.CenterData cdata
            )
        {
            // コンポーネント位置
            cdata.oldComponentWorldPosition = cdata.componentWorldPosition;
            cdata.oldComponentWorldRotation = cdata.componentWorldRotation;
            cdata.oldComponentWorldScale = cdata.componentWorldScale;

            if (tdata.IsRunning)
            {
                // センターを更新
                cdata.oldFrameWorldPosition = cdata.frameWorldPosition;
                cdata.oldFrameWorldRotation = cdata.frameWorldRotation;
                cdata.oldFrameWorldScale = cdata.frameWorldScale;

                // 外力クリア
                tdata.forceMode = ClothForceMode.None;
                tdata.impactForce = 0;

                // スキップ
                tdata.skipCount = 0;
            }

            // アンカー
            cdata.oldAnchorPosition = cdata.anchorPosition;
            cdata.oldAnchorRotation = cdata.anchorRotation;
            cdata.anchorComponentLocalPosition = MathUtility.InverseTransformPoint(cdata.componentWorldPosition, cdata.anchorPosition, cdata.anchorRotation, 1);

            // フラグリセット
            tdata.flag.SetBits(Flag_Reset, false);
            tdata.flag.SetBits(Flag_TimeReset, false);
            tdata.flag.SetBits(Flag_Running, false);
            tdata.flag.SetBits(Flag_StepRunning, false);
            tdata.flag.SetBits(Flag_KeepTeleport, false);
            tdata.flag.SetBits(Flag_InertiaShift, false);
            tdata.flag.SetBits(Flag_NegativeScaleTeleport, false);

            // 時間調整（floatの精度問題への対処）
            const float limitTime = 3600.0f; // 60min
            if (tdata.time > limitTime * 2)
            {
                tdata.time -= limitTime;
                tdata.oldTime -= limitTime;
                tdata.nowUpdateTime -= limitTime;
                tdata.oldUpdateTime -= limitTime;
                tdata.frameUpdateTime -= limitTime;
                tdata.frameOldTime -= limitTime;
            }
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"========== Team Manager ==========");
            if (IsValid() == false)
            {
                sb.AppendLine($"Team Manager. Invalid.");
                sb.AppendLine();
                Debug.Log(sb.ToString());
                allsb.Append(sb);
            }
            else
            {
                sb.AppendLine($"Team Manager. Team:{TeamCount}, Mapping:{MappingCount}, Monitoring:{monitoringProcessSet.Count}");
                sb.AppendLine($"  -teamDataArray:{teamDataArray.ToSummary()}");
                sb.AppendLine($"  -teamWindArray:{teamWindArray.ToSummary()}");
                sb.AppendLine($"  -mappingDataArray:{mappingDataArray.ToSummary()}");
                sb.AppendLine($"  -parameterArray:{parameterArray.ToSummary()}");
                sb.AppendLine($"  -centerDataArray:{centerDataArray.ToSummary()}");
                Debug.Log(sb.ToString());
                allsb.Append(sb);

                for (int i = 1; i < TeamCount; i++)
                {
                    var tdata = teamDataArray[i];
                    if (tdata.IsValid == false)
                        continue;

                    sb.Clear();

                    var mappingList = teamMappingIndexArray[i];

                    var cprocess = GetClothProcess(i);
                    if (cprocess == null)
                    {
                        sb.AppendLine($"ID:{i} cprocess is null!");
                        Debug.LogWarning(sb.ToString());
                        allsb.Append(sb);
                        continue;
                    }
                    var cloth = cprocess.cloth;
                    if (cloth == null)
                    {
                        sb.AppendLine($"ID:{i} cloth is null!");
                        Debug.LogWarning(sb.ToString());
                        allsb.Append(sb);
                        continue;
                    }

                    sb.AppendLine($"ID:{i} [{cprocess.Name}] state:0x{cprocess.GetStateFlag().Value:X}, Flag:0x{tdata.flag.Value:X}, Particle:{tdata.ParticleCount}, Collider:{cprocess.colliderDict.Count} Proxy:{tdata.proxyMeshType}, Mapping:{mappingList.Length}");
                    sb.AppendLine($"  -centerTransformIndex {tdata.centerTransformIndex}");
                    sb.AppendLine($"  -initScale {tdata.initScale}");
                    sb.AppendLine($"  -scaleRatio {tdata.scaleRatio}");
                    sb.AppendLine($"  -animationPoseRatio {tdata.animationPoseRatio}");
                    sb.AppendLine($"  -blendWeight {tdata.blendWeight}");

                    // 同期
                    sb.AppendLine($"  Sync:{cloth.SyncPartnerCloth}, SyncParentCount:{tdata.syncParentTeamId.Length}");

                    // chunk情報
                    sb.AppendLine($"  -ProxyTransformChunk {tdata.proxyTransformChunk}");
                    sb.AppendLine($"  -ProxyCommonChunk {tdata.proxyCommonChunk}");
                    sb.AppendLine($"  -ProxyMeshChunk {tdata.proxyMeshChunk}");
                    sb.AppendLine($"  -ProxyBoneChunk {tdata.proxyBoneChunk}");
                    sb.AppendLine($"  -ProxySkinBoneChunk {tdata.proxySkinBoneChunk}");
                    sb.AppendLine($"  -ProxyTriangleChunk {tdata.proxyTriangleChunk}");
                    sb.AppendLine($"  -ProxyEdgeChunk {tdata.proxyEdgeChunk}");
                    sb.AppendLine($"  -BaseLineChunk {tdata.baseLineChunk}");
                    sb.AppendLine($"  -BaseLineDataChunk {tdata.baseLineDataChunk}");
                    sb.AppendLine($"  -ParticleChunk {tdata.particleChunk}");
                    sb.AppendLine($"  -ColliderChunk {tdata.colliderChunk}");
                    sb.AppendLine($"  -ColliderTrnasformChunk {tdata.colliderTransformChunk}");
                    sb.AppendLine($"  -colliderCount {tdata.colliderCount}");

                    // mapping情報
                    sb.AppendLine($"  *Mapping Count {mappingList.Length}");
                    if (mappingList.Length > 0)
                    {
                        for (int j = 0; j < mappingList.Length; j++)
                        {
                            int mid = mappingList[j];
                            var mdata = mappingDataArray[mid];
                            sb.AppendLine($"  *Mapping Mid:{mid}, Vertex:{mdata.VertexCount}");
                            sb.AppendLine($"    -teamId:{mdata.teamId}");
                            sb.AppendLine($"    -centerTransformIndex:{mdata.centerTransformIndex}");
                            sb.AppendLine($"    -mappingCommonChunk:{mdata.mappingCommonChunk}");
                            sb.AppendLine($"    -toProxyMatrix:{mdata.toProxyMatrix}");
                            sb.AppendLine($"    -toProxyRotation:{mdata.toProxyRotation}");
                            sb.AppendLine($"    -sameSpace:{mdata.sameSpace}");
                            sb.AppendLine($"    -toMappingMatrix:{mdata.toMappingMatrix}");
                            sb.AppendLine($"    -scaleRatio:{mdata.scaleRatio}");
                            sb.AppendLine($"    -renderDataWorkIndex:{mdata.renderDataWorkIndex}");
                        }
                    }

                    // constraint
                    sb.AppendLine($"  +DistanceStartChunk {tdata.distanceStartChunk}");
                    sb.AppendLine($"  +DistanceDataChunk {tdata.distanceDataChunk}");
                    sb.AppendLine($"  +BendingPairChunk {tdata.bendingPairChunk}");
                    sb.AppendLine($"  +selfPointChunk {tdata.selfPointChunk}");
                    sb.AppendLine($"  +selfEdgeChunk {tdata.selfEdgeChunk}");
                    sb.AppendLine($"  +selfTriangleChunk {tdata.selfTriangleChunk}");

                    // wind
                    var wdata = teamWindArray[i];
                    sb.AppendLine($"  #Wind ZoneCount:{wdata.ZoneCount}");
                    for (int j = 0; j < wdata.ZoneCount; j++)
                    {
                        sb.AppendLine($"    [{j}] {wdata.windZoneList[j].ToString()}");
                    }
                    sb.AppendLine($"    [Move] {wdata.movingWind.ToString()}");


                    Debug.Log(sb.ToString());
                    allsb.Append(sb);
                }
                allsb.AppendLine();

                // MappingData
                sb.Clear();
                int mappingCount = mappingDataArray.Count;
                sb.AppendLine($"#MappingData Count:{mappingCount}");
                for (int i = 0; i < mappingCount; i++)
                {
                    var mdata = mappingDataArray[i];
                    if (mdata.IsValid() == false)
                        continue;

                    sb.AppendLine($"[{i}] teamId:{mdata.teamId}, renderDataWorkIndex:{mdata.renderDataWorkIndex}");
                }
                Debug.Log(sb.ToString());
                allsb.Append(sb);
            }
        }
    }
}
