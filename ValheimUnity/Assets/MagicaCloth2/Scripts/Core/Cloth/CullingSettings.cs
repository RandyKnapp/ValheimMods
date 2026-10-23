// Magica Cloth 2.
// Copyright (c) 2025 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class CullingSettings : IDataValidate
    {
        /// <summary>
        /// Camera culling mode.
        /// カメラカリングモード
        /// </summary>
        public enum CameraCullingMode
        {
            /// <summary>
            /// No culling.
            /// カリングは行わない
            /// </summary>
            Off = 0,

            /// <summary>
            /// Simulation resets when hidden from camera.
            /// カメラから非表示になるとシミュレーションはリセットされる
            /// </summary>
            Reset = 10,

            /// <summary>
            /// Simulation pauses when hidden from camera.
            /// カメラから非表示になるとシミュレーションは一時停止する
            /// </summary>
            Keep = 20,

            /// <summary>
            /// Automatically set from linked animator.
            /// 連動アニメーターから自動設定する
            /// - Animator.CullingMode.AlwaysAnimate -> Off
            /// - Animator.CullingMode.CullUpdateTransforms -> Reset
            /// - Animator.CullingMode.CullCompletely -> Keep
            /// </summary>
            AnimatorLinkage = 30,
        }

        /// <summary>
        /// Camera culling method.
        /// カリング方式
        /// </summary>
        public enum CameraCullingMethod
        {
            /// <summary>
            /// Work with an animator.
            /// アニメーターと連動する
            /// </summary>
            AutomaticRenderer = 0,

            /// <summary>
            /// Determine from user-specified renderer.
            /// ユーザー指定のレンダラーから判定する
            /// </summary>
            ManualRenderer = 10,
        }

        /// <summary>
        /// Camera culling mode.
        /// カメラカリングモード
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public CameraCullingMode cameraCullingMode = CameraCullingMode.AnimatorLinkage;

        /// <summary>
        /// Camera culling judgment method.
        /// カメラカリング判定方式
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public CameraCullingMethod cameraCullingMethod = CameraCullingMethod.AutomaticRenderer;

        /// <summary>
        /// User-specified camera culling judgment renderer.
        /// ユーザー指定のカメラカリング判定用レンダラー
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public List<Renderer> cameraCullingRenderers = new List<Renderer>();

        /// <summary>
        /// 距離カリングの状態と距離
        /// Distance Culling State and Distance.
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public CheckSliderSerializeData distanceCullingLength;

        /// <summary>
        /// 距離カリングのフェード割合(0.0 ~ 1.0)
        /// Distance culling fade rate (0.0 to 1.0).
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        [Range(0.0f, 1.0f)]
        public float distanceCullingFadeRatio;

        /// <summary>
        /// 距離カリングの測定対象(None=メインカメラ)
        /// Distance culling measurement target (None = main camera).
        /// [OK] Runtime changes.
        /// [NG] Export/Import with Presets
        /// </summary>
        public GameObject distanceCullingReferenceObject;

        //=========================================================================================
        public struct CullingParams
        {
            public bool useDistanceCulling;
            public float distanceCullingLength;
            public float distanceCullingFadeRatio;

            public void Convert(CullingSettings cullingSettings)
            {
                useDistanceCulling = cullingSettings.distanceCullingLength.use;
                distanceCullingLength = cullingSettings.distanceCullingLength.value;
                distanceCullingFadeRatio = cullingSettings.distanceCullingFadeRatio;
            }
        }

        //=========================================================================================
        public CullingSettings()
        {
            distanceCullingLength = new CheckSliderSerializeData(false, 30.0f);
            distanceCullingFadeRatio = 0.2f;
        }

        public void DataValidate()
        {
            distanceCullingLength.DataValidate(0.0f, Define.System.DistanceCullingMaxLength);
            distanceCullingFadeRatio = Mathf.Clamp01(distanceCullingFadeRatio);
        }

        public CullingSettings Clone()
        {
            return new CullingSettings()
            {
                cameraCullingMode = cameraCullingMode,
                cameraCullingMethod = cameraCullingMethod,
                cameraCullingRenderers = new List<Renderer>(cameraCullingRenderers),
                distanceCullingLength = distanceCullingLength.Clone(),
                distanceCullingFadeRatio = distanceCullingFadeRatio,
                distanceCullingReferenceObject = distanceCullingReferenceObject,
            };
        }

        /// <summary>
        /// エディタメッシュの更新を判定するためのハッシュコード
        /// （このハッシュは実行時には利用されない編集用のもの）
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => 0;
    }
}
