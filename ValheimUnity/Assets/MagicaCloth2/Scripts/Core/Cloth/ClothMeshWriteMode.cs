// Magica Cloth 2.
// Copyright (c) 2024 MagicaSoft.
// https://magicasoft.jp

namespace MagicaCloth2
{
    /// <summary>
    /// メッシュへの書き込み対象
    /// Write target to mesh.
    /// </summary>
    public enum ClothMeshWriteMode
    {
        /// <summary>
        /// 位置と法線
        /// Position, Normal
        /// </summary>
        PositionAndNormal = 0,

        /// <summary>
        /// 位置と法線と接線
        /// Position, Normal, Tangent
        /// </summary>
        PositionAndNormalTangent = 1,
    }
}
