using BepInEx;
using EpicLoot.Adventure;
using EpicLoot.Adventure.Feature;
using EpicLoot.General;
using Jotunn.Managers;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.GatedItemType
{
    public enum GatedPieceTypeMode
    {
        Unlimited,
        BossKillUnlocksCurrentBiomePieces,
        BossKillUnlocksNextBiomePieces
    }

    public enum GatedItemTypeMode
    {
        Unlimited,
        BossKillUnlocksCurrentBiomeItems,
        BossKillUnlocksNextBiomeItems,
        PlayerMustKnowRecipe,
        PlayerMustHaveCraftedItem
    }

    public class GatedItemDetails
    {
        public List<string> RequiredBosses { get; set; }
        public string Type { get; set; }
    }

    public class Fallback
    {
        public string Type { get; set; }
        public string Item { get; set; }
    }

    public static class GatedItemTypeHelper
    {
        public static ItemInfoConfig GatedConfig;

        public static Dictionary<string, Dictionary<string, List<string>>> ItemsByTypeAndBoss =
            new Dictionary<string, Dictionary<string, List<string>>>();
        public static Dictionary<string, GatedItemDetails> AllItemsWithDetails =
            new Dictionary<string, GatedItemDetails>();
        public static Dictionary<string, Fallback> FallbackByType = new Dictionary<string, Fallback>();

        public static List<Heightmap.Biome> BiomesInOrder = new List<Heightmap.Biome>();
        public static Dictionary<Heightmap.Biome, List<string>> BiomesToBossKeys = new Dictionary<Heightmap.Biome, List<string>>();

        public static void Initialize(ItemInfoConfig config)
        {
            GatedConfig = config;
            ItemsByTypeAndBoss.Clear();
            AllItemsWithDetails.Clear();
            FallbackByType.Clear();
            BiomesInOrder.Clear();
            BiomesToBossKeys.Clear();

            // Add to required lists
            foreach (ItemTypeInfo info in config.ItemInfo)
            {
                if (!FallbackByType.ContainsKey(info.Type))
                {
                    FallbackByType.Add(info.Type, new Fallback
                    {
                        Type = info.Fallback,
                        Item = info.ItemFallback
                    });
                }

                Dictionary<string, List<string>> itemsByBoss = new() { };
                foreach (KeyValuePair<string, List<string>> itemByBoss in info.ItemsByBoss)
                {
                    if (itemsByBoss.ContainsKey(itemByBoss.Key))
                    {
                        EpicLoot.Log($"Merging [{itemByBoss.Key}] entries, duplicates will be removed.");
                        itemsByBoss[itemByBoss.Key].Union(itemByBoss.Value).ToList();
                    }
                    else
                    {
                        itemsByBoss.Add(itemByBoss.Key, itemByBoss.Value);
                    }

                    foreach (string item in itemByBoss.Value)
                    {
                        if (AllItemsWithDetails.ContainsKey(item))
                        {
                            List<string> reqBosses = AllItemsWithDetails[item].RequiredBosses;
                            if (!reqBosses.Contains(itemByBoss.Key))
                            {
                                EpicLoot.Log($"{item} already registered, merging boss keys.");
                                reqBosses.Add(itemByBoss.Key);
                            }

                            AllItemsWithDetails[item].RequiredBosses = reqBosses;
                            continue;
                        }

                        AllItemsWithDetails.Add(item, new GatedItemDetails()
                        {
                            Type = info.Type,
                            RequiredBosses = new List<string>() { itemByBoss.Key }
                        });
                    }
                }

                if (!ItemsByTypeAndBoss.ContainsKey(info.Type))
                {
                    ItemsByTypeAndBoss.Add(info.Type, itemsByBoss);
                }
            }

            // Items can be ungated, add a dummy entry to account for this
            BiomesInOrder.Add(Heightmap.Biome.None);
            BiomesToBossKeys.Add(Heightmap.Biome.None, new List<string> { });

            foreach (BountyBossConfig boss in AdventureDataManager.Config.Bounties.Bosses)
            {
                if (!BiomesToBossKeys.ContainsKey(boss.Biome))
                {
                    BiomesToBossKeys.Add(boss.Biome, new List<string> { boss.BossDefeatedKey });
                }
                else
                {
                    if (!BiomesToBossKeys[boss.Biome].Contains(boss.BossDefeatedKey))
                    {
                        BiomesToBossKeys[boss.Biome].Add(boss.BossDefeatedKey);
                    }
                }

                // TODO: make a new user defined data structure to control biome order
                if (!BiomesInOrder.Contains(boss.Biome))
                {
                    BiomesInOrder.Add(boss.Biome);
                }
            }

            EpicLoot.Log($"Gated items configured, total registered: {AllItemsWithDetails.Keys.Count}");
        }

        public static ItemInfoConfig GetCFG()
        {
            return GatedConfig;
        }

        /// <summary>
        /// Attempts to get a valid item of the specified type.
        /// </summary>
        public static string GetGatedItemFromType(string itemType, GatedItemTypeMode mode,
            HashSet<string> currentSelected, List<string> validBosses, bool allowDuplicate = false,
            bool allowTypeFallback = false, bool allowItemFallback = false)
        {
            if (validBosses.Count == 0)
            {
                // TODO: this should never trigger
                return FallbackByType[itemType].Item;
            }

            if (!ItemsByTypeAndBoss.ContainsKey(itemType))
            {
                return null;
            }

            string item = null;
            foreach (string boss in validBosses)
            {
                item = GetGatedItemFromBossTier(itemType, boss, currentSelected,
                    mode, new HashSet<string>(), allowTypeFallback, allowDuplicate);

                if (item != null)
                {
                    return item;
                }
            }

            if (allowItemFallback)
            {
                return FallbackByType[itemType].Item;
            }

            return null;
        }

        /// <summary>
        /// Attempts to select a item of the specified type and boss level.
        /// If an items fails to be selected can search the fallback type at the same boss tier.
        /// </summary>
        private static string GetGatedItemFromBossTier(string itemType, string boss,
            HashSet<string> currentSelected,
            GatedItemTypeMode mode,
            HashSet<string> typesSearched,
            bool allowFallback = true,
            bool allowDuplicate = false)
        {
            if (ItemsByTypeAndBoss[itemType].ContainsKey(boss))
            {
                List<string> items = ItemsByTypeAndBoss[itemType][boss];
                bool gated = true;

                foreach (string item in items.shuffleList())
                {
                    gated = CheckIfItemNeedsGate(mode, item);
                    if (gated)
                    {
                        continue;
                    }

                    if (!allowDuplicate && currentSelected.Contains(item))
                    {
                        continue;
                    }

                    return item;
                }
            }

            if (allowFallback && FallbackByType.ContainsKey(itemType))
            {
                typesSearched.Add(itemType);
                Fallback fallback = FallbackByType[itemType];

                if (!fallback.Type.IsNullOrWhiteSpace() &&
                    !typesSearched.Contains(fallback.Type))
                {
                    return GetGatedItemFromBossTier(fallback.Type,
                        boss, currentSelected, mode, typesSearched, false, true);
                }
            }

            return null;
        }

        public static string GetGatedItemNameFromItemOrType(string itemOrType, GatedItemTypeMode gatedMode)
        {
            if (string.IsNullOrEmpty(itemOrType))
            {
                return null;
            }

            string type = itemOrType;

            List<string> bossList = null;

            // Check if this is a loot table category
            if (LootRoller.LootSetContainsEntry(itemOrType))
            {
                List<LootTable> ltcategory = LootRoller.GetFullyResolvedLootTable(itemOrType);
                List<string> potentialItems = new List<string>();
                foreach (LootTable lt in ltcategory)
                {
                    potentialItems.AddRange(lt.Loot.Select(x => x.Item).ToList());
                }

                if (potentialItems.Count == 0)
                {
                    return null;
                }
                
                string item = potentialItems[UnityEngine.Random.Range(0, potentialItems.Count - 1)];
                
                if (!CheckIfItemNeedsGate(gatedMode, item, out GatedItemDetails itemDetails))
                {
                    // This item doesn't need gating, return it, otherwise we setup the category for a fallback
                    return item;
                }

                if (itemDetails == null)
                {
                    return null;
                }

                type = itemDetails.Type;
                bossList = itemDetails.RequiredBosses;
            }

            // Check if this is a category of ItemType
            if (!ItemsByTypeAndBoss.ContainsKey(itemOrType))
            {
                // Passed string is an item
                if (gatedMode == GatedItemTypeMode.Unlimited)
                {
                    return itemOrType;
                }

                if (!CheckIfItemNeedsGate(gatedMode, itemOrType, out GatedItemDetails itemDetails))
                {
                    return itemOrType;
                }

                if (itemDetails == null)
                {
                    return null;
                }

                type = itemDetails.Type;
                bossList = itemDetails.RequiredBosses;
            }

            List<string> validBosses = DetermineValidBosses(gatedMode, false, bossList);

            return GetGatedItemFromType(type, gatedMode, new HashSet<string> { }, validBosses, true, true, true);
        }

        /// <summary>
        /// Returns a list of defeated bosses in the same order as defined in the configurations.
        /// If gating mode unlocks next biome it will also include the next tier of bosses.
        /// </summary>
        public static List<string> DetermineValidBosses(GatedItemTypeMode mode, bool lowestFirst = true, List<string> requiredBosses = null)
        {
            List<string> validBosses = new List<string>();

            if (BiomesInOrder == null || BiomesInOrder.Count == 0)
            {
                return validBosses;
            }

            // Find index of highest biome allowed
            int highestBiomeIndex = 0;

            if (requiredBosses != null && requiredBosses.Count > 0)
            {
                for (int i = BiomesInOrder.Count - 1; i >= 0; i--)
                {
                    Heightmap.Biome biome = BiomesInOrder[i];
                    if (!BiomesToBossKeys.ContainsKey(biome))
                    {
                        continue;
                    }

                    List<string> bossList = BiomesToBossKeys[biome];

                    foreach (string boss in requiredBosses)
                    {
                        if (!boss.IsNullOrWhiteSpace() && bossList.Contains(boss))
                        {
                            highestBiomeIndex = i;
                            i = -1;
                        }
                    }
                }
            }
            else
            {
                highestBiomeIndex = BiomesInOrder.Count - 1;
            }

            if (mode == GatedItemTypeMode.Unlimited ||
                mode == GatedItemTypeMode.PlayerMustKnowRecipe ||
                mode == GatedItemTypeMode.PlayerMustHaveCraftedItem)
            {
                List<Heightmap.Biome> validBiomes = BiomesInOrder.GetRange(0, highestBiomeIndex + 1);

                foreach (Heightmap.Biome biome in validBiomes)
                {
                    if (BiomesToBossKeys.ContainsKey(biome))
                    {
                        validBosses.AddRange(BiomesToBossKeys[biome]);
                    }
                }
            }
            else
            {
                bool previousAdded = (mode == GatedItemTypeMode.BossKillUnlocksNextBiomeItems);

                for (int i = 0; i <= highestBiomeIndex; i++)
                {
                    bool add = false;
                    Heightmap.Biome biome = BiomesInOrder[i];
                    if (!BiomesToBossKeys.ContainsKey(biome))
                    {
                        continue;
                    }
                    
                    List<string> bosses = BiomesToBossKeys[biome];

                    if (previousAdded && mode == GatedItemTypeMode.BossKillUnlocksNextBiomeItems)
                    {
                        add = true;
                    }

                    bool allKeysPresent = true;
                    foreach (string boss in bosses)
                    {
                        bool hasKey = ZoneSystem.instance.GetGlobalKey(boss);
                        if (hasKey || add)
                        {
                            validBosses.Add(boss);
                        }

                        if (!hasKey)
                        {
                            allKeysPresent = false;
                        }
                    }

                    previousAdded = allKeysPresent;
                }
            }

            if (validBosses.Count > 0)
            {
                validBosses = validBosses.Distinct().ToList();

                if (!lowestFirst)
                {
                    validBosses.Reverse();
                }
            }

            return validBosses;
        }

        private static bool CheckIfItemNeedsGate(GatedItemTypeMode mode, string itemName,
            out GatedItemDetails itemGatingDetails)
        {
            AllItemsWithDetails.TryGetValue(itemName, out itemGatingDetails);
            if (itemGatingDetails == null)
            {
                EpicLoot.Log($"Item {itemName} was not found in the iteminfo configuration, gating not evaluated. " +
                    $"Item will be allowed to drop.");
                return false;
            }

            return CheckIfItemNeedsGate(mode, itemName);
        }

        /// <summary>
        /// Returns true if item is gated, false if the item is not gated.
        /// </summary>
        private static bool CheckIfItemNeedsGate(GatedItemTypeMode mode, string itemName)
        {
            if (Player.m_localPlayer == null)
            {
                return true;
            }

            switch (mode)
            {
                case GatedItemTypeMode.Unlimited:
                    return false;
                case GatedItemTypeMode.PlayerMustKnowRecipe:
                    string name = GetItemName(itemName);
                    return !Player.m_localPlayer.IsRecipeKnown(name);
                case GatedItemTypeMode.PlayerMustHaveCraftedItem:
                    name = GetItemName(itemName);
                    return !Player.m_localPlayer.m_knownMaterial.Contains(name);
                case GatedItemTypeMode.BossKillUnlocksCurrentBiomeItems:
                case GatedItemTypeMode.BossKillUnlocksNextBiomeItems:
                    return CheckGateBossKill(itemName, mode);
                default:
                    return true; // Fallback, item will be gated- we could not gate it properly
            }
        }

        /// <summary>
        /// Returns true if the item is gated, false if allowed.
        /// </summary>
        private static bool CheckGateBossKill(string itemName, GatedItemTypeMode mode)
        {
            GatedItemDetails details;
            AllItemsWithDetails.TryGetValue(itemName, out details);

            if (details == null || details.RequiredBosses == null)
            {
                return false;
            }

            foreach (string boss in details.RequiredBosses)
            {
                string key = GetBossKeyForMode(boss, mode);

                if (!string.IsNullOrEmpty(key) && !ZoneSystem.instance.GetGlobalKey(key))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAllBossKeysForBiome(Heightmap.Biome biome)
        {
            if (!BiomesToBossKeys.ContainsKey(biome))
            {
                return true;
            }

            foreach (string key in BiomesToBossKeys[biome])
            {
                if (!ZoneSystem.instance.GetGlobalKey(key))
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetBossKeyForMode(string bossKey, GatedItemTypeMode mode)
        {
            if (bossKey != null && mode == GatedItemTypeMode.BossKillUnlocksNextBiomeItems)
            {
                string key = Bosses.GetPrevBossKey(bossKey);
                if (key != null)
                {
                    return key;
                }
            }

            return bossKey;
        }

        /// <summary>
        /// Returns a valid biome defined in the BiomesInOrder list based off the GatedItemTypeMode.
        /// </summary>
        public static Heightmap.Biome GetCurrentOrLowerBiomeByDefeatedBossSettings(Heightmap.Biome biome, GatedItemTypeMode mode)
        {
            if (!BiomesInOrder.Contains(biome))
            {
                // TODO: Handle biome definitions user defined lists better.
                // Configurations can have custom biomes not defined in all configuration locations.
                return biome;
            }

            if (mode == GatedItemTypeMode.Unlimited || mode == GatedItemTypeMode.PlayerMustKnowRecipe)
            {
                return biome;
            }

            Heightmap.Biome resultBiome = GetHighestDefeatedBiome(biome);

            if (mode == GatedItemTypeMode.BossKillUnlocksNextBiomeItems)
            {
                int index = BiomesInOrder.IndexOf(resultBiome) + 1;
                if (index < BiomesInOrder.Count)
                {
                    resultBiome = BiomesInOrder[index];
                }
            }

            return resultBiome;
        }

        private static Heightmap.Biome GetHighestDefeatedBiome(Heightmap.Biome startBiome)
        {
            for (int i = BiomesInOrder.IndexOf(startBiome); i >= 0; i--)
            {
                Heightmap.Biome checkBiome = BiomesInOrder[i];
                if (HasAllBossKeysForBiome(checkBiome))
                {
                    return checkBiome;
                }
            }

            return Heightmap.Biome.None;
        }

        private static string GetItemName(string item)
        {
            UnityEngine.GameObject itemPrefab = PrefabManager.Instance.GetPrefab(item);
            if (itemPrefab == null)
            {
                EpicLoot.LogError($"Tried to get gated itemID ({item}) but there is no prefab with that ID!");
                return null;
            }

            ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                EpicLoot.LogError($"Tried to get gated itemID ({item}) but its prefab has no ItemDrop component!");
                return null;
            }

            return itemDrop.m_itemData.m_shared.m_name;
        }
    }
}
