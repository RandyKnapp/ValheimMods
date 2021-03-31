using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace EpicLoot.Adventure.Feature
{
    public enum AdventureFeatureType
    {
        None,
        SecretStash,
        Gamble,
        TreasureMaps,
        Bounties
    }

    public abstract class AdventureFeature
    {
        public abstract AdventureFeatureType Type { get; }
        public abstract int RefreshInterval { get; }

        public int GetSecondsUntilRefresh()
        {
            return GetSecondsUntilIntervalRefresh(RefreshInterval);
        }

        public int GetCurrentInterval()
        {
            return GetCurrentInterval(RefreshInterval);
        }

        public Random GetRandom()
        {
            return GetRandomForInterval(RefreshInterval);
        }

        protected static int GetSecondsUntilIntervalRefresh(int intervalDays)
        {
            if (ZNet.m_world == null || EnvMan.instance == null)
            {
                return -1;
            }

            var currentDay = EnvMan.instance.GetCurrentDay();
            var startOfNextInterval = GetNextMultiple(currentDay, intervalDays);
            var daysRemaining = (startOfNextInterval - currentDay) - EnvMan.instance.m_smoothDayFraction;
            return (int)(daysRemaining * EnvMan.instance.m_dayLengthSec);
        }

        protected static int GetNextMultiple(int n, int multiple)
        {
            return ((n / multiple) + 1) * multiple;
        }

        protected static int GetCurrentInterval(int intervalDays)
        {
            var currentDay = EnvMan.instance.GetCurrentDay();
            return currentDay / intervalDays;
        }

        private static int GetSeedForInterval(int currentInterval)
        {
            return unchecked((ZNet.m_world?.m_seed ?? 0) + currentInterval * 100);
        }

        protected static Random GetRandomForInterval(int currentInterval)
        {
            return new Random(GetSeedForInterval(currentInterval));
        }

        public static List<SecretStashItemInfo> CollectItems(List<SecretStashItemConfig> itemList)
        {
            return CollectItems(itemList, (x) => x.Item, (x) => true);
        }

        protected static List<SecretStashItemInfo> CollectItems(
            List<SecretStashItemConfig> itemList,
            Func<SecretStashItemConfig, string> itemIdPredicate,
            Func<ItemDrop.ItemData, bool> itemOkayToAddPredicate)
        {
            var results = new List<SecretStashItemInfo>();
            foreach (var itemConfig in itemList)
            {
                var itemId = itemIdPredicate(itemConfig);
                var itemPrefab = ObjectDB.instance.GetItemPrefab(itemId);
                if (itemPrefab == null)
                {
                    EpicLoot.LogWarning($"[AdventureData] Could not find item type (gated={itemId} orig={itemConfig.Item}) in ObjectDB!");
                    continue;
                }

                var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    EpicLoot.LogError($"[AdventureData] Item did not have ItemDrop (gated={itemId} orig={itemConfig.Item}!");
                    continue;
                }

                var itemData = itemDrop.m_itemData.Clone();
                if (itemOkayToAddPredicate(itemData))
                {
                    results.Add(new SecretStashItemInfo(itemId, itemData, itemConfig.GetCost()));
                }
            }

            return results;
        }

        protected static void RollOnListNTimes<T>(Random random, List<T> list, int n, List<T> results)
        {
            for (var i = 0; i < n; i++)
            {
                var index = random.Next(0, list.Count);
                var item = list[index];
                results.Add(item);
                list.RemoveAt(index);
            }
        }

        protected static T RollOnList<T>(Random random, List<T> list)
        {
            var index = random.Next(0, list.Count);
            return list[index];
        }

        protected static IEnumerator GetRandomPointInBiome(Heightmap.Biome biome, AdventureSaveData saveData, Action<bool, Vector3, Vector3> onComplete)
        {
            MerchantPanel.ShowInputBlocker(true);
            var rangeTries = 0;
            var radiusRange = GetTreasureMapSpawnRadiusRange(biome, saveData);
            while (rangeTries < 10)
            {
                rangeTries++;

                var tries = 0;
                while (tries < 10)
                {
                    tries++;

                    var randomPoint = UnityEngine.Random.insideUnitCircle;
                    var mag = randomPoint.magnitude;
                    var normalized = randomPoint.normalized;
                    var actualMag = Mathf.Lerp(radiusRange.Item1, radiusRange.Item2, mag);
                    randomPoint = normalized * actualMag;
                    var spawnPoint = new Vector3(randomPoint.x, 0, randomPoint.y);

                    var zoneId = ZoneSystem.instance.GetZone(spawnPoint);
                    while (!ZoneSystem.instance.SpawnZone(zoneId, ZoneSystem.SpawnMode.Full, out _))
                    {
                        EpicLoot.LogWarning($"Spawning Zone ({zoneId})...");
                        yield return null;
                    }

                    ZoneSystem.instance.GetGroundData(ref spawnPoint, out var normal, out var foundBiome, out _, out _);

                    EpicLoot.Log($"Checking biome at ({randomPoint}): {foundBiome} (try {tries})");
                    if (foundBiome != biome)
                    {
                        // Wrong biome
                        continue;
                    }

                    var waterLevel = ZoneSystem.instance.m_waterLevel;
                    var groundHeight = spawnPoint.y;
                    if (waterLevel > groundHeight + 1.0f)
                    {
                        // Too deep, try again
                        continue;
                    }

                    EpicLoot.Log($"Success! (ground={groundHeight} water={waterLevel} placed={spawnPoint.y})");

                    onComplete?.Invoke(true, spawnPoint, normal);
                    MerchantPanel.ShowInputBlocker(false);
                    yield break;
                }

                radiusRange = new Tuple<float, float>(radiusRange.Item1 + 500, radiusRange.Item2 + 500);
            }
            MerchantPanel.ShowInputBlocker(false);
        }

        private static Tuple<float, float> GetTreasureMapSpawnRadiusRange(Heightmap.Biome biome, AdventureSaveData saveData)
        {
            var biomeInfoConfig = GetBiomeInfoConfig(biome);
            var minRadius = biomeInfoConfig?.MinRadius ?? 0;
            var maxRadius = biomeInfoConfig?.MaxRadius ?? 6000;
            var increments = saveData.NumberOfTreasureMapsOrBountiesStarted / AdventureDataManager.Config.TreasureMap.IncreaseRadiusCount;
            var min = Mathf.Min(AdventureDataManager.Config.TreasureMap.StartRadiusMin + increments * AdventureDataManager.Config.TreasureMap.RadiusInterval, minRadius);
            var max = Mathf.Min(AdventureDataManager.Config.TreasureMap.StartRadiusMax + increments * AdventureDataManager.Config.TreasureMap.RadiusInterval, maxRadius);
            return new Tuple<float, float>(min, max);
        }

        private static TreasureMapBiomeInfoConfig GetBiomeInfoConfig(Heightmap.Biome biome)
        {
            return AdventureDataManager.Config.TreasureMap.BiomeInfo.Find(x => x.Biome == biome);
        }
    }
}
