// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using System.Text;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// 各クロスコンポーネントの更新処理
    /// </summary>
    public class ClothManager : IManager, IValid
    {
        // すべて
        internal HashSet<ClothProcess> clothSet = new HashSet<ClothProcess>(256);

        // BoneCloth,BoneSpring
        internal HashSet<ClothProcess> boneClothSet = new HashSet<ClothProcess>();

        // MeshCloth
        internal HashSet<ClothProcess> meshClothSet = new HashSet<ClothProcess>();

        //=========================================================================================
        Dictionary<MagicaObjectId, bool> animatorVisibleDict = new Dictionary<MagicaObjectId, bool>(30);
        Dictionary<MagicaObjectId, bool> rendererVisibleDict = new Dictionary<MagicaObjectId, bool>(100);

        //=========================================================================================
        /// <summary>
        /// マスタージョブハンドル
        /// </summary>
        JobHandle masterJob = default;

        bool isValid = false;

        //=========================================================================================
        public void Dispose()
        {
            isValid = false;

            clothSet.Clear();
            boneClothSet.Clear();
            meshClothSet.Clear();

            // 作業バッファ
            animatorVisibleDict.Clear();
            rendererVisibleDict.Clear();

            // 更新処理
            MagicaManager.afterEarlyUpdateDelegate -= OnEarlyClothUpdate;
            MagicaManager.firstPreUpdateDelegate -= OnFirstPreUpdate;
            MagicaManager.afterLateUpdateDelegate -= OnAfterLateUpdate;
            MagicaManager.beforeLateUpdateDelegate -= OnBeforeLateUpdate;
        }

        public void EnterdEditMode()
        {
            Dispose();
        }

        public void Initialize()
        {
            clothSet.Clear();
            boneClothSet.Clear();
            meshClothSet.Clear();

            // 作業バッファ

            // 更新処理
            MagicaManager.afterEarlyUpdateDelegate += OnEarlyClothUpdate;
            MagicaManager.firstPreUpdateDelegate += OnFirstPreUpdate;
            MagicaManager.afterLateUpdateDelegate += OnAfterLateUpdate;
            MagicaManager.beforeLateUpdateDelegate += OnBeforeLateUpdate;

            isValid = true;
        }

        public bool IsValid()
        {
            return isValid;
        }

        //=========================================================================================
        void ClearMasterJob()
        {
            masterJob = default;
        }

        void CompleteMasterJob()
        {
            masterJob.Complete();
        }

        //=========================================================================================
        internal int AddCloth(ClothProcess cprocess, in ClothParameters clothParams)
        {
            // この段階でProxyMeshは完成している
            if (isValid == false)
                return 0;

            // チーム登録
            var teamId = MagicaManager.Team.AddTeam(cprocess, clothParams);
            if (teamId == 0)
                return 0;

            clothSet.Add(cprocess);
            switch (cprocess.clothType)
            {
                case ClothProcess.ClothType.BoneCloth:
                case ClothProcess.ClothType.BoneSpring:
                    boneClothSet.Add(cprocess);
                    break;
                case ClothProcess.ClothType.MeshCloth:
                    meshClothSet.Add(cprocess);
                    break;
                default:
                    Develop.LogError($"Invalid cloth type! :{cprocess.clothType}");
                    break;
            }

            // チームマネージャの作業バッファへ登録
            MagicaManager.Team.comp2TeamIdMap.Add(cprocess.cloth.GetMagicaId(), teamId);

            return teamId;
        }

        internal void RemoveCloth(ClothProcess cprocess)
        {
            if (isValid == false)
                return;

            // チーム解除
            MagicaManager.Team.RemoveTeam(cprocess.TeamId);

            clothSet.Remove(cprocess);
            boneClothSet.Remove(cprocess);
            meshClothSet.Remove(cprocess);
        }

        //=========================================================================================
        /// <summary>
        /// フレーム開始時に実行される更新処理
        /// </summary>
        void OnEarlyClothUpdate()
        {
            //Debug.Log($"OnEarlyClothUpdate. F:{Time.frameCount}");
            if (MagicaManager.Team.TrueTeamCount > 0) // カリング判定があるのでDisableチームもまわす必要がある
            {
                // カメラカリング更新
                if (MagicaManager.Team.ActiveTeamCount > 0)
                {
                    // この更新は次のTransform復元の前に行う必要がある
                    MagicaManager.Team.CameraCullingPostProcess();
                }

                // BoneClothのTransform復元更新
                ClearMasterJob();
                masterJob = MagicaManager.Bone.RestoreTransform(masterJob);
                CompleteMasterJob();
            }
        }

        /// <summary>
        /// PreUpdate開始時に実行される更新処理
        /// </summary>
        void OnFirstPreUpdate()
        {
            //Debug.Log($"OnFirstPreUpdate. F:{Time.frameCount}");
            if (MagicaManager.Team.TrueTeamCount > 0) // カリング判定があるのでDisableチームもまわす必要がある
            {
                //Debug.Log($"existFixedTeam:{MagicaManager.Bone.existFixedTeam.Value}");
                // FixedUpdateが０回かつFixedTeamが存在する場合のみ
                if (MagicaManager.Time.FixedUpdateCount == 0 && MagicaManager.Bone.existFixedTeam.Value)
                {
                    ClearMasterJob();
                    masterJob = MagicaManager.Bone.RestoreBaseTransform(masterJob);
                    CompleteMasterJob();
                }
            }
        }

        void OnBeforeLateUpdate()
        {
            if (MagicaManager.Time.updateLocation == TimeManager.UpdateLocation.BeforeLateUpdate)
                ClothUpdate();
        }

        void OnAfterLateUpdate()
        {
            if (MagicaManager.Time.updateLocation == TimeManager.UpdateLocation.AfterLateUpdate)
                ClothUpdate();
        }

        //=========================================================================================
        static readonly ProfilerMarker startClothUpdateTimeProfiler = new ProfilerMarker("StartClothUpdate.Time");
        static readonly ProfilerMarker startClothUpdateTeamProfiler = new ProfilerMarker("StartClothUpdate.Team");
        static readonly ProfilerMarker startClothUpdatePrePareProfiler = new ProfilerMarker("StartClothUpdate.Prepare");
        static readonly ProfilerMarker startClothUpdateScheduleProfiler = new ProfilerMarker("StartClothUpdate.Schedule");

        /// <summary>
        /// クロスコンポーネントの更新
        /// </summary>
        void ClothUpdate()
        {
            if (MagicaManager.IsPlaying() == false)
                return;

            // ■コンポーネント０なら終了
            var tm = MagicaManager.Team;
            if (tm.TrueTeamCount == 0)
                return;

            //-----------------------------------------------------------------
            // シミュレーション開始イベント
            MagicaManager.OnPreSimulation?.Invoke();

            //-----------------------------------------------------------------
            var sm = MagicaManager.Simulation;
            var bm = MagicaManager.Bone;
            var wm = MagicaManager.Wind;

            //Debug.Log($"StartClothUpdate. F:{Time.frameCount}");
            //Develop.DebugLog($"StartClothUpdate. F:{Time.frameCount}, dtime:{Time.deltaTime}, stime:{Time.smoothDeltaTime}");

            //-----------------------------------------------------------------
            // ■時間マネージャ更新
            startClothUpdateTimeProfiler.Begin();
            MagicaManager.Time.FrameUpdate();
            startClothUpdateTimeProfiler.End();

            // ■常に実行するチーム更新
            startClothUpdateTeamProfiler.Begin();
            tm.AlwaysTeamUpdate();
            startClothUpdateTeamProfiler.End();

            // ■ここで実行チーム数が０ならば終了
            if (tm.ActiveTeamCount == 0)
                return;

            startClothUpdatePrePareProfiler.Begin();

            // ■常に実行する風ゾーン更新
            wm.AlwaysWindUpdate();

            // ■作業バッファ更新
            sm.WorkBufferUpdate();

            startClothUpdatePrePareProfiler.End();

            //-----------------------------------------------------------------
            startClothUpdateScheduleProfiler.Begin();

            // マスタージョブ初期化
            ClearMasterJob();

            // ■トランスフォーム情報の読み込み
            masterJob = bm.ReadTransformSchedule(masterJob);

            // ■シミュレーションジョブ
            masterJob = sm.ClothSimulationSchedule(masterJob);

            startClothUpdateScheduleProfiler.End();
            //-----------------------------------------------------------------
            //JobHandle.ScheduleBatchedJobs();

            //-----------------------------------------------------------------
            // ■ジョブ完了待ちの間に行う処理
            // カメラカリングの準備
            tm.CameraCullingPreProcess();

            //-----------------------------------------------------------------
            // ■現在は即時実行のためここでジョブの完了待ちを行う
            CompleteMasterJob();

            //-----------------------------------------------------------------
            // シミュレーション終了イベント
            MagicaManager.OnPostSimulation?.Invoke();
        }

        //=========================================================================================
        internal void ClearVisibleDict()
        {
            animatorVisibleDict.Clear();
            rendererVisibleDict.Clear();
        }

        internal bool CheckVisible(Animator ani, List<Renderer> renderers)
        {
            if (ani)
            {
                MagicaObjectId id = ani.GetMagicaId();
                if (animatorVisibleDict.ContainsKey(id))
                    return animatorVisibleDict[id];

                bool visible = CheckRendererVisible(renderers);
                animatorVisibleDict.Add(id, visible);
                return visible;
            }
            else
            {
                return CheckRendererVisible(renderers);
            }
        }

        bool CheckRendererVisible(List<Renderer> renderers)
        {
            foreach (var ren in renderers)
            {
                if (ren)
                {
                    bool visible;
                    MagicaObjectId id = ren.GetMagicaId();
                    if (rendererVisibleDict.ContainsKey(id))
                    {
                        visible = rendererVisibleDict[id];
                    }
                    else
                    {
                        visible = ren.isVisible;
                        rendererVisibleDict.Add(id, visible);
                    }
                    if (visible)
                        return true;
                }
            }

            return false;
        }

        //=========================================================================================
        public void InformationLog(StringBuilder allsb)
        {
        }
    }
}
