// Magica Cloth 2.
// Copyright (c) 2025 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    [System.Serializable]
    public class TransformRecordSerializeData : ITransform
    {
        public Transform transform; // 利用しないが念の為に記録
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale; // lossy scale
        public Matrix4x4 localToWorldMatrix;
        public Matrix4x4 worldToLocalMatrix;

        public void Serialize(TransformRecord tr)
        {
            Debug.Assert(tr != null);

            transform = tr.transform;
            localPosition = tr.localPosition;
            localRotation = tr.localRotation;
            position = tr.position;
            rotation = tr.rotation;
            scale = tr.scale;
            localToWorldMatrix = tr.localToWorldMatrix;
            worldToLocalMatrix = tr.worldToLocalMatrix;
        }

        public void Deserialize(TransformRecord tr)
        {
            Debug.Assert(tr != null);

            // 座標系のみ復元する
            tr.localPosition = localPosition;
            tr.localRotation = localRotation;
            tr.position = position;
            tr.rotation = rotation;
            tr.scale = scale;
            tr.localToWorldMatrix = localToWorldMatrix;
            tr.worldToLocalMatrix = worldToLocalMatrix;
        }

        public int GetLocalHash()
        {
            int hash = 0;
            if (transform)
                hash += (123 + transform.childCount * 345);

            return hash;
        }

        public int GetGlobalHash()
        {
            int hash = 0;

            // ローカル姿勢のみ
            hash += localPosition.GetHashCode();
            hash += localRotation.GetHashCode();

            return hash;
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            if (transform)
                transformSet.Add(transform);
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            MagicaObjectId id = transform != null ? transform.GetMagicaId() : MagicaObjectId.Invalid;
            if (id.IsValid() && replaceDict.ContainsKey(id))
                transform = replaceDict[id];
        }
    }
}
