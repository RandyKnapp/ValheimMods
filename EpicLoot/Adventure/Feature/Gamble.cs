using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.GatedItemType;

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
            var results = new List<SecretStashItemInfo>();

            var availableGambles = GetAvailableGambles();
            RollOnListNTimes(random, availableGambles.ToList(), AdventureDataManager.Config.Gamble.GamblesCount, results);

            availableGambles = GetAvailableGambles();
            var forestTokenGambles = new List<SecretStashItemInfo>();
            RollOnListNTimes(random, availableGambles.ToList(), AdventureDataManager.Config.Gamble.ForestTokenGamblesCount, forestTokenGambles);
            foreach (var forestTokenGamble in forestTokenGambles)
            {
                forestTokenGamble.Cost.Coins = (int)(forestTokenGamble.Cost.Coins * AdventureDataManager.Config.Gamble.ForestTokenGambleCoinsCost);
                forestTokenGamble.Cost.ForestTokens = random.Next(AdventureDataManager.Config.Gamble.ForestTokenGambleCostMin, AdventureDataManager.Config.Gamble.ForestTokenGambleCostMax + 1);
                forestTokenGamble.GuaranteedRarity = true;
                forestTokenGamble.Rarity = ItemRarity.Rare;
                results.Add(forestTokenGamble);
            }

            availableGambles = GetAvailableGambles();
            var ironBountyGambles = new List<SecretStashItemInfo>();
            RollOnListNTimes(random, availableGambles.ToList(), AdventureDataManager.Config.Gamble.IronBountyGamblesCount, ironBountyGambles);
            foreach (var ironBountyGamble in ironBountyGambles)
            {
                ironBountyGamble.Cost.Coins = (int)(ironBountyGamble.Cost.Coins * AdventureDataManager.Config.Gamble.IronBountyGambleCoinsCost);
                ironBountyGamble.Cost.IronBountyTokens = AdventureDataManager.Config.Gamble.IronBountyGambleCost;
                ironBountyGamble.GuaranteedRarity = true;
                ironBountyGamble.Rarity = ItemRarity.Epic;
                results.Add(ironBountyGamble);
            }

            availableGambles = GetAvailableGambles();
            var goldBountyGambles = new List<SecretStashItemInfo>();
            RollOnListNTimes(random, availableGambles.ToList(), AdventureDataManager.Config.Gamble.GoldBountyGamblesCount, goldBountyGambles);
            foreach (var goldBountyGamble in goldBountyGambles)
            {
                goldBountyGamble.Cost.Coins = (int)(goldBountyGamble.Cost.Coins * AdventureDataManager.Config.Gamble.GoldBountyGambleCoinsCost);
                goldBountyGamble.Cost.GoldBountyTokens = AdventureDataManager.Config.Gamble.GoldBountyGambleCost;
                goldBountyGamble.GuaranteedRarity = true;
                goldBountyGamble.Rarity = ItemRarity.Legendary;
                results.Add(goldBountyGamble);
            }

            return results;
        }

        private List<SecretStashItemInfo> GetAvailableGambles()
        {
            var availableGambles = new List<SecretStashItemInfo>();
            foreach (var itemConfig in AdventureDataManager.Config.Gamble.Gambles)
            {
                var gatingMode = EpicLoot.GetGatedItemTypeMode();
                if (gatingMode == GatedItemTypeMode.Unlimited)
                {
                    gatingMode = GatedItemTypeMode.MustKnowRecipe;
                }

                var itemId = GatedItemTypeHelper.GetGatedItemID(itemConfig, gatingMode);
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
                var cost = GetGambleCost(itemId);
                availableGambles.Add(new SecretStashItemInfo(itemId, itemData, cost, true));
            }

            return availableGambles;
        }

        public Currencies GetGambleCost(string itemId)
        {
            var costConfig = AdventureDataManager.Config.Gamble.GambleCosts.Find(x => x.Item == itemId);
            return new Currencies()
            {
                Coins = costConfig?.CoinsCost ?? 1,
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
            var lootTable = new LootTable()
            {
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

            var previousDisabledState = LootRoller.CheatDisableGating;
            LootRoller.CheatDisableGating = true;
            var loot = LootRoller.RollLootTable(lootTable, 1, "Gamble", Player.m_localPlayer.transform.position);
            LootRoller.CheatDisableGating = previousDisabledState;
            return loot.Count > 0 ? loot[0] : null;
        }
    }
}
