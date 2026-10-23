// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Magica manger settings component
    /// </summary>
    [AddComponentMenu("MagicaCloth2/MagicaSettings")]
    [HelpURL("https://magicasoft.jp/en/mc2_settings_component/")]
    public class MagicaSettings : ClothBehaviour
    {
        public enum RefreshMode
        {
            /// <summary>
            /// コンポーネントのAwake()で１度だけ送信する
            /// Send once at the component's Awake()
            /// </summary>
            OnAwake = 0,

            /// <summary>
            /// 毎フレーム内容を送信する
            /// Send content every frame.
            /// </summary>
            EveryFrame = 1,

            /// <summary>
            /// コンポーネントのStart()で一度だけ送信する
            /// Send once at the component's Start().
            /// </summary>
            OnStart = 2,

            /// <summary>
            /// コンポーネントは何もしません。送信には手動でRefresh()を呼ぶ必要があります
            /// The component does nothing. You must manually call Refresh() to submit.
            /// </summary>
            Manual = 3,
        }

        /// <summary>
        /// コンポーネントの内容をマネージャに送信する方法
        /// How to send the contents of a component to the manager.
        /// Refresh mode
        /// </summary>
        public RefreshMode refreshMode = RefreshMode.OnAwake;

        /// <summary>
        /// シミュレーション周波数(30~150, 初期値90)
        /// 周波数を上げると精度が高くなりますが負荷が上がります、下げるげると精度が低くなりますが負荷が下がります
        /// そのため60以下に下げる場合には精度問題に十分注意してください
        /// 
        /// Simulation frequency (30~150, default 90).
        /// Increasing the frequency increases the accuracy but increases the load, and decreasing the frequency decreases the accuracy but reduces the load.
        /// Therefore, if you lower it below 60, be very careful about accuracy issues.
        /// </summary>
        [Range(Define.System.SimulationFrequency_Low, Define.System.SimulationFrequency_Hi)]
        public int simulationFrequency = Define.System.DefaultSimulationFrequency;

        /// <summary>
        /// １フレームの最大シミュレーション回数(1~5, 初期値3)
        /// シミュレーションはフレームレート(fps)とは非同期に実行されます
        /// そのためfpsが下がると１フレームに実行されるシミュレーション回数が増えて負荷が高くなります
        /// これはモバイル端末などで問題になる場合があります
        /// １フレームで実行されるシミュレーション回数を下げることで最大負荷を調整できます
        /// 制限によりシミュレーションがスキップされた場合は補間機能により動作が補われます
        /// 
        /// Maximum number of simulations per frame (1 to 5, default value 3).
        /// The simulation runs asynchronously with the frame rate(fps).
        /// Therefore, when the fps decreases, the number of simulations executed in one frame increases and the load increases.
        /// This can be a problem on mobile devices, for example.
        /// You can adjust the maximum load by lowering the number of simulations executed in one frame.
        /// If the simulation is skipped due to restrictions, the interpolation function compensates for the motion.
        /// </summary>
        [Range(Define.System.MaxSimulationCountPerFrame_Low, Define.System.MaxSimulationCountPerFrame_Hi)]
        public int maxSimulationCountPerFrame = Define.System.DefaultMaxSimulationCountPerFrame;

        /// <summary>
        /// MonoBehaviourでのMagicaClothの初期化場所。
        /// MagicaCloth initialization location in MonoBehaviour.
        /// </summary>
        public MagicaManager.InitializationLocation initializationLocation = MagicaManager.InitializationLocation.Start;

        /// <summary>
        /// シミュレーションの更新場所
        /// BeforeLateUpdate : LateUpdate()の前に実行します。これはUnity 2D Animationで利用する場合に必要です。
        /// AfterLateUpdate  : LateUpdate()の後に実行します。初期設定。
        /// 
        /// Simulation update location.
        /// BeforeLateUpdate : Executes before LateUpdate(). This is required when using Unity 2D Animation.
        /// AfterLateUpdate  : Executes after LateUpdate(). Initial setting.
        /// </summary>
        public TimeManager.UpdateLocation updateLocation = TimeManager.UpdateLocation.AfterLateUpdate;

        /// <summary>
        /// PlayerLoopの監視
        /// MagicaClothのシステムはUnityのPlayerLoopに登録することで動作します
        /// この登録が他の外部アセットにより上書きされてしまうと、MagicaClothのシステムが停止してしまいます
        /// このフラグを有効にすると、PlayerLoopを監視して、上書きされていた場合は再度システムを登録するようになります
        /// 
        /// PlayerLoop monitoring.
        /// The MagicaCloth system works by registering it in Unity's PlayerLoop.
        /// If this registration is overwritten by other external assets, the MagicaCloth system will stop working.
        /// When this flag is enabled, the PlayerLoop will be monitored, and the system will be registered again if it has been overwritten.
        /// </summary>
        public bool monitorPlayerLoop = false;

        /// <summary>
        /// ジョブ分割を適用するプロキシメッシュの頂点数
        /// 
        /// The number of proxy mesh vertices to apply job splitting to.
        /// </summary>
        [Min(0)]
        public int splitProxyMeshVertexCount = Define.System.SplitProxyMeshVertexCount;

        //=========================================================================================
        protected void Awake()
        {
            if (refreshMode == RefreshMode.OnAwake)
                Refresh();
        }

        protected void Start()
        {
            if (refreshMode == RefreshMode.OnStart)
                Refresh();
        }

        protected void Update()
        {
            if (refreshMode == RefreshMode.EveryFrame)
                Refresh();
        }

        protected void OnValidate()
        {
            if (Application.isPlaying)
                Refresh();
        }

        //=========================================================================================
        /// <summary>
        /// コンポーネントの内容をマネージャに送信します
        /// Sends the contents of the component to the manager.
        /// </summary>
        public void Refresh()
        {
            if (MagicaManager.IsPlaying())
            {
                simulationFrequency = Mathf.Clamp(simulationFrequency, Define.System.SimulationFrequency_Low, Define.System.SimulationFrequency_Hi);
                maxSimulationCountPerFrame = Mathf.Clamp(maxSimulationCountPerFrame, Define.System.MaxSimulationCountPerFrame_Low, Define.System.MaxSimulationCountPerFrame_Hi);

                MagicaManager.SetSimulationFrequency(simulationFrequency);
                MagicaManager.SetMaxSimulationCountPerFrame(maxSimulationCountPerFrame);
                MagicaManager.SetInitializationLocation(initializationLocation);
                MagicaManager.SetUpdateLocation(updateLocation);
                MagicaManager.SetSplitProxyMeshVertexCount(splitProxyMeshVertexCount);

                if (monitorPlayerLoop)
                    MagicaManager.InitCustomGameLoop();
            }
            else
            {
                Develop.LogError("MagicaManager is not starting!");
            }
        }
    }
}
