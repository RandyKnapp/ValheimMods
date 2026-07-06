using EpicLoot.Adventure;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using static EpicLoot.Data.CustomZNet;

namespace EpicLoot.Data
{
    internal class CustomZNet
    {
        public abstract class ZNetProperty<T>
        {
            public string Key { get; private set; }
            public T DefaultValue { get; private set; }
            protected readonly ZNetView zNetView;

            protected ZNetProperty(string key, ZNetView zNetView, T defaultValue)
            {
                Key = key;
                DefaultValue = defaultValue;
                this.zNetView = zNetView;
            }

            private void ClaimOwnership()
            {
                if (!zNetView.IsOwner())
                {
                    zNetView.ClaimOwnership();
                }
            }

            public void Set(T value)
            {
                SetValue(value);
            }

            public void ForceSet(T value)
            {
                ClaimOwnership();
                Set(value);
            }

            public abstract T Get();

            protected abstract void SetValue(T value);
        }
    }

    internal class BoolZNetProperty : ZNetProperty<bool>
    {
        public BoolZNetProperty(string key, ZNetView zNetView, bool defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override bool Get()
        {
            return zNetView.GetZDO().GetBool(Key, DefaultValue);
        }

        protected override void SetValue(bool value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }

    internal class LongZNetProperty : ZNetProperty<long>
    {
        public LongZNetProperty(string key, ZNetView zNetView, long defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override long Get()
        {
            return zNetView.GetZDO().GetLong(Key, DefaultValue);
        }

        protected override void SetValue(long value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }

    internal class IntZNetProperty : ZNetProperty<int>
    {
        public IntZNetProperty(string key, ZNetView zNetView, int defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override int Get()
        {
            return zNetView.GetZDO().GetInt(Key, DefaultValue);
        }

        protected override void SetValue(int value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }

    internal class ListVector3ZNetProperty : ZNetProperty<List<Vector3>>
    { 
        BinaryFormatter binFormatter = new BinaryFormatter();
        public ListVector3ZNetProperty(string key, ZNetView zNetView, List<Vector3> defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override List<Vector3> Get()
        {
            var stored = zNetView.GetZDO().GetByteArray(Key);
            // we can't deserialize a null buffer
            if (stored == null) { return new List<Vector3>(); }
            var mStream = new MemoryStream(stored);
            var binDeserialized = (List<Vector3>)binFormatter.Deserialize(mStream);

            return binDeserialized;
        }

        protected override void SetValue(List<Vector3> value)
        {
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, value);

            zNetView.GetZDO().Set(Key, mStream.ToArray());
        }
    }

    // BountyInfo Znet object stored as binary
    // Maybe consider compression?
    internal class BountyInfoZNetProperty : ZNetProperty<BountyInfo>
    {
        BinaryFormatter binFormatter = new BinaryFormatter();
        public BountyInfoZNetProperty(string key, ZNetView zNetView, BountyInfo defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override BountyInfo Get()
        {
            var stored = zNetView.GetZDO().GetByteArray(Key);
            // we can't deserialize a null buffer
            if (stored == null) { return new BountyInfo(); }
            var mStream = new MemoryStream(stored);
            var binDeserialized = (BountyInfo)binFormatter.Deserialize(mStream);

            return binDeserialized;
        }

        protected override void SetValue(BountyInfo value)
        {
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, value);

            zNetView.GetZDO().Set(Key, mStream.ToArray());
        }
    }

    internal class Vector3ZNetProperty : ZNetProperty<Vector3>
    {
        public Vector3ZNetProperty(string key, ZNetView zNetView, Vector3 defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override Vector3 Get()
        {
            return zNetView.GetZDO().GetVec3(Key, DefaultValue);
        }

        protected override void SetValue(Vector3 value)
        {
            zNetView.GetZDO().Set(Key, value);
        }
    }

    internal class TreasureMapChestInfoZNetProperty : ZNetProperty<TreasureMapChestInfo>
    {
        BinaryFormatter binFormatter = new BinaryFormatter();
        public TreasureMapChestInfoZNetProperty(string key, ZNetView zNetView, TreasureMapChestInfo defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override TreasureMapChestInfo Get()
        {
            var stored = zNetView.GetZDO().GetByteArray(Key);
            // we can't deserialize a null buffer
            if (stored == null) { return new TreasureMapChestInfo(); }
            var mStream = new MemoryStream(stored);
            var binDeserialized = (TreasureMapChestInfo)binFormatter.Deserialize(mStream);

            return binDeserialized;
        }

        protected override void SetValue(TreasureMapChestInfo value)
        {
            var mStream = new MemoryStream();
            binFormatter.Serialize(mStream, value);

            zNetView.GetZDO().Set(Key, mStream.ToArray());
        }
    }
}
