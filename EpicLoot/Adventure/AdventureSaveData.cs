using System;
using System.Collections.Generic;
using Common;
using UnityEngine;

namespace EpicLoot.Adventure
{
    [Serializable]
    public class TreasureMapChestInfo
    {
        public int Interval;
        public Heightmap.Biome Biome;
        public SerializableVector3 Position;
        public SerializableVector3 MinimapCircleOffset;
    }

    [Serializable]
    public class AdventureSaveData
    {
        public int NumberOfTreasureMapsStarted;
        public List<TreasureMapChestInfo> PurchasedTreasureMaps = new List<TreasureMapChestInfo>();
        public List<TreasureMapChestInfo> FoundTreasureMapChests = new List<TreasureMapChestInfo>();

        public bool PurchasedTreasureMap(int interval, Heightmap.Biome biome, Vector3 position, Vector3 circleOffset)
        {
            if (HasPurchasedTreasureMap(interval, biome))
            {
                EpicLoot.LogError($"Player has already purchased treasure map! (interval={interval} biome={biome})");
                return false;
            }

            PurchasedTreasureMaps.Add(new TreasureMapChestInfo()
            {
                Interval = interval,
                Biome = biome,
                Position = position,
                MinimapCircleOffset = circleOffset
            });

            NumberOfTreasureMapsStarted++;

            return true;
        }

        public bool FoundTreasureChest(int interval, Heightmap.Biome biome)
        {
            if (!HasFoundTreasureMapChest(interval, biome))
            {
                FoundTreasureMapChests.Add(new TreasureMapChestInfo()
                {
                    Interval = interval,
                    Biome = biome
                });
                return true;
            }

            return false;
        }

        public bool HasPurchasedTreasureMap(int interval, Heightmap.Biome biome)
        {
            return PurchasedTreasureMaps.Exists(x => x.Interval == interval && x.Biome == biome);
        }

        public bool HasFoundTreasureMapChest(int interval, Heightmap.Biome biome)
        {
            return FoundTreasureMapChests.Exists(x => x.Interval == interval && x.Biome == biome);
        }

        public List<TreasureMapChestInfo> GetUnfoundTreasureChests()
        {
            var results = new List<TreasureMapChestInfo>();
            foreach (var info in PurchasedTreasureMaps)
            {
                if (!HasFoundTreasureMapChest(info.Interval, info.Biome))
                {
                    results.Add(info);
                }
            }

            return results;
        }
    }
}
