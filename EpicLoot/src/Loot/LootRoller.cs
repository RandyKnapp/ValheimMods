using BepInEx;
using Common;
using EpicLoot.Adventure;
using EpicLoot.Biomes;
using EpicLoot.Config;
using EpicLoot.Crafting;
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

        // A RefObject entry's own StarMultiplier/Modifiers/StarScaling, keyed by its Object. The entry
        // shares its template's table list (so tables added to the template later still reach every
        // creature using it), which leaves this the only place that creature's own tuning lives.
        private static readonly Dictionary<string, LootTable> CreatureOverrides = new Dictionary<string, LootTable>();

        // Keys whose list is currently another key's (a RefObject entry), so a table of the key's own never
        // lands in the template's shared list.
        private static readonly HashSet<string> _aliasKeys = new HashSet<string>();

        // Loot-config complaints that would otherwise repeat on every kill; reset on each (re)load.
        private static readonly HashSet<string> _warnedLootTables = new HashSet<string>();

        // Biome shard drops name their item set "ShardStone_{Biome}" using the Heightmap.Biome enum name, the
        // same convention as TreasureMapChest_{Biome} and {Biome}_{Rarity}_Unidentified. This one covers a biome
        // with no set of its own — a modded biome, or Heightmap.Biome.None.
        private const string DefaultShardStoneSet = "ShardStone_None";

        // Ceiling on how many sockets a SocketCounts entry may ask for. ShardUI builds its slot panel
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
                { ItemRarity.Ancient,   new[] { new[] { 1f, 20f }, new[] { 2f, 40f }, new[] { 3f, 30f }, new[] { 4f, 10f } } },
            };

        // Mirrors the MagicEffectsCount block in config/loottables.json; used when that block is missing a
        // rarity, which is the normal case for a loottables.json written before a rarity was added and kept
        // by the player-changes check in FilePatching. Keep the two in sync.
        private static readonly Dictionary<ItemRarity, float[][]> DefaultMagicEffectsCount =
            new Dictionary<ItemRarity, float[][]>
            {
                { ItemRarity.Magic,     new[] { new[] { 1f, 80f }, new[] { 2f, 18f }, new[] { 3f, 2f } } },
                { ItemRarity.Rare,      new[] { new[] { 2f, 80f }, new[] { 3f, 18f }, new[] { 4f, 2f } } },
                { ItemRarity.Epic,      new[] { new[] { 3f, 80f }, new[] { 4f, 18f }, new[] { 5f, 2f } } },
                { ItemRarity.Legendary, new[] { new[] { 4f, 80f }, new[] { 5f, 18f }, new[] { 6f, 2f } } },
                { ItemRarity.Mythic,    new[] { new[] { 5f, 80f }, new[] { 6f, 18f }, new[] { 7f, 2f } } },
                { ItemRarity.Ancient,   new[] { new[] { 6f, 80f }, new[] { 7f, 18f }, new[] { 8f, 2f } } },
            };

        // Socket and effect counts are read on every magic item roll, so config complaints are logged once
        // per rarity and reset whenever the loot config is (re)loaded.
        private static readonly HashSet<ItemRarity> _warnedMissingSocketCounts = new HashSet<ItemRarity>();
        private static readonly HashSet<ItemRarity> _warnedInvalidSocketCounts = new HashSet<ItemRarity>();
        private static readonly HashSet<ItemRarity> _warnedMissingEffectCounts = new HashSet<ItemRarity>();
        private static readonly HashSet<ItemRarity> _warnedInvalidEffectCounts = new HashSet<ItemRarity>();

        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedDropCountTable;
        private static WeightedRandomCollection<LootDrop> _weightedLootTable;
        // Deliberately its own collection rather than a reuse of _weightedLootTable: SelectDropType runs
        // inside the same per-drop loop that ResolveLootDrop re-Setup()s _weightedLootTable in.
        private static WeightedRandomCollection<KeyValuePair<LootDropType, float>> _weightedDropTypeTable;
        private static WeightedRandomCollection<MagicItemEffectDefinition> _weightedEffectTable;
        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedEffectCountTable;
        private static WeightedRandomCollection<KeyValuePair<int, float>> _weightedSocketCountTable;
        private static WeightedRandomCollection<KeyValuePair<ItemRarity, float>> _weightedRarityTable;
        private static WeightedRandomCollection<LegendaryInfo> _weightedUniqueTable;
        public static bool CheatRollingItem = false;
        public static int CheatEffectCount;
        public static int CheatSocketCount = -1;
        public static bool CheatDisableGating;
        public static bool CheatForceMagicEffect;
        public static int CheatEffectRarityTier;
        public static bool CheatEffectRarityFirstOnly;
        public static string ForcedMagicEffect = "";
        // The magicsetitem / magicitemset console commands: the unique (or set piece) to roll and the
        // rarity to roll it at.
        public static string CheatForceUniqueID;
        public static ItemRarity? CheatForceUniqueRarity;

        public static void Initialize(LootConfig lootConfig)
        {
            if (lootConfig == null)
            {
                EpicLoot.LogWarning("LootRoller.Initialize called with a null config; keeping the currently loaded loot tables.");
                return;
            }

            Config = lootConfig;

            _weightedDropCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>();
            _weightedLootTable = new WeightedRandomCollection<LootDrop>();
            _weightedDropTypeTable = new WeightedRandomCollection<KeyValuePair<LootDropType, float>>();
            _weightedEffectTable = new WeightedRandomCollection<MagicItemEffectDefinition>();
            _weightedEffectCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>();
            _weightedSocketCountTable = new WeightedRandomCollection<KeyValuePair<int, float>>();
            _weightedRarityTable = new WeightedRandomCollection<KeyValuePair<ItemRarity, float>>();
            _weightedUniqueTable = new WeightedRandomCollection<LegendaryInfo>();

            ItemSets.Clear();
            LootTables.Clear();
            CreatureOverrides.Clear();
            _aliasKeys.Clear();
            _warnedLootTables.Clear();
            _warnedMissingSocketCounts.Clear();
            _warnedInvalidSocketCounts.Clear();
            _warnedMissingEffectCounts.Clear();
            _warnedInvalidEffectCounts.Clear();
          
            AddItemSets(lootConfig.ItemSets);
            AddLootTables(lootConfig.LootTables);
            EliteRunestoneTables.OnLootTablesLoaded();

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

            // The config file and patches are converted before they get here; this is what catches a
            // flat table handed in through the API or edited into the file while the game runs.
            if (LootTableMigration.Normalize(lootTable) && _warnedLootTables.Add($"flat:{key}"))
            {
                EpicLoot.LogWarningForce($"Loot table '{key}' uses the deprecated flat Drops/Loot form. " +
                    "It was converted to LeveledLoot level 1; please update it to LeveledLoot.");
            }

            if (!LootTables.ContainsKey(key))
            {
                LootTables.Add(key, new List<LootTable>());
            }

            var refKey = lootTable.RefObject;
            if (string.IsNullOrEmpty(refKey))
            {
                // A table of the key's own (an API table for a creature that also has a RefObject entry,
                // such as one the creature sorter wrote) replaces the alias. Appended, it would join the
                // template's shared list and roll for every creature on that template.
                if (_aliasKeys.Remove(key))
                {
                    LootTables[key] = new List<LootTable>();
                    CreatureOverrides.Remove(key);
                }

                LootTables[key].Add(lootTable);
            }
            else
            {
                if (!LootTables.ContainsKey(refKey))
                {
                    EpicLoot.LogError($"Loot table '{key}' references missing RefObject '{refKey}'!");
                    return;
                }
                else
                {
                    LootTables[key] = LootTables[refKey];
                    _aliasKeys.Add(key);
                    if (lootTable.StarMultiplier != null || lootTable.Modifiers != null || lootTable.StarScaling != null)
                    {
                        CreatureOverrides[key] = lootTable;
                    }
                    else
                    {
                        CreatureOverrides.Remove(key);
                    }
                }
            }
        }

        /// <summary>The RefObject entry carrying an object's own loot tuning, or null when it has none.</summary>
        public static LootTable GetCreatureOverride(string objectName)
        {
            return objectName != null && CreatureOverrides.TryGetValue(objectName, out var entry) ? entry : null;
        }

        public static List<GameObject> RollLootTableAndSpawnObjects(List<LootTable> lootTables,
            int level, string objectName, Vector3 dropPoint)
        {
            return RollLootTableAndSpawnObjects(lootTables, level, objectName, dropPoint, true);
        }

        // allowBonusRolls false keeps Prosperity out of a roll that is itself a bonus (Lucky Loot's), so
        // the two loot multipliers never compound.
        public static List<GameObject> RollLootTableAndSpawnObjects(List<LootTable> lootTables,
            int level, string objectName, Vector3 dropPoint, bool allowBonusRolls)
        {
            var output = LootOutput.ForObjects();
            RollLootTableInternal(lootTables, level, objectName, dropPoint, output, allowBonusRolls);
            return output.Objects;
        }

        public static List<GameObject> RollLootTableAndSpawnObjects(LootTable lootTable,
            int level, string objectName, Vector3 dropPoint)
        {
            var output = LootOutput.ForObjects();
            RollLootTableInternal(lootTable, level, objectName, dropPoint, output);
            return output.Objects;
        }

        // Chests, treasure maps and gambles. Each drop is built as ItemData straight from its prefab (see
        // LootOutput), so no world object is spawned just to have its data cloned off it.
        public static List<ItemDrop.ItemData> RollLootTable(List<LootTable> lootTables,
            int level, string objectName, Vector3 dropPoint)
        {
            var output = LootOutput.ForItems();
            RollLootTableInternal(lootTables, level, objectName, dropPoint, output, true);
            return output.Items;
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

        private static void RollLootTableInternal(IEnumerable<LootTable> lootTables,
            int level, string objectName, Vector3 dropPoint, LootOutput output, bool allowBonusRolls)
        {
            var tables = lootTables.ToList();

            // Prosperity repeats the whole roll -- drop count, drop type and rarity all re-rolled -- exactly
            // as a second kill would. Cheat spawns and gambles asked for one specific result, and every
            // one of them sets CheatRollingItem, so AnyItemSpawnCheatsActive keeps them out.
            var passes = 1;
            if (allowBonusRolls && !AnyItemSpawnCheatsActive())
            {
                var bonusRolls = global::EpicLoot.Magic.MagicItemEffects.Prosperity.RollBonusRolls(dropPoint);
                if (bonusRolls > 0)
                {
                    EpicLoot.Log($"Prosperity: {bonusRolls} bonus loot rolls for {objectName}.");
                    passes += bonusRolls;
                }
            }

            for (var pass = 0; pass < passes; pass++)
            {
                foreach (var lootTable in tables)
                {
                    RollLootTableInternal(lootTable, level, objectName, dropPoint, output);
                }
            }
        }

        /// <summary>
        /// Where one roll's drops go. A creature kill or a console spawn wants live world objects. A
        /// chest, a treasure map or a gamble only wants the ItemData, and building that straight from the
        /// prefab, the way vanilla's DropTable fills a chest, skips instantiating a whole world object per
        /// drop (renderers, colliders, rigidbody, every component's Awake) only to clone its data and
        /// destroy it again.
        /// </summary>
        private sealed class LootOutput
        {
            public readonly List<GameObject> Objects;
            public readonly List<ItemDrop.ItemData> Items;

            private LootOutput(List<GameObject> objects, List<ItemDrop.ItemData> items)
            {
                Objects = objects;
                Items = items;
            }

            public static LootOutput ForObjects() => new LootOutput(new List<GameObject>(), null);

            public static LootOutput ForItems() => new LootOutput(null, new List<ItemDrop.ItemData>());

            /// <summary>
            /// Creates one drop of <paramref name="prefab"/> and returns its ItemData, or null when the
            /// prefab has no ItemDrop. <paramref name="itemDrop"/> is the spawned object's component, and
            /// null for a data-only drop, which has nothing to save to.
            /// </summary>
            public ItemDrop.ItemData Create(GameObject prefab, Vector3 dropPoint, out ItemDrop itemDrop)
            {
                if (Objects != null)
                {
                    var spawned = SpawnLootForDrop(prefab, dropPoint, true);
                    Objects.Add(spawned);
                    itemDrop = spawned.GetComponent<ItemDrop>();
                    return itemDrop != null ? itemDrop.m_itemData : null;
                }

                itemDrop = null;
                var itemData = CreateLootItemData(prefab);
                if (itemData != null)
                {
                    Items.Add(itemData);
                }

                return itemData;
            }
        }

        /// <summary>
        /// A drop's ItemData made from its prefab without instantiating it: the clone, m_dropPrefab and
        /// world level vanilla's DropTable gives chest contents, plus the ItemData half of what EpicLoot's
        /// ItemDrop.Awake postfixes do to a spawned drop, since no Awake runs here.
        /// </summary>
        internal static ItemDrop.ItemData CreateLootItemData(GameObject prefab)
        {
            var prefabDrop = prefab.GetComponent<ItemDrop>();
            if (prefabDrop == null || prefabDrop.m_itemData == null)
            {
                return null;
            }

            var itemData = prefabDrop.m_itemData.Clone();
            itemData.m_dropPrefab = prefab;
            // The prefab's copy keeps the world level it was built with, not this world's, and items of
            // different world levels never stack together.
            itemData.m_worldLevel = Game.m_worldLevel;
            itemData.InitializeCustomData();
            Crafting.ItemDrop_Awake_Patch.InitializeItemData(itemData, out _);
            return itemData;
        }

        public static bool AnyItemSpawnCheatsActive()
        {
            return CheatRollingItem || CheatDisableGating || CheatForceMagicEffect ||
                !string.IsNullOrEmpty(CheatForceUniqueID) ||
                CheatEffectCount > 0 || CheatEffectRarityTier > 0;
        }

        public static Dictionary<string,float> GetLootTableChances(Vector3 location, List<LootTable> LootTables)
        {
            Dictionary<string, float> results = new Dictionary<string, float>();

            foreach(LootTable lt in LootTables)
            {
                foreach(LootDrop ld in GetLevelOneLoot(lt))
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

            if (lootTables == null || lootTables.Count == 0)
            {
                // A biome/category combination with no tables used to divide by zero below and had
                // nothing to roll from regardless.
                EpicLoot.LogWarning("RollLootNoTableWithSpecifics called with no loot tables; returning no loot.");
                return results;
            }

            // This is effectively an estimate, but we will just keep rolling until we get the number of results we want if this is not enough
            int lootPerCategory = numResults / lootTables.Count;
            if (lootPerCategory < 1)
            {
                lootPerCategory = 1;
            }

            int previousResultCount = -1;
            int stalledPasses = 0;
            while (results.Count < numResults)
            {
                // A pass that produced nothing will not do better on the next spin with the same
                // tables -- bail after a few, instead of freezing the game on the main thread.
                if (results.Count == previousResultCount)
                {
                    stalledPasses++;
                    if (stalledPasses >= 3)
                    {
                        EpicLoot.LogWarningForce($"Loot roll stalled after {results.Count}/{numResults} results " +
                            $"({failures} failures); returning what was rolled. Check iteminfo/loottables for invalid items.");
                        break;
                    }
                }
                else
                {
                    stalledPasses = 0;
                }
                previousResultCount = results.Count;

                foreach (LootTable lt in lootTables.shuffleList())
                {
                    if (results.Count >= numResults)
                    {
                        break;
                    }

                    ItemRarity itemRollRarity = rarity;
                    if (luckUpgradesRarity == true)
                    {
                        // TODO: Expose this as a config?
                        // Most of the weight stays on the rolled rarity and the rest moves one tier up;
                        // the top tier has nowhere to go.
                        LootDrop lootdrop = new() { Rarity = new float[Rarities.Count] };
                        if (rarity == Rarities.Highest)
                        {
                            lootdrop.Rarity[(int)rarity] = 100;
                        }
                        else
                        {
                            lootdrop.Rarity[(int)rarity] = 100 - luckUpgradesRarityFactor;
                            lootdrop.Rarity[(int)rarity + 1] = luckUpgradesRarityFactor;
                        }
                        itemRollRarity = RollItemRarity(lootdrop, luckFactor);
                    }

                    LootDrop[] tableLoot = GetLevelOneLoot(lt);
                    List<LootDrop> looteqrare = new List<LootDrop>();
                    foreach(LootDrop ld in tableLoot)
                    {
                        looteqrare.Add(new LootDrop() { Item = ld.Item, Weight = ld.Weight, Rarity = [1] });
                    }

                    // Never ask a table for more than is still owed: several tables share one category,
                    // and a pass after a gating failure used to roll the full per-table count again,
                    // so an identify handed back more items than it was paid for.
                    _weightedLootTable.Setup(looteqrare.ToArray(), x => x.Weight);
                    List<LootDrop> selectedDrops = _weightedLootTable.Roll(Math.Min(lootPerCategory, numResults - results.Count));

                    EpicLoot.Log($"Available Loot ({tableLoot.Length}) for table: {lt.Object}");
                    foreach (LootDrop lootDrop in tableLoot)
                    {
                        string itemName = lootDrop?.Item ?? "Invalid/Null";
                        float weight = lootDrop?.Weight ?? -1;
                        EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarity} - Weight: {weight}");
                    }

                    EpicLoot.Log($"Selected Drops from: {lt.Object} - {selectedDrops.Count}");
                    foreach (LootDrop lootDrop in selectedDrops)
                    {
                        if (results.Count >= numResults)
                        {
                            break;
                        }

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

                        // Null when the entry resolved to nothing -- which now includes a denied prop. Guard
                        // before PrefabManager: Jotunn's GetPrefab starts with Dictionary.TryGetValue(name),
                        // which throws on a null key.
                        if (string.IsNullOrEmpty(gatedItemName))
                        {
                            failures += 1;
                            continue;
                        }

                        GameObject prefab = PrefabManager.Instance.GetPrefab(gatedItemName);
                        if (prefab == null)
                        {
                            failures += 1;
                            continue;
                        }

                        GameObject selectedPrefab = ObjectDB.instance.GetItemPrefab(gatedItemName);
                        if (selectedPrefab == null)
                        {
                            // The existence check above used PrefabManager -- a different registry --
                            // so ObjectDB can still miss the name.
                            failures += 1;
                            continue;
                        }

                        ItemDrop prefabItemDrop = selectedPrefab.GetComponent<ItemDrop>();
                        if (prefabItemDrop == null || !EpicLoot.CanBeMagicItem(prefabItemDrop.m_itemData))
                        {
                            failures += 1;
                            continue;
                        }

                        GameObject droppedItem;
                        bool previousForceDisableInit = ZNetView.m_forceDisableInit;
                        ZNetView.m_forceDisableInit = true;
                        try
                        {
                            droppedItem = Object.Instantiate(selectedPrefab, location, new Quaternion(0, 0, 0, 0));
                        }
                        finally
                        {
                            // Restore in a finally: a stranded true makes every later ZNetView.Awake
                            // skip registration for the rest of the session.
                            ZNetView.m_forceDisableInit = previousForceDisableInit;
                        }
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

                        var magicItem = RollMagicItem(itemRollRarity, itemDrop.m_itemData, luckFactor, powerLevelMod);

                        if (CheatForceMagicEffect)
                        {
                            AddDebugMagicEffects(magicItem);
                        }

                        API.WithChangeReason(API.ChangeReason.LootRoll, () => itemDrop.m_itemData.SaveMagicItem(magicItem));
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

        private static void RollLootTableInternal(LootTable lootTable,
            int level, string objectName, Vector3 dropPoint, LootOutput output)
        {
            if (lootTable == null || level <= 0 || string.IsNullOrEmpty(objectName))
            {
                return;
            }

            // A third-party caller may hand a flat table straight in, past AddLootTable.
            LootTableMigration.Normalize(lootTable);

            var luckFactor = GetLuckFactor(dropPoint);
            var plan = PlanRoll(lootTable, level, objectName);
            if (plan.DropsAnchor == null)
            {
                if (_warnedLootTables.Add($"nodrops:{lootTable.Object}:{plan.Scaling.EffectiveLevel}"))
                {
                    EpicLoot.LogWarning($"Loot table ({lootTable.Object}) has no Drops at or below level " +
                        $"{plan.Scaling.EffectiveLevel:0.##} (rolled for {objectName}); nothing drops.");
                }

                return;
            }

            var drops = LootScalingMath.ScaleDropChance(ToDropList(plan.DropsAnchor.Drops), plan.DropChanceAdd);
            if (EpicLoot.AlwaysDropCheat)
            {
                drops = drops.Where(x => x.Key > 0).ToList();
            }
            else
            {
                var dropRate = Mathf.Clamp(ELConfig.GlobalDropRateModifier.Value, 0, 4) * plan.DropRate;
                drops = LootScalingMath.ApplyDropRate(drops, dropRate);
            }

            _weightedDropCountTable.Setup(drops, dropPair => dropPair.Value);
            if (_weightedDropCountTable.TotalWeight <= 0)
            {
                return;
            }

            var dropCount = _weightedDropCountTable.Roll().Key;
            dropCount = LootScalingMath.AddBonusDrops(dropCount, plan.BonusDrops, Random.value,
                plan.Scaling.Scaling.MaxDrops);

            if (dropCount <= 0)
            {
                return;
            }

            var loot = plan.LootAnchor?.Loot ?? Array.Empty<LootDrop>();
            var lootSteps = plan.Scaling.LootSteps;

            // These listings build a string per entry (and re-weigh each one) whether or not the line is
            // written, so skip them entirely at the default log level.
            var logInfo = EpicLoot.IsLogEnabled(LogLevel.Info);
            if (logInfo)
            {
                EpicLoot.Log($"Available Loot ({loot.Length}) for table: {lootTable.Object} for level {level} " +
                    $"(effective {plan.Scaling.EffectiveLevel:0.##}, {lootSteps:0.##} past level {plan.LootAnchor?.Level})");
                foreach (var lootDrop in loot)
                {
                    var itemName = lootDrop?.Item ?? "Invalid/Null";
                    var rarity = lootDrop?.Rarity?.Length ?? -1;
                    var weight = LootScalingMath.ScaledWeight(lootDrop, lootSteps);
                    EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarity} - Weight: {weight}");
                }
            }

            _weightedLootTable.Setup(loot, x => LootScalingMath.ScaledWeight(x, lootSteps));
            if (_weightedLootTable.TotalWeight <= 0)
            {
                EpicLoot.Log($"Every loot entry of ({lootTable.Object}) weighs 0 at level {level}; nothing drops.");
                return;
            }

            var selectedDrops = _weightedLootTable.Roll(dropCount);

            if (logInfo)
            {
                EpicLoot.Log($"Selected Drops: {lootTable.Object} for level {level}");
                foreach (var lootDrop in selectedDrops)
                {
                    var itemName = !string.IsNullOrEmpty(lootDrop?.Item) ? lootDrop.Item : "Invalid Item Name";
                    var rarityLength = lootDrop?.Rarity?.Length != null ? lootDrop.Rarity.Length : -1;
                    EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarityLength} - Weight: {lootDrop.Weight}");
                }
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
                var lootDrop = ResolveLootDrop(ld, luckFactor, true, plan.Scaling);

                // A polluted on-disk loottables.json can still name a denied prop (LootDenyList). Skip it here,
                // before the drop-type switch, so it cannot come out as itself, as an unidentified stand-in,
                // or as sacrifice materials either.
                if (LootDenyList.IsDenied(lootDrop?.Item))
                {
                    EpicLoot.Log($"Skipping denied prop item {lootDrop.Item} for ({objectName}).");
                    continue;
                }

                var itemName = !string.IsNullOrEmpty(lootDrop?.Item) ? lootDrop.Item : "Invalid Item Name";
                var rarityLength = lootDrop?.Rarity?.Length != null ? lootDrop.Rarity.Length : -1;
                EpicLoot.Log($"Item: {itemName} - Rarity Count: {rarityLength} - Weight: {lootDrop.Weight}");

                // A drop that is already a shard — rolled from an elite creature's bonus shard set or from a
                // boss's shard table — a runestone (Elite Runestone Drops), an enchanting material (a
                // rarity-keyed set such as EnchantingMats) or a Brokkr's Gift (the ShardSlotChisel set) must
                // not be re-rolled into a biome shard, nor sacrificed for materials: the table picked exactly
                // this item, often by its rarity. The unidentified category needs no such guard:
                // IsAllowedMagicItemType rejects a Material or a Misc item.
                var isShardDrop = lootDrop.Item != null &&
                    lootDrop.Item.EndsWith(global::EpicLoot.ShardStones.Shards.ShardIndicator, StringComparison.Ordinal);

                var dropType = SelectDropType(lootDrop, isShardDrop || IsKeptAsIsDrop(lootDrop), cheatsActive);
                EpicLoot.Log($"Drop type for {lootDrop.Item}: {dropType}");

                var spawned = false;
                switch (dropType)
                {
                    case LootDropType.ShardStone:
                        spawned = TrySpawnBiomeShard(lootDrop, ref dropPoint, luckFactor, output);
                        break;
                    case LootDropType.Unidentified:
                        spawned = TrySpawnUnidentified(lootDrop, ref dropPoint, luckFactor, output);
                        break;
                    case LootDropType.Materials:
                        spawned = TrySpawnMaterials(lootDrop, dropPoint, luckFactor, output);
                        break;
                }

                // Every substitute category can still fail late — a missing prefab, a rarity with no
                // sacrifice products — and each one warns before it does. Falling back to the item the
                // loot table actually named is what keeps a failure from silently eating the drop.
                if (!spawned)
                {
                    SpawnNormalItem(lootDrop, objectName, dropPoint, luckFactor, output);
                }
            }
        }

        // Rolls what a single drop becomes. The four Balance drop ratios are relative weights, not
        // independent chances, so only their proportions matter and any of them may be zeroed to remove
        // that category. Categories this particular drop cannot become are left out of the roll entirely
        // rather than rolled and then rejected — an ineligible category in the pool would silently eat
        // the drop's chance of becoming any of the others.
        // keepAsIs: the drop is a shard, a runestone, an enchanting material or a Brokkr's Gift, which stays
        // itself rather than becoming a biome shard or sacrifice materials.
        private static LootDropType SelectDropType(LootDrop lootDrop, bool keepAsIs, bool cheatsActive)
        {
            // Item spawn cheats asked for a specific thing; never substitute anything for it.
            if (cheatsActive)
            {
                return LootDropType.Item;
            }

            var candidates = new List<KeyValuePair<LootDropType, float>>();
            AddDropTypeCandidate(candidates, LootDropType.Item, ELConfig.ItemDropRatio.Value);

            if (!keepAsIs)
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

        // Runestones and the Dust/Essence/Reagent materials, i.e. what the enchanting table consumes, and
        // Brokkr's Gift. The gift is a Misc item, so without this the Misc sacrifice entry would turn it
        // into Dust and Reagent.
        private static bool IsKeptAsIsDrop(LootDrop lootDrop)
        {
            if (lootDrop.Item.IsNullOrWhiteSpace() || ObjectDB.instance == null)
            {
                return false;
            }

            var prefab = ObjectDB.instance.GetItemPrefab(lootDrop.Item);
            var itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return itemDrop != null &&
                (itemDrop.m_itemData.IsRunestone() || itemDrop.m_itemData.IsMagicCraftingMaterial() ||
                 itemDrop.m_itemData.IsShardSlotChisel());
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
            LootOutput output)
        {
            ZoneSystem.instance.GetGroundData(ref dropPoint, out var _, out var shardBiome, out var _, out var _);

            var shardSetName = $"ShardStone_{BiomeDataManager.GetName(shardBiome)}";
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
            var shardData = output.Create(shardPrefab, dropPoint, out var shardItemDrop);
            if (shardData == null)
            {
                return false;
            }

            // Identity already rides on the prefab's shared data and Awake restores the cosmetic MagicItem,
            // but stamping and saving here keeps the intent explicit and matches the unidentified path.
            // Both calls are idempotent.
            global::EpicLoot.ShardStones.Shards.EnsureShardMetadata(shardData);
            if (shardItemDrop != null)
            {
                shardItemDrop.Save();
            }

            return true;
        }

        // Spawns an unidentified item of the rolled rarity in place of the drop. The biome is the one
        // gating the item's own progression where that is known, falling back to the biome at the drop
        // point.
        private static bool TrySpawnUnidentified(LootDrop lootDrop, ref Vector3 dropPoint, float luckFactor,
            LootOutput output)
        {
            var rarity = RollItemRarity(lootDrop, luckFactor);

            // Determine which biome this item is a part of, and set the drop biome to that tier
            GatedItemTypeHelper.AllItemsWithDetails.TryGetValue(lootDrop.Item, out var itemDetails);
            var biomes = new List<Heightmap.Biome>();
            if (itemDetails != null)
            {
                foreach (string bosskey in itemDetails.RequiredBosses)
                {
                    Heightmap.Biome bossBiome = BiomeDataManager.GetFirstBiomeForBossKey(bosskey);
                    if (bossBiome != Heightmap.Biome.None)
                    {
                        biomes.Add(bossBiome);
                    }
                }
            }

            if (biomes.Count <= 0)
            {
                ZoneSystem.instance.GetGroundData(ref dropPoint, out var _, out var biome, out var _, out var _);
                biomes.Add(biome);
            }

            var selectBiome = BiomeDataManager.GetName(biomes.First());
            var prefab = ObjectDB.instance.GetItemPrefab($"{selectBiome}_{rarity}_Unidentified");
            if (prefab == null)
            {
                // Warn and drop the normal item instead
                EpicLoot.LogWarning($"Tried to spawn unidentified item for {selectBiome}_{rarity}_Unidentified " +
                    $"but prefab was not found! Dropping {lootDrop.Item} instead.");
                return false;
            }

            EpicLoot.Log($"Adding {rarity} unidentified item");
            var itemData = output.Create(prefab, dropPoint, out var itemDrop);
            if (itemData == null)
            {
                return false;
            }

            // Ensure that the unidentified item has the correct magic item data for the rarity
            itemData.SaveMagicItem(new MagicItem
            {
                Rarity = rarity,
                IsUnidentified = true,
            });

            // Persist the rarity/unidentified state into the ZDO so a real world drop survives reload.
            if (itemDrop != null)
            {
                itemDrop.Save();
            }

            return true;
        }

        // Replaces the drop with the magic crafting materials that item would yield if sacrificed. Returns
        // false when nothing could be spawned — an item with no sacrifice products for the rolled rarity
        // drops as itself rather than as nothing at all.
        private static bool TrySpawnMaterials(LootDrop lootDrop, Vector3 dropPoint, float luckFactor,
            LootOutput output)
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

                var materialData = output.Create(materialPrefab, dropPoint, out _);
                if (materialData == null)
                {
                    continue;
                }

                materialData.m_stack = itemAmountConfig.Amount;

                if (materialData.IsMagicCraftingMaterial())
                {
                    materialData.m_variant = EpicLoot.GetRarityIconIndex(rarity);
                }

                spawnedAny = true;
            }

            return spawnedAny;
        }

        // The default path: spawn the item the loot table named, gated by boss progression, and roll its
        // magic item data when the item is eligible for one.
        private static void SpawnNormalItem(LootDrop lootDrop, string objectName, Vector3 dropPoint,
            float luckFactor, LootOutput output)
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

            var itemData = output.Create(itemPrefab, dropPoint, out var itemDrop);

            if (itemData != null && EpicLoot.CanBeMagicItem(itemData) && !ArrayUtils.IsNullOrEmpty(lootDrop.Rarity))
            {
                var magicItem = RollMagicItem(lootDrop, itemData, luckFactor);

                if (CheatForceMagicEffect)
                {
                    AddDebugMagicEffects(magicItem);
                }

                API.WithChangeReason(API.ChangeReason.LootRoll, () => itemData.SaveMagicItem(magicItem));
                if (itemDrop != null)
                {
                    itemDrop.Save();
                }

                InitializeMagicItem(itemData);
                API.RaiseLootGenerated(itemData);
            }
        }

        public static GameObject SpawnLootForDrop(GameObject itemPrefab, Vector3 dropPoint, bool initializeObject)
        {
            Quaternion randomRotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);

            // Save and restore rather than assigning false: Instantiate runs the new object's Awake
            // chain (ItemDrop.Awake and our own postfixes), any of which can throw, and a
            // caller may already be inside another instantiate. m_forceDisableInit is a global that
            // ZNetView.Awake reads to decide whether to register a ZDO at all, so a stranded true makes
            // every later ZNetView awake unregistered, which strands null-ZDO entries in
            // ZNetScene.m_instances and NREs ZNetScene.RemoveObjects every frame for the session.
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
            return ResolveLootDrop(lootDrop, luckFactor, consumeRarityItems, null);
        }

        // scaling is the rolling table's star-scaling context, or null outside a table roll. The entry's
        // Rarity is promoted exactly once, by the context of whichever table supplied it: this one for
        // its own Rarity and for what an ItemSet hands down, a referenced table's own context for what a
        // "Table.Level" reference hands down. It happens before any RarityItems map is consumed, so the
        // rarity that picks the item is the promoted one.
        public static LootDrop ResolveLootDrop(LootDrop lootDrop, float luckFactor, bool consumeRarityItems,
            LootScalingContext scaling)
        {
            var result = new LootDrop
            {
                Item = lootDrop.Item,
                Rarity = ArrayUtils.Copy(lootDrop.Rarity),
                Weight = lootDrop.Weight,
                RarityItems = lootDrop.RarityItems
            };
            var context = scaling;
            if (context != null && !ArrayUtils.IsNullOrEmpty(result.Rarity))
            {
                result.Rarity = context.Promote(result.Rarity);
            }

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
                        if (context != null)
                        {
                            result.Rarity = context.Promote(result.Rarity);
                        }
                    }
                }
                else if (TryResolveTableReference(result.Item, context, out var lootList, out var referenceContext))
                {
                    if (lootList.Length == 0)
                    {
                        EpicLoot.LogError($"Tried to roll using loot table reference ({result.Item}) but its loot list was empty!");
                        break;
                    }
                    var referenceSteps = referenceContext.LootSteps;
                    _weightedLootTable.Setup(lootList, x => LootScalingMath.ScaledWeight(x, referenceSteps));
                    if (_weightedLootTable.TotalWeight <= 0)
                    {
                        EpicLoot.LogError($"Tried to roll using loot table reference ({result.Item}) but every entry weighs 0 there!");
                        break;
                    }
                    var referenceResult = _weightedLootTable.Roll();
                    result.Item = referenceResult.Item;
                    result.Weight = referenceResult.Weight;
                    result.RarityItems = referenceResult.RarityItems;
                    if (ArrayUtils.IsNullOrEmpty(result.Rarity))
                    {
                        result.Rarity = referenceContext.Promote(ArrayUtils.Copy(referenceResult.Rarity));
                    }

                    // Whatever the referenced entry resolves into next inherits under its table's scaling.
                    context = referenceContext;
                }
                else
                {
                    needsResolve = false;
                }
            }

            return result;
        }

        // "Table.N" rolls Table's loot list as a level N creature would; "Table.*" rolls it at the level
        // the referencing table is rolling at, so an elite template can defer its gear to the matching
        // normal template at every level with one entry. Either way the referenced table's own
        // StarScaling decides its item mix and rarity -- the rolling creature's star multiplier reaches it
        // only through the level "*" passes on.
        internal static bool TryResolveTableReference(string lootDropItem, LootScalingContext rolling,
            out LootDrop[] lootList, out LootScalingContext referenceContext)
        {
            lootList = null;
            referenceContext = null;
            var parts = lootDropItem.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var objectName = parts[0];
            var levelText = parts[1];
            float effectiveLevel;
            if (levelText == "*")
            {
                effectiveLevel = rolling?.EffectiveLevel ?? 1f;
            }
            else if (int.TryParse(levelText, out var level))
            {
                effectiveLevel = LootScalingMath.EffectiveLevel(level, GlobalStarLootScaling);
            }
            else
            {
                EpicLoot.LogError($"Tried to get a loot table reference from '{lootDropItem}' but could not parse the level value ({levelText})!");
                return false;
            }

            if (!LootTables.TryGetValue(objectName, out var tables))
            {
                return false;
            }

            var lootTable = tables.FirstOrDefault();
            if (lootTable == null)
            {
                EpicLoot.LogError($"UNLIKELY: LootTables contains entry for {objectName} but no valid loot tables! Weird!");
                return false;
            }

            LootTableMigration.Normalize(lootTable);
            var plan = PlanRollAt(lootTable, effectiveLevel, null, rolling?.RarityExtra ?? 0f);
            lootList = plan.LootAnchor?.Loot ?? Array.Empty<LootDrop>();
            referenceContext = plan.Scaling;
            return true;
        }

        // Stars count this many times over everywhere (the Balance "Star Loot Scaling" entry).
        private static float GlobalStarLootScaling =>
            ELConfig.StarLootScaling == null ? 1f : Mathf.Clamp(ELConfig.StarLootScaling.Value, 0f, 3f);

        /// <summary>
        /// Works out one table's roll for a creature: its effective level (stars times the creature's,
        /// the table's and the global multiplier), the anchor levels its Drops and Loot come from, and the
        /// scaling for the distance past them. objectName picks up the creature's own RefObject tuning.
        /// </summary>
        internal static LootRollPlan PlanRoll(LootTable lootTable, int level, string objectName)
        {
            var own = GetCreatureOverride(objectName);
            if (own == lootTable)
            {
                own = null;
            }

            var multiplier = (own?.StarMultiplier ?? 1f) * (lootTable.StarMultiplier ?? 1f) * GlobalStarLootScaling;
            return PlanRollAt(lootTable, LootScalingMath.EffectiveLevel(level, multiplier), own, 0f);
        }

        internal static LootRollPlan PlanRollAt(LootTable lootTable, float effectiveLevel, LootTable own,
            float inheritedRarityExtra)
        {
            var ownScaling = EliteRunestoneTables.IsEliteRunestoneTable(lootTable) ? null : own?.StarScaling;
            var scaling = LootScalingMath.MergeScaling(ownScaling, lootTable.StarScaling,
                Config?.DefaultStarScaling ?? LootScalingMath.CodeDefaultStarScaling);

            var dropsAnchor = LootScalingMath.FindAnchor(lootTable.LeveledLoot, effectiveLevel,
                def => !ArrayUtils.IsNullOrEmpty(def.Drops));
            var lootAnchor = LootScalingMath.FindAnchor(lootTable.LeveledLoot, effectiveLevel,
                def => !ArrayUtils.IsNullOrEmpty(def.Loot));
            var dropSteps = dropsAnchor == null ? 0f : LootScalingMath.Steps(effectiveLevel, dropsAnchor.Level);
            var lootSteps = lootAnchor == null ? 0f : LootScalingMath.Steps(effectiveLevel, lootAnchor.Level);

            return new LootRollPlan
            {
                DropsAnchor = dropsAnchor,
                LootAnchor = lootAnchor,
                DropChanceAdd = scaling.DropChance * dropSteps,
                DropRate = (own?.Modifiers?.DropRate ?? 1f) * (lootTable.Modifiers?.DropRate ?? 1f),
                BonusDrops = (own?.Modifiers?.BonusDrops ?? 0f) + (lootTable.Modifiers?.BonusDrops ?? 0f) +
                    scaling.BonusDrops * dropSteps,
                Scaling = new LootScalingContext
                {
                    EffectiveLevel = effectiveLevel,
                    LootSteps = lootSteps,
                    RarityExtra = inheritedRarityExtra + (own?.Modifiers?.RarityShift ?? 0f) +
                        (lootTable.Modifiers?.RarityShift ?? 0f),
                    Scaling = scaling
                }
            };
        }

        public static MagicItem RollMagicItem(LootDrop lootDrop, ItemDrop.ItemData baseItem, float luckFactor, float powerlevelMod = 1f)
        {
            var rarity = RollItemRarity(lootDrop, luckFactor);
            return RollMagicItem(rarity, baseItem, luckFactor, powerlevelMod);
        }

        public static MagicItem RollMagicItem(ItemRarity rarity, ItemDrop.ItemData baseItem, float luckFactor, float powerlevelMod = 1f)
        {
            LegendaryInfo forcedUnique = null;
            if (!string.IsNullOrEmpty(CheatForceUniqueID))
            {
                UniqueLegendaryHelper.TryGetLegendaryInfo(CheatForceUniqueID, out forcedUnique);
            }

            // Every rolled value is multiplied by this, so a mis-authored upgrade value (0 or negative
            // in enchantingupgrades.json) would otherwise zero out the whole item.
            if (float.IsNaN(powerlevelMod) || powerlevelMod <= 0f)
            {
                powerlevelMod = 1f;
            }

            if (forcedUnique != null && CheatForceUniqueRarity.HasValue)
            {
                rarity = CheatForceUniqueRarity.Value;
            }

            var magicItem = new MagicItem { Rarity = rarity };

            magicItem.SocketCount = GatedItemTypeHelper.IsSocketable(baseItem)
                ? (CheatSocketCount >= 0 ? CheatSocketCount : RollSocketCountPerRarity(magicItem.Rarity))
                : 0;

            var effectCount = CheatEffectCount >= 1 ? CheatEffectCount : RollEffectCountPerRarity(magicItem.Rarity);

            // Uniques and set pieces roll at whichever rarities legendaries.json enables them for, not only
            // Legendary and Mythic. A rarity nothing is enabled at spends no extra random rolls.
            LegendaryInfo itemInfo = forcedUnique ?? RollUniqueInfo(rarity, baseItem, magicItem);
            if (itemInfo != null)
            {
                magicItem.SetID = UniqueLegendaryHelper.GetSetForLegendaryItem(itemInfo);

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
                        if (!MagicItemEffectDefinitions.TryGet(guaranteedMagicEffect.Type, out var effectDef))
                        {
                            EpicLoot.LogError($"Could not find magic effect (Type={guaranteedMagicEffect.Type}) " +
                                $"while creating legendary/mythic item (ID={itemInfo.ID})");
                            continue;
                        }

                        var effect = RollEffect(effectDef, rarity,
                            UniqueLegendaryHelper.ResolveValues(guaranteedMagicEffect, rarity), powerlevelMod);
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

                _weightedEffectTable.Setup(NarrowToCheatRarity(availableEffects, i), x => x.SelectionWeight);
                var effectDef = _weightedEffectTable.Roll();

                // Same power scaling as the guaranteed effects above. Only identification passes
                // anything but 1 (the Sacrifice upgrade's "identified item power").
                var effect = RollEffect(effectDef, magicItem.Rarity, null, powerlevelMod);
                magicItem.Effects.Add(effect);
            }

            if (string.IsNullOrEmpty(magicItem.DisplayName))
            {
                magicItem.DisplayName = MagicItemNames.GetNameForItem(baseItem, magicItem);
            }

            return magicItem;
        }

        /// <summary>
        /// Picks the unique a fresh <paramref name="rarity"/> item becomes, or null for a plain item. With
        /// probability SetItemDropChance it tries a set piece first; when no set piece enabled at this
        /// rarity fits the item it falls through to the regular unique roll rather than making the item
        /// generic. The regular roll includes <see cref="UniqueLegendaryHelper.GenericLegendaryInfo"/>.
        /// </summary>
        private static LegendaryInfo RollUniqueInfo(ItemRarity rarity, ItemDrop.ItemData baseItem, MagicItem magicItem)
        {
            if (UniqueLegendaryHelper.AnySetEnabledAt(rarity))
            {
                var roll = Random.Range(0.0f, 1.0f);
                var rollSetItem = roll < ELConfig.SetItemDropChance.Value;
                EpicLoot.Log($"Rolling {rarity} unique: set={rollSetItem} ({roll:#.##}/{ELConfig.SetItemDropChance.Value})");
                if (rollSetItem)
                {
                    var availablePieces = UniqueLegendaryHelper.GetAvailableSetPieces(baseItem, magicItem, rarity);
                    if (availablePieces.Count > 0)
                    {
                        EpicLoot.Log($"Available {rarity} set pieces: {string.Join(", ", availablePieces.Select(x => x.ID))}");
                        _weightedUniqueTable.Setup(availablePieces, x => x.SelectionWeight);
                        return _weightedUniqueTable.Roll();
                    }

                    EpicLoot.Log($"No {rarity} set piece fits {baseItem.m_shared.m_name}; rolling a regular unique instead.");
                }
            }

            if (!UniqueLegendaryHelper.AnyUniqueEnabledAt(rarity))
            {
                return null;
            }

            var availableUniques = UniqueLegendaryHelper.GetAvailableUniques(baseItem, magicItem, rarity);
            EpicLoot.Log($"Available {rarity} uniques: {string.Join(", ", availableUniques.Select(x => x.ID))}");
            _weightedUniqueTable.Setup(availableUniques, x => x.SelectionWeight);
            return _weightedUniqueTable.Roll();
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
            if (countPercents.Count == 0)
            {
                return 0;
            }

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

                // Nothing else bounds this value and ShardUI sizes its slot row straight from the
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

        // The most shard slots an item of this rarity can hold: the highest count with a non-zero weight
        // in its SocketCounts row. Zero-weight entries are skipped -- a count that can never roll is not
        // a cap. Brokkr's Gift is bounded by this, so a config change to SocketCounts moves the ceiling
        // for existing items immediately, with nothing persisted.
        public static int GetMaxSocketCountForRarity(ItemRarity rarity)
        {
            var max = 0;
            foreach (var entry in GetSocketCountsPerRarity(rarity))
            {
                if (entry.Value > 0 && entry.Key > max)
                {
                    max = entry.Key;
                }
            }

            // GetSocketCountsPerRarity already drops out-of-range entries, but clamp anyway: this value
            // sizes the socket UI row, and nothing else bounds MagicItem.SocketCount itself.
            return Mathf.Min(max, MaxSocketCount);
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
                case ItemRarity.Ancient: return socketCounts.Ancient;
                default: throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
        }

        private static float[][] GetConfiguredEffectCounts(ItemRarity rarity)
        {
            var effectCounts = Config?.MagicEffectsCount;
            if (effectCounts == null)
            {
                return null;
            }

            switch (rarity)
            {
                case ItemRarity.Magic: return effectCounts.Magic;
                case ItemRarity.Rare: return effectCounts.Rare;
                case ItemRarity.Epic: return effectCounts.Epic;
                case ItemRarity.Legendary: return effectCounts.Legendary;
                case ItemRarity.Mythic: return effectCounts.Mythic;
                case ItemRarity.Ancient: return effectCounts.Ancient;
                default: throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
        }

        public static List<KeyValuePair<int, float>> GetEffectCountsPerRarity(ItemRarity rarity, bool useEnchantingUpgrades)
        {
            var configured = GetConfiguredEffectCounts(rarity);
            if (ArrayUtils.IsNullOrEmpty(configured))
            {
                // A loottables.json written before this rarity existed keeps winning over the embedded
                // default (see FilePatching.LoadPatchedJSON), and this runs mid-roll on a drop that has
                // already picked its rarity -- so fall back rather than throwing the whole roll away and
                // handing out an unenchanted item.
                if (_warnedMissingEffectCounts.Add(rarity))
                {
                    EpicLoot.LogWarning($"loottables.json has no MagicEffectsCount entry for {rarity}, " +
                        $"using the built-in default distribution. Accept the config update prompt on " +
                        $"startup, or add a \"{rarity}\" row to the \"MagicEffectsCount\" block in " +
                        $"loottables.json, to configure it.");
                }

                configured = DefaultMagicEffectsCount[rarity];
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

                // No upper bound: RollMagicItem stops once the item's eligible effect pool is used up, so
                // a count larger than the pool just yields every effect that pool can give. A negative
                // weight is dropped, since WeightedRandomCollection would quietly skew the whole table.
                var count = (int)entry[0];
                if (count < 0 || entry[1] < 0)
                {
                    droppedEntry = true;
                    continue;
                }

                result.Add(new KeyValuePair<int, float>(count, entry[1]));
            }

            if (droppedEntry && _warnedInvalidEffectCounts.Add(rarity))
            {
                EpicLoot.LogWarning($"MagicEffectsCount entries for {rarity} in loottables.json were " +
                    $"ignored: each entry must be [count, weight] with a count of 0 or more and a " +
                    $"weight of 0 or more.");
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

        // Testing aid for the rarity flare: drops everything below the cheat tier out of the pool so a
        // rare effect rolls on demand. Falls back to the full pool, loudly, when nothing qualifies --
        // the eligible set is already narrowed by item type and rarity, so asking for 3 stars on a slot
        // that has none would otherwise leave an empty table and break the roll.
        private static List<MagicItemEffectDefinition> NarrowToCheatRarity(
            List<MagicItemEffectDefinition> availableEffects, int effectIndex)
        {
            if (CheatEffectRarityTier <= 0 || (CheatEffectRarityFirstOnly && effectIndex > 0))
            {
                return availableEffects;
            }

            var narrowed = availableEffects
                .Where(x => MagicEffectRarity.GetTier(x) >= CheatEffectRarityTier)
                .ToList();

            if (narrowed.Count > 0)
            {
                return narrowed;
            }

            EpicLoot.LogWarningForce($"[cheatrarity] no available effect reaches {CheatEffectRarityTier} " +
                $"star(s) for this item; rolling from the normal pool of {availableEffects.Count} instead");
            return availableEffects;
        }

        public static List<MagicItemEffect> RollEffects(List<MagicItemEffectDefinition> availableEffects,
            ItemRarity itemRarity, int count, bool removeOnSelect = true)
        {
            var results = new List<MagicItemEffect>();

            _weightedEffectTable.Setup(NarrowToCheatRarity(availableEffects, 0), x => x.SelectionWeight, removeOnSelect);
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

        internal static float[] GetSingleRarityWeights(ItemRarity rarity)
        {
            var weights = new float[Rarities.Count];
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
            // Positional: index N is the weight of rarity ordinal N. A shorter array leaves the higher
            // rarities at 0, which is what keeps every pre-existing five-entry table valid.
            var rarityWeights = new Dictionary<ItemRarity, float>();
            foreach (ItemRarity itemRarity in Rarities.All)
            {
                var index = (int)itemRarity;
                rarityWeights[itemRarity] = rarity.Length > index ? rarity[index] : 0;
            }

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
            LootTable lootTableFromRefs = LootTable.Simple(name, [[1, 1]], setResults.ToArray());

            results.Add(lootTableFromRefs);

            EpicLoot.Log($"GetFullyResolvedLootTable({name}) found {results.Count} loot tables");
            return results;
        }

        private static bool CheckForSet(string lootdrop, List<LootDrop> current_results, out List<LootDrop> results)
        {
            return CheckForSet(lootdrop, current_results, out results, null);
        }

        // Returns true when the name WAS an item set (its members were expanded into results);
        // callers add the entry itself only on false. The old version returned false
        // unconditionally, so set names leaked into resolved loot lists as if they were items.
        private static bool CheckForSet(string lootdrop, List<LootDrop> current_results,
            out List<LootDrop> results, HashSet<string> visited)
        {
            results = current_results;
            if (!ItemSets.TryGetValue(lootdrop, out LootItemSet lootset))
            {
                return false;
            }

            visited ??= new HashSet<string>();
            if (!visited.Add(lootdrop))
            {
                // A set referencing itself (directly or via a cycle) used to recurse forever.
                EpicLoot.LogWarningForce($"Item set '{lootdrop}' references itself (directly or via a cycle); skipping the repeated expansion.");
                return true;
            }

            foreach (LootDrop ld in lootset.Loot)
            {
                if (!CheckForSet(ld.Item, current_results, out results, visited))
                {
                    results.Add(ld);
                }
            }

            return true;
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

        // The authored Drops a level falls back to (the highest level at or below it that has any), without
        // star scaling. useNextHighestIfNotPresent: false asks for that exact level only.
        public static List<KeyValuePair<int, float>> GetDropsForLevel([NotNull] LootTable lootTable,
            int level, bool useNextHighestIfNotPresent = true)
        {
            var found = FindLevel(lootTable, level, useNextHighestIfNotPresent, def => !ArrayUtils.IsNullOrEmpty(def.Drops));
            if (found != null)
            {
                return ToDropList(found.Drops);
            }

            if (useNextHighestIfNotPresent)
            {
                EpicLoot.LogError($"Could not find any leveled drops for ({lootTable.Object} lvl {level}), " +
                    $"but a loot table exists for this object!");
            }

            return null;
        }

        private static LeveledLootDef FindLevel(LootTable lootTable, int level, bool orBelow,
            Func<LeveledLootDef, bool> has)
        {
            LootTableMigration.Normalize(lootTable);
            if (orBelow)
            {
                return LootScalingMath.FindAnchor(lootTable.LeveledLoot, level, has);
            }

            return lootTable.LeveledLoot?.Find(def => def != null && def.Level == level && has(def));
        }

        // The loot list a level-1 roll of this table draws from, or an empty one. Quiet on purpose: the
        // identify and gating paths ask this of every table in a category.
        internal static LootDrop[] GetLevelOneLoot(LootTable lootTable)
        {
            return FindLevel(lootTable, 1, true, def => !ArrayUtils.IsNullOrEmpty(def.Loot))?.Loot
                ?? Array.Empty<LootDrop>();
        }

        private static List<KeyValuePair<int, float>> ToDropList(float[][] drops)
        {
            return drops.Select(x => new KeyValuePair<int, float>((int) x[0], x[1])).ToList();
        }

        // The authored Loot list a level falls back to, as written: no WeightPerStar and no rarity
        // promotion. PlanRoll is what a real roll uses.
        public static LootDrop[] GetLootForLevel([NotNull] LootTable lootTable, int level,
            bool useNextHighestIfNotPresent = true)
        {
            var found = FindLevel(lootTable, level, useNextHighestIfNotPresent, def => !ArrayUtils.IsNullOrEmpty(def.Loot));
            if (found != null)
            {
                return found.Loot.ToArray();
            }

            if (useNextHighestIfNotPresent)
            {
                EpicLoot.LogError($"Could not find any leveled loot for ({lootTable.Object} lvl {level}), " +
                    $"but a loot table exists for this object!");
            }

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

            // The effect being replaced is left out of the candidate checks, valueless or not; GetAvailableEffects
            // only offers it back when it has a value to re-roll.
            var availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(item, magicItem, effectIndex);

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
                    // Count every retry (a null re-roll included), so an exhausted pool can never
                    // spin this loop forever.
                    fallbackAttempts++;
                    MagicItemEffect nmieffect = RollEffects(availableEffects, rarity, 1, true).FirstOrDefault();
                    if (nmieffect == null)
                    {
                        continue;
                    }

                    newEffect = nmieffect;
                }

                if (newEffect == null)
                {
                    // Nothing left to offer for this choice slot (exhausted effect pool).
                    break;
                }

                results.Add(newEffect);
                currentEffectTypes.Add(newEffect.EffectType);
                var newEffectIsValueless = MagicItemEffectDefinitions.IsValuelessEffect(newEffect.EffectType);
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

        // Player-ZDO key holding the local player's Luck, mirrored by Multiplayer_Player_Patch.UpdateRichesAndLuck.
        // A float: diminishing shard stacking gives fractional totals, which the old int key "el-luk" truncated.
        public const string LuckZdoValueKey = "el-luck";

        public static float GetLuckFactor(Vector3 fromPoint)
        {
            var luckFactor = EpicLoot.GetWorldLuckFactor();
            var players = new List<Player>();
            Player.GetPlayersInRange(fromPoint, 100f, players);

            foreach (var player in players)
            {
                var zdo = player?.m_nview?.GetZDO();
                if (zdo != null)
                {
                    luckFactor += zdo.GetFloat(LuckZdoValueKey) * 0.01f;
                }
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
                    Debug.LogWarning($"{index++}: {player?.m_name}: {player?.m_nview?.GetZDO()?.GetFloat(LuckZdoValueKey)}");
                }
            }
        }

        public static Dictionary<ItemRarity, float> ModifyRarityByLuck(
            IReadOnlyDictionary<ItemRarity, float> rarityWeights, float luckFactor = 0)
        {
            var results = new Dictionary<ItemRarity, float>();
            for (var rarity = ItemRarity.Magic; rarity <= Rarities.Highest; rarity++)
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
                case ItemRarity.Ancient: return 1.2f;
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
                lootDrop.Rarity = GetSingleRarityWeights(ItemRarity.Magic);
                EpicLoot.LogWarning($"No rarity table was found for {loot_info.Value[0]} using default: 100% Magic");
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
