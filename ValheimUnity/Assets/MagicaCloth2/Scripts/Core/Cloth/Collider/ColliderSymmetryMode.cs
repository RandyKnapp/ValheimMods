// Magica Cloth 2.
// Copyright (c) 2025 MagicaSoft.
// https://magicasoft.jp
namespace MagicaCloth2
{
    /// <summary>
    /// シンメトリーモード
    /// Symmetry mode.
    /// </summary>
    public enum ColliderSymmetryMode
    {
        None = 0,

        /// <summary>
        /// 人体の骨格を参照しすべて自動設定する
        /// キャラクターにAnimatorコンポーネントが必要です
        /// Automatically set everything based on the human skeleton.
        /// Character must have an Animator component.
        /// </summary>
        AutomaticHumanBody = 1,

        /// <summary>
        /// SymmetryTargetの姿勢から自動設定します
        /// Automatically set based on the SymmetryTarget's posture.
        /// </summary>
        AutomaticTarget = 2,

        /// <summary>
        /// X軸を左右対称
        /// Symmetry on the X axis.
        /// </summary>
        X_Symmetry = 100,

        /// <summary>
        /// Y軸を左右対称
        /// Symmetry on the Y axis.
        /// </summary>
        Y_Symmetry = 101,

        /// <summary>
        /// Z軸を左右対称
        /// Symmetry on the Z axis.
        /// </summary>
        Z_Symmetry = 102,

        /// <summary>
        /// XYZ軸を左右対称
        /// Symmetry on the XYZ axis.
        /// </summary>
        XYZ_Symmetry = 200,
    }
}
