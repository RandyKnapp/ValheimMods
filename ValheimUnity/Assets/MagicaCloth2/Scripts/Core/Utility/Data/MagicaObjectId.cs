// Magica Cloth 2.
// Copyright (c) 2026 MagicaSoft.
// https://magicasoft.jp
using System;
using UnityEngine;

namespace MagicaCloth2
{
    /// <summary>
    /// Unityのバージョン差異（InstanceID vs EntityId）を吸収するID構造体。
    /// 現在はUnity6.4以上でEntityIdに切り替え
    /// </summary>
    public readonly struct MagicaObjectId : IEquatable<MagicaObjectId>
    {
        // -------------------------------------------------------
        // 内部データ保持
        // -------------------------------------------------------
#if UNITY_6000_4_OR_NEWER
        private readonly EntityId _value;

        public MagicaObjectId(EntityId id)
        {
            _value = id;
        }

        // 無効なIDの定義（EntityIdの仕様に合わせる）
        public static readonly MagicaObjectId Invalid = new(EntityId.None);

        public bool IsValid() => _value.IsValid();

#else
        private readonly int _value;

        public MagicaObjectId(int id)
        {
            _value = id;
        }

        // 従来のInstanceIDでは0が事実上の無効値として扱われることが多い
        public static readonly MagicaObjectId Invalid = new MagicaObjectId(0);

        public bool IsValid() => _value != 0;
#endif

        public bool Equals(MagicaObjectId other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            return obj is MagicaObjectId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static bool operator ==(MagicaObjectId left, MagicaObjectId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MagicaObjectId left, MagicaObjectId right)
        {
            return !left.Equals(right);
        }

        // これはデバッグ用途以外では使わない
        public override string ToString()
        {
            return _value.ToString();
        }
    }

    // -------------------------------------------------------
    // 拡張メソッド
    // -------------------------------------------------------
    public static class MagicaObjectIdExtensions
    {
        /// <summary>
        /// GameObjectやComponentからバージョンに合わせたIDを取得する
        /// </summary>
        public static MagicaObjectId GetMagicaId(this UnityEngine.Object obj)
        {
            if (obj == null) return MagicaObjectId.Invalid;

#if UNITY_6000_4_OR_NEWER
            // Unity 6.4 API 
            return new MagicaObjectId(obj.GetEntityId());
#else
            // 従来の API
            return new MagicaObjectId(obj.GetInstanceID());
#endif
        }
    }
}
