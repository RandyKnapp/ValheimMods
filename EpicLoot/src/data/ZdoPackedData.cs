using Common;
using EpicLoot.Adventure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace EpicLoot.Data
{
    /// <summary>
    /// Byte-array ZDO values for adventure spawners. Any client can send a ZDO, so these bytes are untrusted:
    /// they are written as a ZPackage behind a marker and read back field by field. Spawners saved before
    /// that hold BinaryFormatter streams, which are still read, but only through a binder that resolves the
    /// handful of types those streams were made of and refuses everything else.
    /// </summary>
    internal static class ZdoPackedData
    {
        // A BinaryFormatter stream starts 00 01 00 00, so it can never begin with this.
        private const int Marker = 0x315A4C45; // "ELZ1"

        public static byte[] Encode(Action<ZPackage> write)
        {
            var pkg = new ZPackage();
            pkg.Write(Marker);
            write(pkg);
            return pkg.GetArray();
        }

        public static bool TryDecode<T>(byte[] stored, Func<ZPackage, T> read, out T value) where T : class
        {
            value = null;
            if (stored == null || stored.Length < 4)
            {
                return false;
            }

            try
            {
                if (BitConverter.ToInt32(stored, 0) == Marker)
                {
                    var pkg = new ZPackage(stored);
                    pkg.ReadInt();
                    value = read(pkg);
                }
                else
                {
                    using var stream = new MemoryStream(stored);
                    var formatter = new BinaryFormatter { Binder = new LegacyAdventureBinder() };
                    value = formatter.Deserialize(stream) as T;
                }
            }
            catch (Exception e)
            {
                EpicLoot.LogWarning($"Could not read adventure spawner data ({typeof(T).Name}): {e.Message}");
                value = null;
            }

            return value != null;
        }

        private sealed class LegacyAdventureBinder : SerializationBinder
        {
            private static readonly Dictionary<string, Type> Allowed = new()
            {
                [typeof(BountyInfo).FullName] = typeof(BountyInfo),
                [typeof(BountyTargetInfo).FullName] = typeof(BountyTargetInfo),
                [typeof(BountyTargetInfo[]).FullName] = typeof(BountyTargetInfo[]),
                [typeof(BountyState).FullName] = typeof(BountyState),
                [typeof(TreasureMapChestInfo).FullName] = typeof(TreasureMapChestInfo),
                [typeof(TreasureMapState).FullName] = typeof(TreasureMapState),
                [typeof(SerializableVector3).FullName] = typeof(SerializableVector3),
                [typeof(Heightmap.Biome).FullName] = typeof(Heightmap.Biome),
            };

            private static readonly string BountyTargetListPrefix =
                $"System.Collections.Generic.List`1[[{typeof(BountyTargetInfo).FullName},";

            public override Type BindToType(string assemblyName, string typeName)
            {
                if (Allowed.TryGetValue(typeName, out Type type))
                {
                    return type;
                }

                if (typeName.StartsWith(BountyTargetListPrefix, StringComparison.Ordinal))
                {
                    return typeof(List<BountyTargetInfo>);
                }

                throw new SerializationException($"Type {typeName} is not allowed in adventure spawner data");
            }
        }
    }
}
