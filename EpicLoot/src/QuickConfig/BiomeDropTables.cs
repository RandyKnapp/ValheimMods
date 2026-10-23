using EpicLoot.Biomes;
using EpicLoot.Magic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace EpicLoot.QuickConfig;

/// <summary>
/// One editable drop target of loottables.json: a tier template or boss at one creature level, or a
/// chest. Amount is the Drops table (how many items drop, count:weight), Rarity the per-rarity weights
/// the target's Loot entries carry (BiomeDropTables.RarityTargets says which), one entry per rarity
/// (index = rarity). Either is null when the file has none. Identity is Object + Occurrence (a boss's
/// item table and shard table share the Object name) + Level.
/// </summary>
internal sealed class BiomeDropRow {
    internal string Biome = BiomeDropTables.Other;
    internal string Object = "";
    internal int Occurrence;
    internal int? Level;
    internal string Label = "";
    internal List<WeightEntry> Amount;
    internal List<WeightEntry> Rarity;
    /// <summary>True when the entries the row covers carry different Rarity arrays; the first is shown.</summary>
    internal bool Mixed;
    /// <summary>Length of the first Rarity array in the file (5 or 6), so a write can keep a 5-wide file 5-wide.</summary>
    internal int RarityLength;

    internal string Id => $"{Object}#{Occurrence}@{(Level.HasValue ? Level.Value.ToString(CultureInfo.InvariantCulture) : "-")}";

    internal BiomeDropRow Clone() => new BiomeDropRow {
        Biome = Biome, Object = Object, Occurrence = Occurrence, Level = Level, Label = Label,
        Amount = WeightTable.Clone(Amount), Rarity = WeightTable.Clone(Rarity), Mixed = Mixed, RarityLength = RarityLength
    };

    internal static bool Same(BiomeDropRow a, BiomeDropRow b) {
        return a.Id == b.Id && WeightTable.Same(a.Amount, b.Amount) && WeightTable.Same(a.Rarity, b.Rarity);
    }
}

/// <summary>
/// Reads loottables.json's LootTables into BiomeDropRows grouped by biome, and the text forms the rows
/// use. Grouping: tier templates through itemsorter.json's BiomeSorterData (a biome owns Tier{N}Mob and
/// Tier{N}EliteMob for its Tier), bosses through the biome registry's boss keys, chests through a name
/// table; anything else lands under "Other" so nothing is hidden.
/// </summary>
internal static class BiomeDropTables {
    internal const string Other = "Other";
    private const int LabelCreatureNames = 4;

    private static readonly Regex TierTemplate = new Regex(@"^Tier(\d+)(Elite)?Mob$", RegexOptions.Compiled);

    // Explicit chest names first (lowercase substrings); after these, TreasureMapChest_<Biome> and any
    // registry biome name found inside the object name.
    private static readonly (string Needle, string Biome)[] ChestBiomes = {
        ("forestcrypt", "BlackForest"), ("fcrypt", "BlackForest"), ("trollcave", "BlackForest"), ("shipwreck_karve_chest", "BlackForest"),
        ("sunkencrypt", "Swamp"), ("mountaincave", "Mountain"), ("heath", "Plains"), ("plains_stone", "Plains"),
        ("dvergr", "Mistlands"), ("charredfortress", "AshLands"), ("ashland_stone", "AshLands"),
        ("deepnorth_village", "DeepNorth"), ("memorial_buried", "DeepNorth"), ("morkhalla_", "DeepNorth")
    };

    // ------------------------------------------------------------------------------------------------
    //  Reading
    // ------------------------------------------------------------------------------------------------

    /// <summary>The rows for the live loot config, grouped by biome in progression order then Other; null when not loaded.</summary>
    internal static List<BiomeDropRow> Read() => Read(LootRoller.Config);

    /// <summary>The rows of any loot config (the shipped default, for a page reset); its tables must be leveled.</summary>
    internal static List<BiomeDropRow> Read(LootConfig config) {
        LootTable[] tables = config?.LootTables;
        if (tables == null) { return null; }

        // Creatures that reference a template give the template its label.
        Dictionary<string, List<string>> references = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (LootTable table in tables) {
            if (table == null || string.IsNullOrEmpty(table.Object) || string.IsNullOrEmpty(table.RefObject)) { continue; }
            if (references.TryGetValue(table.RefObject, out List<string> list) == false) {
                list = new List<string>();
                references[table.RefObject] = list;
            }
            list.Add(table.Object);
        }

        HashSet<string> keyedSets = RarityKeyedSets(config.ItemSets);
        List<BiomeDropRow> rows = new List<BiomeDropRow>();
        Dictionary<string, int> occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (LootTable table in tables) {
            if (table == null || string.IsNullOrEmpty(table.Object) || string.IsNullOrEmpty(table.RefObject) == false) { continue; }
            // Every table is LeveledLoot by the time it is loaded (LootTableMigration).
            if (table.LeveledLoot == null || table.LeveledLoot.Count == 0) { continue; }

            occurrences.TryGetValue(table.Object, out int occurrence);
            occurrences[table.Object] = occurrence + 1;
            references.TryGetValue(table.Object, out List<string> creatures);
            string biome = GroupOf(table.Object, creatures);
            string suffix = OccurrenceSuffix(table, occurrence);

            // A single-level table (chests, and templates collapsed onto one anchor) needs no level label.
            bool singleLevel = table.LeveledLoot.Count(def => def != null) == 1;
            foreach (LeveledLootDef def in table.LeveledLoot) {
                if (def == null) { continue; }
                string label = singleLevel
                    ? $"{table.Object}{suffix}{CreatureList(creatures)}"
                    : $"{table.Object} · level {def.Level}{suffix}{CreatureList(creatures)}";
                rows.Add(MakeRow(biome, table.Object, occurrence, def.Level, def.Drops, def.Loot, label, keyedSets));
            }
        }

        List<string> order = GroupOrder();
        return rows
            .Select((row, index) => (Row: row, Index: index))
            .OrderBy(pair => GroupIndex(order, pair.Row.Biome))
            .ThenBy(pair => pair.Index)
            .Select(pair => pair.Row)
            .ToList();
    }

    private static BiomeDropRow MakeRow(string biome, string obj, int occurrence, int? level, float[][] drops, LootDrop[] loot,
        string label, HashSet<string> keyedSets) {
        BiomeDropRow row = new BiomeDropRow {
            Biome = biome, Object = obj, Occurrence = occurrence, Level = level, Label = label,
            Amount = drops != null ? WeightTable.FromRows(drops) : null
        };
        float[] first = null;
        if (loot != null) {
            List<LootDrop> targets = RarityTargets(loot, entry => entry.Rarity != null,
                entry => IsRarityKeyed(entry.Item, entry.RarityItems != null && entry.RarityItems.Count > 0, keyedSets));
            foreach (LootDrop entry in targets) {
                if (first == null) {
                    first = entry.Rarity;
                    row.RarityLength = entry.Rarity.Length;
                } else if (Padded(entry.Rarity).SequenceEqual(Padded(first)) == false) {
                    row.Mixed = true;
                }
            }
        }
        row.Rarity = first != null ? WeightTable.FromWeights(first, Rarities.Count) : null;
        return row;
    }

    // A boss's second table with the same Object name is its shard table; anything else is numbered.
    private static string OccurrenceSuffix(LootTable table, int occurrence) {
        if (occurrence == 0) { return ""; }
        IEnumerable<LootDrop> loot = table.LeveledLoot != null
            ? table.LeveledLoot.Where(def => def?.Loot != null).SelectMany(def => def.Loot)
            : Array.Empty<LootDrop>();
        List<LootDrop> entries = loot.Where(entry => entry != null).ToList();
        bool shards = entries.Count > 0 && entries.All(entry => (entry.Item ?? "").IndexOf("Shard", StringComparison.OrdinalIgnoreCase) >= 0);
        return shards ? " (shards)" : $" #{occurrence + 1}";
    }

    private static string CreatureList(List<string> creatures) {
        if (creatures == null || creatures.Count == 0) { return ""; }
        string names = string.Join(", ", creatures.Take(LabelCreatureNames));
        if (creatures.Count > LabelCreatureNames) { names += $", +{creatures.Count - LabelCreatureNames} more"; }
        return $" ({names})";
    }

    // ------------------------------------------------------------------------------------------------
    //  Grouping
    // ------------------------------------------------------------------------------------------------

    /// <summary>Registry biome names in progression order, then Other.</summary>
    internal static List<string> GroupOrder() {
        List<string> order = new List<string>();
        try {
            foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder) {
                if (string.IsNullOrEmpty(definition?.Name) == false && order.Contains(definition.Name) == false) { order.Add(definition.Name); }
            }
        } catch (Exception) {
            // Registry not loaded: everything groups under Other.
        }
        order.Add(Other);
        return order;
    }

    private static int GroupIndex(List<string> order, string biome) {
        int index = order.IndexOf(biome);
        return index >= 0 ? index : order.Count;
    }

    internal static string GroupDisplayName(string group) {
        if (string.IsNullOrEmpty(group)) { return ""; }
        return group == Other ? Other : QuickConfigBindings.BiomeDisplayName(group);
    }

    private static string GroupOf(string obj, List<string> creatures) {
        string biome = CreatureBiome(creatures) ?? LadderBiome(obj) ?? TemplateBiome(obj) ?? BossBiome(obj) ?? ChestBiome(obj);
        return string.IsNullOrEmpty(biome) ? Other : biome;
    }

    // Where most of the creatures using a template live, as the creature sorter placed them on its last
    // run. Tier numbers are not biomes (Meadows creatures use Tier0Mob), so this beats any name rule.
    private static string CreatureBiome(List<string> creatures) {
        List<CreatureDecision> decisions = CreatureSorterRunner.LastDecisions;
        if (decisions == null || creatures == null || creatures.Count == 0) { return null; }
        HashSet<string> names = new HashSet<string>(creatures, StringComparer.Ordinal);
        string biome = decisions
            .Where(decision => decision.Biome != null && names.Contains(decision.Name))
            .GroupBy(decision => decision.Biome, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(group => group.Count())
            .Select(group => group.Key)
            .FirstOrDefault();
        return biome != null ? RegistryName(biome) ?? biome : null;
    }

    // The first biome, in progression order, whose itemsorter Creatures ladder uses the template as its
    // Normal rung, else at any rung.
    private static string LadderBiome(string obj) {
        Dictionary<string, AutoAddEnchantableItems.SortingData> sorter = AutoAddEnchantableItems.Config?.BiomeSorterData;
        if (sorter == null) { return null; }
        List<(string Biome, CreatureLadder Ladder)> ladders = new List<(string, CreatureLadder)>();
        foreach (KeyValuePair<string, AutoAddEnchantableItems.SortingData> pair in sorter) {
            string biome = pair.Value?.Creatures != null ? RegistryName(pair.Key) : null;
            if (biome != null) { ladders.Add((biome, pair.Value.Creatures)); }
        }
        List<string> order = GroupOrder();
        ladders = ladders.OrderBy(entry => GroupIndex(order, entry.Biome)).ToList();
        foreach ((string biome, CreatureLadder ladder) in ladders) {
            if (string.Equals(ladder.Normal, obj, StringComparison.Ordinal)) { return biome; }
        }
        foreach ((string biome, CreatureLadder ladder) in ladders) {
            foreach (CreatureClass rung in Enum.GetValues(typeof(CreatureClass))) {
                if (string.Equals(ladder.Get(rung), obj, StringComparison.Ordinal)) { return biome; }
            }
        }
        return null;
    }

    // Tier{N}Mob / Tier{N}EliteMob belong to the biome whose sorter entry names that tier.
    private static string TemplateBiome(string obj) {
        Match match = TierTemplate.Match(obj);
        if (match.Success == false) { return null; }
        string tier = "Tier" + match.Groups[1].Value;
        Dictionary<string, AutoAddEnchantableItems.SortingData> sorter = AutoAddEnchantableItems.Config?.BiomeSorterData;
        if (sorter == null) { return null; }
        foreach (KeyValuePair<string, AutoAddEnchantableItems.SortingData> pair in sorter) {
            if (pair.Value == null || string.Equals(pair.Value.Tier, tier, StringComparison.OrdinalIgnoreCase) == false) { continue; }
            return RegistryName(pair.Key);
        }
        return null;
    }

    // "defeated_gdking" and "gd_king" agree once the prefix, underscores and case are dropped.
    private static string BossBiome(string obj) {
        string normalizedObject = NormalizeKey(obj);
        if (normalizedObject.Length < 4) { return null; }
        try {
            foreach (string bossKey in BiomeDataManager.BossKeysInOrder) {
                string normalizedKey = NormalizeKey(bossKey);
                if (normalizedKey.Length < 4) { continue; }
                if (normalizedKey != normalizedObject && normalizedKey.Contains(normalizedObject) == false
                    && normalizedObject.Contains(normalizedKey) == false) { continue; }
                Heightmap.Biome biome = BiomeDataManager.GetFirstBiomeForBossKey(bossKey);
                if (biome != Heightmap.Biome.None && BiomeDataManager.IsKnown(biome)) { return BiomeDataManager.GetName(biome); }
            }
        } catch (Exception) {
            // Registry not loaded.
        }
        return null;
    }

    private static string NormalizeKey(string value) {
        string lower = (value ?? "").ToLowerInvariant();
        if (lower.StartsWith("defeated_", StringComparison.Ordinal)) { lower = lower.Substring("defeated_".Length); }
        return new string(lower.Where(char.IsLetterOrDigit).ToArray());
    }

    private static string ChestBiome(string obj) {
        string lower = obj.ToLowerInvariant();
        foreach ((string needle, string biome) in ChestBiomes) {
            if (lower.Contains(needle)) { return RegistryName(biome) ?? biome; }
        }
        const string mapChest = "TreasureMapChest_";
        if (obj.StartsWith(mapChest, StringComparison.OrdinalIgnoreCase)) {
            string name = obj.Substring(mapChest.Length);
            return RegistryName(name) ?? name;
        }
        try {
            foreach (BiomeDefinition definition in BiomeDataManager.BiomesInOrder) {
                if (string.IsNullOrEmpty(definition?.Name) == false && lower.Contains(definition.Name.ToLowerInvariant())) { return definition.Name; }
            }
        } catch (Exception) {
            // Registry not loaded.
        }
        return null;
    }

    // The registry's spelling of a biome name, or null when the registry does not know it.
    private static string RegistryName(string name) {
        try {
            if (BiomeDataManager.TryResolve(name, out Heightmap.Biome biome) && biome != Heightmap.Biome.None && BiomeDataManager.IsKnown(biome)) {
                return BiomeDataManager.GetName(biome);
            }
        } catch (Exception) {
            // Registry not loaded.
        }
        return null;
    }

    // ------------------------------------------------------------------------------------------------
    //  Rarity weights
    // ------------------------------------------------------------------------------------------------

    /// <summary>
    /// Which of a target's Loot entries its Rarity row covers: those carrying a Rarity array, less the
    /// rarity-keyed ones. A keyed entry's Rarity picks the item itself (a shardstone's grade, the tier
    /// of EnchantingMats), so taking the gear's spread would change what drops. A target holding only
    /// keyed entries (a boss's shard table) is all of them, so it stays editable. The read and the
    /// write both go through here so they always agree.
    /// </summary>
    internal static List<T> RarityTargets<T>(IEnumerable<T> entries, Func<T, bool> hasRarity, Func<T, bool> keyed) where T : class {
        List<T> carrying = entries.Where(entry => entry != null && hasRarity(entry)).ToList();
        List<T> plain = carrying.Where(entry => keyed(entry) == false).ToList();
        return plain.Count > 0 ? plain : carrying;
    }

    /// <summary>An entry with its own RarityItems map, or one naming a set whose members carry one.</summary>
    internal static bool IsRarityKeyed(string item, bool ownMap, HashSet<string> keyedSets) {
        return ownMap || (item != null && keyedSets.Contains(item));
    }

    /// <summary>The ItemSets with a member carrying a RarityItems map (EnchantingMats, the ShardT sets).</summary>
    internal static HashSet<string> RarityKeyedSets(IEnumerable<LootItemSet> sets) {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        foreach (LootItemSet set in sets ?? Enumerable.Empty<LootItemSet>()) {
            if (set?.Name == null || set.Loot == null) { continue; }
            if (set.Loot.Any(member => member?.RarityItems != null && member.RarityItems.Count > 0)) { names.Add(set.Name); }
        }
        return names;
    }

    /// <summary>The same, read from loottables.json's ItemSets array.</summary>
    internal static HashSet<string> RarityKeyedSets(JArray sets) {
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal);
        foreach (JObject set in sets?.OfType<JObject>() ?? Enumerable.Empty<JObject>()) {
            string name = (string)set["Name"];
            if (name == null || set["Loot"] is JArray loot == false) { continue; }
            if (loot.OfType<JObject>().Any(HasRarityItems)) { names.Add(name); }
        }
        return names;
    }

    internal static bool HasRarityItems(JObject entry) => entry["RarityItems"] is JObject map && map.HasValues;

    private static float[] Padded(float[] rarity) {
        float[] padded = new float[Rarities.Count];
        for (int i = 0; i < padded.Length && i < rarity.Length; i++) { padded[i] = rarity[i]; }
        return padded;
    }

    /// <summary>How many weights to write: the file's own width when it was narrower and the extra weights are all 0.</summary>
    internal static int WriteLength(float[] weights, int fileLength) {
        if (fileLength <= 0 || fileLength >= weights.Length) { return weights.Length; }
        for (int i = fileLength; i < weights.Length; i++) {
            if (weights[i] != 0f) { return weights.Length; }
        }
        return fileLength;
    }
}
