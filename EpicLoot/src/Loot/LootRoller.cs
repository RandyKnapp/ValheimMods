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
    // What a single rolled loot drop actually becomes. Every drop the loot tables produce picks exactly
    // one of these, weighted by the four Balance drop ratios (see LootRoller.SelectDropType), so the
    // categories compete for the same drop slot rather than each getting an independent coin flip.
    public enum LootDropType
    {
        Item,
        ShardStone,
        Unidentified,
        Materials
    }

    public static class LootRoller
    {
        public static LootConfig Config;
        public static readonly Dictionary<string, LootItemSet> ItemSets = new Dictionary<string, LootItemSet>();
        public static readonly Dictionary<string, List<LootTable>> LootTables = new Dictionary<string, List<LootTable>>();

        // Biome shard drops name their item set "ShardStone_{Biome}" using the Heightmap.Biome enum name, the
        // same convention as TreasureMapChest_{Biome} and {Biome}_{Rarity}_Unidentified. This one covers a biome
        // with no set of its own — a modded biome, or Heightmap.Biome.None.
        private const string DefaultShardStoneSet = "ShardStone_None";

        // Ceiling on how many sockets a SocketCounts entry may ask for. SocketsUI builds its inventory
        // row directly from the socket count, so a runaway config value would break the UI.
        public const int MaxSocketCount = 6;

        // Mirrors the SocketCounts block in config/loottables.json; used when that block is missing, which
        // is the normal case for a loottables.json written before SocketCounts existed. Keep the two in sync.
        private static readonly Dictionary<ItemRarity, float[][]> DefaultSocketCounts =
            new Dictionary<ItemRarity, float[][]>
            {
                { ItemRarity.Magic,     new[] { new[] { 0f, 90f }, new[] { 1f, 10f } } },
                { ItemRarity.Rare,      new[] { new[] { 0f, 75f }, new[] { 1f, 25f } } },
                { ItemRarity.Epic,      new[] { new[] { 0f, 50f }, new[] { 1f, 45f }, new[] { 2f,  5f } } },
                { ItemRarity.Legendary, new[] { new[] { 0f, 15f }, new[] { 1f, 55f }, new[] { 2f, 30f } } },
                { ItemRarity.Mythic,    new[] { new[] { 0f,  5f }, new[] { 1f, 30f }, new[] { 2f, 40f }, new[] { 3f, 25f } } },
            };

        // Socket counts are read on every magic item roll, so config complaints are logged once per rarity
        // and reset whenever the loot config is (re)loaded.
        private static readonly HashSet<ItemRarity> _warnedMissingSocketCounts = new HashSet<ItemRarity>();
        private static readonly HashSet<ItemRarity> _warnedInvalidSocketCounts = new HashSet<ItemRarity>();

        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedDropCountTable;
        private static WeightedRandomCollection<LootDrop> _weightedLootTable;
        // Deliberately its own collection rather than a reuse of _weightedLootTable: SelectDropType runs
        // inside the same per-drop loop that ResolveLootDrop re-Setup()s _weightedLootTable in.
        private static WeightedRandomCollection<KeyValuePair<LootDropType, float>> _weightedDropTypeTable;
        private static WeightedRandomCollection<MagicItemEffectDefinition> _weightedEffectTable;
        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedEffectCountTable;
        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedSocketCountTable;
        private static WeightedRandomCollection<KeyValuePair<ItemRarity, float>> _weightedRarityTable;
        private static WeightedRandomCollection<LegendaryInfo> _weightedLegendaryTable;
        private static WeightedRandomCollection<LegendaryInfo> _weightedMythicTable;
        public static bool CheatRollingItem = false;
        public static int CheatEffectCount;
        public static int CheatSocketCount = -1;
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
            _weightedDropTypeTable = new WeightedRandomCollection<KeyValuePair<LootDropType, float>>();
            _weightedEffectTable = new WeightedRandomCollection<MagicItemEffectDefinition>();
            _weightedEffectCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>();
            _weightedSocketCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>();
            _weightedRarityTable = new WeightedRandomCollection<KeyValuePair<ItemRarity, float>>();
            _weightedLegendaryTable = new WeightedRandomCollection<LegendaryInfo>();
            _weightedMythicTable = new WeightedRandomCollection<LegendaryInfo>();

            ItemSets.Clear();
            LootTables.Clear();
            _warnedMissingSocketCounts.Clear();
            _warnedInvalidSocketCounts.Clear();
          
            AddItemSets(lootConfig.ItemSets);
            AddLootTables(lootConfig.LootTables);

            // Initialize clears LootTables, so anything an external plugin registered through
            // API.AddLootTables has just been wiped. Same contract as the other config subsystems'
            // OnSetup* events: subscribers re-apply their own additions.
            OnSetupLootTables?.Invoke();
        }

        public static event Action OnSetupLootTables;

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

        /// <summary>
        /// Drops previously added tables by reference. AddLootTable appends rather than replaces, so
        /// re-registering an updated table without this would leave the old one rolling alongside it.
        /// </summary>
        public static void RemoveLootTables([NotNull] IEnumerable<LootTable> lootTables)
        {
            foreach (var lootTable in lootTables)
            {
                if (lootTable?.Object == null || !LootTables.TryGetValue(lootTable.Object, out var tables))
                {
                    continue;
                }

                tables.Remove(lootTable);
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

                // ZNetScene.Destroy is the right call either way: it no-ops the ZDO half when the
                // ZNetView never registered (the normal case here, since these are spawned with
                // m_forceDisableInit), and unregisters properly if it did. Plain Object.Destroy would
                // strand a live entry in ZNetScene.m_instances in that second case.
                if (ZNetScene.instance != null)
                {
                    ZNetScene.instance.Destroy(itemObject);
                }
                else
                {
                    Object.Destroy(itemObject);
                }
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

                        API.WithChangeReason(API.ChangeReason.LootRoll, () => magicItemComponent.SetMagicItem(magicItem));
                        itemDrop.Save();
                        InitializeMagicItem(itemDrop.m_itemData);
                        API.RaiseLootGenerated(itemDrop.m_itemData);
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

                // Resolution consumes any per-rarity map on the entry, so by the time the branches below
                // look a prefab up the name is concrete and the drop's Rarity has been pinned to the
                // rarity that chose it.
                var lootDrop = ResolveLootDrop(ld, luckFactor);

                var itemName = !string.IsNullOrEmpty(lootDrop?.Item) ? lootDrop.Item : "Invalid Item Name";
                var rarityLength = lootDrop?.Rarity?.Length != null ? lootDrop.Rarity.Length : -1;
                EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarityLength} - Weight: {lootDrop.Weight}");

                // A drop that is already a shard — rolled from an elite creature's bonus shard set or from a
                // boss's shard table — must not be re-rolled into a biome shard, nor sacrificed for
                // materials. The unidentified category needs no such guard: IsAllowedMagicItemType rejects
                // a Material.
                var isShardDrop = lootDrop.Item != null &&
                    lootDrop.Item.EndsWith(global::EpicLoot.ShardStones.Shards.ShardIndicator, StringComparison.Ordinal);

                var dropType = SelectDropType(lootDrop, isShardDrop, cheatsActive);
                EpicLoot.Log($"Drop type for {lootDrop.Item}: {dropType}");

                var spawned = false;
                switch (dropType)
                {
                    case LootDropType.ShardStone:
                        spawned = TrySpawnBiomeShard(lootDrop, ref dropPoint, luckFactor, initializeObject, results);
                        break;
                    case LootDropType.Unidentified:
                        spawned = TrySpawnUnidentified(lootDrop, ref dropPoint, luckFactor, initializeObject, results);
                        break;
                    case LootDropType.Materials:
                        spawned = TrySpawnMaterials(lootDrop, dropPoint, luckFactor, results);
                        break;
                }

                // Every substitute category can still fail late — a missing prefab, a rarity with no
                // sacrifice products — and each one warns before it does. Falling back to the item the
                // loot table actually named is what keeps a failure from silently eating the drop.
                if (!spawned)
                {
                    SpawnNormalItem(lootDrop, objectName, dropPoint, luckFactor, initializeObject, results);
                }
            }

            return results;
        }

        // Rolls what a single drop becomes. The four Balance drop ratios are relative weights, not
        // independent chances, so only their proportions matter and any of them may be zeroed to remove
        // that category. Categories this particular drop cannot become are left out of the roll entirely
        // rather than rolled and then rejected — an ineligible category in the pool would silently eat
        // the drop's chance of becoming any of the others.
        private static LootDropType SelectDropType(LootDrop lootDrop, bool isShardDrop, bool cheatsActive)
        {
            // Item spawn cheats asked for a specific thing; never substitute anything for it.
            if (cheatsActive)
            {
                return LootDropType.Item;
            }

            var candidates = new List<KeyValuePair<LootDropType, float>>();
            AddDropTypeCandidate(candidates, LootDropType.Item, ELConfig.ItemDropRatio.Value);

            if (!isShardDrop)
            {
                AddDropTypeCandidate(candidates, LootDropType.ShardStone, ELConfig.ShardStoneDropRatio.Value);
                AddDropTypeCandidate(candidates, LootDropType.Materials, ELConfig.MaterialsDropRatio.Value);
            }

            // Only equippable loot has an unidentified counterpart; materials and everything else stay as
            // they are. This is why the check lives here rather than inside TrySpawnUnidentified.
            if (IsAllowedUnidentifiedDrop(lootDrop))
            {
                AddDropTypeCandidate(candidates, LootDropType.Unidentified, ELConfig.ItemsUnidentifiedDropRatio.Value);
            }

            // No category is possible, or every weight is zero: drop the item the loot table named.
            if (candidates.Count == 0)
            {
                return LootDropType.Item;
            }

            _weightedDropTypeTable.Setup(candidates, x => x.Value);
            return _weightedDropTypeTable.Roll().Key;
        }

        // Zero-weight categories are omitted rather than added with weight 0, so a roll can never land on
        // a disabled category through float rounding at the bottom of the weight range.
        private static void AddDropTypeCandidate(List<KeyValuePair<LootDropType, float>> candidates,
            LootDropType dropType, float weight)
        {
            if (weight > 0)
            {
                candidates.Add(new KeyValuePair<LootDropType, float>(dropType, weight));
            }
        }

        // True when the drop names an equippable item, i.e. one an unidentified item could stand in for.
        private static bool IsAllowedUnidentifiedDrop(LootDrop lootDrop)
        {
            if (lootDrop.Item.IsNullOrWhiteSpace() || ObjectDB.instance == null)
            {
                return false;
            }

            var prefab = ObjectDB.instance.GetItemPrefab(lootDrop.Item);
            var itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return itemDrop != null && EpicLoot.IsAllowedMagicItemType(itemDrop.m_itemData);
        }

        // Spawns a shard stone drawn from the biome the loot is dropping in. The biome names its item set
        // (ShardStone_{Biome} in loottables.json), which resolves to one of the ShardT1..ShardT7 tier sets,
        // so the whole biome preset stays config-patchable.
        private static bool TrySpawnBiomeShard(LootDrop lootDrop, ref Vector3 dropPoint, float luckFactor,
            bool initializeObject, List<GameObject> results)
        {
            ZoneSystem.instance.GetGroundData(ref dropPoint, out var _, out var shardBiome, out var _, out var _);

            var shardSetName = $"ShardStone_{shardBiome}";
            if (!ItemSets.ContainsKey(shardSetName))
            {
                EpicLoot.LogWarning($"No shard stone item set found for biome {shardBiome} " +
                    $"({shardSetName}), falling back to {DefaultShardStoneSet}.");
                shardSetName = DefaultShardStoneSet;
            }

            if (!ItemSets.ContainsKey(shardSetName))
            {
                EpicLoot.LogWarning($"Tried to spawn a shard stone but no item set named " +
                    $"{DefaultShardStoneSet} exists! Dropping {lootDrop.Item} instead.");
                return false;
            }

            // Seed a fresh LootDrop with no Rarity: ResolveLootDrop only inherits the resolved set's own
            // Rarity[] when the incoming one is empty, so reusing lootDrop here would leak the gear entry's
            // rarity weights and defeat the per-tier rarity the ShardT sets encode.
            var shardDrop = ResolveLootDrop(new LootDrop { Item = shardSetName, Weight = 1 }, luckFactor);

            GameObject shardPrefab = null;
            if (!shardDrop.Item.IsNullOrWhiteSpace())
            {
                shardPrefab = ObjectDB.instance.GetItemPrefab(shardDrop.Item);
            }

            if (shardPrefab == null)
            {
                EpicLoot.LogWarning($"Tried to spawn shard stone ({shardDrop.Item}) for biome " +
                    $"{shardBiome} but the item prefab was not found! Dropping {lootDrop.Item} instead.");
                return false;
            }

            EpicLoot.Log($"Adding {shardDrop.Item} shard stone for biome {shardBiome}");
            var shardObject = SpawnLootForDrop(shardPrefab, dropPoint, initializeObject);
            var shardItemDrop = shardObject.GetComponent<ItemDrop>();

            // Identity already rides on the prefab's shared data and Awake restores the cosmetic MagicItem,
            // but stamping and saving here keeps the intent explicit and matches the unidentified path.
            // Both calls are idempotent.
            global::EpicLoot.ShardStones.Shards.EnsureShardMetadata(shardItemDrop.m_itemData);
            shardItemDrop.Save();

            results.Add(shardObject);
            return true;
        }

        // Spawns an unidentified item of the rolled rarity in place of the drop. The biome is the one
        // gating the item's own progression where that is known, falling back to the biome at the drop
        // point.
        private static bool TrySpawnUnidentified(LootDrop lootDrop, ref Vector3 dropPoint, float luckFactor,
            bool initializeObject, List<GameObject> results)
        {
            var rarity = RollItemRarity(lootDrop, luckFactor);

            // Determine which biome this item is a part of, and set the drop biome to that tier
            GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(lootDrop.Item, out var itemDetails);
            var biomes = new List<Heightmap.Biome>();
            if (itemDetails != null)
            {
                foreach (string bosskey in itemDetails.RequiredBosses)
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

            var selectBiome = biomes.First().ToString();
            var prefab = ObjectDB.instance.GetItemPrefab($"{selectBiome}_{rarity}_Unidentified");
            if (prefab == null)
            {
                // Warn and drop the normal item instead
                EpicLoot.LogWarning($"Tried to spawn unidentified item for {selectBiome}_{rarity}_Unidentified " +
                    $"but prefab was not found! Dropping {lootDrop.Item} instead.");
                return false;
            }

            EpicLoot.Log($"Adding {rarity} unidentified item");
            var randomRotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);

            // m_forceDisableInit is a global that ZNetView.Awake reads to decide whether to register a
            // ZDO at all. Restore whatever it was, in a finally: leaving it stuck true makes every
            // later ZNetView awake unregistered, which strands null-ZDO entries in ZNetScene.m_instances
            // and NREs ZNetScene.RemoveObjects every frame for the rest of the session.
            var priorForceDisableInit = ZNetView.m_forceDisableInit;
            GameObject lootdrop;
            try
            {
                ZNetView.m_forceDisableInit = !initializeObject;
                lootdrop = Object.Instantiate(prefab, dropPoint, randomRotation);
                // Ensure that the unidentified item has the correct magic item data for the rarity
                var id = lootdrop.GetComponent<ItemDrop>();
                var mic = id.m_itemData.Data().GetOrCreate<MagicItemComponent>();
                mic.SetMagicItem(new MagicItem
                {
                    Rarity = rarity,
                    IsUnidentified = true,
                });
                // Persist the rarity/unidentified state into the ZDO so a real world drop survives reload.
                // No-op for the container path where the ZNetView was disabled (Save early-returns on
                // invalid nview).
                id.Save();
            }
            finally
            {
                ZNetView.m_forceDisableInit = priorForceDisableInit;
            }

            results.Add(lootdrop);
            return true;
        }

        // Replaces the drop with the magic crafting materials that item would yield if sacrificed. Returns
        // false when nothing could be spawned — an item with no sacrifice products for the rolled rarity
        // drops as itself rather than as nothing at all.
        private static bool TrySpawnMaterials(LootDrop lootDrop, Vector3 dropPoint, float luckFactor,
            List<GameObject> results)
        {
            GameObject prefab = null;

            if (!lootDrop.Item.IsNullOrWhiteSpace())
            {
                prefab = ObjectDB.instance.GetItemPrefab(lootDrop.Item);
            }

            var sourceItemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (sourceItemDrop == null)
            {
                return false;
            }

            var rarity = RollItemRarity(lootDrop, luckFactor);
            var itemType = sourceItemDrop.m_itemData.m_shared.m_itemType;
            var disenchantProducts = EnchantCostsHelper.GetSacrificeProducts(true, itemType, rarity);
            if (disenchantProducts == null)
            {
                return false;
            }

            var spawnedAny = false;
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
                spawnedAny = true;
            }

            return spawnedAny;
        }

        // The default path: spawn the item the loot table named, gated by boss progression, and roll its
        // magic item data when the item is eligible for one.
        private static void SpawnNormalItem(LootDrop lootDrop, string objectName, Vector3 dropPoint,
            float luckFactor, bool initializeObject, List<GameObject> results)
        {
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
                return;
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

                API.WithChangeReason(API.ChangeReason.LootRoll, () => magicItemComponent.SetMagicItem(magicItem));
                itemDrop.m_itemData = itemData;
                itemDrop.Save();
                InitializeMagicItem(itemData);
                API.RaiseLootGenerated(itemData);
            }

            results.Add(item);
        }

        public static GameObject SpawnLootForDrop(GameObject itemPrefab, Vector3 dropPoint, bool initializeObject)
        {
            Quaternion randomRotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);

            // Save and restore rather than assigning false: Instantiate runs the new object's Awake
            // chain (ItemDrop.Awake, ItemDataManager, our own postfixes), any of which can throw, and
            // the chest path can run nested inside another instantiate. A stranded true here breaks
            // ZNetScene for the rest of the session — see the comment in TrySpawnUnidentified.
            var priorForceDisableInit = ZNetView.m_forceDisableInit;
            try
            {
                ZNetView.m_forceDisableInit = !initializeObject;
                return Object.Instantiate(itemPrefab, dropPoint, randomRotation);
            }
            finally
            {
                ZNetView.m_forceDisableInit = priorForceDisableInit;
            }
        }

        // Resolves a loot entry down to a name that ObjectDB can look up, following per-rarity maps,
        // ItemSets and "Object.Level" table references for as long as any of them apply. Returns a fresh
        // copy, so callers are free to mutate the result.
        //
        // luckFactor only matters for entries carrying a RarityItems map, which is why it defaults: the
        // console commands resolve entries outside of any drop and have no luck to apply.
        //
        // consumeRarityItems: false stops resolution at the first entry carrying a per-rarity map, leaving
        // its authored Rarity spread intact. Only the luck-test command wants that — rolling a rarity is
        // exactly what it is trying to report on rather than perform.
        public static LootDrop ResolveLootDrop(LootDrop lootDrop, float luckFactor = 0f, bool consumeRarityItems = true)
        {
            var result = new LootDrop
            {
                Item = lootDrop.Item,
                Rarity = ArrayUtils.Copy(lootDrop.Rarity),
                Weight = lootDrop.Weight,
                RarityItems = lootDrop.RarityItems
            };
            var needsResolve = true;

            // Every branch below can hand the loop another name to resolve, so a cyclic config (set A
            // -> set B -> set A, or a loot table referencing itself) spins here forever on the main
            // thread with nothing logged. No legitimate chain is anywhere near this deep; trip the cap
            // and name the trail instead of hanging the game.
            const int maxResolveSteps = 32;
            var resolveSteps = 0;
            var resolveTrail = new List<string>();

            while (needsResolve)
            {
                resolveTrail.Add(result.Item);
                if (++resolveSteps > maxResolveSteps)
                {
                    EpicLoot.LogError($"ResolveLootDrop exceeded {maxResolveSteps} steps resolving " +
                        $"'{lootDrop.Item}' -- the loot config almost certainly has a cycle. " +
                        $"Chain: {string.Join(" -> ", resolveTrail)}");
                    break;
                }

                // Checked first, and before any name lookup: the map is what decides which name this entry
                // even has. Whatever it names is then resolved by the branches below, so a rarity may point
                // at an ItemSet or another table just as Item may.
                if (consumeRarityItems && ResolveRarityItem(result, luckFactor))
                {
                    continue;
                }

                if (ItemSets.TryGetValue(result.Item, out var itemSet))
                {
                    // Rolling an empty list returns null, so stop here rather than dereference it. The
                    // result keeps naming the set; the caller's prefab lookup then fails with its own
                    // message, right after this one names the actual cause.
                    if (itemSet.Loot.Length == 0)
                    {
                        EpicLoot.LogError($"Tried to roll using ItemSet ({itemSet.Name}) but its loot list was empty!");
                        break;
                    }
                    _weightedLootTable.Setup(itemSet.Loot, x => x.Weight);
                    var itemSetResult = _weightedLootTable.Roll();
                    result.Item = itemSetResult.Item;
                    result.Weight = itemSetResult.Weight;
                    // A rarity map belongs to the name it was authored next to, so unlike Rarity it always
                    // replaces what came in — the entry we just rolled is the one that knows its prefabs.
                    result.RarityItems = itemSetResult.RarityItems;
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
                        break;
                    }
                    _weightedLootTable.Setup(lootList, x => x.Weight);
                    var referenceResult = _weightedLootTable.Roll();
                    result.Item = referenceResult.Item;
                    result.Weight = referenceResult.Weight;
                    result.RarityItems = referenceResult.RarityItems;
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

            magicItem.SocketCount = CheatSocketCount >= 0 ? CheatSocketCount : RollSocketCountPerRarity(magicItem.Rarity);

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

        // internal rather than private: API.TryMakeMagicItem reproduces the full drop flow for external
        // plugins, and randomized wear is part of that flow.
        internal static void InitializeMagicItem(ItemDrop.ItemData baseItem)
        {
            // Callers run SetMagicItem first, which already synced Indestructible — so an
            // indestructible drop reads m_useDurability == false here and skips the wear roll.
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

        // Rolls the number of shard sockets an item gets at loot-generation time, weighted per rarity
        // by the SocketCounts table in loottables.json.
        // Unlike effect counts, sockets are not affected by enchanting-table upgrades.
        public static int RollSocketCountPerRarity(ItemRarity rarity)
        {
            var countPercents = GetSocketCountsPerRarity(rarity);
            if (countPercents.Count == 0)
            {
                return 0;
            }

            _weightedSocketCountTable.Setup(countPercents, x => x.Value);
            return _weightedSocketCountTable.Roll().Key;
        }

        public static List<KeyValuePair<int, float>> GetSocketCountsPerRarity(ItemRarity rarity)
        {
            var configured = GetConfiguredSocketCounts(rarity);
            if (ArrayUtils.IsNullOrEmpty(configured))
            {
                // A loottables.json written before SocketCounts existed keeps winning over the embedded
                // default (see FilePatching.LoadPatchedJSON), so fall back rather than silently rolling
                // zero sockets for everything.
                if (_warnedMissingSocketCounts.Add(rarity))
                {
                    EpicLoot.LogWarning($"loottables.json has no SocketCounts entry for {rarity}, " +
                        $"using the built-in default distribution. Accept the config update prompt on " +
                        $"startup, or add a \"SocketCounts\" block to loottables.json, to configure it.");
                }

                configured = DefaultSocketCounts[rarity];
            }

            var result = new List<KeyValuePair<int, float>>();
            var droppedEntry = false;
            foreach (var entry in configured)
            {
                if (entry == null || entry.Length < 2)
                {
                    droppedEntry = true;
                    continue;
                }

                // Nothing else bounds this value and SocketsUI sizes its inventory row straight from the
                // socket count, so an out-of-range entry is dropped instead of trusted. A negative weight
                // goes too, since WeightedRandomCollection would quietly skew the whole table.
                var count = (int)entry[0];
                if (count < 0 || count > MaxSocketCount || entry[1] < 0)
                {
                    droppedEntry = true;
                    continue;
                }

                result.Add(new KeyValuePair<int, float>(count, entry[1]));
            }

            if (droppedEntry && _warnedInvalidSocketCounts.Add(rarity))
            {
                EpicLoot.LogWarning($"SocketCounts entries for {rarity} in loottables.json were ignored: " +
                    $"each entry must be [count, weight] with a count between 0 and {MaxSocketCount} " +
                    $"and a weight of 0 or more.");
            }

            return result;
        }

        private static float[][] GetConfiguredSocketCounts(ItemRarity rarity)
        {
            var socketCounts = Config?.SocketCounts;
            if (socketCounts == null)
            {
                return null;
            }

            switch (rarity)
            {
                case ItemRarity.Magic: return socketCounts.Magic;
                case ItemRarity.Rare: return socketCounts.Rare;
                case ItemRarity.Epic: return socketCounts.Epic;
                case ItemRarity.Legendary: return socketCounts.Legendary;
                case ItemRarity.Mythic: return socketCounts.Mythic;
                default: throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
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

        // Consumes a resolved entry's RarityItems map: rolls the rarity from its Rarity[] weights, swaps
        // the matching name into Item, and pins Rarity to exactly that rarity. Returns false (touching
        // nothing) when the entry carries no map, which is the common case.
        //
        // Pinning is what makes the feature safe to use for anything other than shards. Every later stage
        // — the magic item roll in SpawnNormalItem, and the Unidentified and Materials substitutions --
        // re-reads Rarity, so leaving the original spread in place would let a drop be selected as one
        // rarity and then rolled as another. Shards do not care (they are Materials and carry their rarity
        // in their own prefab's shared data), but a rarity map pointing at gear would.
        //
        // The map is cleared as it is consumed so the caller's while-loop can keep resolving whatever was
        // substituted — an ItemSet or an "Object.Level" reference — without re-entering here.
        private static bool ResolveRarityItem(LootDrop lootDrop, float luckFactor)
        {
            if (lootDrop?.RarityItems == null || lootDrop.RarityItems.Count == 0)
            {
                return false;
            }

            var rarity = RollItemRarity(lootDrop, luckFactor);
            var item = SelectRarityItem(lootDrop.RarityItems, rarity, out var usedRarity);

            lootDrop.RarityItems = null;
            lootDrop.Rarity = GetSingleRarityWeights(usedRarity);

            // An empty pick means every key in the map was blank; keep the entry's own Item as the default
            // rather than resolving to nothing.
            if (!item.IsNullOrWhiteSpace())
            {
                lootDrop.Item = item;
            }

            return true;
        }

        // Picks the entry for a rolled rarity, falling back to the nearest rarity the map does define.
        // Snapping rather than failing is deliberate: a config patch is free to re-weight an entry's
        // Rarity[] without knowing which rarities that particular item exists at (shard colors each
        // declare their own set), and the nearest neighbour is always a better answer than a name that
        // resolves to no prefab. Ties go to the lower rarity.
        private static string SelectRarityItem(Dictionary<ItemRarity, string> rarityItems, ItemRarity rarity,
            out ItemRarity usedRarity)
        {
            usedRarity = rarity;
            if (rarityItems.TryGetValue(rarity, out var exact))
            {
                return exact;
            }

            var bestDiff = int.MaxValue;
            string best = null;
            foreach (var entry in rarityItems)
            {
                var diff = Math.Abs((int)entry.Key - (int)rarity);
                if (diff < bestDiff || (diff == bestDiff && entry.Key < usedRarity))
                {
                    bestDiff = diff;
                    best = entry.Value;
                    usedRarity = entry.Key;
                }
            }

            EpicLoot.Log($"Rarity {rarity} has no entry in a RarityItems map; using {usedRarity} ({best}).");
            return best;
        }

        private static float[] GetSingleRarityWeights(ItemRarity rarity)
        {
            var weights = new float[5];
            weights[(int)rarity] = 1;
            return weights;
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
            // Stop short of consuming a per-rarity map: doing so would pin Rarity to the single rarity it
            // rolled, which is the very spread this test exists to report.
            lootDrop = ResolveLootDrop(lootDrop, 0, consumeRarityItems: false);
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
