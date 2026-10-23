// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Serialize data (2)
    /// Parts that cannot be exported externally.
    /// </summary>
    [System.Serializable]
    public class ClothSerializeData2 : IDataValidate, IValid, ITransform
    {
        /// <summary>
        /// Initialization Data.
        /// </summary>
        [SerializeField]
        public ClothInitSerializeData initData = new ClothInitSerializeData();

        /// <summary>
        /// 頂点ペイントデータ
        /// vertex paint data.
        /// </summary>
        [SerializeField]
        public SelectionData selectionData = new SelectionData();

        /// <summary>
        /// Transformと頂点属性辞書データ
        /// 実行時でのBoneCloth/BoneSpring作成時にはこの辞書にTransformと頂点属性のペアを格納することで頂点ペイントデータの代わりにすることができます。
        /// Transform and vertex attribute dictionary data.
        /// When creating BoneCloth/BoneSpring at runtime, you can store Transform and vertex attribute pairs in this dictionary and use it instead of vertex paint data.
        /// </summary>
        [System.NonSerialized]
        public Dictionary<Transform, VertexAttribute> boneAttributeDict = new Dictionary<Transform, VertexAttribute>();

        /// <summary>
        /// Rendererに対応する頂点属性データ
        /// 実行時にMeshClothを構築する場合に、このリストにレンダラーごとのメッシュ頂点数分の頂点属性を格納することでセレクションデータの代わりにすることができます
        /// Vertex attribute data corresponding to the Renderer.
        /// When constructing MeshCloth at runtime, you can substitute selection data by storing vertex attributes in this list for the number of mesh vertices per renderer.
        /// </summary>
        [System.NonSerialized]
        public List<VertexAttribute[]> vertexAttributeList = new List<VertexAttribute[]>();

        /// <summary>
        /// PreBuild Data.
        /// </summary>
        public PreBuildSerializeData preBuildData = new PreBuildSerializeData();

        //=========================================================================================
        public ClothSerializeData2()
        {
        }

        /// <summary>
        /// クロスを構築するための最低限の情報が揃っているかチェックする
        /// Check if you have the minimum information to construct the cloth.
        /// </summary>
        /// <returns></returns>
        public bool IsValid()
        {
            return true;
        }

        public void DataValidate()
        {
        }

        /// <summary>
        /// エディタメッシュの更新を判定するためのハッシュコード
        /// Hashcode for determining editor mesh updates.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 0;
            return hash;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            initData.GetUsedTransform(transformSet);
            preBuildData.GetUsedTransform(transformSet);
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            initData.ReplaceTransform(replaceDict);
            preBuildData.ReplaceTransform(replaceDict);
        }
    }
}
