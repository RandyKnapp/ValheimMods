using EpicLoot.GatedItemType;
using System.Collections.Generic;
using System.Linq;
using Random = System.Random;

namespace EpicLoot.Adventure.Feature
{
    public class GambleAdventureFeature : AdventureFeature
    {
        public override AdventureFeatureType Type => AdventureFeatureType.Gamble;
        public override int RefreshInterval => AdventureDataManager.Config.Gamble.RefreshInterval;

        public List<SecretStashItemInfo> GetGambleItems()
        {
            var player = Player.m_localPlayer;
            if (player == null || AdventureDataManager.Config == null)
            {
                return new List<SecretStashItemInfo>();
            }

            var random = GetRandom();

            List<SecretStashItemInfo> availableGambles = GetAvailableGambles();
            RollOnListNTimes(random, availableGambles, AdventureDataManager.Config.Gamble.GamblesCount,
                out List<SecretStashItemInfo> results);

            RollOnListNTimes(random, availableGambles,
                AdventureDataManager.Config.Gamble.ForestTokenGamblesCount,
                out List<SecretStashItemInfo> forestTokenGambles);

            foreach (var forestTokenGamble in forestTokenGambles)
            {
                var cost = forestTokenGamble.Cost.Clone();
                SecretStashItemInfo config = new SecretStashItemInfo(
                    forestTokenGamble.ItemID, forestTokenGamble.Item, cost, true);
                config.Cost.Coins = (int)(forestTokenGamble.Cost.Coins *
                    AdventureDataManager.Config.Gamble.ForestTokenGambleCoinsCost);
                config.Cost.ForestTokens = random.Next(
                    AdventureDataManager.Config.Gamble.ForestTokenGambleCostMin,
                    AdventureDataManager.Config.Gamble.ForestTokenGambleCostMax + 1);
                config.Cost.IronBountyTokens = 0;
                config.Cost.GoldBountyTokens = 0;
                config.GuaranteedRarity = true;
                config.Rarity = ItemRarity.Rare;
                results.Add(config);
            }

            RollOnListNTimes(random, availableGambles,
                AdventureDataManager.Config.Gamble.IronBountyGamblesCount,
                out List<SecretStashItemInfo> ironBountyGambles);
            foreach (var ironBountyGamble in ironBountyGambles)
            {
                var cost = ironBountyGamble.Cost.Clone();
                SecretStashItemInfo config = new SecretStashItemInfo(
                    ironBountyGamble.ItemID, ironBountyGamble.Item, cost, true);
                config.Cost.Coins = (int)(ironBountyGamble.Cost.Coins * AdventureDataManager.Config.Gamble.IronBountyGambleCoinsCost);
                config.Cost.ForestTokens = 0;
                config.Cost.IronBountyTokens = AdventureDataManager.Config.Gamble.IronBountyGambleCost;
                config.Cost.GoldBountyTokens = 0;
                config.GuaranteedRarity = true;
                config.Rarity = ItemRarity.Epic;
                results.Add(config);
            }

            RollOnListNTimes(random, availableGambles,
                AdventureDataManager.Config.Gamble.GoldBountyGamblesCount,
                out List<SecretStashItemInfo> goldBountyGambles);
            foreach (var goldBountyGamble in goldBountyGambles)
            {
                var cost = goldBountyGamble.Cost.Clone();
                SecretStashItemInfo config = new SecretStashItemInfo(
                    goldBountyGamble.ItemID, goldBountyGamble.Item, cost, true);
                config.Cost.Coins = (int)(goldBountyGamble.Cost.Coins *
                    AdventureDataManager.Config.Gamble.GoldBountyGambleCoinsCost);
                config.Cost.ForestTokens = 0;
                config.Cost.IronBountyTokens = 0;
                config.Cost.GoldBountyTokens = AdventureDataManager.Config.Gamble.GoldBountyGambleCost;
                config.GuaranteedRarity = true;
                config.Rarity = ItemRarity.Legendary;
                results.Add(config);
            }

            results = SortListByRarity(results);

            return results;
        }

        private List<SecretStashItemInfo> GetAvailableGambles()
        {
            var availableGambles = new List<SecretStashItemInfo>();
            var selectedItems = new HashSet<string>();
            foreach (var itemConfig in AdventureDataManager.Config.Gamble.Gambles)
            {
                // Entirely possible we want to pull out the number of items we grab here to be configurable
                // If this is reduced to one, it means we basically grab current biome weapons 100% of the time-
                // assuming that type is available
                // Since vanilla is missing a handful of weapon types this does result in a few previous
                // biome items and regular duplicates without grabbing 2 items
                int itemsPerType = 2;
                var gatingMode = EpicLoot.GetGatedItemTypeMode();
                if (gatingMode == GatedItemTypeMode.Unlimited)
                {
                    gatingMode = GatedItemTypeMode.PlayerMustKnowRecipe;
                }

                for (int i = 0; i < itemsPerType; i++)
                {
                    if (string.IsNullOrEmpty(itemConfig))
                    {
                        continue;
                    }

                    List<string> validBosses = GatedItemTypeHelper.DetermineValidBosses(gatingMode, false);

                    var itemId = GatedItemTypeHelper.GetGatedItemFromType(
                        itemConfig, gatingMode, selectedItems, validBosses, false, false, false);
                    if (string.IsNullOrEmpty(itemId))
                    {
                        continue;
                    }

                    var itemDrop = CreateItemDrop(itemId);
                    if (itemDrop == null)
                    {
                        continue;
                    }

                    var itemData = itemDrop.m_itemData;
                    var cost = GetGambleCost(itemId);
                    availableGambles.Add(new SecretStashItemInfo(itemId, itemData, cost, true));
                    selectedItems.Add(itemId);
                    ZNetScene.instance.Destroy(itemDrop.gameObject);
                }
            }

            return availableGambles;
        }

        public Currencies GetGambleCost(string itemId)
        {
            var costConfig = AdventureDataManager.Config.Gamble.GambleCosts.Find(x => x.Item == itemId);
            return new Currencies()
            {
                Coins = costConfig?.CoinsCost ?? 10000,
                ForestTokens = costConfig?.ForestTokenCost ?? 0,
                IronBountyTokens = costConfig?.IronBountyTokenCost ?? 0,
                GoldBountyTokens = costConfig?.GoldBountyTokenCost ?? 0
            };
        }

        public ItemDrop.ItemData GenerateGambleItem(SecretStashItemInfo itemInfo)
        {
            var gambleRarity = AdventureDataManager.Config.Gamble.GambleRarityChance;
            if (itemInfo.GuaranteedRarity)
            {
                gambleRarity = AdventureDataManager.Config.Gamble.GambleRarityChanceByRarity[(int)itemInfo.Rarity];
            }

            var nonMagicWeight = gambleRarity.Length > 0 ? gambleRarity[0] : 1;

            var random = new Random();
            var totalWeight = gambleRarity.Sum();
            var nonMagic = (random.NextDouble() * totalWeight) < nonMagicWeight;
            if (nonMagic)
            {
                return itemInfo.Item.Clone();
            }

            var rarityTable = new[]
            {
                gambleRarity.Length > 1 ? gambleRarity[1] : 1,
                gambleRarity.Length > 2 ? gambleRarity[2] : 1,
                gambleRarity.Length > 3 ? gambleRarity[3] : 1,
                gambleRarity.Length > 4 ? gambleRarity[4] : 1,
                gambleRarity.Length > 5 ? gambleRarity[5] : 1
            };

            var lootTable = new LootTable()
            {
                Object = "Console",
                LeveledLoot = new List<LeveledLootDef>() {
                    new LeveledLootDef() {
                        Level = 1,
                        Drops = new[] { new float[] { 1, 1 } },
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

            var previousDisabledState = LootRoller.CheatDisableGating;
            LootRoller.CheatDisableGating = true;
            LootRoller.CheatRollingItem = true;
            var loot = LootRoller.RollLootTable(lootTable, 1, "Gamble", Player.m_localPlayer.transform.position);
            LootRoller.CheatRollingItem = false;
            LootRoller.CheatDisableGating = previousDisabledState;
            return loot.Count > 0 ? loot[0] : null;
        }
    }
}
