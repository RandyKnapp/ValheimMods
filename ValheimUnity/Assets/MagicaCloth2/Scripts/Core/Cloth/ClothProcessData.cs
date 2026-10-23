// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// MagicaClothコンポーネント処理のデータ部分
    /// </summary>
    public partial class ClothProcess : IDisposable, IValid, ITransform
    {
        public MagicaCloth cloth { get; internal set; }

        /// <summary>
        /// 同期中の参照クロス。これは同期階層の最上位のクロスを指す
        /// </summary>
        public MagicaCloth SyncTopCloth { get; internal set; }

        /// <summary>
        /// 状態フラグ(0 ~ 31)
        /// </summary>
        public const int State_Valid = 0;
        public const int State_Enable = 1;
        //public const int State_ParameterDirty = 2;
        public const int State_InitSuccess = 3;
        public const int State_InitComplete = 4;
        public const int State_Build = 5;
        public const int State_Running = 6;
        public const int State_DisableAutoBuild = 7;
        public const int State_CameraCullingInvisible = 8; // チームデータの同フラグのコピー
        public const int State_CameraCullingKeep = 9; // チームデータの同フラグのコピー
        public const int State_SkipWriting = 10; // 書き込み停止（ストップモーション用）
        //public const int State_SkipWritingDirty = 11; // 書き込み停止フラグ更新サイン
        public const int State_UsePreBuild = 12; // PreBuildを利用
        public const int State_DistanceCullingInvisible = 13; // チームデータの同フラグのコピー
        public const int State_UpdateTangent = 14; // 接線の更新
        public const int State_Component = 15; // コンポーネントの有効状態
        public const int State_Verification = 16; // 検証結果による有効状態

        /// <summary>
        /// 現在の状態
        /// </summary>
        internal BitField32 stateFlag;

        /// <summary>
        /// 初期クロスコンポーネントトランスフォーム状態
        /// </summary>
        internal TransformRecord clothTransformRecord { get; private set; } = null;

        /// <summary>
        /// レンダー情報へのハンドル
        /// （レンダラーのセットアップデータ）
        /// </summary>
        internal List<MagicaObjectId> renderHandleList = new List<MagicaObjectId>();

        /// <summary>
        /// BoneClothのセットアップデータ
        /// </summary>
        internal RenderSetupData boneClothSetupData;

        /// <summary>
        /// レンダーメッシュの管理
        /// </summary>
        public class RenderMeshInfo
        {
            public MagicaObjectId renderHandle;
            public VirtualMeshContainer renderMeshContainer;
            public DataChunk mappingChunk;
            //public DataChunk renderMeshPositionAndNormalChunk;
            //public DataChunk renderMeshTangentChunk;
            public int renderDataWorkIndex;
        }
        internal List<RenderMeshInfo> renderMeshInfoList = new List<RenderMeshInfo>();

        /// <summary>
        /// カスタムスキニングのボーン情報
        /// </summary>
        internal List<TransformRecord> customSkinningBoneRecords = new List<TransformRecord>();

        /// <summary>
        /// 法線調整用のトランスフォーム状態
        /// </summary>
        internal TransformRecord normalAdjustmentTransformRecord { get; private set; } = null;

        //=========================================================================================
        /// <summary>
        /// ペイントマップ情報
        /// </summary>
        public class PaintMapData
        {
            public const byte ReadFlag_Fixed = 0x01;
            public const byte ReadFlag_Move = 0x02;
            public const byte ReadFlag_Limit = 0x04;

            public Color32[] paintData;
            public int paintMapWidth;
            public int paintMapHeight;
            public ExBitFlag8 paintReadFlag;
        }

        //=========================================================================================
        /// <summary>
        /// 処理結果
        /// </summary>
        internal ResultCode result;
        public ResultCode Result => result;

        /// <summary>
        /// 初期化データ参照結果
        /// </summary>
        public ResultCode InitDataResult { get; internal set; }

        /// <summary>
        /// Cloth Type
        /// </summary>
        public enum ClothType
        {
            MeshCloth = 0,
            BoneCloth = 1,
            BoneSpring = 10,
        }
        internal ClothType clothType { get; private set; }

        /// <summary>
        /// リダクション設定（外部から設定する）
        /// </summary>
        ReductionSettings reductionSettings;

        /// <summary>
        /// シミュレーションパラメータ
        /// </summary>
        public ClothParameters parameters { get; private set; }

        /// <summary>
        /// プロキシメッシュ
        /// </summary>
        public VirtualMeshContainer ProxyMeshContainer { get; private set; } = null;

        /// <summary>
        /// 登録中のコライダー
        /// int2 (メインコライダー・ローカルインデックス, シンメトリーコライダー・ローカルインデックス)
        /// メインコライダーのインデックス０はあり得る
        /// シンメトリーコライダーのインデックス０はシンメトリーが存在しないことを示す
        /// </summary>
        internal Dictionary<ColliderComponent, int2> colliderDict = new Dictionary<ColliderComponent, int2>();

        //=========================================================================================
        /// <summary>
        /// チームID
        /// </summary>
        public int TeamId { get; private set; } = 0;

        /// <summary>
        /// 慣性制約データ
        /// </summary>
        internal InertiaConstraint.ConstraintData inertiaConstraintData;

        /// <summary>
        /// 距離制約データ
        /// </summary>
        internal DistanceConstraint.ConstraintData distanceConstraintData;

        /// <summary>
        /// 曲げ制約データ
        /// </summary>
        internal TriangleBendingConstraint.ConstraintData bendingConstraintData;

        //=========================================================================================
        /// <summary>
        /// 連動アニメーター
        /// ・カリング
        /// ・更新モード
        /// </summary>
        internal Animator interlockingAnimator = null;

        /// <summary>
        /// カリング用アニメーター配下のレンダラーリスト
        /// </summary>
        internal List<Renderer> interlockingAnimatorRenderers = new List<Renderer>();

        /// <summary>
        /// 現在アンカーとして設定されているTransformのインスタンスID
        /// </summary>
        internal MagicaObjectId anchorTransformId = MagicaObjectId.Invalid;

        /// <summary>
        /// 現在距離カリングの参照として設定されているオブジェクトのインスタンスID
        /// </summary>
        internal MagicaObjectId distanceReferenceObjectId = MagicaObjectId.Invalid;

        /// <summary>
        /// コンポーネントの登録TransformIndex
        /// tdata.componentTransformIndexのコピー
        /// </summary>
        //internal int componentTransformIndex = 0;

        internal Animator cameraCullingAnimator = null;
        internal List<Renderer> cameraCullingRenderers = null;
        internal CullingSettings.CameraCullingMode cameraCullingMode;
        internal bool cameraCullingOldInvisible = false;

        //=========================================================================================
        /// <summary>
        /// キャンセルトークン
        /// </summary>
        CancellationTokenSource cts = new CancellationTokenSource();
        volatile object lockObject = new object();
        //volatile object lockState = new object();

        /// <summary>
        /// 初期化待機カウンター
        /// </summary>
        //volatile int suspendCounter = 0;

        /// <summary>
        /// 破棄フラグ
        /// </summary>
        volatile bool isDestory = false;

        /// <summary>
        /// 内部データまで完全に破棄されたかどうか
        /// </summary>
        volatile bool isDestoryInternal = false;

        /// <summary>
        /// 構築中フラグ
        /// </summary>
        volatile bool isBuild = false;

        public BitField32 GetStateFlag()
        {
            //lock (lockState)
            {
                // copy
                var state = stateFlag;
                return state;
            }
        }

        public bool IsState(int state)
        {
            //lock (lockState)
            {
                return stateFlag.IsSet(state);
            }
        }

        public void SetState(int state, bool sw)
        {
            //lock (lockState)
            {
                stateFlag.SetBits(state, sw);
            }
        }

        public bool IsValid() => IsState(State_Valid);
        public bool IsRunning() => IsState(State_Running);
        public bool IsCameraCullingInvisible() => IsState(State_CameraCullingInvisible);
        public bool IsCameraCullingKeep() => IsState(State_CameraCullingKeep);
        public bool IsDistanceCullingInvisible() => IsState(State_DistanceCullingInvisible);
        public bool IsSkipWriting() => IsState(State_SkipWriting);
        public bool IsUpdateTangent() => IsState(State_UpdateTangent);

        public bool IsEnable
        {
            get
            {
                if (IsValid() == false || TeamId == 0)
                    return false;
                return MagicaManager.Team.IsEnable(TeamId);
            }
        }

        public bool HasProxyMesh
        {
            get
            {
                if (IsValid() == false || TeamId == 0)
                    return false;
                return ProxyMeshContainer?.shareVirtualMesh?.IsSuccess ?? false;
            }
        }

        public string Name => cloth != null ? cloth.name : "(none)";

        //=========================================================================================
        public ClothProcess()
        {
            // 初期状態
            result = ResultCode.Empty;
        }

        public void Dispose()
        {
            lock (lockObject)
            {
                isDestory = true;
                SetState(State_Valid, false);
                result.Clear();
                cts.Cancel();
            }

            DisposeInternal();
            //Debug.Log($"ClothProcessData.Dispose()!");
        }

        void DisposeInternal()
        {
            lock (lockObject)
            {
                // すでに破棄完了ならば不要
                if (isDestoryInternal)
                    return;

                // ビルド中は破棄を保留する
                if (isBuild)
                    return;

                // マネージャから削除
                MagicaManager.Simulation?.ExitProxyMesh(this);
                MagicaManager.VMesh?.ExitProxyMesh(TeamId); // マッピングメッシュも解放される
                MagicaManager.Collider?.Exit(this);
                MagicaManager.Cloth?.RemoveCloth(this);

                // レンダーメッシュの破棄
                foreach (var info in renderMeshInfoList)
                {
                    if (info == null)
                        continue;

                    // 仮想メッシュ破棄
                    info.renderMeshContainer?.Dispose();
                }
                renderMeshInfoList.Clear();
                renderMeshInfoList = null;

                // レンダーデータの利用終了
                foreach (MagicaObjectId renderHandle in renderHandleList)
                {
                    MagicaManager.Render?.RemoveRenderer(renderHandle);
                }
                renderHandleList.Clear();
                renderHandleList = null;

                // BoneClothセットアップデータ
                boneClothSetupData?.Dispose();
                boneClothSetupData = null;

                // プロキシメッシュ破棄
                ProxyMeshContainer?.Dispose();
                ProxyMeshContainer = null;

                colliderDict.Clear();

                interlockingAnimator = null;
                interlockingAnimatorRenderers.Clear();

                // PreBuildデータ解除
                MagicaManager.PreBuild?.UnregisterPreBuildData(cloth != null ? cloth.GetSerializeData2()?.preBuildData.GetSharePreBuildData() : null);

                // 作業バッファ破棄
                SyncTopCloth = null;
                MagicaObjectId compId = cloth.GetMagicaId();
                MagicaManager.Team?.comp2SuspendCounterMap.Remove(compId);
                MagicaManager.Team?.comp2TeamIdMap.Remove(compId);
                MagicaManager.Team?.comp2SyncPartnerCompMap.Remove(compId);
                MagicaManager.Team?.comp2SyncTopCompMap.Remove(compId);

                // 完全破棄フラグ
                isDestoryInternal = true;
            }
            Develop.DebugLog($"Cloth dispose internal.");

            // 破棄監視リストから削除する
            MagicaManager.Team?.RemoveMonitoringProcess(this);
        }

        internal void IncrementSuspendCounter()
        {
            //suspendCounter++;
            var tm = MagicaManager.Team;
            MagicaObjectId compId = cloth.GetMagicaId();
            if (tm.comp2SuspendCounterMap.TryGetValue(compId, out int cnt))
            {
                cnt++;
                //tm.comp2SuspendCounterMap.Add(compId, cnt);
                tm.comp2SuspendCounterMap[compId] = cnt;
            }
            else
                tm.comp2SuspendCounterMap.Add(compId, 1);
        }

        internal void DecrementSuspendCounter()
        {
            //suspendCounter--;
            var tm = MagicaManager.Team;
            MagicaObjectId compId = cloth.GetMagicaId();
            if (tm.comp2SuspendCounterMap.TryGetValue(compId, out int cnt))
            {
                cnt--;
                if (cnt > 0)
                    tm.comp2SuspendCounterMap[compId] = cnt;
                else
                    tm.comp2SuspendCounterMap.Remove(compId);
            }
        }

        internal int GetSuspendCounter()
        {
            //return suspendCounter;
            var tm = MagicaManager.Team;
            MagicaObjectId compId = cloth.GetMagicaId();
            if (tm.comp2SuspendCounterMap.TryGetValue(compId, out int cnt))
                return cnt;
            else
                return 0;
        }

        public RenderMeshInfo GetRenderMeshInfo(int index)
        {
            if (index >= 0 && index < renderMeshInfoList.Count)
                return renderMeshInfoList[index];
            else
                return null;
        }

        internal void SyncParameters()
        {
            parameters = cloth.SerializeData.GetClothParameters();
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            cloth.SerializeData.GetUsedTransform(transformSet);
            cloth.serializeData2.GetUsedTransform(transformSet);
            clothTransformRecord?.GetUsedTransform(transformSet);
            boneClothSetupData?.GetUsedTransform(transformSet);
            renderHandleList.ForEach(handle => MagicaManager.Render.GetRendererData(handle).GetUsedTransform(transformSet));
            customSkinningBoneRecords.ForEach(rd => rd.GetUsedTransform(transformSet));
            normalAdjustmentTransformRecord?.GetUsedTransform(transformSet);

            // nullを除外する
            if (transformSet.Contains(null))
                transformSet.Remove(null);
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            cloth.SerializeData.ReplaceTransform(replaceDict);
            cloth.serializeData2.ReplaceTransform(replaceDict);
            clothTransformRecord?.ReplaceTransform(replaceDict);
            boneClothSetupData?.ReplaceTransform(replaceDict);
            renderHandleList.ForEach(handle => MagicaManager.Render.GetRendererData(handle).ReplaceTransform(replaceDict));
            customSkinningBoneRecords.ForEach(rd => rd.ReplaceTransform(replaceDict));
            normalAdjustmentTransformRecord?.ReplaceTransform(replaceDict);
        }

        internal void SetSkipWriting(bool sw)
        {
            // ここではフラグのみ更新する
            // 実際の更新はチームのAlwaysTeamUpdate()で行われる
            SetState(State_SkipWriting, sw);
            //SetState(State_SkipWritingDirty, true);
            MagicaManager.Team.skipWritingDirtyList.Add(this);
        }

        internal ClothUpdateMode GetClothUpdateMode()
        {
            switch (cloth.SerializeData.updateMode)
            {
                case ClothUpdateMode.Normal:
                case ClothUpdateMode.UnityPhysics:
                case ClothUpdateMode.Unscaled:
                    return cloth.SerializeData.updateMode;
                case ClothUpdateMode.AnimatorLinkage:
                    if (interlockingAnimator)
                    {
                        switch (interlockingAnimator.updateMode)
                        {
                            case AnimatorUpdateMode.Normal:
                                return ClothUpdateMode.Normal;
#if UNITY_2023_1_OR_NEWER
                            case AnimatorUpdateMode.Fixed:
                                return ClothUpdateMode.UnityPhysics;
#else
                            case AnimatorUpdateMode.AnimatePhysics:
                                return ClothUpdateMode.UnityPhysics;
#endif
                            case AnimatorUpdateMode.UnscaledTime:
                                return ClothUpdateMode.Unscaled;
                            default:
                                Develop.DebugLogWarning($"[{cloth.name}] Unknown Animator UpdateMode:{interlockingAnimator.updateMode}");
                                break;
                        }
                    }
                    return ClothUpdateMode.Normal;
                default:
                    Develop.LogError($"[{cloth.name}] Unknown Cloth Update Mode:{cloth.SerializeData.updateMode}");
                    return ClothUpdateMode.Normal;
            }
        }
    }
}
