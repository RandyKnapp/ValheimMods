using EpicLoot.Adventure;
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

    internal class BountyInfoZNetProperty : ZNetProperty<BountyInfo>
    {
        public BountyInfoZNetProperty(string key, ZNetView zNetView, BountyInfo defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override BountyInfo Get()
        {
            return ZdoPackedData.TryDecode(zNetView.GetZDO().GetByteArray(Key), BountyInfo.FromPackage, out BountyInfo value)
                ? value
                : new BountyInfo();
        }

        protected override void SetValue(BountyInfo value)
        {
            zNetView.GetZDO().Set(Key, ZdoPackedData.Encode(value.ToPackage));
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
        public TreasureMapChestInfoZNetProperty(string key, ZNetView zNetView, TreasureMapChestInfo defaultValue) : base(key, zNetView, defaultValue)
        {
        }

        public override TreasureMapChestInfo Get()
        {
            return ZdoPackedData.TryDecode(zNetView.GetZDO().GetByteArray(Key), TreasureMapChestInfo.FromPackage,
                out TreasureMapChestInfo value)
                ? value
                : new TreasureMapChestInfo();
        }

        protected override void SetValue(TreasureMapChestInfo value)
        {
            zNetView.GetZDO().Set(Key, ZdoPackedData.Encode(value.ToPackage));
        }
    }
}
