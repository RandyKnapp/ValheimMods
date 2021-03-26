using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using Random = System.Random;

namespace EpicLoot.Adventure
{
    public class SecretStashItemInfo
    {
        public ItemDrop.ItemData Item;
        public int Cost;
        public string ItemID;
        public bool IsGamble;

        public SecretStashItemInfo(string itemId, ItemDrop.ItemData item, int cost, bool isGamble = false)
        {
            ItemID = itemId;
            Item = item;
            Cost = cost;
            IsGamble = isGamble;
        }
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
                availableGambles.Add(new SecretStashItemInfo(itemId, itemData, cost?.CoinsCost ?? 100, true));
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
                    results.Add(new SecretStashItemInfo(itemId, itemData, itemConfig.CoinsCost));
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
    }
}
