using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            return GetRandomForInterval(GetCurrentInterval(), RefreshInterval);
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

        private static int GetSeedForInterval(int currentInterval, int intervalDays)
        {
            var worldSeed = ZNet.m_world?.m_seed ?? 0;
            var playerId = (int)(Player.m_localPlayer?.GetPlayerID() ?? 0);
            return unchecked(worldSeed + playerId + currentInterval * 1000 + intervalDays * 100);
        }

        protected static Random GetRandomForInterval(int currentInterval, int intervalDays)
        {
            return new Random(GetSeedForInterval(currentInterval, intervalDays));
        }

        public static ItemDrop CreateItemDrop(string prefabName)
        {
            var itemPrefab = ObjectDB.instance.GetItemPrefab(prefabName);
            if (itemPrefab == null)
            {
                return null;
            }

            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                return null;
            }

            ZNetView.m_forceDisableInit = true;
            var item = UnityEngine.Object.Instantiate(itemDrop);
            ZNetView.m_forceDisableInit = false;
            return item;
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
                var item = CreateItemDrop(itemId);
                if (item == null)
                {
                    EpicLoot.LogWarning($"[AdventureData] Could not find item type (gated={itemId} orig={itemConfig}) in ObjectDB!");
                    continue;
                }

                if (item.GetComponent<ItemDrop>() == null)
                {
                    EpicLoot.LogError($"[AdventureData] Item did not have ItemDrop (gated={itemId} orig={itemConfig}!");
                    continue;
                }

                var itemData = item.m_itemData;
                if (itemOkayToAddPredicate(itemData))
                {
                    results.Add(new SecretStashItemInfo(itemId, itemData, itemConfig.GetCost()));
                }
            }

            return results;
        }

        protected static void RollOnListNTimes<T>(Random random, List<T> list, int n, List<T> results)
        {
            for (var i = 0; i < n && i < list.Count; i++)
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
            const int maxRangeIncreases = 10;
            const int maxPointsInRange = 15;

            MerchantPanel.ShowInputBlocker(true);

            var rangeTries = 0;
            var radiusRange = GetTreasureMapSpawnRadiusRange(biome, saveData);
            while (rangeTries < maxRangeIncreases)
            {
                rangeTries++;

                var tries = 0;
                while (tries < maxPointsInRange)
                {
                    tries++;

                    var randomPoint = UnityEngine.Random.insideUnitCircle;
                    var mag = randomPoint.magnitude;
                    var normalized = randomPoint.normalized;
                    var actualMag = Mathf.Lerp(radiusRange.Item1, radiusRange.Item2, mag);
                    randomPoint = normalized * actualMag;
                    var spawnPoint = new Vector3(randomPoint.x, 0, randomPoint.y);

                    var zoneId = ZoneSystem.instance.GetZone(spawnPoint);
                    while (!ZoneSystem.instance.SpawnZone(zoneId, ZoneSystem.SpawnMode.Client, out _))
                    {
                        EpicLoot.LogWarning($"Spawning Zone ({zoneId})...");
                        yield return null;
                    }

                    ZoneSystem.instance.GetGroundData(ref spawnPoint, out var normal, out var foundBiome, out _, out _);
                    var groundHeight = spawnPoint.y;

                    EpicLoot.Log($"Checking biome at ({randomPoint}): {foundBiome} (try {tries})");
                    if (foundBiome != biome)
                    {
                        // Wrong biome
                        continue;
                    }

                    var solidHeight = ZoneSystem.instance.GetSolidHeight(spawnPoint);
                    var offsetFromGround = Math.Abs(solidHeight - groundHeight);
                    if (offsetFromGround > 5)
                    {
                        // Don't place too high off the ground (on top of tree or something?
                        EpicLoot.Log($"Spawn Point rejected: too high off of ground (groundHeight:{groundHeight}, solidHeight:{solidHeight})");
                        continue;
                    }

                    // But also don't place inside rocks
                    spawnPoint.y = solidHeight;

                    var placedNearPlayerBase = EffectArea.IsPointInsideArea(spawnPoint, EffectArea.Type.PlayerBase, AdventureDataManager.Config.TreasureMap.MinimapAreaRadius);
                    if (placedNearPlayerBase)
                    {
                        // Don't place near player base
                        EpicLoot.Log("Spawn Point rejected: too close to player base");
                        continue;
                    }

                    EpicLoot.Log($"Wards: {PrivateArea.m_allAreas.Count}");
                    var tooCloseToWard = PrivateArea.m_allAreas.Any(x => x.IsInside(spawnPoint, AdventureDataManager.Config.TreasureMap.MinimapAreaRadius));
                    if (tooCloseToWard)
                    {
                        EpicLoot.Log("Spawn Point rejected: too close to player ward");
                        continue;
                    }

                    var waterLevel = ZoneSystem.instance.m_waterLevel;
                    if (waterLevel > groundHeight + 1.0f)
                    {
                        // Too deep, try again
                        EpicLoot.Log($"Spawn Point rejected: too deep underwater (waterLevel:{waterLevel}, groundHeight:{groundHeight})");
                        continue;
                    }

                    EpicLoot.Log($"Success! (ground={groundHeight} water={waterLevel} placed={spawnPoint.y})");

                    onComplete?.Invoke(true, spawnPoint, normal);
                    MerchantPanel.ShowInputBlocker(false);
                    yield break;
                }

                radiusRange = new Tuple<float, float>(radiusRange.Item1 + 500, radiusRange.Item2 + 500);
            }

            onComplete?.Invoke(false, new Vector3(), new Vector3());
            MerchantPanel.ShowInputBlocker(false);
        }

        private static Tuple<float, float> GetTreasureMapSpawnRadiusRange(Heightmap.Biome biome, AdventureSaveData saveData)
        {
            var biomeInfoConfig = GetBiomeInfoConfig(biome);
            var minRadius = biomeInfoConfig?.MinRadius ?? 0;
            var maxRadius = biomeInfoConfig?.MaxRadius ?? 6000;
            var numberOfBounties = AdventureDataManager.CheatNumberOfBounties >= 0 ? AdventureDataManager.CheatNumberOfBounties : saveData.NumberOfTreasureMapsOrBountiesStarted;
            var increments = numberOfBounties / AdventureDataManager.Config.TreasureMap.IncreaseRadiusCount;
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
