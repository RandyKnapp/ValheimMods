using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using ExtendedItemDataFramework;
using JetBrains.Annotations;
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

        public static event Action<ExtendedItemData, MagicItem> MagicItemGenerated;

        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedDropCountTable;
        private static WeightedRandomCollection<LootDrop> _weightedLootTable;
        private static WeightedRandomCollection<MagicItemEffectDefinition> _weightedEffectTable;
        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedEffectCountTable;
        private static WeightedRandomCollection<KeyValuePair<ItemRarity, float>> _weightedRarityTable;
        private static WeightedRandomCollection<LegendaryInfo> _weightedLegendaryTable;
        public static bool CheatRollingItem = false;
        public static int CheatEffectCount;
        public static bool CheatDisableGating;
        public static bool CheatForceMagicEffect;
        public static string ForcedMagicEffect = "";
        public static string CheatForceLegendary;

        public static void Initialize(LootConfig lootConfig)
        {
            Config = lootConfig;
            
            var random = new System.Random();
            _weightedDropCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>(random);
            _weightedLootTable = new WeightedRandomCollection<LootDrop>(random);
            _weightedEffectTable = new WeightedRandomCollection<MagicItemEffectDefinition>(random);
            _weightedEffectCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>(random);
            _weightedRarityTable = new WeightedRandomCollection<KeyValuePair<ItemRarity, float>>(random);
            _weightedLegendaryTable = new WeightedRandomCollection<LegendaryInfo>(random);

            ItemSets.Clear();
            LootTables.Clear();
            if (Config == null)
            {
                EpicLoot.LogWarning("Initialized LootRoller with null");
                return;
            }
          
            AddItemSets(lootConfig.ItemSets);
            AddLootTables(lootConfig.LootTables);
        }

        private static void AddItemSets([NotNull] IEnumerable<LootItemSet> itemSets)
        {
            foreach (var itemSet in itemSets)
            {
                if (string.IsNullOrEmpty(itemSet.Name))
                {
                    EpicLoot.LogError($"Tried to add ItemSet with no name!");
                    continue;
                }

                if (!ItemSets.ContainsKey(itemSet.Name))
                {
                    EpicLoot.Log($"Added ItemSet: {itemSet.Name}");
                    ItemSets.Add(itemSet.Name, itemSet);
                }
                else
                {
                    EpicLoot.LogError($"Tried to add ItemSet {itemSet.Name}, but it already exists!");
                }
            }
        }

        public static void AddLootTables([NotNull] IEnumerable<LootTable> lootTables)
        {

            // Add loottables for mobs or objects that do not reference another template
            foreach (var lootTable in lootTables.Where(x => x.RefObject == null || x.RefObject == ""))
            {
                AddLootTable(lootTable);
                EpicLoot.Log($"Added loottable for {lootTable.Object}");
            }

            // Add loottables that are based off mob or object templates
            foreach (var lootTable in lootTables.Where(x => x.RefObject != null && x.RefObject != ""))
            {
                AddLootTable(lootTable);
                EpicLoot.Log($"Added loottable for {lootTable.Object} using {lootTable.RefObject} as reference");
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

            EpicLoot.Log($"Added LootTable: {key}");
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

        public static List<GameObject> RollLootTableAndSpawnObjects(List<LootTable> lootTables, int level, string objectName, Vector3 dropPoint)
        {
            return RollLootTableInternal(lootTables, level, objectName, dropPoint, true);
        }

        public static List<GameObject> RollLootTableAndSpawnObjects(LootTable lootTable, int level, string objectName, Vector3 dropPoint)
        {
            return RollLootTableInternal(lootTable, level, objectName, dropPoint, true);
        }

        public static List<ItemDrop.ItemData> RollLootTable(List<LootTable> lootTables, int level, string objectName, Vector3 dropPoint)
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

        public static List<ItemDrop.ItemData> RollLootTable(LootTable lootTable, int level, string objectName, Vector3 dropPoint)
        {
            return RollLootTable(new List<LootTable> {lootTable}, level, objectName, dropPoint);
        }

        public static List<ItemDrop.ItemData> RollLootTable(string lootTableName, int level, string objectName, Vector3 dropPoint)
        {
            var lootTable = GetLootTable(lootTableName);
            if (lootTable == null)
            {
                return new List<ItemDrop.ItemData>();
            }

            return RollLootTable(lootTable, level, objectName, dropPoint);
        }

        private static List<GameObject> RollLootTableInternal(IEnumerable<LootTable> lootTables, int level, string objectName, Vector3 dropPoint, bool initializeObject)
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
            return CheatRollingItem || CheatDisableGating || CheatForceMagicEffect || !string.IsNullOrEmpty(CheatForceLegendary) || CheatEffectCount > 0;
        }

        private static List<GameObject> RollLootTableInternal(LootTable lootTable, int level, string objectName, Vector3 dropPoint, bool initializeObject)
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
            else if (Mathf.Abs(EpicLoot.GlobalDropRateModifier.Value - 1) > float.Epsilon)
            {
                var clampedDropRate = Mathf.Clamp(EpicLoot.GlobalDropRateModifier.Value, 0, 4);
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
            _weightedLootTable.Setup(loot, x => x.Weight);
            var selectedDrops = _weightedLootTable.Roll(dropCount);

            var cheatsActive = AnyItemSpawnCheatsActive();
            foreach (var ld in selectedDrops)
            {
                if (ld == null)
                {
                    EpicLoot.LogError($"Loot drop was null! RollLootTableInternal({lootTable.Object}, {level}, {objectName})");
                    continue;
                }
                var lootDrop = ResolveLootDrop(ld);
                
                if (!cheatsActive && EpicLoot.ItemsToMaterialsDropRatio.Value > 0)
                {
                    var clampedConvertRate = Mathf.Clamp(EpicLoot.ItemsToMaterialsDropRatio.Value, 0.0f, 1.0f);
                    var replaceWithMats = Random.Range(0.0f, 1.0f) < clampedConvertRate;
                    if (replaceWithMats)
                    {
                        var prefab = ObjectDB.instance.GetItemPrefab(lootDrop.Item);
                        if (prefab != null)
                        {
                            var rarity = RollItemRarity(lootDrop, luckFactor);
                            var itemType = prefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_itemType;
                            var disenchantProducts = EnchantCostsHelper.GetDisenchantProducts(true, itemType, rarity);
                            if (disenchantProducts != null)
                            {
                                foreach (var itemAmountConfig in disenchantProducts)
                                {
                                    var materialPrefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                                    var materialItem = SpawnLootForDrop(materialPrefab, dropPoint, true);
                                    var materialItemDrop = materialItem.GetComponent<ItemDrop>();
                                    materialItemDrop.m_itemData.m_stack = itemAmountConfig.Amount;
                                    if (materialItemDrop.m_itemData.IsMagicCraftingMaterial())
                                        materialItemDrop.m_itemData.m_variant = EpicLoot.GetRarityIconIndex(rarity);
                                    results.Add(materialItem);
                                }
                            }
                        }

                        continue;
                    }
                }

                var itemID = (CheatDisableGating) ? lootDrop.Item : GatedItemTypeHelper.GetGatedItemID(lootDrop.Item);

                var itemPrefab = ObjectDB.instance.GetItemPrefab(itemID);
                if (itemPrefab == null)
                {
                    EpicLoot.LogError($"Tried to spawn loot ({itemID}) for ({objectName}), but the item prefab was not found!");
                    continue;
                }

                var item = SpawnLootForDrop(itemPrefab, dropPoint, initializeObject);
                var itemDrop = item.GetComponent<ItemDrop>();
                if (EpicLoot.CanBeMagicItem(itemDrop.m_itemData) && !ArrayUtils.IsNullOrEmpty(lootDrop.Rarity))
                {
                    var itemData = new ExtendedItemData(itemDrop.m_itemData);
                    var magicItemComponent = itemData.AddComponent<MagicItemComponent>();
                    var magicItem = RollMagicItem(lootDrop, itemData, luckFactor);
                    if (CheatForceMagicEffect)
                    {
                        AddDebugMagicEffects(magicItem);
                    }

                    magicItemComponent.SetMagicItem(magicItem);

                    itemDrop.m_itemData = itemData;
                    itemDrop.Save();
                    InitializeMagicItem(itemData);

                    MagicItemGenerated?.Invoke(itemData, magicItem);
                }

                results.Add(item);
            }

            return results;
        }

        public static GameObject SpawnLootForDrop(GameObject itemPrefab, Vector3 dropPoint, bool initializeObject)
        {
            var randomRotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
            ZNetView.m_forceDisableInit = !initializeObject;
            var item = Object.Instantiate(itemPrefab, dropPoint, randomRotation);
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

        public static MagicItem RollMagicItem(LootDrop lootDrop, ExtendedItemData baseItem, float luckFactor)
        {
            var rarity = RollItemRarity(lootDrop, luckFactor);
            return RollMagicItem(rarity, baseItem, luckFactor);
        }

        public static MagicItem RollMagicItem(ItemRarity rarity, ExtendedItemData baseItem, float luckFactor)
        {
            var cheatLegendary = !string.IsNullOrEmpty(CheatForceLegendary);
            if (cheatLegendary)
            {
                rarity = ItemRarity.Legendary;
            }

            var magicItem = new MagicItem { Rarity = rarity };

            var effectCount = CheatEffectCount >= 1 ? CheatEffectCount : RollEffectCountPerRarity(magicItem.Rarity);

            if (rarity == ItemRarity.Legendary)
            {
                LegendaryInfo legendary = null;
                if (cheatLegendary)
                {
                    UniqueLegendaryHelper.TryGetLegendaryInfo(CheatForceLegendary, out legendary);
                }
                
                if (legendary == null)
                {
                    var roll = Random.Range(0.0f, 1.0f);
                    var rollSetItem = roll < EpicLoot.SetItemDropChance.Value;
                    Debug.LogWarning($"Rolling Legendary: set={rollSetItem} ({roll}/{EpicLoot.SetItemDropChance.Value})");
                    var availableLegendaries = UniqueLegendaryHelper.GetAvailableLegendaries(baseItem, magicItem, rollSetItem);
                    Debug.LogWarning($"Available Legendaries: {string.Join(", ", availableLegendaries.Select(x => x.ID))}");
                    _weightedLegendaryTable.Setup(availableLegendaries, x => x.SelectionWeight);
                    legendary = _weightedLegendaryTable.Roll();
                }

                if (legendary.IsSetItem)
                {
                    var setID = UniqueLegendaryHelper.GetSetForLegendaryItem(legendary);
                    magicItem.SetID = setID;
                }

                if (!UniqueLegendaryHelper.IsGenericLegendary(legendary))
                {
                    magicItem.LegendaryID = legendary.ID;
                    magicItem.DisplayName = legendary.Name;

                    if (legendary.GuaranteedEffectCount > 0)
                    {
                        effectCount = legendary.GuaranteedEffectCount;
                    }

                    foreach (var guaranteedMagicEffect in legendary.GuaranteedMagicEffects)
                    {
                        var effectDef = MagicItemEffectDefinitions.Get(guaranteedMagicEffect.Type);
                        if (effectDef == null)
                        {
                            EpicLoot.LogError($"Could not find magic effect (Type={guaranteedMagicEffect.Type}) while creating legendary item (ID={legendary.ID})");
                            continue;
                        }

                        var effect = RollEffect(effectDef, ItemRarity.Legendary, guaranteedMagicEffect.Values);
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
                    EpicLoot.LogWarning($"Tried to add more effects to magic item ({baseItem.m_shared.m_name}) but there were no more available effects. " +
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

        private static void InitializeMagicItem(ExtendedItemData baseItem)
        {
            Indestructible.MakeItemIndestructible(baseItem);
            if (baseItem.m_shared.m_useDurability)
            {
                baseItem.m_durability = Random.Range(0.2f, 1.0f) * baseItem.GetMaxDurability();
            }
        }

        public static int RollEffectCountPerRarity(ItemRarity rarity)
        {
            var countPercents = GetEffectCountsPerRarity(rarity);
            _weightedEffectCountTable.Setup(countPercents, x => x.Value);
            return _weightedEffectCountTable.Roll().Key;
        }

        public static List<KeyValuePair<int, float>> GetEffectCountsPerRarity(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Magic:
                    return Config.MagicEffectsCount.Magic.Select(x => new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                case ItemRarity.Rare:
                    return Config.MagicEffectsCount.Rare.Select(x => new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                case ItemRarity.Epic:
                    return Config.MagicEffectsCount.Epic.Select(x => new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                case ItemRarity.Legendary:
                    return Config.MagicEffectsCount.Legendary.Select(x => new KeyValuePair<int, float>((int)x[0], x[1])).ToList();
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
        }

        public static MagicItemEffect RollEffect(MagicItemEffectDefinition effectDef, ItemRarity itemRarity, MagicItemEffectDefinition.ValueDef valuesOverride = null)
        {
            float value = 0;
            var valuesDef = valuesOverride ?? effectDef.GetValuesForRarity(itemRarity);
            if (valuesDef != null)
            {
                value = valuesDef.MinValue;
                if (valuesDef.Increment != 0)
                {
                    EpicLoot.Log($"RollEffect: {effectDef.Type} {itemRarity} value={value} (min={valuesDef.MinValue} max={valuesDef.MaxValue})");
                    var incrementCount = (int)((valuesDef.MaxValue - valuesDef.MinValue) / valuesDef.Increment);
                    value = valuesDef.MinValue + (Random.Range(0, incrementCount + 1) * valuesDef.Increment);
                }
            }

            return new MagicItemEffect(effectDef.Type, value);
        }

        public static List<MagicItemEffect> RollEffects(List<MagicItemEffectDefinition> availableEffects, ItemRarity itemRarity, int count, bool removeOnSelect = true)
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
                { ItemRarity.Legendary, rarity.Length >= 4 ? rarity[3] : 0 }
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

        public static List<KeyValuePair<int, float>> GetDropsForLevel([NotNull] LootTable lootTable, int level, bool useNextHighestIfNotPresent = true)
        {
            if (level == 3 && !ArrayUtils.IsNullOrEmpty(lootTable.Drops3))
            {
                if (lootTable.LeveledLoot.Any(x => x.Level == level))
                {
                    EpicLoot.LogWarning($"Duplicated leveled drops for ({lootTable.Object} lvl {level}), using 'Drops{level}'");
                }
                return ToDropList(lootTable.Drops3);
            }
            
            if ((level == 2 || level == 3) && !ArrayUtils.IsNullOrEmpty(lootTable.Drops2))
            {
                if (lootTable.LeveledLoot.Any(x => x.Level == level))
                {
                    EpicLoot.LogWarning($"Duplicated leveled drops for ({lootTable.Object} lvl {level}), using 'Drops{level}'");
                }
                return ToDropList(lootTable.Drops2);
            }

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

            EpicLoot.LogError($"Could not find any leveled drops for ({lootTable.Object} lvl {level}), but a loot table exists for this object!");
            return null;
        }

        private static List<KeyValuePair<int, float>> ToDropList(float[][] drops)
        {
            return drops.Select(x => new KeyValuePair<int, float>((int) x[0], x[1])).ToList();
        }

        public static LootDrop[] GetLootForLevel([NotNull] LootTable lootTable, int level, bool useNextHighestIfNotPresent = true)
        {
            if (level == 3 && !ArrayUtils.IsNullOrEmpty(lootTable.Loot3))
            {
                if (lootTable.LeveledLoot.Any(x => x.Level == level))
                {
                    EpicLoot.LogWarning($"Duplicated leveled loot for ({lootTable.Object} lvl {level}), using 'Loot{level}'");
                }
                return lootTable.Loot3.ToArray();
            }

            if ((level == 2 || level == 3) && !ArrayUtils.IsNullOrEmpty(lootTable.Loot2))
            {
                if (lootTable.LeveledLoot.Any(x => x.Level == level))
                {
                    EpicLoot.LogWarning($"Duplicated leveled loot for ({lootTable.Object} lvl {level}), using 'Loot{level}'");
                }
                return lootTable.Loot2.ToArray();
            }
            
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

            EpicLoot.LogError($"Could not find any leveled loot for ({lootTable.Object} lvl {level}), but a loot table exists for this object!");
            return null;
        }

        public static List<MagicItemEffect> RollAugmentEffects(ExtendedItemData item, MagicItem magicItem, int effectIndex)
        {
            var results = new List<MagicItemEffect>();

            if (item == null || magicItem == null)
            {
                EpicLoot.LogError($"[RollAugmentEffects] Null inputs: item={item}, magicItem={magicItem}");
                return results;
            }

            if (effectIndex < 0 || effectIndex >= magicItem.Effects.Count)
            {
                EpicLoot.LogError($"[RollAugmentEffects] Bad effect index ({effectIndex}), effects count: {magicItem.Effects.Count}");
                return results;
            }

            var rarity = magicItem.Rarity;
            var currentEffect = magicItem.Effects[effectIndex];
            results.Add(currentEffect);

            var valuelessEffect = MagicItemEffectDefinitions.IsValuelessEffect(currentEffect.EffectType, rarity);
            var availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(item, magicItem, valuelessEffect ? -1 : effectIndex);

            for (var i = 0; i < 2 && i < availableEffects.Count; i++)
            {
                var newEffect = RollEffects(availableEffects, rarity, 1, false).FirstOrDefault();
                if (newEffect == null)
                {
                    EpicLoot.LogError($"Rolled a null effect: item:{item.m_shared.m_name}, index:{effectIndex}");
                    continue;
                }

                results.Add(newEffect);

                var newEffectIsValueless = MagicItemEffectDefinitions.IsValuelessEffect(newEffect.EffectType, rarity);
                if (newEffectIsValueless)
                {
                    availableEffects.RemoveAll(x => x.Type == newEffect.EffectType);
                }
            }

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
            var players = Player.m_players;
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

        public static Dictionary<ItemRarity, float> ModifyRarityByLuck(IReadOnlyDictionary<ItemRarity, float> rarityWeights, float luckFactor = 0)
        {
            var results = new Dictionary<ItemRarity, float>();
            for (var rarity = ItemRarity.Magic; rarity <= ItemRarity.Legendary; rarity++)
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
            var lootTable = GetLootTable(lootTableName)[0];
            var lootDrop = GetLootForLevel(lootTable, 1)[0];
            lootDrop = ResolveLootDrop(lootDrop);
            var rarityBase = GetRarityWeights(lootDrop.Rarity, 0);
            var rarityLuck = GetRarityWeights(lootDrop.Rarity, luckFactor);

            var sb = new StringBuilder();
            sb.AppendLine($"Luck Test: {lootTableName}, {luckFactor}");
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

            Debug.LogWarning(sb.ToString());
        }

        public static void PrintLootResolutionTest(string lootTableName, int level, int itemIndex)
        {
            Debug.LogWarning($"{lootTableName}:{level}:{itemIndex}");

            var lootTable = GetLootTable(lootTableName)[0];
            var lootDrop = GetLootForLevel(lootTable, level)[itemIndex];
            lootDrop = ResolveLootDrop(lootDrop);
            var rarity = lootDrop.Rarity;

            Debug.LogWarning($"> rarity=[ {rarity[0]}, {rarity[1]}, {rarity[2]}, {rarity[3]} ]");
        }
    }
}
