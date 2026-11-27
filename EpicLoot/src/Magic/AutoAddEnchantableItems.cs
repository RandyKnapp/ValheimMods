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

            // How creatures without a loot table are given one; see CreatureSorter. Left null when absent
            // so a file that predates it falls through to the code-side defaults field by field.
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public CreatureSorterConfig CreatureSorter;
        }

        public class SortingData
        {
            public string Tier { get; set; } = "Tier0";
            public string BossKey { get; set; } = NONE;
            public List<string> BiomeMaterials { get; set; } = new List<string>();
            public List<string> BiomeSpecificCraftingStations { get; set; } = new List<string>();

            // The loot table template each class of creature in this biome gets from the creature sorter.
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public CreatureLadder Creatures { get; set; }
        }

        public static void InitializeConfig(AutoSorterConfiguration config)
        {
            Config = config;
            // The creature rules live in this file too; a live edit (or a patch) re-sorts the creatures
            // while a world is loaded. At startup nothing is loaded yet and this does nothing.
            CreatureSorterRunner.RunIfWorldLoaded("itemsorter.json reload");
        }

        public static AutoSorterConfiguration GetCFG()
        {
            return Config;
        }

        public static AutoSorterConfiguration Config;

        // What the per-item loops look up, built once at the start of a pass; null outside one.
        private static PassLookups _pass;
        public static readonly string NONE = "none";

        // The subscription must be a stored delegate: unsubscribing a freshly written lambda is a
        // no-op (two lambda sites compile to two different methods).
        public static readonly Action OnMapDataLoadedHandler = () => CheckAndAddAllEnchantableItems();

        private static bool _ranOnHeadlessServer;

        /// <summary>
        /// The trigger for a headless (dedicated) server, which never fires the minimap hook: Minimap.Update
        /// returns before LoadMapData when there is no graphics device. Called from the creature sorter's
        /// ZoneSystem.SetupLocations postfix, after the sorter, which is the order a host gets too. Once per
        /// session, like the minimap hook's self-unsubscribe.
        /// </summary>
        internal static void RunOnHeadlessWorldLoad()
        {
            if (!Jotunn.Managers.GUIManager.IsHeadless() || _ranOnHeadlessServer || !CreatureSorterRunner.OwnsLootConfig())
            {
                return;
            }

            _ranOnHeadlessServer = true;
            CheckAndAddAllEnchantableItems();
        }

        /// <summary>
        /// Re-arms the minimap hook, so the pass runs on the next world this machine loads itself even if it
        /// already ran this session. For a patch rebuild the pass cannot follow straight away (no world is
        /// loaded, or this client is on someone else's server): the rebuilt files start from the embedded
        /// defaults, without the items the pass adds.
        /// </summary>
        internal static void RunOnNextWorldLoad()
        {
            // Removed first: a handler that is still subscribed would otherwise be added twice and run twice.
            Jotunn.Managers.MinimapManager.OnVanillaMapDataLoaded -= OnMapDataLoadedHandler;
            Jotunn.Managers.MinimapManager.OnVanillaMapDataLoaded += OnMapDataLoadedHandler;
        }

        /// <summary>
        /// The baseconfig files this pass writes back out. A caller that needs the live config to
        /// match disk without waiting on the file watchers reloads exactly these.
        /// </summary>
        public static readonly string[] RewrittenConfigFiles =
            ["adventuredata.json", "iteminfo.json", "loottables.json"];

        /// <param name="force">Run even when nothing changed since the last run (see AutoAddRunState).</param>
        public static void CheckAndAddAllEnchantableItems(bool deregister = true, bool force = false)
        {
            // The pass merges onto the live configs and writes them over this machine's baseconfig files, so
            // it must not run where those configs are a server's push. Checked before unsubscribing, so a
            // world this session later loads itself still gets the pass.
            if (!CreatureSorterRunner.OwnsLootConfig())
            {
                EpicLoot.Log("Equipment auto-add skipped: this client is connected to a server, whose configs are in effect.");
                return;
            }

            if (deregister)
            {
                Jotunn.Managers.MinimapManager.OnVanillaMapDataLoaded -= OnMapDataLoadedHandler;
            }

            if (ELConfig.AutoAddEquipment.Value == false && ELConfig.AutoRemoveEquipmentNotFound.Value == false)
            {
                return;
            }

            List<ItemDrop> allItems = Resources.FindObjectsOfTypeAll<ItemDrop>().ToList();
            List<ItemDrop> allEquipment = allItems.Where(i => i.m_itemData != null &&
                i.m_itemData.m_shared != null &&
                i.m_autoPickup == true &&
                string.IsNullOrEmpty(i.m_itemData.m_shared.m_dlc) &&
                !string.IsNullOrEmpty(i.m_itemData.m_shared.m_description) &&
                EpicLoot.IsAllowedMagicItemType(i.m_itemData) &&
                !LootDenyList.IsDenied(i.name) &&
                !AttackKillsWielder(i.m_itemData)).ToList();
            HashSet<string> magicMats = new HashSet<string>(allItems.Where(i => i.m_itemData != null &&
                i.m_itemData.m_dropPrefab != null &&
                (i.m_itemData.IsMagicCraftingMaterial() || i.m_itemData.IsRunestone()))
                .Select(x => x.m_itemData.m_dropPrefab.name), StringComparer.Ordinal);
            HashSet<string> selfKilling = new HashSet<string>(allItems
                .Where(i => AttackKillsWielder(i.m_itemData))
                .Select(i => i.name), StringComparer.Ordinal);

            // Skipped when everything it reads is as its last complete run left it: the result would be the
            // files already on disk, and the configs in memory were read from them.
            string gameInputs = AutoAddRunState.DescribeGameInputs(
                allEquipment.Select(item => $"{item.name}|{ItemTypeFor(item.m_itemData)}"), magicMats, selfKilling);
            if (!force && AutoAddRunState.Matches(AutoAddRunState.Fingerprint(gameInputs)))
            {
                EpicLoot.Log("Equipment auto-add skipped: the game, mods, settings and configs are as its last run left them.");
                return;
            }

            _pass = new PassLookups();
            try
            {
                RunPass(allEquipment, magicMats, selfKilling, gameInputs);
            }
            finally
            {
                _pass = null;
            }
        }

        private static void RunPass(List<ItemDrop> allEquipment, HashSet<string> magicMats, HashSet<string> selfKilling,
            string gameInputs)
        {
            List<ItemTypeInfo> currentConfigs = GatedItemTypeHelper.GatedConfig.ItemInfo;
            // Taken before the passes below, which replace these entries' lists in place.
            Dictionary<string, HashSet<string>> previousTypes = TypesByItem(currentConfigs);

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

            EpicLoot.Log($"Checking all equipment in game.");
            foundByCategory = EnsureItemsInConfigMutating(foundByCategory, itemsByCategory, allEquipment);

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
            List<ItemTypeInfo> newConfig = MergeItemsByBossConfig(itemsByCategory);

            RemovePropItemsFromConfig(newConfig, selfKilling);

            BaseConfigWriter.Result? vendor = AddRemoveItemsFromVendor(newConfig);
            BaseConfigWriter.Result? loot = AddRemoveItemsFromLootLists(magicMats, foundByCategory, newConfig, previousTypes);

            string contents;
            try
            {
                contents = JsonConvert.SerializeObject(new ItemInfoConfig() { ItemInfo = newConfig }, Formatting.Indented);
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Failed to auto-add items to iteminfo.json: {e.Message}");
                return;
            }

            if (BaseConfigWriter.Write("iteminfo", contents, "equipment auto-add") == BaseConfigWriter.Result.Failed)
            {
                return;
            }

            // Reloaded now rather than on the scheduler's next poll. iteminfo always, since the passes above
            // replaced its live lists in place; adventuredata whenever the vendor step replaced the live gamble
            // costs, which dropped the ones other mods add through the API; loottables only when written.
            List<string> reload = ["iteminfo.json"];
            if (vendor != null)
            {
                reload.Add("adventuredata.json");
            }

            if (loot == BaseConfigWriter.Result.Written)
            {
                reload.Add("loottables.json");
            }

            ELConfig.ReloadBaseConfigsFromDisk(reload);

            // Recorded only when every file reached disk, with the files as this run left them.
            if (vendor != BaseConfigWriter.Result.Failed && loot != BaseConfigWriter.Result.Failed)
            {
                AutoAddRunState.Save(AutoAddRunState.Fingerprint(gameInputs));
            }
        }

        // The iteminfo.json type the pass files an item under: its raw-field classification, or for a
        // non-equippable item that iteminfo explicitly allows to be enchanted, the type configured there.
        private static string ItemTypeFor(ItemDrop.ItemData itemData)
        {
            // Raw-field classification only: this is what GENERATES iteminfo.json, so it must not consult the
            // configured answer (ItemTypeClassifier.GetItemInfoType) -- doing so would make the sorter
            // self-confirming and unable to ever re-sort a mis-filed item.
            string itemType = ItemTypeClassifier.ClassifyFromFields(itemData);
            if (!itemData.IsEquipable() && itemData.m_dropPrefab != null &&
                GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(itemData.m_dropPrefab.name, out GatedItemDetails details) &&
                details.Enchantable)
            {
                itemType = details.Type;
            }

            return itemType;
        }

        /// <summary>
        /// True for a weapon that kills whoever swings it: its attack sets m_attackKillsSelf, which
        /// Attack.Trigger (assembly_valheim/Attack.cs:578) answers with 9,999,999 untyped true damage to the
        /// wielder via ApplyDamage as the swing completes.
        ///
        /// <para>This is the BACKSTOP, not the main defence. The Deep North SP_ weapons set the flag, but
        /// their FW_ twins and every prop armor piece do not, and they are otherwise field-for-field
        /// identical to real gear -- so the known props are excluded by name through
        /// <see cref="LootDenyList"/>. The flag test stays to catch a self-killing prop that a later update
        /// adds under a name the deny list does not know yet.</para>
        /// </summary>
        private static bool AttackKillsWielder(ItemDrop.ItemData item)
        {
            return item?.m_shared != null &&
                (item.m_shared.m_attack?.m_attackKillsSelf == true ||
                 item.m_shared.m_secondaryAttack?.m_attackKillsSelf == true);
        }

        /// <summary>
        /// Purges prop items from an already-written iteminfo config: anything on <see cref="LootDenyList"/>,
        /// plus any item whose attack kills its wielder. Matched on prefab name -- the identity
        /// EnsureItemsInConfigMutating writes.
        ///
        /// <para>Runs on the merged result rather than relying on the equipment scan alone, because an entry
        /// already in iteminfo.json is carried forward before any ignore check is consulted
        /// (EnsureItemsInConfigMutating's "already in the config" branch), so a prop written by an earlier
        /// build would otherwise be kept forever.</para>
        /// </summary>
        /// <param name="selfKilling">The names of every loaded item whose attack kills its wielder.</param>
        private static void RemovePropItemsFromConfig(List<ItemTypeInfo> config, HashSet<string> selfKilling)
        {
            SortedSet<string> removedNames = new SortedSet<string>(StringComparer.Ordinal);
            bool IsProp(string name)
            {
                if (!LootDenyList.IsDenied(name) && !selfKilling.Contains(name))
                {
                    return false;
                }

                removedNames.Add(name);
                return true;
            }

            int removed = 0;
            foreach (ItemTypeInfo itemType in config)
            {
#pragma warning disable 612 // Items is obsolete, but a config written by an older build may still use it.
                removed += itemType.Items.RemoveAll(IsProp);
#pragma warning restore 612
                foreach (KeyValuePair<string, List<string>> byBoss in itemType.ItemsByBoss)
                {
                    removed += byBoss.Value.RemoveAll(IsProp);
                }
            }

            if (removed > 0)
            {
                EpicLoot.LogWarningForce($"Removed {removed} prop item entries ({removedNames.Count} distinct) " +
                    $"from iteminfo.json: {string.Join(", ", removedNames)}. These are creature weapons and NPC " +
                    "props -- some invisible when worn, some killing whoever attacks with them -- and must never be loot.");
            }
        }

        // Null when the step is off.
        private static BaseConfigWriter.Result? AddRemoveItemsFromLootLists(HashSet<string> magicMats,
            Dictionary<string, ItemTypeInfo> foundByCategory,
            List<ItemTypeInfo> newConfig,
            Dictionary<string, HashSet<string>> previousTypes)
        {
            if (!ELConfig.AutoAddRemoveEquipmentFromLootLists.Value)
            {
                return null;
            }

            EpicLoot.Log("Adding/Removing entries in the loot drop configuration.");
            LootConfig defaultcfg = LootRoller.Config;
            List<LootTable> updatedLootTables = [];
            List<LootItemSet> updatedItemSets = [];
            Dictionary<string, HashSet<string>> newTypes = TypesByItem(newConfig);

            HashSet<string> metaItemSetNames = new HashSet<string>(LootRoller.Config.ItemSets.Select(x => x.Name), StringComparer.Ordinal);
            // Every table name up front: a reference to a table further down the file -- or a boss table's
            // reference to its own level 1 -- is as valid as one to a table above it. ItemSets are checked
            // against it too: a set member or a rarity-keyed set's map may name "Table.N" as well.
            HashSet<string> metaLootTables = new HashSet<string>(LootRoller.Config.LootTables
                .Where(x => !string.IsNullOrEmpty(x?.Object))
                .Select(x => x.Object), StringComparer.Ordinal);
            HashSet<string> validItems = new HashSet<string>(StringComparer.Ordinal);
            foreach (ItemTypeInfo entry in foundByCategory.Values)
            {
                foreach (KeyValuePair<string, List<string>> iteme in entry.ItemsByBoss)
                {
                    validItems.UnionWith(iteme.Value);
                }
            }

            foreach (LootItemSet lis in LootRoller.Config.ItemSets)
            {
                List<LootDrop> entries = new List<LootDrop>();
                HashSet<string> addedItems = new HashSet<string>(StringComparer.Ordinal);
                // The item categories a tiered set (Tier6Weapons) is filled from; null for any other set.
                List<string> setCategories = null;
                bool tiered = DetermineTierAndType(lis.Name, out string tier, out string loottype);
                if (tiered)
                {
                    Config.LootSetsToItemCategories.TryGetValue(loottype, out setCategories);
                }

                EpicLoot.Log($"Checking LootSet entry: {lis.Name}");
                foreach (LootDrop loot in lis.Loot)
                {
                    if (WasRefiledOutOf(loot.Item, setCategories, previousTypes, newTypes))
                    {
                        EpicLoot.Log($"{loot.Item} is now filed under {string.Join(", ", newTypes[loot.Item])} " +
                            $"and is removed from {lis.Name}.");
                        continue;
                    }

                    if (IsValidLootEntryName(loot.Item, metaItemSetNames, metaLootTables, validItems, magicMats))
                    {
                        PruneRarityItems(loot, lis.Name, metaItemSetNames, metaLootTables, validItems, magicMats);
                        entries.Add(loot);
                        addedItems.Add(loot.Item);
                        continue;
                    }

                    EpicLoot.Log($"{loot.Item} is not a found item and will be removed from the loot tables.");
                }

                if (tiered)
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

                // Keep the set even when validation emptied it: metaItemSetNames was taken before any
                // pruning, so references to the set survive, and a missing set would be reported as a
                // missing item prefab far from the real cause.
                if (entries.Count == 0)
                {
                    EpicLoot.LogWarning($"LootSet {lis.Name} has no valid entries left after validation. " +
                        $"Keeping it so references to it stay resolvable, but it will drop nothing.");
                }

                updatedItemSets.Add(new LootItemSet { Name = lis.Name, Loot = entries.ToArray() });
            }

            EpicLoot.Log($"Checking loot tables for invalid entries.");
            foreach (LootTable lt in LootRoller.Config.LootTables)
            {
                if (lt == null)
                {
                    continue;
                }

                // A list that would come out empty is left as written: with nothing valid left to roll it
                // was already broken, and emptying it on disk would make a validation gap permanent.
                if (lt.LeveledLoot != null)
                {
                    foreach (LeveledLootDef level in lt.LeveledLoot)
                    {
                        if (level?.Loot == null || level.Loot.Length == 0)
                        {
                            continue;
                        }

                        string owner = $"{lt.Object} level {level.Level}";
                        List<LootDrop> valid = ValidateLootList(owner, level.Loot, metaLootTables,
                            metaItemSetNames, validItems, magicMats);
                        if (valid.Count == 0)
                        {
                            EpicLoot.LogWarning($"Loot table {owner}: no entry passed validation. Keeping " +
                                "it as written; it will drop nothing until its entries are fixed.");
                            continue;
                        }

                        level.Loot = valid.ToArray();
                    }
                }

                updatedLootTables.Add(lt);
            }

            EpicLoot.Log($"Finished Validating loottable.");
            // A copy of the live config, not a new one built field by field, so every other root field
            // (DefaultStarScaling, and anything added later) is written back too.
            LootConfig newLootConfig = LootRoller.Config.ShallowCopy();
            newLootConfig.ItemSets = updatedItemSets.ToArray();
            newLootConfig.LootTables = updatedLootTables.ToArray();
            return LootConfigFile.Write(newLootConfig, "equipment auto-add");
        }

        // Item prefab name -> every iteminfo.json type that lists it, under any boss key.
        private static Dictionary<string, HashSet<string>> TypesByItem(IEnumerable<ItemTypeInfo> config)
        {
            Dictionary<string, HashSet<string>> types = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (ItemTypeInfo itemType in config)
            {
                if (itemType?.ItemsByBoss == null)
                {
                    continue;
                }

                foreach (List<string> items in itemType.ItemsByBoss.Values)
                {
                    if (items == null)
                    {
                        continue;
                    }

                    foreach (string item in items)
                    {
                        if (string.IsNullOrEmpty(item))
                        {
                            continue;
                        }

                        if (!types.TryGetValue(item, out HashSet<string> itemTypes))
                        {
                            itemTypes = new HashSet<string>(StringComparer.Ordinal);
                            types.Add(item, itemTypes);
                        }

                        itemTypes.Add(itemType.Type);
                    }
                }
            }

            return types;
        }

        // True when this pass moved the item out of every category a tiered set is filled from (the grappling
        // hook, once filed under Bows, now under Tools): its entry in that set is the sorter's own stale output,
        // and validation alone keeps any real item. Only a move made on this pass counts, so an entry put in a
        // set by hand stays -- the shipped Torch in Tier0Weapons is a Torches item.
        private static bool WasRefiledOutOf(string item, List<string> setCategories,
            Dictionary<string, HashSet<string>> previousTypes, Dictionary<string, HashSet<string>> newTypes)
        {
            return setCategories != null && !string.IsNullOrEmpty(item) &&
                previousTypes.TryGetValue(item, out HashSet<string> before) && before.Overlaps(setCategories) &&
                newTypes.TryGetValue(item, out HashSet<string> after) && !after.Overlaps(setCategories);
        }

        // Null when the step is off. When it runs it replaces the live config's gamble costs.
        private static BaseConfigWriter.Result? AddRemoveItemsFromVendor(List<ItemTypeInfo> newConfig)
        {
            if (ELConfig.AutoAddRemoveEquipmentFromVendor.Value == false)
            {
                return null;
            }

            EpicLoot.Log("Adding/Removing entries for the vendor from detected equipment.");
            Dictionary<string, SecretStashItemConfig> existingVendorItems = new Dictionary<string, SecretStashItemConfig>();
            HashSet<string> foundItemEntry = new HashSet<string>(StringComparer.Ordinal);

            EpicLoot.Log("Adding Entries to the vendor list.");
            foreach (SecretStashItemConfig gamble in AdventureDataManager.Config.Gamble.GambleCosts)
            {
                if (existingVendorItems.ContainsKey(gamble.Item))
                {
                    continue;
                }

                existingVendorItems.Add(gamble.Item, gamble);
            }

            foreach (ItemTypeInfo itemType in newConfig)
            {
                foreach (KeyValuePair<string, List<string>> bossEntry in itemType.ItemsByBoss)
                {
                    foreach (string itemName in bossEntry.Value)
                    {
                        if (existingVendorItems.ContainsKey(itemName))
                        {
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

            EpicLoot.Log("Removing invalid entries.");
            List<SecretStashItemConfig> newGambleItems = existingVendorItems
                .Where(x => foundItemEntry.Contains(x.Key)).Select(x => x.Value).ToList();
            EpicLoot.Log("Building config.");
            AdventureDataConfig AdventureDataConfigReplacement = AdventureDataManager.Config;
            AdventureDataConfigReplacement.Gamble.GambleCosts = newGambleItems;

            EpicLoot.Log("Writing config.");
            string contents;
            try
            {
                contents = JsonConvert.SerializeObject(AdventureDataConfigReplacement, Formatting.Indented);
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Failed to auto-add vendor items to adventuredata.json: {e.Message}");
                return BaseConfigWriter.Result.Failed;
            }

            return BaseConfigWriter.Write("adventuredata", contents, "equipment auto-add");
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
                        Fallback = item.Value.Fallback,
                        ItemFallback = item.Value.ItemFallback,
                        Type = item.Value.Type,
                        Enchantable = item.Value.Enchantable,
                        Socketable = item.Value.Socketable,
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
            // Per category, which items the config ignores and which boss key lists each item: the first one
            // in the config's order, as a scan of the lists would find.
            Dictionary<string, (HashSet<string> Ignored, Dictionary<string, string> BossKeys)> configured =
                new Dictionary<string, (HashSet<string>, Dictionary<string, string>)>();
            foreach (KeyValuePair<string, ItemTypeInfo> category in itemsByCategory)
            {
                Dictionary<string, string> bossKeys = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, List<string>> entry in category.Value.ItemsByBoss)
                {
                    foreach (string listed in entry.Value)
                    {
                        if (listed != null && !bossKeys.ContainsKey(listed))
                        {
                            bossKeys.Add(listed, entry.Key);
                        }
                    }
                }

                configured[category.Key] = (new HashSet<string>(category.Value.IgnoredItems.Where(name => name != null),
                    StringComparer.Ordinal), bossKeys);
            }

            foreach (ItemDrop item in allEquipment)
            {
                string itemType = ItemTypeFor(item.m_itemData);
                string itemName = item.name;
                if (configured.TryGetValue(itemType, out var inConfig) && foundByCategory.ContainsKey(itemType))
                {
                    if (inConfig.Ignored.Contains(itemName))
                    {
                        foundByCategory[itemType].IgnoredItems.Add(itemName);
                        continue;
                    }

                    if (inConfig.BossKeys.TryGetValue(itemName, out string configuredKey))
                    {
                        Dictionary<string, List<string>> catEntry = foundByCategory[itemType].ItemsByBoss;
                        if (!catEntry.ContainsKey(configuredKey))
                        {
                            catEntry.Add(configuredKey, new List<string>());
                        }

                        catEntry[configuredKey].Add(itemName);
                        continue;
                    }
                }

                string key = DetermineBossLevelForItem(item);
                if (_pass.UncraftableKeys.TryGetValue(itemName, out string uncraftableKey))
                {
                    if (foundByCategory[itemType].ItemsByBoss.ContainsKey(uncraftableKey))
                    {
                        foundByCategory[itemType].ItemsByBoss[uncraftableKey].Add(itemName);
                    }
                    else
                    {
                        foundByCategory[itemType].ItemsByBoss.Add(uncraftableKey, new List<string>() { itemName });
                    }

                    continue;
                }

                if ((ELConfig.OnlyAddEquipmentWithRecipes.Value == true && key == NONE) ||
                    (key == NONE && itemType == NONE) ||
                    itemType == ItemTypeClassifier.Unknown ||
                    _pass.RestrictedItems.Contains(itemName))
                {
                    EpicLoot.Log($"skipping name:{itemName} type:{itemType} techlevel:{key}");
                    continue;
                }

                EpicLoot.Log($"{itemType} {key} add {itemName}");
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
            foreach (KeyValuePair<string, ItemTypeInfo> fbc in foundByCategory)
            {
                if (ELConfig.AutoAddEquipment.Value)
                {
                    if (!itemsByCategory.ContainsKey(fbc.Key))
                    {
                        continue;
                    }

                    itemsByCategory[fbc.Key].IgnoredItems = itemsByCategory[fbc.Key].IgnoredItems
                        .Union(fbc.Value.IgnoredItems).ToList();

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

        private static List<LootDrop> ValidateLootList(string owner, LootDrop[] lootList,
            HashSet<string> metaLootTables, HashSet<string> metaItemSetNames, HashSet<string> validItems,
            HashSet<string> magicMats)
        {
            List<LootDrop> updatedLootDrop = new List<LootDrop>();
            foreach (LootDrop loot in lootList)
            {
                if (loot == null)
                {
                    continue;
                }

                if (!IsValidLootEntryName(loot.Item, metaItemSetNames, metaLootTables, validItems, magicMats))
                {
                    EpicLoot.Log($"REMOVING: Loot table ({owner}) Item {loot.Item} not found.");
                    continue;
                }

                PruneRarityItems(loot, owner, metaItemSetNames, metaLootTables, validItems, magicMats);
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
        // Both passes accept table references: ResolveLootDrop follows one from an ItemSet member or a
        // rarity map as readily as from a table, so rejecting it here would delete a working entry.
        private static bool IsValidLootEntryName(string name, HashSet<string> metaItemSetNames,
            HashSet<string> metaLootTables, HashSet<string> validItems, HashSet<string> magicMats)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // Denied props are rejected first: the ObjectDB fallback at the bottom accepts any real ItemDrop.
            if (LootDenyList.IsDenied(name))
            {
                EpicLoot.Log($"REMOVING denied prop item {name} from the loot configuration.");
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

            if (validItems.Contains(name) || metaItemSetNames.Contains(name) || magicMats.Contains(name))
            {
                return true;
            }

            // ObjectDB.m_items also holds a few vanilla non-item prefabs (SnowRoller, ...), which
            // LootRoller can never spawn as a drop, so a name has to resolve to an actual ItemDrop.
            // ...and a self-killing prop the deny list does not know yet is rejected on its flag.
            GameObject prefab = ObjectDB.instance.GetItemPrefab(name);
            return prefab != null && prefab.TryGetComponent(out ItemDrop itemDrop) &&
                !AttackKillsWielder(itemDrop.m_itemData);
        }

        // Drops only the unresolvable rarities from an entry's per-rarity map, leaving the entry itself
        // alone -- its Item already validated, and it stays a working drop at every rarity that remains.
        // An emptied map is removed outright so the rewritten config does not carry a dead "RarityItems".
        private static void PruneRarityItems(LootDrop loot, string owner, HashSet<string> metaItemSetNames,
            HashSet<string> metaLootTables, HashSet<string> validItems, HashSet<string> magicMats)
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

            return [97, 2, 1, 0, 0, 0];
        }

        public static string DetermineBossLevelForItem(ItemDrop itemDrop)
        {
            Recipe itemRecipe = FindEnabledRecipe(itemDrop);
            if (itemRecipe == null || itemRecipe.m_resources == null)
            {
                return NONE;
            }

            // Highest tier first: an item belongs to the highest biome whose station or materials its recipe
            // uses. An item with no recipe or materials belongs in UncraftableItemsAlwaysAllowed.
            foreach (BiomeSorting biome in _pass?.BiomesHighestFirst ?? PassLookups.BiomesHighestFirstFromConfig())
            {
                if (itemRecipe.m_craftingStation != null &&
                    biome.Stations.Contains(itemRecipe.m_craftingStation.name))
                {
                    return biome.BossKey;
                }

                foreach (Piece.Requirement req in itemRecipe.m_resources)
                {
                    if (req.m_resItem != null && biome.Materials.Contains(req.m_resItem.name))
                    {
                        return biome.BossKey;
                    }
                }
            }

            return NONE;
        }

        /// <summary>
        /// The enabled recipe that crafts this exact prefab. Not ObjectDB.GetRecipe: that matches on
        /// m_shared.m_name, the display token, which creature weapons copy from the item they imitate. The
        /// Dvergr rogues' DvergerArbalest_shoot* are all "$item_crossbow_arbalest", so they were handed the real
        /// Arbalest's recipe, passed Only Add Equipment With Recipes and were filed beside it.
        /// </summary>
        private static Recipe FindEnabledRecipe(ItemDrop itemDrop)
        {
            if (itemDrop == null || ObjectDB.instance == null)
            {
                return null;
            }

            if (_pass != null)
            {
                return _pass.RecipesByItem.TryGetValue(itemDrop.name, out Recipe found) ? found : null;
            }

            foreach (Recipe recipe in ObjectDB.instance.m_recipes)
            {
                if (recipe != null && recipe.m_enabled && recipe.m_item != null && recipe.m_item.name == itemDrop.name)
                {
                    return recipe;
                }
            }

            return null;
        }

        // One biome of BiomeSorterData, as DetermineBossLevelForItem matches recipes against it.
        private sealed class BiomeSorting
        {
            public string BossKey;
            public HashSet<string> Stations;
            public HashSet<string> Materials;
        }

        /// <summary>
        /// What the per-item loops of one pass look up, built once at its start instead of searched per item:
        /// with a few thousand items and recipes the searches were a large share of the pass.
        /// </summary>
        private sealed class PassLookups
        {
            // The first enabled recipe for each item prefab name, in ObjectDB order (what FindEnabledRecipe's
            // scan returns).
            public readonly Dictionary<string, Recipe> RecipesByItem = new Dictionary<string, Recipe>(StringComparer.Ordinal);
            public readonly List<BiomeSorting> BiomesHighestFirst = BiomesHighestFirstFromConfig();
            // Item -> the first UncraftableItemsAlwaysAllowed key that lists it.
            public readonly Dictionary<string, string> UncraftableKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            // Read per pass, so an edited RestrictedItems list applies to the next run.
            public readonly HashSet<string> RestrictedItems =
                new HashSet<string>(LootRoller.Config?.RestrictedItems ?? new List<string>(), StringComparer.Ordinal);

            public PassLookups()
            {
                foreach (Recipe recipe in ObjectDB.instance?.m_recipes ?? new List<Recipe>())
                {
                    if (recipe != null && recipe.m_enabled && recipe.m_item != null &&
                        !RecipesByItem.ContainsKey(recipe.m_item.name))
                    {
                        RecipesByItem.Add(recipe.m_item.name, recipe);
                    }
                }

                foreach (KeyValuePair<string, List<string>> uncraftable in Config.UncraftableItemsAlwaysAllowed)
                {
                    foreach (string item in uncraftable.Value ?? new List<string>())
                    {
                        if (item != null && !UncraftableKeys.ContainsKey(item))
                        {
                            UncraftableKeys.Add(item, uncraftable.Key);
                        }
                    }
                }
            }

            // BiomeSorterData from the highest tier down, the order DetermineBossLevelForItem tries it in.
            public static List<BiomeSorting> BiomesHighestFirstFromConfig()
            {
                return Config.BiomeSorterData.Reverse()
                    .Select(entry => new BiomeSorting
                    {
                        BossKey = entry.Value.BossKey,
                        Stations = new HashSet<string>(entry.Value.BiomeSpecificCraftingStations ?? new List<string>(), StringComparer.Ordinal),
                        Materials = new HashSet<string>(entry.Value.BiomeMaterials ?? new List<string>(), StringComparer.Ordinal)
                    })
                    .ToList();
            }
        }
    }
}
