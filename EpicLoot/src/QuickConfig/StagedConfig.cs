using BepInEx.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.QuickConfig;

/// <summary>One biome of adventuredata.json TreasureMap.BiomeInfo: the map's coin cost (-1 = not offered) and forest-token cost.</summary>
internal sealed class BiomeCostEntry {
    internal string Biome = "";
    internal int Cost;
    internal int ForestTokens;

    internal BiomeCostEntry Clone() => new BiomeCostEntry { Biome = Biome, Cost = Cost, ForestTokens = ForestTokens };
}

/// <summary>One item of an iteminfo.json category, with the boss key it is gated behind ("none" = ungated).</summary>
internal sealed class ItemByBossEntry {
    internal string Boss = "none";
    internal string Item = "";

    internal ItemByBossEntry Clone() => new ItemByBossEntry { Boss = Boss, Item = Item };
}

/// <summary>
/// Every iteminfo.json category (ItemTypeInfo.Type, in file order) with its ItemsByBoss table
/// flattened to (boss, item) rows in key order. Staged whole, so switching the category a row shows
/// never loses an edit made under another one.
/// </summary>
internal sealed class ItemCategoriesValue {
    internal readonly List<string> Categories = new List<string>();
    internal readonly Dictionary<string, List<ItemByBossEntry>> Entries = new Dictionary<string, List<ItemByBossEntry>>(StringComparer.Ordinal);

    internal List<ItemByBossEntry> Get(string category) {
        return category != null && Entries.TryGetValue(category, out List<ItemByBossEntry> list) ? list : null;
    }

    internal ItemCategoriesValue Clone() {
        ItemCategoriesValue clone = new ItemCategoriesValue();
        clone.Categories.AddRange(Categories);
        foreach (KeyValuePair<string, List<ItemByBossEntry>> pair in Entries) {
            clone.Entries[pair.Key] = pair.Value.Select(entry => entry.Clone()).ToList();
        }
        return clone;
    }

    internal static bool SameList(List<ItemByBossEntry> a, List<ItemByBossEntry> b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null || a.Count != b.Count) { return false; }
        for (int i = 0; i < a.Count; i++) {
            if (a[i].Boss != b[i].Boss || a[i].Item != b[i].Item) { return false; }
        }
        return true;
    }

    internal static bool Same(ItemCategoriesValue a, ItemCategoriesValue b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null || a.Categories.Count != b.Categories.Count) { return false; }
        for (int i = 0; i < a.Categories.Count; i++) {
            string category = a.Categories[i];
            if (category != b.Categories[i] || SameList(a.Get(category), b.Get(category)) == false) { return false; }
        }
        return true;
    }
}

/// <summary>
/// One value the panel can stage. Every slot reads its live value on Snapshot; a .cfg slot also knows
/// how to write it back, a JSON slot knows how to mutate the on-disk JObject.
/// </summary>
internal abstract class ConfigSlot {
    internal string Key;

    /// <summary>The live value, or null when the source is not loaded (no effect definition, no config).</summary>
    internal abstract object ReadLive();

    /// <summary>
    /// What a page reset stages: the shipped default, as a new value built from <paramref name="current"/>
    /// where only part of it has a default (a loot table another mod added keeps its values). Null when
    /// the slot has no known default.
    /// </summary>
    internal abstract object DefaultFor(object current, ShippedDefaults shipped);
}

internal sealed class CfgSlot : ConfigSlot {
    /// <summary>The entry behind the slot; null only for a composite part that has no entry of its own.</summary>
    internal ConfigEntryBase Entry;
    internal Func<object> Read;
    internal Action<object> Write;
    /// <summary>The entry's default in the slot's own form, when Read transforms the value (a part, a normalized name).</summary>
    internal Func<object> ReadDefault;

    internal override object ReadLive() => Read();

    internal override object DefaultFor(object current, ShippedDefaults shipped) => ReadDefault != null ? ReadDefault() : Entry?.DefaultValue;
}

internal sealed class JsonSlot : ConfigSlot {
    /// <summary>The baseconfig file, with extension ("adventuredata.json").</summary>
    internal string File;
    internal Func<object> Read;
    /// <summary>The shipped default (current staged value, the embedded files) -> value; null when unknown.</summary>
    internal Func<object, ShippedDefaults, object> Default;
    internal Action<JObject, object> Write;
    /// <summary>Alternative to Write for values that must only touch the parts that changed: (root, staged, baseline).</summary>
    internal Action<JObject, object, object> WriteWithBaseline;
    /// <summary>Optional "does this file's part of the value differ" test (staged, baseline); ValuesEqual when null.</summary>
    internal Func<object, object, bool> Changed;

    internal bool CanWrite => Write != null || WriteWithBaseline != null;

    internal bool IsDirty(object staged, object baseline) {
        return Changed != null ? Changed(staged, baseline) : StagedConfig.ValuesEqual(staged, baseline) == false;
    }

    internal override object ReadLive() => Read();

    internal override object DefaultFor(object current, ShippedDefaults shipped) => Default?.Invoke(current, shipped);

    internal void Apply(JObject root, object staged, object baseline) {
        if (WriteWithBaseline != null) {
            WriteWithBaseline(root, staged, baseline);
        } else {
            Write?.Invoke(root, staged);
        }
    }
}

/// <summary>
/// What the pages edit, and what was live when the panel opened (or was last saved). Unsaved changes
/// are the difference between the two, so an edit that is put back is not an edit. Values are boxed
/// and keyed by the row key rule (see QuickConfigBindings): bool, float, int, string, a boxed enum,
/// or one of the table values (EffectConfigsValue, BountiesValue, ItemCategoriesValue, RarityCountsValue,
/// UpgradeCostsValue, the biome lists).
/// </summary>
internal sealed class StagedConfig {
    /// <summary>The baseconfig files the panel writes, in the order they are applied (magiceffects last).</summary>
    internal static readonly string[] JsonFileOrder = {
        "loottables.json", "shardstones.json", "adventuredata.json", "iteminfo.json", "enchantingupgrades.json", "magiceffects.json"
    };

    private readonly Dictionary<string, object> values = new Dictionary<string, object>(StringComparer.Ordinal);
    private readonly HashSet<string> available = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>The balance preset pressed since the last save, or null. Forces the overhaul rewrite on save.</summary>
    internal string PresetPressed;

    internal static StagedConfig Snapshot() {
        StagedConfig snapshot = new StagedConfig();
        snapshot.Reload(null);
        return snapshot;
    }

    /// <summary>Re-reads the live value of every slot the filter accepts (all of them when null), in place.</summary>
    internal void Reload(Func<ConfigSlot, bool> filter) {
        foreach (ConfigSlot slot in QuickConfigBindings.Slots) {
            if (filter != null && filter(slot) == false) { continue; }
            object value = null;
            try {
                value = slot.ReadLive();
            } catch (Exception e) {
                EpicLoot.LogWarning($"Quick Configure could not read {slot.Key}: {e.Message}");
            }
            if (value == null) {
                available.Remove(slot.Key);
                values.Remove(slot.Key);
                continue;
            }
            available.Add(slot.Key);
            values[slot.Key] = value;
        }
    }

    /// <summary>Takes every value the other snapshot holds, keeping this instance (the widgets are bound to it).</summary>
    internal void CopyFrom(StagedConfig other) {
        values.Clear();
        available.Clear();
        foreach (KeyValuePair<string, object> pair in other.values) {
            values[pair.Key] = CloneValue(pair.Value);
        }
        foreach (string key in other.available) { available.Add(key); }
        PresetPressed = other.PresetPressed;
    }

    /// <summary>
    /// Takes the other snapshot's value of every key whose slots all pass the filter. A key fed by several
    /// slots (Effect Tuning spans two files) is only taken when every one of them passes, so a part that
    /// was not written keeps its staged edits.
    /// </summary>
    internal void TakeFrom(StagedConfig other, Func<ConfigSlot, bool> filter) {
        foreach (IGrouping<string, ConfigSlot> key in QuickConfigBindings.Slots.GroupBy(slot => slot.Key)) {
            if (key.All(filter) == false) { continue; }
            if (other.values.TryGetValue(key.Key, out object value)) {
                values[key.Key] = CloneValue(value);
                available.Add(key.Key);
            }
        }
    }

    /// <summary>True when the slot's source was loaded at the last Snapshot/Reload.</summary>
    internal bool IsAvailable(string key) => available.Contains(key);

    internal object GetRaw(string key) {
        return values.TryGetValue(key, out object value) ? value : null;
    }

    internal T Get<T>(string key, T fallback = default) {
        if (values.TryGetValue(key, out object value) && value is T typed) { return typed; }
        return fallback;
    }

    internal void Set(string key, object value) {
        if (value == null) { return; }
        values[key] = value;
        available.Add(key);
    }

    internal bool Matches(StagedConfig other) {
        if (other == null) { return false; }
        if (PresetPressed != other.PresetPressed) { return false; }
        foreach (ConfigSlot slot in QuickConfigBindings.Slots) {
            if (ValuesEqual(GetRaw(slot.Key), other.GetRaw(slot.Key)) == false) { return false; }
        }
        return true;
    }

    /// <summary>The first validation failure across every bound row, or null when everything can be saved.</summary>
    internal string ValidationError() {
        bool canEditFiles = QuickConfigBindings.CanEditFiles();
        foreach (Binding binding in QuickConfigBindings.All) {
            if (binding.Validate == null || IsAvailable(binding.Key) == false) { continue; }
            // A row this player cannot edit (JSON for a non-admin off-host) must not block the save with
            // a value the server chose.
            if (binding.EditsFiles && canEditFiles == false) { continue; }
            string error;
            try {
                error = binding.Validate(this);
            } catch (Exception e) {
                error = $"{binding.DisplayName}: {e.Message}";
            }
            if (string.IsNullOrEmpty(error) == false) { return error; }
        }
        return null;
    }

    /// <summary>
    /// Writes every .cfg entry whose staged value differs from the baseline, so a value that moved
    /// while the panel was open (another admin, a file edit) is not clobbered by an untouched row.
    /// The caller batches this under SaveOnConfigSet = false. Returns how many entries were written.
    /// </summary>
    internal int ApplyTo(StagedConfig baseline) {
        int written = 0;
        foreach (ConfigSlot slot in QuickConfigBindings.Slots) {
            if (slot is CfgSlot cfg == false || cfg.Write == null) { continue; }
            object staged = GetRaw(slot.Key);
            if (staged == null) { continue; }
            if (ValuesEqual(staged, baseline?.GetRaw(slot.Key))) { continue; }
            cfg.Write(staged);
            written++;
        }
        return written;
    }

    /// <summary>The baseconfig files with at least one changed value, in apply order (magiceffects last).</summary>
    internal List<string> DirtyJsonFiles(StagedConfig baseline) {
        List<string> dirty = new List<string>();
        foreach (string file in JsonFileOrder) {
            if (DirtyJsonSlots(file, baseline).Count > 0) { dirty.Add(file); }
        }
        return dirty;
    }

    internal List<JsonSlot> DirtyJsonSlots(string file, StagedConfig baseline) {
        List<JsonSlot> dirty = new List<JsonSlot>();
        foreach (ConfigSlot slot in QuickConfigBindings.Slots) {
            if (slot is JsonSlot json == false || json.File != file || json.CanWrite == false) { continue; }
            object staged = GetRaw(slot.Key);
            if (staged == null || IsAvailable(slot.Key) == false) { continue; }
            if (json.IsDirty(staged, baseline?.GetRaw(slot.Key)) == false) { continue; }
            dirty.Add(json);
        }
        return dirty;
    }

    internal static bool ValuesEqual(object a, object b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null) { return false; }
        if (a is EffectConfigsValue effectsA && b is EffectConfigsValue effectsB) {
            return EffectConfigsValue.Same(effectsA, effectsB);
        }
        if (a is BountiesValue bountiesA && b is BountiesValue bountiesB) {
            return BountiesValue.Same(bountiesA, bountiesB);
        }
        if (a is List<BiomeCostEntry> biomesA && b is List<BiomeCostEntry> biomesB) {
            if (biomesA.Count != biomesB.Count) { return false; }
            for (int i = 0; i < biomesA.Count; i++) {
                if (biomesA[i].Biome != biomesB[i].Biome || biomesA[i].Cost != biomesB[i].Cost
                    || biomesA[i].ForestTokens != biomesB[i].ForestTokens) { return false; }
            }
            return true;
        }
        if (a is ItemCategoriesValue categoriesA && b is ItemCategoriesValue categoriesB) {
            return ItemCategoriesValue.Same(categoriesA, categoriesB);
        }
        if (a is List<BiomeDropRow> dropsA && b is List<BiomeDropRow> dropsB) {
            if (dropsA.Count != dropsB.Count) { return false; }
            for (int i = 0; i < dropsA.Count; i++) {
                if (BiomeDropRow.Same(dropsA[i], dropsB[i]) == false) { return false; }
            }
            return true;
        }
        if (a is RarityCountsValue countsA && b is RarityCountsValue countsB) {
            return RarityCountsValue.Same(countsA, countsB);
        }
        if (a is UpgradeCostsValue costsA && b is UpgradeCostsValue costsB) {
            return UpgradeCostsValue.Same(costsA, costsB);
        }
        return a.Equals(b);
    }

    private static object CloneValue(object value) {
        switch (value) {
            case EffectConfigsValue effects: return effects.Clone();
            case BountiesValue bounties: return bounties.Clone();
            case List<BiomeCostEntry> biomes: return biomes.Select(entry => entry.Clone()).ToList();
            case ItemCategoriesValue categories: return categories.Clone();
            case List<BiomeDropRow> drops: return drops.Select(row => row.Clone()).ToList();
            case RarityCountsValue counts: return counts.Clone();
            case UpgradeCostsValue costs: return costs.Clone();
            default: return value;
        }
    }
}
