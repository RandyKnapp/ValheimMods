using UnityEngine;
using System;
// ReSharper disable InconsistentNaming

namespace Common
{
    [Serializable]
    public struct SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
            => $"[{x}, {y}, {z}]";

        public static implicit operator Vector3(SerializableVector3 s)
            => new Vector3(s.x, s.y, s.z);

        public static implicit operator SerializableVector3(Vector3 v)
            => new SerializableVector3(v.x, v.y, v.z);

        public static SerializableVector3 operator +(SerializableVector3 a, SerializableVector3 b)
            => new SerializableVector3(a.x + b.x, a.y + b.y, a.z + b.z);

        public static SerializableVector3 operator -(SerializableVector3 a, SerializableVector3 b)
            => new SerializableVector3(a.x - b.x, a.y - b.y, a.z - b.z);

        public static SerializableVector3 operator -(SerializableVector3 a)
            => new SerializableVector3(-a.x, -a.y, -a.z);

        public static SerializableVector3 operator *(SerializableVector3 a, float m)
            => new SerializableVector3(a.x * m, a.y * m, a.z * m);

        public static SerializableVector3 operator *(float m, SerializableVector3 a)
            => new SerializableVector3(a.x * m, a.y * m, a.z * m);

        public static SerializableVector3 operator /(SerializableVector3 a, float d)
            => new SerializableVector3(a.x / d, a.y / d, a.z / d);
    }
}
