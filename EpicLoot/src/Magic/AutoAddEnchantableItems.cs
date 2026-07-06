using EpicLoot.Adventure;
using EpicLoot.Config;
using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicLoot.Magic
{
    public static class AutoAddEnchantableItems
    {
        public class AutoSorterConfiguration
        {
            public Dictionary<string, List<string>> UncraftableItemsAlwaysAllowed = new Dictionary<string, List<string>>();
            public Dictionary<string, List<string>> LootSetsToItemCategories = new Dictionary<string, List<string>>();
            public Dictionary<string, SortingData> BiomeSorterData = new Dictionary<string, SortingData>();
            public Dictionary<string, List<float>> TierRarityProbabilities = new Dictionary<string, List<float>>();
            public Dictionary<string, int> VendorCostByBiomeKey = new Dictionary<string, int>();
        }

        public class SortingData
        {
            public string Tier { get; set; } = "Tier0";
            public string BossKey { get; set; } = NONE;
            public List<string> BiomeMaterials { get; set; } = new List<string>();
            public List<string> BiomeSpecificCraftingStations { get; set; } = new List<string>();

        }

        public static void InitializeConfig(AutoSorterConfiguration config)
        {
            Config = config;
        }

        public static AutoSorterConfiguration GetCFG()
        {
            return Config;
        }

        private static readonly List<string> IgnoredItems = LootRoller.Config.RestrictedItems.ToList();

        public static AutoSorterConfiguration Config;
        public static readonly string NONE = "none";

        public static void CheckAndAddAllEnchantableItems(bool deregister = true)
        {
            if (deregister)
            {
                Jotunn.Managers.MinimapManager.OnVanillaMapDataLoaded -=
                    () => AutoAddEnchantableItems.CheckAndAddAllEnchantableItems();
            }

            if (ELConfig.AutoAddEquipment.Value == false && ELConfig.AutoRemoveEquipmentNotFound.Value == false)
            {
                return;
            }

            List<ItemTypeInfo> currentConfigs = GatedItemTypeHelper.GatedConfig.ItemInfo;

            Dictionary<string, ItemTypeInfo> itemsByCategory = new Dictionary<string, ItemTypeInfo>();
            Dictionary<string, ItemTypeInfo> foundByCategory = new Dictionary<string, ItemTypeInfo>();

            foreach (ItemTypeInfo currentConfig in currentConfigs)
            {
                if (!itemsByCategory.ContainsKey(currentConfig.Type))
                {
                    itemsByCategory.Add(currentConfig.Type, currentConfig);
                }
                else
                {
                    // Only need to print the error once
                    EpicLoot.LogWarning($"Duplicate Type keys found for {currentConfig.Type}. " +
                        $"Please check your iteminfo.json file and patches for conflicts.");
                }

                if (!foundByCategory.ContainsKey(currentConfig.Type))
                {
                    foundByCategory.Add(currentConfig.Type, new ItemTypeInfo()
                    {
                        ItemsByBoss = new Dictionary<string, List<string>>() {
                            { NONE, new List<string>() },
                            { "defeated_eikthyr", new List<string>() },
                            { "defeated_gdking", new List<string>() },
                            { "defeated_bonemass", new List<string>() },
                            { "defeated_dragon", new List<string>() },
                            { "defeated_goblinking", new List<string>() },
                            { "defeated_queen", new List<string>() },
                            { "defeated_fader", new List<string>() }
                        },
                    });
                }
            }

            List<ItemDrop> allItems = Resources.FindObjectsOfTypeAll<ItemDrop>().ToList();
            List<ItemDrop> allEquipment = allItems.Where(i => i.m_itemData != null &&
                i.m_itemData.m_shared != null &&
                i.m_autoPickup == true &&
                string.IsNullOrEmpty(i.m_itemData.m_shared.m_dlc) &&
                !string.IsNullOrEmpty(i.m_itemData.m_shared.m_description) &&
                EpicLoot.IsAllowedMagicItemType(i.m_itemData)).ToList();

            EpicLoot.Log($"Checking all equipment in game.");
            foundByCategory = EnsureItemsInConfigMutating(foundByCategory, itemsByCategory, allEquipment);

            // Compare the found items with the current config, if enabled add items, if enabled remove missing items
            if (ELConfig.AutoRemoveEquipmentNotFound.Value)
            {
                EpicLoot.Log($"Add/Remove not-found equipment processing.");
                itemsByCategory = AddRemoveMissingItemsInConfigMutating(foundByCategory, itemsByCategory);
            }
            else
            {
                EpicLoot.Log("Adding found equipment that was not listed.");
                itemsByCategory = AddMissingItemsInConfigMutating(foundByCategory, itemsByCategory);
            }

            EpicLoot.Log("Merging datasets and ensuring no duplicate entries.");
            // merge dataset and ensure unique values
            List<ItemTypeInfo> newConfig = MergeItemsByBossConfig(itemsByCategory);

            // Add/remove items from vendor if enabled.
            AddRemoveItemsFromVendor(newConfig);

            List<string> magicMats = allItems.Where(i => i.m_itemData != null &&
                i.m_itemData.m_dropPrefab != null &&
                (i.m_itemData.IsMagicCraftingMaterial() || i.m_itemData.IsRunestone()))
                .Select(x => x.m_itemData.m_dropPrefab.name).ToList();
            AddRemoveItemsFromLootLists(magicMats, foundByCategory, newConfig);

            // Write out the new config, which will trigger a reload of the config
            try
            {
                string contents = JsonConvert.SerializeObject(new ItemInfoConfig() { ItemInfo = newConfig }, Formatting.Indented);
                string overhaulFileLocation = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), "iteminfo.json");
                File.WriteAllText(overhaulFileLocation, contents);
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Failed to auto-add items to iteminfo.json: {e.Message}");
                return;
            }
        }

        private static void AddRemoveItemsFromLootLists(List<string> magicMats,
            Dictionary<string, ItemTypeInfo> foundByCategory,
            List<ItemTypeInfo> newConfig)
        {
            if (!ELConfig.AutoAddRemoveEquipmentFromLootLists.Value)
            {
                return;
            }

            EpicLoot.Log("Adding/Removing entries in the loot drop configuration.");
            LootConfig defaultcfg = LootRoller.Config;
            List<LootTable> updatedLootTables = [];
            List<LootItemSet> updatedItemSets = [];

            // entry of all of the currently defined meta sets as they are valid targets also
            List<string> metaItemSetNames = LootRoller.Config.ItemSets.Select(x => x.Name).ToList();
            // List of all of the currently valid items so we can always determine if its at least valid
            List<string> validItems = [];
            foreach (ItemTypeInfo entry in foundByCategory.Values)
            {
                foreach (KeyValuePair<string, List<string>> iteme in entry.ItemsByBoss)
                {
                    validItems.AddRange(iteme.Value);
                }
            }

            foreach (LootItemSet lis in LootRoller.Config.ItemSets)
            {
                List<LootDrop> entries = new List<LootDrop>();
                List<string> addedItems = new List<string>();
                // Validate existing entries in the lootset
                EpicLoot.Log($"Checking LootSet entry: {lis.Name}");
                foreach (LootDrop loot in lis.Loot)
                {
                    if (validItems.Contains(loot.Item) ||
                        metaItemSetNames.Contains(loot.Item) ||
                        magicMats.Contains(loot.Item) ||
                        ObjectDB.instance.GetItemPrefab(loot.Item) != null)
                    {
                        entries.Add(loot);
                        addedItems.Add(loot.Item);
                        continue;
                    }

                    EpicLoot.Log($"{loot.Item} is not a found item and will be removed from the loot tables.");
                }

                if (DetermineTierAndType(lis.Name, out string tier, out string loottype))
                {
                    string bosskey = "none";
                    foreach (KeyValuePair<string, SortingData> entry in Config.BiomeSorterData)
                    {
                        if (entry.Value.Tier == tier)
                        {
                            bosskey = entry.Value.BossKey;
                            break;
                        }
                    }

                    foreach (ItemTypeInfo itemType in newConfig)
                    {
                        if (!Config.LootSetsToItemCategories.ContainsKey(loottype) ||
                            !Config.LootSetsToItemCategories[loottype].Contains(itemType.Type) ||
                            !itemType.ItemsByBoss.ContainsKey(bosskey))
                        {
                            continue;
                        }

                        foreach (string gateditem in itemType.ItemsByBoss[bosskey])
                        {
                            if (addedItems.Contains(gateditem))
                            {
                                continue;
                            }

                            entries.Add(new LootDrop() { Item = gateditem, Rarity = DetermineRarityForLoot(tier) });
                        }
                    }
                }

                if (entries.Count > 0)
                {
                    updatedItemSets.Add(new LootItemSet { Name = lis.Name, Loot = entries.ToArray() });
                }
            }

            EpicLoot.Log($"Checking loot tables for invalid entries.");
            List<string> metaLootTables = new List<string>();
            //LootRoller.Config.LootTables
            foreach (LootTable lt in LootRoller.Config.LootTables)
            {
                List<LootDrop> updatedLootDrop = new List<LootDrop>();
                List<LeveledLootDef> levelListDef = new List<LeveledLootDef>();

                // Valid existing entries
                if (lt.Loot != null)
                {
                    updatedLootDrop.AddRange(ValidateLootList(lt, metaLootTables, metaItemSetNames, validItems));
                }

                // Validate existing entries in the leveled loot drops
                if (lt.LeveledLoot != null)
                {
                    foreach (LeveledLootDef lloot in lt.LeveledLoot)
                    {
                        List<LootDrop> updatedLootTableLL = new List<LootDrop>();
                        foreach (LootDrop ld in lloot.Loot)
                        {
                            if (validItems.Contains(ld.Item) || metaItemSetNames.Contains(ld.Item))
                            {
                                updatedLootTableLL.Add(ld);
                            }
                        }
                        LeveledLootDef lld = new LeveledLootDef();
                        lld.Loot = updatedLootTableLL.ToArray();
                        levelListDef.Add(lld);
                    }
                }

                LootTable ltc = lt;
                ltc.Loot = updatedLootDrop.ToArray();
                updatedLootTables.Add(ltc);
                metaLootTables.Add(lt.Object);
            }

            EpicLoot.Log($"Finished Validating loottable.");
            // Write out the new config, which will trigger a reload of the config
            try
            {
                LootConfig newLootConfig = new LootConfig()
                {
                    ItemSets = updatedItemSets.ToArray(),
                    LootTables = updatedLootTables.ToArray(),
                    MagicEffectsCount = LootRoller.Config.MagicEffectsCount,
                    RestrictedItems = LootRoller.Config.RestrictedItems
                };
                string contents = JsonConvert.SerializeObject(newLootConfig, Formatting.Indented);
                string overhaulFileLocation = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), "loottables.json");
                File.WriteAllText(overhaulFileLocation, contents);
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Failed to auto-update loottables.json: {e.Message}");
            }
        }

        private static void AddRemoveItemsFromVendor(List<ItemTypeInfo> newConfig)
        {
            if (ELConfig.AutoAddRemoveEquipmentFromVendor.Value == false)
            {
                return;
            }

            EpicLoot.Log("Adding/Removing entries for the vendor from detected equipment.");
            Dictionary<string, SecretStashItemConfig> existingVendorItems = new Dictionary<string, SecretStashItemConfig>();
            List<string> foundItemEntry = new List<string>();

            // Add all of the items currently in the vendor items list
            EpicLoot.Log("Adding Entries to the vendor list.");
            foreach (SecretStashItemConfig gamble in AdventureDataManager.Config.Gamble.GambleCosts)
            {
                if (existingVendorItems.ContainsKey(gamble.Item))
                {
                    continue;
                }

                existingVendorItems.Add(gamble.Item, gamble);
            }

            // Check the iteminfo configs for existing and new items
            foreach (ItemTypeInfo itemType in newConfig)
            {
                foreach (KeyValuePair<string, List<string>> bossEntry in itemType.ItemsByBoss)
                {
                    foreach (string itemName in bossEntry.Value)
                    {
                        if (existingVendorItems.ContainsKey(itemName))
                        {
                            // Found this entry
                            EpicLoot.Log($"Found existing vendor entry for {itemName} - price {existingVendorItems[itemName].CoinsCost}, keeping it in the list.");
                            foundItemEntry.Add(itemName);
                        }
                        else
                        {
                            foundItemEntry.Add(itemName);
                            existingVendorItems.Add(itemName, new SecretStashItemConfig()
                            {
                                Item = itemName,
                                CoinsCost = DetermineCoinsCostForItem(bossEntry.Key)
                            });
                            EpicLoot.Log($"Adding new vendor entry for {itemName} with price {existingVendorItems[itemName].CoinsCost}.");
                        }
                    }
                }
            }

            // Remove Items which are not found
            EpicLoot.Log("Removing invalid entries.");
            List<SecretStashItemConfig> newGambleItems = existingVendorItems
                .Where(x => foundItemEntry.Contains(x.Key)).Select(x => x.Value).ToList();
            EpicLoot.Log("Building config.");
            AdventureDataConfig AdventureDataConfigReplacement = AdventureDataManager.Config;
            AdventureDataConfigReplacement.Gamble.GambleCosts = newGambleItems;

            // Write out the new config, which will trigger a reload of the config
            EpicLoot.Log("Writing config.");
            try
            {
                string contents = JsonConvert.SerializeObject(AdventureDataConfigReplacement, Formatting.Indented);
                string overhaulFileLocation = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), "adventuredata.json");
                File.WriteAllText(overhaulFileLocation, contents);
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Failed to auto-add vendor items to adventuredata.json: {e.Message}");
            }
        }

        private static List<ItemTypeInfo> MergeItemsByBossConfig(Dictionary<string, ItemTypeInfo> itemsByCategory)
        {
            List<ItemTypeInfo> newConfig = new List<ItemTypeInfo>();
            foreach (KeyValuePair<string, ItemTypeInfo> item in itemsByCategory)
            {
                if (item.Value.ItemsByBoss.Count > 0 || item.Value.IgnoredItems.Count > 0)
                {
                    Dictionary<string, List<string>> itemsByBossUniques = new();
                    foreach (KeyValuePair<string, List<string>> entry in item.Value.ItemsByBoss)
                    {
                        itemsByBossUniques.Add(entry.Key, entry.Value.Distinct().ToList());
                    }

                    ItemTypeInfo uniqueItems = new ItemTypeInfo()
                    {
                        IgnoredItems = item.Value.IgnoredItems.Distinct().ToList(),
                        ItemFallback = item.Value.ItemFallback,
                        Type = item.Value.Type,
                        ItemsByBoss = itemsByBossUniques
                    };

                    newConfig.Add(uniqueItems);
                }
            }

            return newConfig;
        }

        private static Dictionary<string, ItemTypeInfo> EnsureItemsInConfigMutating(
            Dictionary<string, ItemTypeInfo> foundByCategory,
            Dictionary<string, ItemTypeInfo> itemsByCategory,
            List<ItemDrop> allEquipment)
        {
            foreach (ItemDrop item in allEquipment)
            {
                string itemType = DetermineItemType(item.m_itemData);
                string itemName = item.name;
                // Check if the item is already in the config
                // If it is, add it to the foundBy
                bool itemfound = false;
                if (itemsByCategory.ContainsKey(itemType) && foundByCategory.ContainsKey(itemType))
                {
                    if (itemsByCategory[itemType].IgnoredItems.Contains(itemName))
                    {
                        foundByCategory[itemType].IgnoredItems.Add(itemName);
                        itemfound = true;
                        continue;
                    }
                    else
                    {
                        foreach (KeyValuePair<string, List<string>> entry in itemsByCategory[itemType].ItemsByBoss)
                        {
                            Dictionary<string, List<string>> catEntry = foundByCategory[itemType].ItemsByBoss;
                            if (entry.Value.Contains(itemName))
                            {
                                if (!catEntry.ContainsKey(entry.Key))
                                {
                                    catEntry.Add(entry.Key, new List<string>());
                                }

                                catEntry[entry.Key].Add(itemName);
                                itemfound = true;
                                break;
                            }
                        }
                    }
                }

                if (itemfound)
                {
                    continue;
                }

                string key = DetermineBossLevelForItem(item.m_itemData);
                bool uncraftableFound = false;
                foreach(KeyValuePair<string, List<string>> uncraftable in Config.UncraftableItemsAlwaysAllowed)
                {
                    if (uncraftable.Value == null || uncraftable.Value.Count == 0)
                    {
                        continue;
                    }

                    if (uncraftable.Value.Contains(itemName))
                    {
                        if (foundByCategory[itemType].ItemsByBoss.ContainsKey(uncraftable.Key))
                        {
                            foundByCategory[itemType].ItemsByBoss[uncraftable.Key].Add(itemName);
                        }
                        else
                        {
                            foundByCategory[itemType].ItemsByBoss.Add(uncraftable.Key, new List<string>() { itemName });
                        }

                        uncraftableFound = true;
                        break;
                    }
                }

                if (uncraftableFound)
                {
                    continue;
                }

                // Item already exists in the config | Or we are not auto-adding items
                //if (itemfound || ELConfig.AutoAddEquipment.Value == false) { continue; }
                if ((ELConfig.OnlyAddEquipmentWithRecipes.Value == true && key == NONE) ||
                    (key == NONE && itemType == NONE) ||
                    itemType == "Unkown" ||
                    IgnoredItems.Contains(itemName))
                {
                    EpicLoot.Log($"skipping name:{itemName} type:{itemType} techlevel:{key}");
                    continue;
                }

                EpicLoot.Log($"{itemType} {key} add {itemName}");
                // Ensure gating required boss keys exist
                if (!foundByCategory[itemType].ItemsByBoss.ContainsKey(key))
                {
                    foundByCategory[itemType].ItemsByBoss.Add(key, new List<string>() { });
                }

                foundByCategory[itemType].ItemsByBoss[key].Add(itemName);
            }
            return foundByCategory;
        }

        private static Dictionary<string, ItemTypeInfo> AddMissingItemsInConfigMutating(
            Dictionary<string, ItemTypeInfo> foundByCategory,
            Dictionary<string, ItemTypeInfo> itemsByCategory)
        {
            // Just add found items, dont remove missing items
            foreach (KeyValuePair<string, ItemTypeInfo> fbc in foundByCategory)
            {
                if (ELConfig.AutoAddEquipment.Value)
                {
                    if (!itemsByCategory.ContainsKey(fbc.Key))
                    {
                        continue;
                    }

                    // Replace entries with only the found values, removes non-found items and adds new ones
                    itemsByCategory[fbc.Key].IgnoredItems = itemsByCategory[fbc.Key].IgnoredItems
                        .Union(itemsByCategory[fbc.Key].IgnoredItems).ToList();

                    foreach (KeyValuePair<string, List<string>> entry in fbc.Value.ItemsByBoss)
                    {
                        if (!itemsByCategory[fbc.Key].ItemsByBoss.ContainsKey(entry.Key))
                        {
                            continue;
                        }

                        itemsByCategory[fbc.Key].ItemsByBoss[entry.Key] =
                            itemsByCategory[fbc.Key].ItemsByBoss[entry.Key].Union(entry.Value).ToList();
                    }
                }
            }
            return itemsByCategory;
        }

        private static Dictionary<string, ItemTypeInfo> AddRemoveMissingItemsInConfigMutating(
            Dictionary<string, ItemTypeInfo> foundByCategory,
            Dictionary<string, ItemTypeInfo> itemsByCategory)
        {
            foreach (KeyValuePair<string, ItemTypeInfo> fbc in foundByCategory)
            {
                if (!itemsByCategory.ContainsKey(fbc.Key) || !foundByCategory.ContainsKey(fbc.Key))
                {
                    continue;
                }

                if (ELConfig.AutoAddEquipment.Value)
                {
                    // Replace entries with only the found values, removes non-found items and adds new ones
                    itemsByCategory[fbc.Key].IgnoredItems = foundByCategory[fbc.Key].IgnoredItems;
                    foreach (string key in itemsByCategory[fbc.Key].ItemsByBoss.Keys)
                    {
                        if (itemsByCategory[fbc.Key].ItemsByBoss.ContainsKey(key) &&
                            foundByCategory[fbc.Key].ItemsByBoss.ContainsKey(key) &&
                            itemsByCategory[fbc.Key].ItemsByBoss[key].Count != foundByCategory[fbc.Key].ItemsByBoss[key].Count)
                        {
                            List<string> toaddlist = foundByCategory[fbc.Key].ItemsByBoss[key]
                                .Except(itemsByCategory[fbc.Key].ItemsByBoss[key]).ToList();
                            List<string> toremovelist = itemsByCategory[fbc.Key].ItemsByBoss[key]
                                .Except(foundByCategory[fbc.Key].ItemsByBoss[key]).ToList();

                            if (toaddlist.Count > 0)
                            {
                                EpicLoot.Log($"Adding entries in {key} that are not found in the config: {string.Join(", ", toaddlist)}");
                            }

                            if (toremovelist.Count > 0)
                            {
                                EpicLoot.Log($"Removing entries in {key} that are not found in the config: {string.Join(", ", toremovelist)}");
                            }
                        }
                    }

                    itemsByCategory[fbc.Key].ItemsByBoss = foundByCategory[fbc.Key].ItemsByBoss;
                }
                else
                {
                    // Just remove items that are not found in the config
                    itemsByCategory[fbc.Key].IgnoredItems = foundByCategory[fbc.Key].IgnoredItems
                        .Where(e => itemsByCategory[fbc.Key].IgnoredItems.Contains(e)).ToList();

                    foreach (KeyValuePair<string, List<string>> entry in foundByCategory[fbc.Key].ItemsByBoss)
                    {
                        if (!itemsByCategory[fbc.Key].ItemsByBoss.ContainsKey(entry.Key))
                        {
                            continue;
                        }

                        List<string> reducedItems = itemsByCategory[fbc.Key].ItemsByBoss[entry.Key]
                            .Where(e => entry.Value.Contains(e)).ToList();

                        if (reducedItems.Count != itemsByCategory[fbc.Key].ItemsByBoss[entry.Key].Count)
                        {
                            EpicLoot.Log($"Removing items from {fbc.Key} {entry.Key} that are not found in the config: " +
                                $"{string.Join(", ", itemsByCategory[fbc.Key].ItemsByBoss[entry.Key].Except(reducedItems))}");
                        }

                        itemsByCategory[fbc.Key].ItemsByBoss[entry.Key] = reducedItems;
                    }
                }
            }
            return itemsByCategory;
        }

        private static List<LootDrop> ValidateLootList(LootTable lt,
            List<string> metaLootTables, List<string> metaItemSetNames, List<string> validItems)
        {
            List<LootDrop> updatedLootDrop = new List<LootDrop>();
            foreach (LootDrop loot in lt.Loot)
            {
                if (loot.Item.Contains("."))
                {
                    string[] referenceAndIndex = loot.Item.Split('.');
                    EpicLoot.Log($"Validating meta reference {loot.Item} {referenceAndIndex[0]}");
                    if (metaItemSetNames.Contains(referenceAndIndex[0]) || metaLootTables.Contains(referenceAndIndex[0]))
                    {
                        updatedLootDrop.Add(loot);
                        continue;
                    }
                }

                if (!validItems.Contains(loot.Item) && !metaItemSetNames.Contains(loot.Item))
                {
                    EpicLoot.Log($"REMOVING: Loot table ({lt.Object}) Item {loot.Item} not found.");
                    continue;
                }
                updatedLootDrop.Add(loot);
            }
            return updatedLootDrop;
        }

        private static int DetermineCoinsCostForItem(string bosskey)
        {
            if (Config.VendorCostByBiomeKey.ContainsKey(bosskey))
            {
                return Config.VendorCostByBiomeKey[bosskey];
            }

            return 999;
        }

        private static bool DetermineTierAndType(string name, out string tier, out string type)
        {
            tier = null;
            type = null;

            if (!name.Contains("Tier"))
            {
                EpicLoot.Log("Non Tiered entry");
                return false;
            }

            tier = name.Substring(0, 5);
            type = name.Substring(5);
            // Maybe we want to ensure the everything groups are properly setup? How much loot table validation should we do?
            if (type == "Tier" || type == "Everything")
            {
                return false;
            }

            return true;
        }

        private static float[] DetermineRarityForLoot(string tier)
        {
            if (Config.TierRarityProbabilities.ContainsKey(tier))
            {
                return Config.TierRarityProbabilities[tier].ToArray();
            }

            return [97, 2, 1, 0, 0];
        }

        private static string DetermineItemType(ItemDrop.ItemData item)
        {
            ItemDrop.ItemData.ItemType itemType = item.m_shared.m_itemType;
            switch (itemType)
            {
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                    switch (item.m_shared.m_skillType)
                    {
                        case Skills.SkillType.Spears:
                            return "Spears";
                        case Skills.SkillType.Swords:
                            return "Swords";
                        case Skills.SkillType.Clubs:
                            return (itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) ? "Clubs" : "Sledges";
                        case Skills.SkillType.Axes:
                            return (itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) ? "Axes" : "TwoHandAxes";
                        case Skills.SkillType.Knives:
                            return "Knives";
                        case Skills.SkillType.Unarmed:
                            return "Fists";
                        case Skills.SkillType.ElementalMagic:
                        case Skills.SkillType.BloodMagic:
                            return "Staffs";
                        case Skills.SkillType.Polearms:
                            return "Polearms";
                        case Skills.SkillType.Pickaxes:
                            return "Pickaxes";
                        case Skills.SkillType.Sneak:
                            return "Torches";
                    }
                    break;
                case ItemDrop.ItemData.ItemType.Shield:
                    if (item.m_shared.m_timedBlockBonus > 0)
                    {
                        return (item.m_shared.m_timedBlockBonus >= 2.5f) ? "Bucklers" : "RoundShields";
                    }
                    else
                    {
                        return "TowerShields";
                    }
                case ItemDrop.ItemData.ItemType.Bow:
                    return "Bows";
                case ItemDrop.ItemData.ItemType.Helmet:
                    return "HeadArmor";
                case ItemDrop.ItemData.ItemType.Chest:
                    return "ChestArmor";
                case ItemDrop.ItemData.ItemType.Legs:
                    return "LegsArmor";
                case ItemDrop.ItemData.ItemType.Shoulder:
                    return "ShouldersArmor";
                case ItemDrop.ItemData.ItemType.Torch:
                    return "Torches";
                case ItemDrop.ItemData.ItemType.Tool:
                    return "Tools";
                case ItemDrop.ItemData.ItemType.Utility:
                case ItemDrop.ItemData.ItemType.Trinket:
                case ItemDrop.ItemData.ItemType.Misc:
                    return "Utility";
            }

            // It is possible that the item is not a known skill type
            // This happens with weapons that use mod skills eg: scythes
            switch (item.m_shared.m_animationState)
            {
                // Its either an axe or a sword, currently this is only therzies throwing axes which get to this point
                case ItemDrop.ItemData.AnimationState.OneHanded:
                    return "Axes";
                case ItemDrop.ItemData.AnimationState.DualAxes:
                    return "TwoHandAxes";
                case ItemDrop.ItemData.AnimationState.Unarmed:
                    if (item.m_shared.m_skillType == Skills.SkillType.None)
                    {
                        // This is likely a throwable bomb, make sure it remains unknown
                        break;
                    }
                    return "Fists";
                case ItemDrop.ItemData.AnimationState.MagicItem:
                    return "Staffs";
                case ItemDrop.ItemData.AnimationState.Scythe:
                case ItemDrop.ItemData.AnimationState.Atgeir:
                    return "Polearms";
                case ItemDrop.ItemData.AnimationState.Bow:
                case ItemDrop.ItemData.AnimationState.Crossbow:
                    return "Bows";
                case ItemDrop.ItemData.AnimationState.Feaster:
                case ItemDrop.ItemData.AnimationState.FishingRod:
                    return "Tools";
                case ItemDrop.ItemData.AnimationState.Torch:
                case ItemDrop.ItemData.AnimationState.LeftTorch:
                    return "Torches";
                case ItemDrop.ItemData.AnimationState.Greatsword:
                    return "Swords";
                case ItemDrop.ItemData.AnimationState.TwoHandedClub:
                    return "Sledges";
            }

            EpicLoot.Log($"Unknown item type for item {item.m_shared.m_name}: {itemType}");
            return "Unkown";
        }

        public static string DetermineBossLevelForItem(ItemDrop.ItemData item)
        {
            if (item == null || ObjectDB.instance == null)
            {
                return NONE;
            }

            Recipe itemRecipe = ObjectDB.instance.GetRecipe(item);
            if (itemRecipe == null || itemRecipe.m_enabled == false || itemRecipe.m_resources == null)
            {
                return NONE;
            }

            // This goes through the biome tiers in reverse order, starting from the highest tier
            // and checking if the current item has materials from that biome
            // if not it goes down a biome until it finds materials required to craft the item
            // if an item does not require any materials or has no recipe, it should be listed in UncraftableItemsAlwaysAllowed
            foreach (KeyValuePair<string, SortingData> sortdata in Config.BiomeSorterData.Reverse())
            {
                // TODO: Update this logic to use a more concrete biome order list
                if (itemRecipe.m_craftingStation != null &&
                    sortdata.Value.BiomeSpecificCraftingStations.Contains(itemRecipe.m_craftingStation.name))
                {
                    return sortdata.Value.BossKey;
                }

                foreach (Piece.Requirement req in itemRecipe.m_resources)
                {
                    if (req.m_resItem != null && sortdata.Value.BiomeMaterials.Contains(req.m_resItem.name))
                    {
                        return sortdata.Value.BossKey;
                    }
                }
            }

            return NONE;
        }
    }
}
