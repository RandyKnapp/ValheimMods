using EpicLoot_UnityLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace EpicLoot.QuickConfig;

/// <summary>
/// One row of a weighted table: a count (effects, sockets, items dropped) and its weight, or, in a
/// fixed-key table such as a loot entry's rarity weights, the key's index and its weight.
/// </summary>
internal sealed class WeightEntry {
    internal int Count;
    internal float Weight;

    internal WeightEntry Clone() => new WeightEntry { Count = Count, Weight = Weight };
}

/// <summary>
/// The panel's form of the weighted tables in loottables.json (MagicEffectsCount, SocketCounts, a loot
/// table's Drops and Rarity). Weights are percentages that should add up to 100: the sliders move freely,
/// a warning shows while a table is off, and Save rebalances it (<see cref="Rebalance"/>).
/// </summary>
internal static class WeightTable {
    internal const float Target = 100f;
    private const float Epsilon = 0.001f;

    internal static List<WeightEntry> FromRows(float[][] rows) {
        List<WeightEntry> entries = new List<WeightEntry>();
        if (rows == null) { return entries; }
        foreach (float[] row in rows) {
            if (row == null || row.Length < 2) { continue; }
            entries.Add(new WeightEntry { Count = (int)Math.Round(row[0]), Weight = row[1] });
        }
        return entries;
    }

    /// <summary>A fixed-key table: one entry per key, index as the count, padded with zeros to <paramref name="length"/>.</summary>
    internal static List<WeightEntry> FromWeights(float[] weights, int length) {
        List<WeightEntry> entries = new List<WeightEntry>();
        for (int i = 0; i < length; i++) {
            entries.Add(new WeightEntry { Count = i, Weight = weights != null && i < weights.Length ? weights[i] : 0f });
        }
        return entries;
    }

    /// <summary>[count, weight] rows in count order, zero weights left out (they never roll).</summary>
    internal static float[][] ToRows(List<WeightEntry> entries) {
        return entries
            .Where(entry => entry.Weight > 0f)
            .OrderBy(entry => entry.Count)
            .Select(entry => new[] { (float)entry.Count, entry.Weight })
            .ToArray();
    }

    internal static float[] ToWeights(List<WeightEntry> entries, int length) {
        float[] weights = new float[length];
        foreach (WeightEntry entry in entries) {
            if (entry.Count >= 0 && entry.Count < length) { weights[entry.Count] = entry.Weight; }
        }
        return weights;
    }

    internal static float Total(List<WeightEntry> entries) => entries?.Sum(entry => entry.Weight) ?? 0f;

    internal static bool IsBalanced(List<WeightEntry> entries) => Math.Abs(Total(entries) - Target) < Epsilon;

    /// <summary>The warning a list shows while its weights do not add up to 100, else null.</summary>
    internal static string TotalWarning(List<WeightEntry> entries) {
        if (entries == null || entries.Count == 0 || IsBalanced(entries)) { return null; }
        return $"Total {Total(entries).ToString("0.#", CultureInfo.InvariantCulture)}% (should be 100%)";
    }

    internal static List<WeightEntry> Clone(List<WeightEntry> entries) => entries?.Select(entry => entry.Clone()).ToList();

    /// <summary>Equal entries in the same order; a missing table and an empty one are the same.</summary>
    internal static bool Same(List<WeightEntry> a, List<WeightEntry> b) {
        if (ReferenceEquals(a, b)) { return true; }
        if ((a == null || a.Count == 0) && (b == null || b.Count == 0)) { return true; }
        if (a == null || b == null || a.Count != b.Count) { return false; }
        for (int i = 0; i < a.Count; i++) {
            if (a[i].Count != b[i].Count || Math.Abs(a[i].Weight - b[i].Weight) > Epsilon) { return false; }
        }
        return true;
    }

    /// <summary>
    /// The table as Save writes it: when the weights do not add up to 100, the ones the player changed
    /// (compared with <paramref name="baseline"/>, matched by count) keep their values and the ones left
    /// alone are scaled to fill the rest, so raising one weight lowers the others. When the changed ones
    /// already reach 100 the rest drop to 0, and when nothing was left alone the changed ones are scaled.
    /// Weights are whole numbers afterwards, adding up to exactly 100.
    /// </summary>
    internal static List<WeightEntry> Rebalance(List<WeightEntry> current, List<WeightEntry> baseline) {
        List<WeightEntry> result = Clone(current) ?? new List<WeightEntry>();
        if (Total(result) <= 0f || IsBalanced(result)) { return result; }

        Dictionary<int, float> before = new Dictionary<int, float>();
        foreach (WeightEntry entry in baseline ?? new List<WeightEntry>()) {
            if (before.ContainsKey(entry.Count) == false) { before[entry.Count] = entry.Weight; }
        }
        List<WeightEntry> changed = result
            .Where(entry => before.TryGetValue(entry.Count, out float was) == false || Math.Abs(was - entry.Weight) > Epsilon)
            .ToList();
        List<WeightEntry> untouched = result.Where(entry => changed.Contains(entry) == false).ToList();
        float changedTotal = Total(changed);
        float untouchedTotal = Total(untouched);

        if (changedTotal >= Target - Epsilon) {
            foreach (WeightEntry entry in untouched) { entry.Weight = 0f; }
            Scale(changed, Target);
        } else if (untouchedTotal > 0f) {
            Scale(untouched, Target - changedTotal);
            // The changed weights stay as typed; only a fractional one is rounded, into the untouched share.
            RoundInto(result);
        } else {
            Scale(changed, Target);
        }
        return result;
    }

    // Scales the entries to add up to target, as whole numbers (largest remainder), so the total is exact.
    private static void Scale(List<WeightEntry> entries, float target) {
        float total = Total(entries);
        if (entries.Count == 0 || total <= 0f) { return; }
        int whole = (int)Math.Round(target);
        double[] exact = entries.Select(entry => (double)entry.Weight * whole / total).ToArray();
        int[] floors = exact.Select(value => (int)Math.Floor(value)).ToArray();
        int remainder = whole - floors.Sum();
        foreach (int index in Enumerable.Range(0, entries.Count).OrderByDescending(i => exact[i] - floors[i]).Take(Math.Max(0, remainder))) {
            floors[index]++;
        }
        for (int i = 0; i < entries.Count; i++) { entries[i].Weight = floors[i]; }
    }

    // After scaling one part, makes every weight whole and the total exactly 100.
    private static void RoundInto(List<WeightEntry> entries) {
        if (entries.All(entry => Math.Abs(entry.Weight - Math.Round(entry.Weight)) < Epsilon) && IsBalanced(entries)) { return; }
        Scale(entries, Target);
    }

    /// <summary>An error for a table Save cannot write, else null.</summary>
    internal static string Validate(List<WeightEntry> entries, int minCount, int maxCount, string what) {
        if (entries == null || entries.Count == 0 || Total(entries) <= 0f) { return $"{what}: at least one weight must be above 0."; }
        HashSet<int> counts = new HashSet<int>();
        foreach (WeightEntry entry in entries) {
            if (entry.Count < minCount || entry.Count > maxCount) {
                return maxCount == int.MaxValue
                    ? $"{what}: count {entry.Count} must be {minCount} or more."
                    : $"{what}: count {entry.Count} must be between {minCount} and {maxCount}.";
            }
            if (counts.Add(entry.Count) == false) { return $"{what}: count {entry.Count} is listed twice."; }
            if (entry.Weight < 0f) { return $"{what}: weights must be 0 or more."; }
        }
        return null;
    }

    /// <summary>The lowest count from <paramref name="minCount"/> up that the table does not list yet, or -1.</summary>
    internal static int NextFreeCount(List<WeightEntry> entries, int minCount, int maxCount) {
        HashSet<int> used = new HashSet<int>(entries.Select(entry => entry.Count));
        for (int count = minCount; count <= maxCount && count - minCount < 1000; count++) {
            if (used.Contains(count) == false) { return count; }
        }
        return -1;
    }
}

/// <summary>
/// loottables.json MagicEffectsCount and SocketCounts, one weighted table per rarity each. Staged whole,
/// so switching the rarity the row shows never loses an edit made under another one.
/// </summary>
internal sealed class RarityCountsValue {
    internal readonly Dictionary<string, List<WeightEntry>> Effects = new Dictionary<string, List<WeightEntry>>(StringComparer.Ordinal);
    internal readonly Dictionary<string, List<WeightEntry>> Sockets = new Dictionary<string, List<WeightEntry>>(StringComparer.Ordinal);

    internal static List<WeightEntry> Get(Dictionary<string, List<WeightEntry>> tables, string rarity) {
        if (rarity == null) { return null; }
        if (tables.TryGetValue(rarity, out List<WeightEntry> entries) == false) {
            entries = new List<WeightEntry>();
            tables[rarity] = entries;
        }
        return entries;
    }

    internal RarityCountsValue Clone() {
        RarityCountsValue clone = new RarityCountsValue();
        foreach (KeyValuePair<string, List<WeightEntry>> pair in Effects) { clone.Effects[pair.Key] = WeightTable.Clone(pair.Value); }
        foreach (KeyValuePair<string, List<WeightEntry>> pair in Sockets) { clone.Sockets[pair.Key] = WeightTable.Clone(pair.Value); }
        return clone;
    }

    internal static bool Same(RarityCountsValue a, RarityCountsValue b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null) { return false; }
        return SameTables(a.Effects, b.Effects) && SameTables(a.Sockets, b.Sockets);
    }

    internal static bool SameTables(Dictionary<string, List<WeightEntry>> a, Dictionary<string, List<WeightEntry>> b) {
        if (a.Count != b.Count) { return false; }
        foreach (KeyValuePair<string, List<WeightEntry>> pair in a) {
            if (b.TryGetValue(pair.Key, out List<WeightEntry> other) == false || WeightTable.Same(pair.Value, other) == false) { return false; }
        }
        return true;
    }
}

/// <summary>One item of an enchanting table upgrade cost.</summary>
internal sealed class CostEntry {
    internal string Item = "";
    internal int Amount = 1;

    internal CostEntry Clone() => new CostEntry { Item = Item, Amount = Amount };
}

/// <summary>
/// enchantingupgrades.json UpgradeCosts: per feature, one cost list per level (index 0 unlocks the
/// feature). A level the file leaves null has no cost and stays null until an item is added to it.
/// </summary>
internal sealed class UpgradeCostsValue {
    internal readonly Dictionary<EnchantingFeature, List<List<CostEntry>>> Levels = new Dictionary<EnchantingFeature, List<List<CostEntry>>>();

    internal List<List<CostEntry>> Get(EnchantingFeature feature) {
        return Levels.TryGetValue(feature, out List<List<CostEntry>> levels) ? levels : null;
    }

    internal UpgradeCostsValue Clone() {
        UpgradeCostsValue clone = new UpgradeCostsValue();
        foreach (KeyValuePair<EnchantingFeature, List<List<CostEntry>>> pair in Levels) {
            clone.Levels[pair.Key] = pair.Value.Select(level => level?.Select(entry => entry.Clone()).ToList()).ToList();
        }
        return clone;
    }

    /// <summary>Equal items in the same order; a level with no cost list and an empty one are the same.</summary>
    internal static bool SameLevel(List<CostEntry> a, List<CostEntry> b) {
        if (ReferenceEquals(a, b)) { return true; }
        if ((a == null || a.Count == 0) && (b == null || b.Count == 0)) { return true; }
        if (a == null || b == null || a.Count != b.Count) { return false; }
        for (int i = 0; i < a.Count; i++) {
            if (a[i].Item != b[i].Item || a[i].Amount != b[i].Amount) { return false; }
        }
        return true;
    }

    internal static bool Same(UpgradeCostsValue a, UpgradeCostsValue b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null || a.Levels.Count != b.Levels.Count) { return false; }
        foreach (KeyValuePair<EnchantingFeature, List<List<CostEntry>>> pair in a.Levels) {
            List<List<CostEntry>> other = b.Get(pair.Key);
            if (other == null || other.Count != pair.Value.Count) { return false; }
            for (int i = 0; i < pair.Value.Count; i++) {
                if (SameLevel(pair.Value[i], other[i]) == false) { return false; }
            }
        }
        return true;
    }
}
