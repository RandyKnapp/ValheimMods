// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using System.Collections.Generic;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// トランスフォーム情報の一時記録
    /// </summary>
    public class TransformRecord : IValid, ITransform
    {
        public Transform transform;
        public MagicaObjectId id;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale; // lossy scale
        public Matrix4x4 localToWorldMatrix;
        public Matrix4x4 worldToLocalMatrix;
        public MagicaObjectId pid;

        public TransformRecord(Transform t, bool read)
        {
            if (t)
            {
                transform = t;
                id = t.GetMagicaId();

                if (read)
                {
                    localPosition = t.localPosition;
                    localRotation = t.localRotation;
                    position = t.position;
                    rotation = t.rotation;
                    scale = t.lossyScale;
                    localToWorldMatrix = t.localToWorldMatrix;
                    worldToLocalMatrix = t.worldToLocalMatrix;
                }

                if (t.parent)
                    pid = t.parent.GetMagicaId();
            }
        }

        public Vector3 InverseTransformDirection(Vector3 dir)
        {
            return Quaternion.Inverse(rotation) * dir;
        }

        public bool IsValid()
        {
            return id.IsValid();
        }

        public void GetUsedTransform(HashSet<Transform> transformSet)
        {
            transformSet.Add(transform);
        }

        public void ReplaceTransform(Dictionary<MagicaObjectId, Transform> replaceDict)
        {
            if (replaceDict.ContainsKey(id))
            {
                var t = replaceDict[id];
                transform = t;
                id = transform.GetMagicaId();
            }
            if (replaceDict.ContainsKey(pid))
            {
                var t = replaceDict[pid];
                pid = t.GetMagicaId();
            }
        }
    }
}
