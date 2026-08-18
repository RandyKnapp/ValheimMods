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
                // Claim this as the mod's own output so a later launch does not read it as a player edit.
                ConfigVersionManager.RecordWrittenContent("iteminfo", contents);
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
                    if (IsValidLootEntryName(loot.Item, metaItemSetNames, null, validItems, magicMats))
                    {
                        PruneRarityItems(loot, lis.Name, metaItemSetNames, null, validItems, magicMats);
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

                // Keep the set even when validation emptied it. Dropping it here used to turn one bad
                // entry check into a chain of dead references: metaItemSetNames is snapshotted above,
                // before any pruning, so every reference to the set survives while the set itself
                // vanishes -- and a reference that resolves to nothing is reported as a missing item
                // prefab, miles from the real cause. An empty set is a config problem worth saying out
                // loud; ResolveLootDrop reports it again if anything actually rolls on it.
                if (entries.Count == 0)
                {
                    EpicLoot.LogWarning($"LootSet {lis.Name} has no valid entries left after validation. " +
                        $"Keeping it so references to it stay resolvable, but it will drop nothing.");
                }

                updatedItemSets.Add(new LootItemSet { Name = lis.Name, Loot = entries.ToArray() });
            }

            EpicLoot.Log($"Checking loot tables for invalid entries.");
            List<string> metaLootTables = new List<string>();
            //LootRoller.Config.LootTables
            foreach (LootTable lt in LootRoller.Config.LootTables)
            {
                List<LootDrop> updatedLootDrop = new List<LootDrop>();

                // Valid existing entries. Only lt.Loot is validated -- LeveledLoot is deliberately left
                // untouched. Boss drops live there (Eikthyr_{Rarity}_ShardStone and friends), and a
                // level-gated entry has no independent existence to check that ValidateLootList would
                // not already cover, so validating it only adds ways to delete working loot.
                if (lt.Loot != null)
                {
                    updatedLootDrop.AddRange(ValidateLootList(lt, metaLootTables, metaItemSetNames, validItems, magicMats));
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
                    SocketCounts = LootRoller.Config.SocketCounts,
                    RestrictedItems = LootRoller.Config.RestrictedItems
                };
                string contents = JsonConvert.SerializeObject(newLootConfig, Formatting.Indented);
                string overhaulFileLocation = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), "loottables.json");
                File.WriteAllText(overhaulFileLocation, contents);
                // Claim this as the mod's own output so a later launch does not read it as a player edit.
                ConfigVersionManager.RecordWrittenContent("loottables", contents);
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
                // Claim this as the mod's own output so a later launch does not read it as a player edit.
                ConfigVersionManager.RecordWrittenContent("adventuredata", contents);
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
                // Raw-field classification only: this loop is what GENERATES iteminfo.json, so it must
                // not consult the configured answer (ItemTypeClassifier.GetItemInfoType) -- doing so
                // would make the sorter self-confirming and unable to ever re-sort a mis-filed item.
                string itemType = ItemTypeClassifier.ClassifyFromFields(item.m_itemData);
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
                    itemType == ItemTypeClassifier.Unknown ||
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
            List<string> metaLootTables, List<string> metaItemSetNames, List<string> validItems,
            List<string> magicMats)
        {
            List<LootDrop> updatedLootDrop = new List<LootDrop>();
            foreach (LootDrop loot in lt.Loot)
            {
                if (!IsValidLootEntryName(loot.Item, metaItemSetNames, metaLootTables, validItems, magicMats))
                {
                    EpicLoot.Log($"REMOVING: Loot table ({lt.Object}) Item {loot.Item} not found.");
                    continue;
                }

                PruneRarityItems(loot, lt.Object, metaItemSetNames, metaLootTables, validItems, magicMats);
                updatedLootDrop.Add(loot);
            }
            return updatedLootDrop;
        }

        // The single answer to "may a loot entry name this?", shared by the ItemSet pass and the loot
        // table pass so the two cannot drift. Anything this rejects is deleted from the rewritten
        // loottables.json permanently, so every legitimate shape has to be represented here:
        // a gated equipment item, an ItemSet or loot table name, a magic crafting material, an
        // "Object.Level" reference to another table, or any other real prefab -- which is what covers
        // shard stones and every non-equipment item a table may drop.
        //
        // metaLootTables is null for the ItemSet pass, where table references are not a valid target.
        private static bool IsValidLootEntryName(string name, List<string> metaItemSetNames,
            List<string> metaLootTables, List<string> validItems, List<string> magicMats)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            if (metaLootTables != null && name.Contains("."))
            {
                string reference = name.Split('.')[0];
                EpicLoot.Log($"Validating meta reference {name} {reference}");
                if (metaItemSetNames.Contains(reference) || metaLootTables.Contains(reference))
                {
                    return true;
                }
            }

            return validItems.Contains(name)
                || metaItemSetNames.Contains(name)
                || magicMats.Contains(name)
                || ObjectDB.instance.GetItemPrefab(name) != null;
        }

        // Drops only the unresolvable rarities from an entry's per-rarity map, leaving the entry itself
        // alone -- its Item already validated, and it stays a working drop at every rarity that remains.
        // An emptied map is removed outright so the rewritten config does not carry a dead "RarityItems".
        private static void PruneRarityItems(LootDrop loot, string owner, List<string> metaItemSetNames,
            List<string> metaLootTables, List<string> validItems, List<string> magicMats)
        {
            if (loot.RarityItems == null || loot.RarityItems.Count == 0)
            {
                return;
            }

            List<ItemRarity> invalid = null;
            foreach (KeyValuePair<ItemRarity, string> entry in loot.RarityItems)
            {
                if (IsValidLootEntryName(entry.Value, metaItemSetNames, metaLootTables, validItems, magicMats))
                {
                    continue;
                }

                EpicLoot.Log($"REMOVING: ({owner}) {loot.Item} rarity {entry.Key} item {entry.Value} not found.");
                (invalid ??= new List<ItemRarity>()).Add(entry.Key);
            }

            if (invalid == null)
            {
                return;
            }

            foreach (ItemRarity rarity in invalid)
            {
                loot.RarityItems.Remove(rarity);
            }

            if (loot.RarityItems.Count == 0)
            {
                loot.RarityItems = null;
            }
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
