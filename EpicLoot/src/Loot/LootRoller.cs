using BepInEx;
using Common;
using EpicLoot.Adventure;
using EpicLoot.Config;
using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.GatedItemType;
using EpicLoot.General;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot_UnityLib;
using JetBrains.Annotations;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace EpicLoot
{
    public static class LootRoller
    {
        public static LootConfig Config;
        public static readonly Dictionary<string, LootItemSet> ItemSets = new Dictionary<string, LootItemSet>();
        public static readonly Dictionary<string, List<LootTable>> LootTables = new Dictionary<string, List<LootTable>>();

        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedDropCountTable;
        private static WeightedRandomCollection<LootDrop> _weightedLootTable;
        private static WeightedRandomCollection<MagicItemEffectDefinition> _weightedEffectTable;
        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedEffectCountTable;
        private static WeightedRandomCollection<KeyValuePair<ItemRarity, float>> _weightedRarityTable;
        private static WeightedRandomCollection<LegendaryInfo> _weightedLegendaryTable;
        private static WeightedRandomCollection<LegendaryInfo> _weightedMythicTable;
        public static bool CheatRollingItem = false;
        public static int CheatEffectCount;
        public static bool CheatDisableGating;
        public static bool CheatForceMagicEffect;
        public static string ForcedMagicEffect = "";
        public static string CheatForceLegendary;
        public static string CheatForceMythic;

        public static void Initialize(LootConfig lootConfig)
        {
            Config = lootConfig;

            _weightedDropCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>();
            _weightedLootTable = new WeightedRandomCollection<LootDrop>();
            _weightedEffectTable = new WeightedRandomCollection<MagicItemEffectDefinition>();
            _weightedEffectCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>();
            _weightedRarityTable = new WeightedRandomCollection<KeyValuePair<ItemRarity, float>>();
            _weightedLegendaryTable = new WeightedRandomCollection<LegendaryInfo>();
            _weightedMythicTable = new WeightedRandomCollection<LegendaryInfo>();

            ItemSets.Clear();
            LootTables.Clear();
          
            AddItemSets(lootConfig.ItemSets);
            AddLootTables(lootConfig.LootTables);
        }

        public static LootConfig GetCFG()
        {
            return Config;
        }

        private static void AddItemSets([NotNull] IEnumerable<LootItemSet> itemSets)
        {
            foreach (var itemSet in itemSets)
            {
                if (string.IsNullOrEmpty(itemSet.Name))
                {
                    EpicLoot.LogWarning($"Tried to add ItemSet with no name!");
                    continue;
                }

                if (!ItemSets.ContainsKey(itemSet.Name))
                {
                    ItemSets.Add(itemSet.Name, itemSet);
                }
            }
        }

        public static void AddLootTables([NotNull] IEnumerable<LootTable> lootTables)
        {
            // Add loottables for mobs or objects that do not reference another template
            foreach (var lootTable in lootTables.Where(x => x.RefObject == null || x.RefObject == ""))
            {
                AddLootTable(lootTable);
            }

            // Add loottables that are based off mob or object templates
            foreach (var lootTable in lootTables.Where(x => x.RefObject != null && x.RefObject != ""))
            {
                AddLootTable(lootTable);
            }
        }

        public static void AddLootTable([NotNull] LootTable lootTable)
        {
            var key = lootTable.Object;
            if (string.IsNullOrEmpty(key))
            {
                EpicLoot.LogError("Loot table missing Object name!");
                return;
            }

            if (!LootTables.ContainsKey(key))
            {
                LootTables.Add(key, new List<LootTable>());
            }

            var refKey = lootTable.RefObject;
            if (string.IsNullOrEmpty(refKey))
            {
                LootTables[key].Add(lootTable);
            }
            else
            {
                if (!LootTables.ContainsKey(refKey))
                {
                    EpicLoot.LogError("Loot table missing RefObject name!");
                    return;
                }
                else
                {
                    LootTables[key] = LootTables[refKey];
                }
            }
        }

        public static List<GameObject> RollLootTableAndSpawnObjects(List<LootTable> lootTables,
            int level, string objectName, Vector3 dropPoint)
        {
            return RollLootTableInternal(lootTables, level, objectName, dropPoint, true);
        }

        public static List<GameObject> RollLootTableAndSpawnObjects(LootTable lootTable, 
            int level, string objectName, Vector3 dropPoint)
        {
            return RollLootTableInternal(lootTable, level, objectName, dropPoint, true);
        }

        public static List<ItemDrop.ItemData> RollLootTable(List<LootTable> lootTables,
            int level, string objectName, Vector3 dropPoint)
        {
            var results = new List<ItemDrop.ItemData>();
            var gameObjects = RollLootTableInternal(lootTables, level, objectName, dropPoint, false);
            foreach (var itemObject in gameObjects)
            {
                results.Add(itemObject.GetComponent<ItemDrop>().m_itemData.Clone());
                ZNetScene.instance.Destroy(itemObject);
            }

            return results;
        }

        public static List<ItemDrop.ItemData> RollLootTable(LootTable lootTable,
            int level, string objectName, Vector3 dropPoint)
        {
            return RollLootTable(new List<LootTable> {lootTable}, level, objectName, dropPoint);
        }

        public static List<ItemDrop.ItemData> RollLootTable(string lootTableName,
            int level, string objectName, Vector3 dropPoint)
        {
            var lootTable = GetLootTable(lootTableName);
            if (lootTable == null)
            {
                return new List<ItemDrop.ItemData>();
            }

            return RollLootTable(lootTable, level, objectName, dropPoint);
        }

        private static List<GameObject> RollLootTableInternal(IEnumerable<LootTable> lootTables,
            int level, string objectName, Vector3 dropPoint, bool initializeObject)
        {
            var results = new List<GameObject>();
            foreach (var lootTable in lootTables)
            {
                results.AddRange(RollLootTableInternal(lootTable, level, objectName, dropPoint, initializeObject));
            }
            return results;
        }

        public static bool AnyItemSpawnCheatsActive()
        {
            return CheatRollingItem || CheatDisableGating || CheatForceMagicEffect ||
                !string.IsNullOrEmpty(CheatForceLegendary) || !string.IsNullOrEmpty(CheatForceMythic) ||
                CheatEffectCount > 0;
        }

        public static Dictionary<string,float> GetLootTableChances(Vector3 location, List<LootTable> LootTables)
        {
            Dictionary<string, float> results = new Dictionary<string, float>();

            foreach(LootTable lt in LootTables)
            {
                foreach(LootDrop ld in lt.Loot)
                {
                    if (ItemSets.ContainsKey(ld.Item))
                    {
                        ItemSets[ld.Item].Loot.ToList().ForEach(x =>
                        {
                            if (results.ContainsKey(x.Item))
                            {
                                results[x.Item] += ld.Weight;
                            }
                            else
                            {
                                results.Add(x.Item, ld.Weight);
                            }
                        });
                    }

                    if (results.ContainsKey(ld.Item))
                    {
                        results[ld.Item] += ld.Weight;
                    }
                    else
                    {
                        results.Add(ld.Item, ld.Weight);
                    }
                }
            }

            return results;
        }

        public static List<ItemDrop.ItemData> RollLootNoTableWithSpecifics(Vector3 location,
            List<LootTable> lootTables,
            int numResults = 1,
            ItemRarity rarity = ItemRarity.Magic,
            bool luckUpgradesRarity = true,
            int luckUpgradesRarityFactor = 2,
            float powerLevelMod = 1.0f)
        {
            var luckFactor = GetLuckFactor(location);
            List<ItemDrop.ItemData> results = new List<ItemDrop.ItemData>();
            HashSet<string> rolledItems = new HashSet<string>();
            int failures = 0;

            // This is effectively an estimate, but we will just keep rolling until we get the number of results we want if this is not enough
            int lootPerCategory = numResults / lootTables.Count;
            if (lootPerCategory < 1)
            {
                lootPerCategory = 1;
            }

            while (results.Count < numResults)
            {
                foreach (LootTable lt in lootTables.shuffleList())
                {
                    if (results.Count >= numResults)
                    {
                        break;
                    }

                    ItemRarity itemRollRarity = rarity;
                    if (luckUpgradesRarity == true)
                    {
                        LootDrop lootdrop = new() { Rarity = [] };
                        // TODO: Expose this as a config?
                        switch (rarity)
                        {
                            case ItemRarity.Magic:
                                lootdrop.Rarity = [100 - luckUpgradesRarityFactor, luckUpgradesRarityFactor, 0, 0, 0];
                                break;
                            case ItemRarity.Rare:
                                lootdrop.Rarity = [0, 100 - luckUpgradesRarityFactor, luckUpgradesRarityFactor, 0, 0];
                                break;
                            case ItemRarity.Epic:
                                lootdrop.Rarity = [0, 0, 100 - luckUpgradesRarityFactor, luckUpgradesRarityFactor, 0];
                                break;
                            case ItemRarity.Legendary:
                                lootdrop.Rarity = [0, 0, 0, 100 - luckUpgradesRarityFactor, luckUpgradesRarityFactor];
                                break;
                            case ItemRarity.Mythic:
                                lootdrop.Rarity = [0, 0, 0, 0, 100];
                                break;
                        }
                        itemRollRarity = RollItemRarity(lootdrop, luckFactor);
                    }

                    List<LootDrop> looteqrare = new List<LootDrop>();
                    foreach(LootDrop ld in lt.Loot)
                    {
                        looteqrare.Add(new LootDrop() { Item = ld.Item, Weight = ld.Weight, Rarity = [1] });
                    }

                    _weightedLootTable.Setup(looteqrare.ToArray(), x => x.Weight);
                    List<LootDrop> selectedDrops = _weightedLootTable.Roll(lootPerCategory);

                    EpicLoot.Log($"Available Loot ({lt.Loot.Length}) for table: {lt.Object}");
                    foreach (LootDrop lootDrop in lt.Loot)
                    {
                        string itemName = lootDrop?.Item ?? "Invalid/Null";
                        float weight = lootDrop?.Weight ?? -1;
                        EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarity} - Weight: {weight}");
                    }

                    EpicLoot.Log($"Selected Drops from: {lt.Object} - {selectedDrops.Count}");
                    foreach (LootDrop lootDrop in selectedDrops)
                    {
                        string itemName = !string.IsNullOrEmpty(lootDrop?.Item) ? lootDrop.Item : "Invalid Item Name";
                        int rarityLength = lootDrop?.Rarity?.Length != null ? lootDrop.Rarity.Length : -1;
                        EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarityLength} - Weight: {lootDrop.Weight}");

                        if (itemName == "Invalid Item Name")
                        {
                            failures += 1;
                            continue;
                        }

                        string gatedItemName = (CheatDisableGating) ?
                            GatedItemTypeHelper.GetGatedItemNameFromItemOrType(lootDrop.Item, GatedItemTypeMode.Unlimited) :
                            GatedItemTypeHelper.GetGatedItemNameFromItemOrType(lootDrop.Item, EpicLoot.GetGatedItemTypeMode());

                        GameObject prefab = PrefabManager.Instance.GetPrefab(gatedItemName);
                        if (prefab == null)
                        {
                            failures += 1;
                            continue;
                        }

                        GameObject selectedPrefab = ObjectDB.instance.GetItemPrefab(gatedItemName);
                        ZNetView.m_forceDisableInit = true;
                        GameObject droppedItem = Object.Instantiate(selectedPrefab, location, new Quaternion(0, 0, 0, 0));
                        ZNetView.m_forceDisableInit = false;
                        if (droppedItem == null)
                        {
                            failures += 1;
                            continue;
                        }

                        droppedItem.SetActive(false); // Don't make the object a real thing in the world yet
                        ItemDrop itemDrop = droppedItem.GetComponent<ItemDrop>();
                        if (itemDrop == null)
                        {
                            failures += 1;
                            ZNetScene.instance.Destroy(droppedItem);
                            continue;
                        }

                        var magicItemComponent = itemDrop.m_itemData.Data().GetOrCreate<MagicItemComponent>();
                        var magicItem = RollMagicItem(itemRollRarity, itemDrop.m_itemData, luckFactor, powerLevelMod);

                        if (CheatForceMagicEffect)
                        {
                            AddDebugMagicEffects(magicItem);
                        }

                        magicItemComponent.SetMagicItem(magicItem);
                        itemDrop.Save();
                        InitializeMagicItem(itemDrop.m_itemData);
                        results.Add(itemDrop.m_itemData);
                        ZNetScene.instance.Destroy(droppedItem); // Destroy the object, we just needed the itemdata
                    }
                }
            }

            if (failures > 0)
            {
                EpicLoot.LogWarningForce($"{failures} during item selection, this may have triggered a fallback. " +
                    $"Ensure your iteminfo does not have invalid items.");
            }

            return results;
        }

        private static List<GameObject> RollLootTableInternal(LootTable lootTable,
            int level, string objectName, Vector3 dropPoint, bool initializeObject)
        {
            var results = new List<GameObject>();
            if (lootTable == null || level <= 0 || string.IsNullOrEmpty(objectName))
            {
                return results;
            }

            var luckFactor = GetLuckFactor(dropPoint);

            var drops = GetDropsForLevel(lootTable, level);
            if (drops.Count == 0)
            {
                return results;
            }

            if (EpicLoot.AlwaysDropCheat)
            {
                drops = drops.Where(x => x.Key > 0).ToList();
            }
            else if (Mathf.Abs(ELConfig.GlobalDropRateModifier.Value - 1) > float.Epsilon)
            {
                var clampedDropRate = Mathf.Clamp(ELConfig.GlobalDropRateModifier.Value, 0, 4);
                var modifiedDrops = new List<KeyValuePair<int, float>>();
                foreach (var dropPair in drops)
                {
                    if (dropPair.Key == 0)
                        modifiedDrops.Add(new KeyValuePair<int, float>(dropPair.Key, dropPair.Value / clampedDropRate));
                    else
                        modifiedDrops.Add(new KeyValuePair<int, float>(dropPair.Key, dropPair.Value * clampedDropRate));
                }

                drops = modifiedDrops;
            }

            _weightedDropCountTable.Setup(drops, dropPair => dropPair.Value);
            var dropCountRollResult = _weightedDropCountTable.Roll();
            var dropCount = dropCountRollResult.Key;

            if (dropCount == 0)
            {
                return results;
            }

            var loot = GetLootForLevel(lootTable, level);

            if (loot == null)
            {
                loot = new LootDrop[] { };
            }

            EpicLoot.Log($"Available Loot ({loot.Length}) for table: {lootTable.Object} for level {level}");
            foreach (var lootDrop in loot)
            {
                var itemName = lootDrop?.Item ?? "Invalid/Null";
                var rarity = lootDrop?.Rarity?.Length ?? -1;
                var weight = lootDrop?.Weight ?? -1;
                EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarity} - Weight: {weight}");
            }

            _weightedLootTable.Setup(loot, x => x.Weight);
            var selectedDrops = _weightedLootTable.Roll(dropCount);

            EpicLoot.Log($"Selected Drops: {lootTable.Object} for level {level}");
            foreach (var lootDrop in selectedDrops)
            {
                var itemName = !string.IsNullOrEmpty(lootDrop?.Item) ? lootDrop.Item : "Invalid Item Name";
                var rarityLength = lootDrop?.Rarity?.Length != null ? lootDrop.Rarity.Length : -1;
                EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarityLength} - Weight: {lootDrop.Weight}");
            }


            var cheatsActive = AnyItemSpawnCheatsActive();
            foreach (var ld in selectedDrops)
            {
                if (ld == null)
                {
                    continue;
                }

                var lootDrop = ResolveLootDrop(ld);

                var itemName = !string.IsNullOrEmpty(lootDrop?.Item) ? lootDrop.Item : "Invalid Item Name";
                var rarityLength = lootDrop?.Rarity?.Length != null ? lootDrop.Rarity.Length : -1;
                EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarityLength} - Weight: {lootDrop.Weight}");

                // Check if lootDrop.Item is not an equipment piece- which means its a material or other, so we let it drop instead of replacing it with an unidentified item

                // Set the drop as an unidentified item of the selected rarity
                if (!cheatsActive && ELConfig.ItemsUnidentifiedDropRatio.Value > 0)
                {
                    var clampedUnidentifiedRate = Mathf.Clamp(ELConfig.ItemsUnidentifiedDropRatio.Value, 0.0f, 1.0f);
                    var setUnidentified = Random.Range(0.0f, 1.0f) < clampedUnidentifiedRate;
                    EpicLoot.Log($"Checking if item should be unidentified for {lootDrop.Item} ({clampedUnidentifiedRate} {setUnidentified})");
                    
                    if (setUnidentified)
                    {
                        // If the item is rolled to be unidentified, we need to check if it is a valid item type, if its not we don't make it unidentified
                        GameObject lootTableDrop = ObjectDB.instance.GetItemPrefab(lootDrop.Item);
                        if (lootTableDrop != null)
                        {
                            // If loot table drop is null, or has no item drop component, or is not equippable, then we don't make it unidentified
                            ItemDrop lootItemDrop = lootTableDrop.GetComponent<ItemDrop>();
                            if (lootItemDrop != null && EpicLoot.IsAllowedMagicItemType(lootItemDrop.m_itemData))
                            {
                                var rarity = RollItemRarity(lootDrop, luckFactor);

                                // Determine which biome this item is a part of, and set the drop biome to that tier
                                GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(lootDrop.Item, out var itemDetails);
                                List<Heightmap.Biome> biomes = new List<Heightmap.Biome>();
                                if (itemDetails != null)
                                {
                                    foreach(string bosskey in itemDetails.RequiredBosses)
                                    {
                                        foreach (BountyBossConfig bossEntry in AdventureDataManager.Config.Bounties.Bosses)
                                        {
                                            if (bossEntry.BossDefeatedKey != bosskey)
                                            {
                                                continue;
                                            }
                                            biomes.Add(bossEntry.Biome);
                                        }
                                    }
                                }

                                if (biomes.Count <= 0)
                                {
                                    ZoneSystem.instance.GetGroundData(ref dropPoint, out var _, out var biome, out var _, out var _);
                                    biomes.Add(biome);
                                }

                                string selectBiome = biomes.First().ToString();
                                GameObject prefab = ObjectDB.instance.GetItemPrefab($"{selectBiome}_{rarity}_Unidentified");
                                if (prefab == null)
                                {
                                    // Warn and drop the normal item instead
                                    EpicLoot.LogWarning($"Tried to spawn unidentified item for {selectBiome}_{rarity}_Unidentified " +
                                        $"but prefab was not found! Dropping {lootDrop.Item} instead.");
                                }
                                else
                                {
                                    EpicLoot.Log($"Adding {rarity} unidentified item");
                                    Quaternion randomRotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
                                    ZNetView.m_forceDisableInit = !initializeObject;
                                    GameObject lootdrop = Object.Instantiate(prefab, dropPoint, randomRotation);
                                    // Ensure that the unidentified item has the correct magic item data for the rarity
                                    var mic = lootdrop.GetComponent<ItemDrop>().m_itemData.Data().GetOrCreate<MagicItemComponent>();
                                    mic.SetMagicItem(new MagicItem
                                    {
                                        Rarity = rarity,
                                        IsUnidentified = true,
                                    });
                                    ZNetView.m_forceDisableInit = false;
                                    results.Add(lootdrop);
                                    continue;
                                }
                            }
                        }
                    }
                }

                if (!cheatsActive && ELConfig.ItemsToMaterialsDropRatio.Value > 0)
                {
                    var clampedConvertRate = Mathf.Clamp(ELConfig.ItemsToMaterialsDropRatio.Value, 0.0f, 1.0f);
                    var replaceWithMats = Random.Range(0.0f, 1.0f) < clampedConvertRate;
                    if (replaceWithMats)
                    {
                        GameObject prefab = null;

                        if (!lootDrop.Item.IsNullOrWhiteSpace())
                        {
                            prefab = ObjectDB.instance.GetItemPrefab(lootDrop.Item);
                        }

                        if (prefab == null)
                        {
                            continue;
                        }

                        var rarity = RollItemRarity(lootDrop, luckFactor);
                        var itemType = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType;
                        var disenchantProducts = EnchantCostsHelper.GetSacrificeProducts(true, itemType, rarity);
                        if (disenchantProducts != null)
                        {
                            foreach (var itemAmountConfig in disenchantProducts)
                            {
                                GameObject materialPrefab = null;

                                if (itemAmountConfig != null && !itemAmountConfig.Item.IsNullOrWhiteSpace())
                                {
                                    materialPrefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                                }

                                if (materialPrefab == null)
                                {
                                    continue;
                                }

                                var materialItem = SpawnLootForDrop(materialPrefab, dropPoint, true);
                                var materialItemDrop = materialItem.GetComponent<ItemDrop>();
                                materialItemDrop.m_itemData.m_stack = itemAmountConfig.Amount;

                                if (materialItemDrop.m_itemData.IsMagicCraftingMaterial())
                                {
                                    materialItemDrop.m_itemData.m_variant = EpicLoot.GetRarityIconIndex(rarity);
                                }

                                results.Add(materialItem);
                            }
                        }

                        continue;
                    }
                }

                var gatedItemName = (CheatDisableGating) ?
                    GatedItemTypeHelper.GetGatedItemNameFromItemOrType(lootDrop.Item, GatedItemTypeMode.Unlimited) :
                    GatedItemTypeHelper.GetGatedItemNameFromItemOrType(lootDrop.Item, EpicLoot.GetGatedItemTypeMode());

                GameObject itemPrefab = null;

                if (!gatedItemName.IsNullOrWhiteSpace())
                {
                    itemPrefab = ObjectDB.instance.GetItemPrefab(gatedItemName);
                }

                if (itemPrefab == null)
                {
                    EpicLoot.LogError($"Tried to spawn loot ({gatedItemName}) for ({objectName}), " +
                        $"but the item prefab was not found!");
                    continue;
                }

                var item = SpawnLootForDrop(itemPrefab, dropPoint, initializeObject);
                var itemDrop = item.GetComponent<ItemDrop>();

                if (itemDrop != null && EpicLoot.CanBeMagicItem(itemDrop.m_itemData) && !ArrayUtils.IsNullOrEmpty(lootDrop.Rarity))
                {
                    var itemData = itemDrop.m_itemData;
                    var magicItemComponent = itemData.Data().GetOrCreate<MagicItemComponent>();
                    var magicItem = RollMagicItem(lootDrop, itemData, luckFactor);

                    if (CheatForceMagicEffect)
                    {
                        AddDebugMagicEffects(magicItem);
                    }

                    magicItemComponent.SetMagicItem(magicItem);
                    itemDrop.m_itemData = itemData;
                    itemDrop.Save();
                    InitializeMagicItem(itemData);
                }

                results.Add(item);
            }

            return results;
        }

        public static GameObject SpawnLootForDrop(GameObject itemPrefab, Vector3 dropPoint, bool initializeObject)
        {
            Quaternion randomRotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
            ZNetView.m_forceDisableInit = !initializeObject;
            GameObject item = Object.Instantiate(itemPrefab, dropPoint, randomRotation);
            ZNetView.m_forceDisableInit = false;
            return item;
        }

        private static LootDrop ResolveLootDrop(LootDrop lootDrop)
        {
            var result = new LootDrop { Item = lootDrop.Item, Rarity = ArrayUtils.Copy(lootDrop.Rarity), Weight = lootDrop.Weight };
            var needsResolve = true;
            while (needsResolve)
            {
                if (ItemSets.TryGetValue(result.Item, out var itemSet))
                {
                    if (itemSet.Loot.Length == 0)
                    {
                        EpicLoot.LogError($"Tried to roll using ItemSet ({itemSet.Name}) but its loot list was empty!");
                    }
                    _weightedLootTable.Setup(itemSet.Loot, x => x.Weight);
                    var itemSetResult = _weightedLootTable.Roll();
                    result.Item = itemSetResult.Item;
                    result.Weight = itemSetResult.Weight;
                    if (ArrayUtils.IsNullOrEmpty(result.Rarity))
                    {
                        result.Rarity = ArrayUtils.Copy(itemSetResult.Rarity);
                    }
                }
                else if (IsLootTableRefence(result.Item, out var lootList))
                {
                    if (lootList.Length == 0)
                    {
                        EpicLoot.LogError($"Tried to roll using loot table reference ({result.Item}) but its loot list was empty!");
                    }
                    _weightedLootTable.Setup(lootList, x => x.Weight);
                    var referenceResult = _weightedLootTable.Roll();
                    result.Item = referenceResult.Item;
                    result.Weight = referenceResult.Weight;
                    if (ArrayUtils.IsNullOrEmpty(result.Rarity))
                    {
                        result.Rarity = ArrayUtils.Copy(referenceResult.Rarity);
                    }
                }
                else
                {
                    needsResolve = false;
                }
            }

            return result;
        }

        private static bool IsLootTableRefence(string lootDropItem, out LootDrop[] lootList)
        {
            lootList = null;
            var parts = lootDropItem.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var objectName = parts[0];
            var levelText = parts[1];
            if (!int.TryParse(levelText, out var level))
            {
                EpicLoot.LogError($"Tried to get a loot table reference from '{lootDropItem}' but could not parse the level value ({levelText})!");
                return false;
            }

            if (LootTables.ContainsKey(objectName))
            {
                var lootTable = LootTables[objectName].FirstOrDefault();
                if (lootTable != null)
                {
                    lootList = GetLootForLevel(lootTable, level);
                    return true;
                }

                EpicLoot.LogError($"UNLIKELY: LootTables contains entry for {objectName} but no valid loot tables! Weird!");
            }

            return false;
        }

        public static MagicItem RollMagicItem(LootDrop lootDrop, ItemDrop.ItemData baseItem, float luckFactor, float powerlevelMod = 1f)
        {
            var rarity = RollItemRarity(lootDrop, luckFactor);
            return RollMagicItem(rarity, baseItem, luckFactor, powerlevelMod);
        }

        public static MagicItem RollMagicItem(ItemRarity rarity, ItemDrop.ItemData baseItem, float luckFactor, float powerlevelMod = 1f)
        {
            var cheatLegendary = !string.IsNullOrEmpty(CheatForceLegendary);
            var cheatMythic = !string.IsNullOrEmpty(CheatForceMythic);
            
            if (cheatMythic)
            {
                rarity = ItemRarity.Mythic;
            }
            else if (cheatLegendary)
            {
                rarity = ItemRarity.Legendary;
            }

            var magicItem = new MagicItem { Rarity = rarity };

            var effectCount = CheatEffectCount >= 1 ? CheatEffectCount : RollEffectCountPerRarity(magicItem.Rarity);

            if (rarity == ItemRarity.Legendary || rarity == ItemRarity.Mythic)
            {
                LegendaryInfo itemInfo = null;
                if (cheatMythic)
                {
                    UniqueLegendaryHelper.TryGetLegendaryInfo(CheatForceMythic, out itemInfo);
                }
                else if (cheatLegendary)
                {
                    UniqueLegendaryHelper.TryGetLegendaryInfo(CheatForceLegendary, out itemInfo);
                }

                if (itemInfo == null)
                {
                    var roll = Random.Range(0.0f, 1.0f);
                    var rollSetItem = roll < ELConfig.SetItemDropChance.Value;
                    EpicLoot.Log($"Rolling Legendary/Mythic: set={rollSetItem} ({roll:#.##}/{ELConfig.SetItemDropChance.Value})");
                    if (rarity == ItemRarity.Legendary)
                    {
                        var availableLegendaries = UniqueLegendaryHelper.GetAvailableLegendaries(baseItem, magicItem, rollSetItem);
                        EpicLoot.Log($"Available Legendaries: {string.Join(", ", availableLegendaries.Select(x => x.ID))}");
                        _weightedLegendaryTable.Setup(availableLegendaries, x => x.SelectionWeight);
                        itemInfo = _weightedLegendaryTable.Roll();
                    }
                    else
                    {
                        var availableMythics = UniqueLegendaryHelper.GetAvailableMythics(baseItem, magicItem, rollSetItem);
                        EpicLoot.Log($"Available Mythics: {string.Join(", ", availableMythics.Select(x => x.ID))}");
                        _weightedMythicTable.Setup(availableMythics, x => x.SelectionWeight);
                        itemInfo = _weightedMythicTable.Roll();
                    }
                }

                if (itemInfo.IsSetItem)
                {
                    var setID = UniqueLegendaryHelper.GetSetForLegendaryItem(itemInfo);
                    magicItem.SetID = setID;
                }

                if (!UniqueLegendaryHelper.IsGenericLegendary(itemInfo))
                {
                    magicItem.LegendaryID = itemInfo.ID;
                    magicItem.DisplayName = itemInfo.Name;

                    if (itemInfo.GuaranteedEffectCount > 0)
                    {
                        effectCount = itemInfo.GuaranteedEffectCount;
                    }

                    foreach (var guaranteedMagicEffect in itemInfo.GuaranteedMagicEffects)
                    {
                        var effectDef = MagicItemEffectDefinitions.Get(guaranteedMagicEffect.Type);
                        if (effectDef == null)
                        {
                            EpicLoot.LogError($"Could not find magic effect (Type={guaranteedMagicEffect.Type}) " +
                                $"while creating legendary/mythic item (ID={itemInfo.ID})");
                            continue;
                        }

                        var effect = RollEffect(effectDef, rarity, guaranteedMagicEffect.Values, powerlevelMod);
                        magicItem.Effects.Add(effect);
                        effectCount--;
                    }
                }
            }

            for (var i = 0; i < effectCount; i++)
            {
                var availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(baseItem, magicItem);
                if (availableEffects.Count == 0)
                {
                    EpicLoot.LogWarning($"Tried to add more effects to magic item ({baseItem.m_shared.m_name}) " +
                        $"but there were no more available effects. " +
                        $"Current Effects: {(string.Join(", ", magicItem.Effects.Select(x => x.EffectType.ToString())))}");
                    break;
                }

                _weightedEffectTable.Setup(availableEffects, x => x.SelectionWeight);
                var effectDef = _weightedEffectTable.Roll();

                var effect = RollEffect(effectDef, magicItem.Rarity);
                magicItem.Effects.Add(effect);
            }

            if (string.IsNullOrEmpty(magicItem.DisplayName))
            {
                magicItem.DisplayName = MagicItemNames.GetNameForItem(baseItem, magicItem);
            }

            return magicItem;
        }

        private static void InitializeMagicItem(ItemDrop.ItemData baseItem)
        {
            Indestructible.MakeItemIndestructible(baseItem);
            if (baseItem.m_shared.m_useDurability)
            {
                baseItem.m_durability = Random.Range(0.2f, 1.0f) * baseItem.GetMaxDurability();
            }
        }

        public static int RollEffectCountPerRarity(ItemRarity rarity)
        {
            var countPercents = GetEffectCountsPerRarity(rarity, true);
            _weightedEffectCountTable.Setup(countPercents, x => x.Value);
            return _weightedEffectCountTable.Roll().Key;
        }

        public static List<KeyValuePair<int, float>> GetEffectCountsPerRarity(ItemRarity rarity, bool useEnchantingUpgrades)
        {
            List<KeyValuePair<int, float>> result;
            switch (rarity)
            {
                case ItemRarity.Magic:
                    result = Config.MagicEffectsCount.Magic.Select(x => 
                        new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                    break;
                case ItemRarity.Rare:
                    result = Config.MagicEffectsCount.Rare.Select(x => 
                        new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                    break;
                case ItemRarity.Epic:
                    result = Config.MagicEffectsCount.Epic.Select(x => 
                        new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                    break;
                case ItemRarity.Legendary:
                    result = Config.MagicEffectsCount.Legendary.Select(x => 
                        new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                    break;
                case ItemRarity.Mythic:
                    result = Config.MagicEffectsCount.Mythic.Select(x => 
                        new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }

            var featureValues = useEnchantingUpgrades && EnchantingTableUI.instance && EnchantingTableUI.instance.SourceTable
                ? EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Enchant)
                : new Tuple<float, float>(float.NaN, float.NaN);
            var highValueBonus = float.IsNaN(featureValues.Item1) ? 0 : featureValues.Item1;
            var midValueBonus = float.IsNaN(featureValues.Item2) ? 0 : featureValues.Item2;
            if (result.Count > 0)
            {
                var entry = result[result.Count - 1];
                result[result.Count - 1] = new KeyValuePair<int, float>(entry.Key, entry.Value + highValueBonus);
            }

            if (result.Count > 1)
            {
                var entry = result[result.Count - 2];
                result[result.Count - 2] = new KeyValuePair<int, float>(entry.Key, entry.Value + midValueBonus);
            }

            if (result.Count > 2)
            {
                var entry = result[0];
                result[0] = new KeyValuePair<int, float>(entry.Key, entry.Value - highValueBonus - midValueBonus);
            }

            return result;
        }

        public static MagicItemEffect RollEffect(MagicItemEffectDefinition effectDef, ItemRarity itemRarity,
            MagicItemEffectDefinition.ValueDef valuesOverride = null, float powerlevelMod = 1f)
        {
            float value = MagicItemEffect.DefaultValue;
            var valuesDef = valuesOverride ?? effectDef.GetValuesForRarity(itemRarity);
            if (valuesDef != null)
            {
                value = valuesDef.MinValue;
                if (valuesDef.Increment != 0)
                {
                    EpicLoot.Log($"RollEffect: {effectDef.Type} {itemRarity} value={value} " +
                        $"(min={valuesDef.MinValue} max={valuesDef.MaxValue})");
                    var incrementCount = (int)((valuesDef.MaxValue - valuesDef.MinValue) / valuesDef.Increment);
                    value = valuesDef.MinValue + (Random.Range(0, incrementCount + 1) * valuesDef.Increment);
                    value *= powerlevelMod;
                }
            }

            return new MagicItemEffect(effectDef.Type, value);
        }

        public static List<MagicItemEffect> RollEffects(List<MagicItemEffectDefinition> availableEffects,
            ItemRarity itemRarity, int count, bool removeOnSelect = true)
        {
            var results = new List<MagicItemEffect>();

            _weightedEffectTable.Setup(availableEffects, x => x.SelectionWeight, removeOnSelect);
            var effectDefs = _weightedEffectTable.Roll(count);

            foreach (var effectDef in effectDefs)
            {
                if (effectDef == null)
                {
                    EpicLoot.LogError($"EffectDef was null! RollEffects({itemRarity}, {count})");
                    continue;
                }
                results.Add(RollEffect(effectDef, itemRarity));
            }

            return results;
        }

        public static ItemRarity RollItemRarity(LootDrop lootDrop, float luckFactor)
        {
            if (lootDrop.Rarity == null || lootDrop.Rarity.Length == 0)
            {
                return ItemRarity.Magic;
            }

            var rarityWeights = GetRarityWeights(lootDrop.Rarity, luckFactor);

            _weightedRarityTable.Setup(rarityWeights, x => x.Value);
            return _weightedRarityTable.Roll().Key;
        }

        public static Dictionary<ItemRarity, float> GetRarityWeights(float[] rarity, float luckFactor)
        {
            var rarityWeights = new Dictionary<ItemRarity, float>()
            {
                { ItemRarity.Magic, rarity.Length >= 1 ? rarity[0] : 0 },
                { ItemRarity.Rare, rarity.Length >= 2 ? rarity[1] : 0 },
                { ItemRarity.Epic, rarity.Length >= 3 ? rarity[2] : 0 },
                { ItemRarity.Legendary, rarity.Length >= 4 ? rarity[3] : 0 },
                { ItemRarity.Mythic, rarity.Length >= 5 ? rarity[4] : 0 }
            };

            return ModifyRarityByLuck(rarityWeights, luckFactor);
        }

        public static List<LootTable> GetLootTable(string objectName)
        {
            var results = new List<LootTable>();
            if (LootTables.TryGetValue(objectName, out var lootTables))
            {
                foreach (var lootTable in lootTables)
                {
                    results.Add(lootTable);
                }
            }
            return results;
        }

        public static List<LootTable> GetFullyResolvedLootTable(string name)
        {
            List<LootTable> results = new List<LootTable>();
            if (LootTables.TryGetValue(name, out var lootTables))
            {
                foreach (var lootTable in lootTables)
                {
                    results.Add(lootTable);
                }
            }

            List<LootDrop> setDrops = new List<LootDrop>();
            CheckForSet(name, setDrops, out List<LootDrop> setResults);
            LootTable lootTableFromRefs = new LootTable()
            {
                Object = name,
                Drops = [[1, 1]],
                Loot = setResults.ToArray(),
            };

            results.Add(lootTableFromRefs);

            EpicLoot.Log($"GetFullyResolvedLootTable({name}) found {results.Count} loot tables");
            return results;
        }

        private static bool CheckForSet(string lootdrop, List<LootDrop> current_results, out List<LootDrop> results)
        {
            results = current_results;
            if (ItemSets.TryGetValue(lootdrop, out LootItemSet lootset))
            {
                foreach (LootDrop ld in lootset.Loot)
                {
                    if (!CheckForSet(ld.Item, current_results, out results))
                    {
                        results.Add(ld);
                    }
                }
            }

            return false;
        }

        public static bool LootSetContainsEntry(string lootdrop)
        {
            return ItemSets.ContainsKey(lootdrop);
        }

        public static KeyValuePair<string, List<LootTable>> GetLootTableOrDefault(string objectName)
        {
            KeyValuePair<string, List<LootTable>> results = LootTables.FirstOrDefault(x => x.Key == objectName);
            if (results.Key != objectName)
            {
                if (results.Key == null)
                {
                    results = LootTables.First();
                }
                EpicLoot.LogWarning($"Requested Loot table ({objectName}) does not exist, defaulting to ({results.Key})");
            }

            return results;
        }

        public static List<KeyValuePair<int, float>> GetDropsForLevel([NotNull] LootTable lootTable,
            int level, bool useNextHighestIfNotPresent = true)
        {
            if (level <= 3 && !ArrayUtils.IsNullOrEmpty(lootTable.Drops))
            {
                if (lootTable.LeveledLoot.Any(x => x.Level == level))
                {
                    EpicLoot.LogWarning($"Duplicated leveled drops for ({lootTable.Object} lvl {level}), using 'Drops'");
                }

                return ToDropList(lootTable.Drops);
            }

            for (var lvl = level; lvl >= 1; --lvl)
            {
                var found = lootTable.LeveledLoot.Find(x => x.Level == lvl);
                if (found != null && !ArrayUtils.IsNullOrEmpty(found.Drops))
                {
                    return ToDropList(found.Drops);
                }

                if (!useNextHighestIfNotPresent)
                {
                    return null;
                }
            }

            EpicLoot.LogError($"Could not find any leveled drops for ({lootTable.Object} lvl {level}), " +
                $"but a loot table exists for this object!");
            return null;
        }

        private static List<KeyValuePair<int, float>> ToDropList(float[][] drops)
        {
            return drops.Select(x => new KeyValuePair<int, float>((int) x[0], x[1])).ToList();
        }

        public static LootDrop[] GetLootForLevel([NotNull] LootTable lootTable, int level,
            bool useNextHighestIfNotPresent = true)
        {
            
            if (level <= 3 && !ArrayUtils.IsNullOrEmpty(lootTable.Loot))
            {
                if (lootTable.LeveledLoot.Any(x => x.Level == level))
                {
                    EpicLoot.LogWarning($"Duplicated leveled loot for ({lootTable.Object} lvl {level}), using 'Loot'");
                }
                return lootTable.Loot.ToArray();
            }

            for (var lvl = level; lvl >= 1; --lvl)
            {
                var found = lootTable.LeveledLoot.Find(x => x.Level == lvl);
                if (found != null && !ArrayUtils.IsNullOrEmpty(found.Loot))
                {
                    return found.Loot.ToArray();
                }

                if (!useNextHighestIfNotPresent)
                {
                    return null;
                }
            }

            EpicLoot.LogError($"Could not find any leveled loot for ({lootTable.Object} lvl {level}), " +
                $"but a loot table exists for this object!");
            return null;
        }

        public static List<MagicItemEffect> RollAugmentEffects(ItemDrop.ItemData item, MagicItem magicItem, int effectIndex)
        {
            var results = new List<MagicItemEffect>();

            if (item == null || magicItem == null)
            {
                EpicLoot.LogError($"[RollAugmentEffects] Null inputs: item={item}, magicItem={magicItem}");
                return results;
            }

            if (effectIndex < 0 || effectIndex >= magicItem.Effects.Count)
            {
                EpicLoot.LogError($"[RollAugmentEffects] Bad effect index ({effectIndex}), " +
                    $"effects count: {magicItem.Effects.Count}");
                return results;
            }

            var rarity = magicItem.Rarity;
            var currentEffect = magicItem.Effects[effectIndex];
            

            var valuelessEffect = MagicItemEffectDefinitions.IsValuelessEffect(currentEffect.EffectType, rarity);
            var availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(item, magicItem, valuelessEffect ? 
                -1 : effectIndex);

            var augmentChoices = 2;
            var featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
            if (!float.IsNaN(featureValues.Item1))
                augmentChoices = (int)featureValues.Item1;

            List<string> currentEffectTypes = new List<string>();

            for (var i = 0; i < augmentChoices && i < availableEffects.Count; i++)
            {
                int fallbackAttempts = 0;
                var newEffect = RollEffects(availableEffects, rarity, 1, true).FirstOrDefault();
                while (newEffect != null && currentEffectTypes.Contains(newEffect.EffectType) && fallbackAttempts < 5)
                {
                    // If we rolled the same effect as the current one, try again a few times
                    EpicLoot.LogWarning($"Rolled a duplicate effect: {newEffect.EffectType} for item: {item.m_shared.m_name}, retrying...");
                    MagicItemEffect nmieffect = RollEffects(availableEffects, rarity, 1, true).FirstOrDefault();
                    if (nmieffect == null)
                    {
                        continue;
                    }

                    newEffect = nmieffect;
                    fallbackAttempts++;
                }

                results.Add(newEffect);
                currentEffectTypes.Add(newEffect.EffectType);
                var newEffectIsValueless = MagicItemEffectDefinitions.IsValuelessEffect(newEffect.EffectType, rarity);
                if (newEffectIsValueless)
                {
                    availableEffects.RemoveAll(x => x.Type == newEffect.EffectType);
                }
            }

            results.Add(currentEffect);
            results.Reverse();
            return results;
        }

        public static void AddDebugMagicEffects(MagicItem item)
        {
            if (!string.IsNullOrEmpty(ForcedMagicEffect) && !item.HasEffect(ForcedMagicEffect))
            {
                EpicLoot.Log($"AddDebugMagicEffect {ForcedMagicEffect}");
                item.Effects.Add(RollEffect(MagicItemEffectDefinitions.Get(ForcedMagicEffect), item.Rarity));
            }
        }

        public static float GetLuckFactor(Vector3 fromPoint)
        {
            var luckFactor = EpicLoot.GetWorldLuckFactor();
            var players = new List<Player>();
            Player.GetPlayersInRange(fromPoint, 100f, players);

            if (players.Count > 0)
            {
                var totalLuckFactor = players
                    .Select(x => x.m_nview.GetZDO().GetInt("el-luk") * 0.01f)
                    .DefaultIfEmpty(0)
                    .Sum();
                luckFactor += totalLuckFactor;
            }

            return luckFactor;
        }

        public static void DebugLuckFactor()
        {
            var players = Player.s_players;
            if (players != null)
            {
                Debug.LogWarning($"DebugLuckFactor ({players.Count} players)");
                var index = 0;
                foreach (var player in players)
                {
                    Debug.LogWarning($"{index++}: {player?.m_name}: {player?.m_nview?.GetZDO()?.GetInt("el-luk")}");
                }
            }
        }

        public static Dictionary<ItemRarity, float> ModifyRarityByLuck(
            IReadOnlyDictionary<ItemRarity, float> rarityWeights, float luckFactor = 0)
        {
            var results = new Dictionary<ItemRarity, float>();
            for (var rarity = ItemRarity.Magic; rarity <= ItemRarity.Mythic; rarity++)
            {
                var skewFactor = GetSkewFactor(rarity);
                results.Add(rarity, rarityWeights[rarity] * GetSkewedLuckFactor(luckFactor, skewFactor));
            }

            return results;
        }

        public static float GetSkewFactor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Magic: return -0.2f;
                case ItemRarity.Rare: return 0.0f;
                case ItemRarity.Epic: return 0.2f;
                case ItemRarity.Legendary: return 1;
                case ItemRarity.Mythic: return 1.1f;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
        }

        public static float GetSkewedLuckFactor(float luckFactor, float skewFactor)
        {
            return Mathf.Max(0, 1 + luckFactor * skewFactor);
        }

        public static void PrintLuckTest(string lootTableName, float luckFactor)
        {
            KeyValuePair<string, List<LootTable>> loot_info =  GetLootTableOrDefault(lootTableName);
            LootDrop lootDrop = GetLootForLevel(loot_info.Value[0], 1)[0];
            lootDrop = ResolveLootDrop(lootDrop);
            if (lootDrop.Rarity == null)
            {
                lootDrop.Rarity = [100, 0, 0, 0, 0];
                EpicLoot.LogWarning($"No rarity table was found for {loot_info.Value[0]} using default: [100, 0, 0, 0, 0]");
            }

            var rarityBase = GetRarityWeights(lootDrop.Rarity, 0);
            var rarityLuck = GetRarityWeights(lootDrop.Rarity, luckFactor);

            var sb = new StringBuilder();
            sb.AppendLine($"Luck Test: {loot_info.Key}, {luckFactor}");
            sb.AppendLine("Rarity     Base    %       Luck    %       Diff    Factor");
            sb.AppendLine("=====================================================");

            var rarityBaseTotal = rarityBase.Sum(x => x.Value);
            var rarityLuckTotal = rarityLuck.Sum(x => x.Value);
            for (var index = 0; index < 4; index++)
            {
                var rarity = (ItemRarity)index;
                var baseWeight = rarityBase[rarity];
                var luckWeight = rarityLuck[rarity];

                var basePercent = baseWeight / rarityBaseTotal;
                var luckPercent = luckWeight / rarityLuckTotal;
                sb.AppendFormat("{0}{1}{2}{3}{4}{5}{6}\n",
                    rarity.ToString().PadRight(11),
                    baseWeight.ToString("0.##").PadRight(8),
                    basePercent.ToString("0.##%").PadRight(8),
                    luckWeight.ToString("0.##").PadRight(8),
                    luckPercent.ToString("0.##%").PadRight(8),
                    (luckPercent - basePercent).ToString("+0.##%;-0.##%").PadRight(8),
                    (luckPercent / basePercent).ToString("0.##").PadRight(8));
            }

            Console.instance.Print(sb.ToString());
        }

        public static void PrintLootResolutionTest(string lootTableName, int level, int itemIndex)
        {
            Debug.LogWarning($"{lootTableName}:{level}:{itemIndex}");

            var lootTable = GetLootTable(lootTableName)[0];
            var lootDrop = GetLootForLevel(lootTable, level)[itemIndex];
            lootDrop = ResolveLootDrop(lootDrop);
            var rarity = lootDrop.Rarity;

            if (rarity.Length < 1)
            {
                return;
            }

            string rarityStr = "> rarity=[ ";
            for (int i = 0; i < rarity.Length - 1; i++)
            {
                rarityStr += $"{rarity[i]},";
            }

            rarityStr += $"{rarity[rarity.Length - 1]} ]";

            Debug.LogWarning(rarityStr);
        }
    }
}
