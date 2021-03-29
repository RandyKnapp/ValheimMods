using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common;
using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace EpicLoot.Adventure
{
    public class SecretStashItemInfo
    {
        public ItemDrop.ItemData Item;
        public int CoinsCost;
        public int ForestTokenCost;
        public string ItemID;
        public bool IsGamble;

        public SecretStashItemInfo(string itemId, ItemDrop.ItemData item, int coinsCost, int forestTokenCost, bool isGamble = false)
        {
            ItemID = itemId;
            Item = item;
            CoinsCost = coinsCost;
            ForestTokenCost = forestTokenCost;
            IsGamble = isGamble;
        }
    }

    public class TreasureMapItemInfo
    {
        public Heightmap.Biome Biome;
        public int Cost;
        public bool AlreadyPurchased;
    }

    //ZNet.m_world.m_seed
    //EnvMan.instance.GetCurrentDay()
    public static class AdventureDataManager
    {
        public const int SecretStashRefreshInterval = 1;
        public const int TreasureMapRefreshInterval = 2;
        public const int BountiesRefreshInterval = 4;

        public static AdventureDataConfig Config;

        public static void Initialize(AdventureDataConfig config)
        {
            Config = config;
        }

        public static int GetSecondsUntilSecretStashRefresh()
        {
            return GetSecondsUntilIntervalRefresh(SecretStashRefreshInterval);
        }

        public static int GetCurrentSecretStashInterval()
        {
            return GetCurrentInterval(SecretStashRefreshInterval);
        }

        public static List<SecretStashItemInfo> GetItemsForSecretStash()
        {
            var player = Player.m_localPlayer;
            if (player == null || Config == null)
            {
                return new List<SecretStashItemInfo>();
            }

            var random = GetRandomForSecretStash();
            var results = new List<SecretStashItemInfo>();

            var availableMaterialsList = CollectItems(Config.SecretStash.Materials, 
                (x) => x.Item, 
                (x) => x.IsMagic() || x.IsMagicCraftingMaterial() || x.IsRunestone());
            var availableMaterials = new MultiValueDictionary<ItemRarity, SecretStashItemInfo>();
            availableMaterialsList.ForEach(x => availableMaterials.Add(x.Item.GetRarity(), x));

            // Roll N times on each rarity list
            foreach (ItemRarity section in Enum.GetValues(typeof(ItemRarity)))
            {
                var items = availableMaterials.GetValues(section, true).ToList();
                var rolls = Config.SecretStash.RollsPerRarity[(int)section];
                RollOnListNTimes(random, items, rolls, results);
            }

            // Remove the results that the player doesn't know about yet
            results.RemoveAll(result =>
            {
                if (result.Item.IsMagicCraftingMaterial() || result.Item.IsRunestone())
                {
                    return !player.m_knownMaterial.Contains(result.Item.m_shared.m_name);
                }
                return false;
            });

            var availableOtherItems = CollectItems(Config.SecretStash.OtherItems);
            RollOnListNTimes(random, availableOtherItems, Config.SecretStash.OtherItemsRolls, results);

            return results;
        }

        public static List<SecretStashItemInfo> GetGamblesForSecretStash()
        {
            var player = Player.m_localPlayer;
            if (player == null || Config == null)
            {
                return new List<SecretStashItemInfo>();
            }

            var random = GetRandomForSecretStash();
            var results = new List<SecretStashItemInfo>();

            var availableGambles = new List<SecretStashItemInfo>();
            foreach (var itemConfig in Config.SecretStash.Gambles)
            {
                var itemId = GatedItemTypeHelper.GetGatedItemID(itemConfig, GatedItemTypeMode.MustHaveCrafted);
                var itemPrefab = ObjectDB.instance.GetItemPrefab(itemId);
                if (itemPrefab == null)
                {
                    EpicLoot.LogWarning($"[AdventureData] Could not find item type (gated={itemId} orig={itemConfig}) in ObjectDB!");
                    continue;
                }

                var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    EpicLoot.LogError($"[AdventureData] Item did not have ItemDrop (gated={itemId} orig={itemConfig}!");
                    continue;
                }

                var itemData = itemDrop.m_itemData.Clone();
                var cost = Config.SecretStash.GambleCosts.Find(x => x.Item == itemId);
                availableGambles.Add(new SecretStashItemInfo(itemId, itemData, cost?.CoinsCost ?? 1, cost?.ForestTokenCost ?? 0, true));
            }

            RollOnListNTimes(random, availableGambles, Config.SecretStash.GamblesCount, results);

            return results;
        }

        public static List<SecretStashItemInfo> CollectItems(List<SecretStashItemConfig> itemList)
        {
            return CollectItems(itemList, (x) => x.Item, (x) => true);
        }

        public static List<SecretStashItemInfo> CollectItems(
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
                    results.Add(new SecretStashItemInfo(itemId, itemData, itemConfig.CoinsCost, itemConfig.ForestTokenCost));
                }
            }

            return results;
        }

        public static void RollOnListNTimes<T>(Random random, List<T> list, int n, List<T> results)
        {
            for (var i = 0; i < n; i++)
            {
                var index = random.Next(0, list.Count);
                var item = list[index];
                results.Add(item);
                list.RemoveAt(index);
            }
        }

        public static int GetSecondsUntilTreasureMapRefresh()
        {
            return GetSecondsUntilIntervalRefresh(TreasureMapRefreshInterval);
        }

        public static int GetCurrentTreasureMapInterval()
        {
            return GetCurrentInterval(TreasureMapRefreshInterval);
        }

        public static int GetSecondsUntilBountiesRefresh()
        {
            return GetSecondsUntilIntervalRefresh(BountiesRefreshInterval);
        }

        public static int GetCurrentBountiesInterval()
        {
            return GetCurrentInterval(BountiesRefreshInterval);
        }

        public static int GetSecondsUntilIntervalRefresh(int intervalDays)
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

        public static int GetCurrentInterval(int intervalDays)
        {
            var currentDay = EnvMan.instance.GetCurrentDay();
            return currentDay / intervalDays;
        }

        private static int GetNextMultiple(int n, int multiple)
        {
            return ((n / multiple) + 1) * multiple;
        }

        private static int GetSeedForInterval(int currentInterval)
        {
            return unchecked((ZNet.m_world?.m_seed ?? 0) + currentInterval * 100);
        }

        private static Random GetRandomForInterval(int currentInterval)
        {
            return new Random(GetSeedForInterval(currentInterval));
        }

        private static Random GetRandomForSecretStash()
        {
            return GetRandomForInterval(GetCurrentSecretStashInterval());
        }

        public static ItemDrop.ItemData GenerateGambleItem(SecretStashItemInfo itemInfo)
        {
            var gambleRarity = Config.SecretStash.GambleRarityChance;
            var nonMagicWeight = gambleRarity.Length > 0 ? gambleRarity[0] : 1;

            var random = new Random();
            var totalWeight = gambleRarity.Sum();
            var nonMagic = random.Next(0, totalWeight) < nonMagicWeight;
            if (nonMagic)
            {
                return itemInfo.Item.Clone();
            }

            var rarityTable = new[]
            {
                gambleRarity.Length > 1 ? gambleRarity[1] : 1,
                gambleRarity.Length > 2 ? gambleRarity[2] : 1,
                gambleRarity.Length > 3 ? gambleRarity[3] : 1,
                gambleRarity.Length > 4 ? gambleRarity[4] : 1
            };
            var lootTable = new LootTable() {
                Object = "Console",
                LeveledLoot = new List<LeveledLootDef>() {
                    new LeveledLootDef() {
                        Level = 1,
                        Drops = new[] { new[] { 1, 1 } },
                        Loot = new[] {
                            new LootDrop() {
                                Item = itemInfo.ItemID,
                                Rarity = rarityTable,
                                Weight = 1
                            }
                        }
                    }
                }
            };

            var loot = LootRoller.RollLootTable(lootTable, 1, "Gamble", Player.m_localPlayer.transform.position);
            return loot.Count > 0 ? loot[0] : null;
        }

        public static List<TreasureMapItemInfo> GetTreasureMaps()
        {
            var results = new List<TreasureMapItemInfo>();

            var player = Player.m_localPlayer;
            var currentInterval = GetCurrentTreasureMapInterval();
            if (player != null)
            {
                var saveData = player.GetAdventureSaveData();
                foreach (var biome in player.m_knownBiome)
                {
                    var lootTableName = $"TreasureMapChest_{biome}";
                    var lootTableExists = LootRoller.GetLootTable(lootTableName).Count > 0;
                    if (lootTableExists)
                    {
                        var purchased = saveData.HasPurchasedTreasureMap(currentInterval, biome);
                        var cost = Config.TreasureMap.BiomeInfo.Find(x => x.Biome == biome);
                        results.Add(new TreasureMapItemInfo()
                        {
                            Biome = biome,
                            Cost = cost?.Cost ?? 99,
                            AlreadyPurchased = purchased
                        });
                    }
                }
            }

            return results.OrderBy(x => x.Cost).ToList();
        }

        public static List<SecretStashItemInfo> GetForestTokenItems()
        {
            var player = Player.m_localPlayer;
            if (player == null || Config == null)
            {
                return new List<SecretStashItemInfo>();
            }

            var results = CollectItems(Config.TreasureMap.SaleItems,
                (x) => x.Item,
                (x) => player.m_knownMaterial.Contains(x.m_shared.m_name));

            return results;
        }

        public static IEnumerator SpawnTreasureChest(Heightmap.Biome biome, Player player, Action<bool, Vector3> callback)
        {
            player.Message(MessageHud.MessageType.Center, "Preparing Treasure Map...");
            var saveData = player.GetAdventureSaveData();
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
                        Debug.LogWarning($"Spawning Zone ({zoneId})...");
                        yield return null;
                    }

                    ZoneSystem.instance.GetGroundData(ref spawnPoint, out var normal, out var foundBiome, out _, out _);

                    Debug.LogWarning($"Checking biome at ({randomPoint}): {foundBiome} (try {tries})");
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

                    Debug.LogWarning($"Success! (ground={groundHeight} water={waterLevel} placed={spawnPoint.y})");

                    const string treasureChestPrefabName = "piece_chest_wood";
                    var treasureChestPrefab = ZNetScene.instance.GetPrefab(treasureChestPrefabName);
                    var treasureChestObject = Object.Instantiate(treasureChestPrefab, spawnPoint, Quaternion.FromToRotation(Vector3.up, normal));
                    var treasureChest = treasureChestObject.AddComponent<TreasureMapChest>();
                    treasureChest.Setup(player, biome, GetCurrentTreasureMapInterval());

                    var offset2 = UnityEngine.Random.insideUnitCircle * Config.TreasureMap.MinimapAreaRadius;
                    var offset = new Vector3(offset2.x, 0, offset2.y);
                    saveData.PurchasedTreasureMap(GetCurrentTreasureMapInterval(), biome, spawnPoint, offset);
                    Minimap.instance.ShowPointOnMap(spawnPoint + offset);

                    callback?.Invoke(true, spawnPoint);
                    yield break;
                }

                radiusRange = new Tuple<float, float>(radiusRange.Item1 + 500, radiusRange.Item2 + 500);
            }

            callback?.Invoke(false, Vector3.zero);
        }

        private static Tuple<float, float> GetTreasureMapSpawnRadiusRange(Heightmap.Biome biome, AdventureSaveData saveData)
        {
            var biomeInfoConfig = GetBiomeInfoConfig(biome);
            var minRadius = biomeInfoConfig?.MinRadius ?? 0;
            var maxRadius = biomeInfoConfig?.MaxRadius ?? 6000;
            var increments = saveData.NumberOfTreasureMapsStarted / Config.TreasureMap.IncreaseRadiusCount;
            var min = Mathf.Min(Config.TreasureMap.StartRadiusMin + increments * Config.TreasureMap.RadiusInterval, minRadius);
            var max = Mathf.Min(Config.TreasureMap.StartRadiusMax + increments * Config.TreasureMap.RadiusInterval, maxRadius);
            return new Tuple<float, float>(min, max);
        }

        private static TreasureMapBiomeInfoConfig GetBiomeInfoConfig(Heightmap.Biome biome)
        {
            return Config.TreasureMap.BiomeInfo.Find(x => x.Biome == biome);
        }
    }
}
