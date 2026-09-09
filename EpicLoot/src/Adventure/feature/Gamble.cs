using EpicLoot.Config;
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

        /// <summary>
        /// Building the candidate pool draws a variable number of times (once per gamble type, per
        /// valid boss tier, plus fallback recursion), so it gets its own stream. Selection stays on
        /// stream 0 -- the seed this feature has always used.
        /// </summary>
        private const int PoolStream = 1;

        public List<SecretStashItemInfo> GetGambleItems()
        {
            var player = Player.m_localPlayer;
            if (player == null || AdventureDataManager.Config == null)
            {
                return new List<SecretStashItemInfo>();
            }

            var random = GetRandom();

            List<SecretStashItemInfo> availableGambles = GetAvailableGambles(GetRandom(PoolStream));
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

            // Offers the player already took this interval drop out for the rest of it. Filtering
            // after the roll rather than before it keeps the roll interval-deterministic: the list
            // only ever shrinks, the remaining offers never shuffle or get replaced.
            if (ELConfig.RemovePurchasedGambles.Value)
            {
                var saveData = player.GetAdventureSaveData();
                var interval = GetCurrentInterval();
                results.RemoveAll(x => saveData.HasPurchasedGamble(interval, GetGambleID(x)));
            }

            return results;
        }

        /// <summary>
        /// Identifies one offer well enough to record that it was bought.
        ///
        /// The same item can appear both as a plain coin gamble and as a token gamble with a
        /// guaranteed rarity, and those are separate offers that have to be bought separately -- so
        /// the rarity is part of the key. Within one currency tier the pool never holds the same item
        /// twice, because GetAvailableGambles passes allowDuplicate: false.
        /// </summary>
        public static string GetGambleID(SecretStashItemInfo itemInfo)
        {
            return itemInfo.GuaranteedRarity ? $"{itemInfo.ItemID}|{itemInfo.Rarity}" : itemInfo.ItemID;
        }

        /// <summary>
        /// Records a taken offer. Deliberately not gated on the config: the setting controls whether
        /// the record is *read*, so turning it on mid-interval behaves the same as having had it on
        /// all along. Re-recording the same offer is a no-op.
        /// </summary>
        public void RecordGamblePurchase(Player player, SecretStashItemInfo itemInfo)
        {
            if (player == null || itemInfo == null || !itemInfo.IsGamble)
            {
                return;
            }

            player.GetAdventureSaveData().PurchasedGamble(GetCurrentInterval(), GetGambleID(itemInfo));
        }

        /// <summary>
        /// <paramref name="random"/> is threaded all the way down to the shuffle inside
        /// GatedItemTypeHelper. Without it the pool was drawn from the global Unity RNG, so both its
        /// contents AND its order changed on every call -- and since the stock is chosen by index,
        /// the merchant rerolled every time the panel opened, after every purchase, and on relog.
        /// </summary>
        private List<SecretStashItemInfo> GetAvailableGambles(Random random)
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
                        itemConfig, gatingMode, selectedItems, validBosses, false, false, false, random);
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

            // Deliberately unseeded: this is the roll made when the player BUYS the gamble, not the
            // stock list. Tying it to the interval seed would make every gamble bought within one
            // interval come out the same rarity. Same goes for the RollLootTable call below.
            var random = new Random();
            var totalWeight = gambleRarity.Sum();
            var nonMagic = (random.NextDouble() * totalWeight) < nonMagicWeight;
            if (nonMagic)
            {
                return itemInfo.Item.Clone();
            }

            // Column 0 was the non-magic weight (handled above); columns 1.. are one per rarity.
            var rarityTable = new float[Rarities.Count];
            for (var i = 0; i < rarityTable.Length; i++)
            {
                rarityTable[i] = gambleRarity.Length > i + 1 ? gambleRarity[i + 1] : 1;
            }

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
