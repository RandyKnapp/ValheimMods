using EpicLoot.Config;
using EpicLoot.Magic.MagicItemEffects.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EpicLoot.QuickConfig;

/// <summary>Where an effect's Config values are written on Save.</summary>
internal enum EffectSource {
    /// <summary>The MagicItemEffects element of baseconfig/magiceffects.json.</summary>
    Overhaul,
    /// <summary>Every shardstones.json slot entry (TypeEffects.* / UniformEffect) carrying the effect and a Config.</summary>
    Shard,
    /// <summary>Registered by another mod or synthesized in code: shown, not written.</summary>
    ReadOnly
}

internal sealed class EffectConfigEntry {
    internal string Key = "";
    internal float Value;

    internal EffectConfigEntry Clone() => new EffectConfigEntry { Key = Key, Value = Value };
}

/// <summary>One loaded effect definition's Config, with the file it came from.</summary>
internal sealed class EffectConfigSet {
    internal string Type = "";
    internal string DisplayName = "";
    internal EffectSource Source;
    /// <summary>True for Riches, whose keys are item names: rows can be added, removed and renamed.</summary>
    internal bool OpenKeys;
    internal List<EffectConfigEntry> Entries = new List<EffectConfigEntry>();

    internal string PickerName => Source == EffectSource.ReadOnly ? DisplayName + " (read-only)" : DisplayName;

    internal EffectConfigSet Clone() {
        return new EffectConfigSet {
            Type = Type, DisplayName = DisplayName, Source = Source, OpenKeys = OpenKeys,
            Entries = Entries.Select(entry => entry.Clone()).ToList()
        };
    }

    internal static bool SameEntries(EffectConfigSet a, EffectConfigSet b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null || a.Entries.Count != b.Entries.Count) { return false; }
        for (int i = 0; i < a.Entries.Count; i++) {
            if (a.Entries[i].Key != b.Entries[i].Key || a.Entries[i].Value != b.Entries[i].Value) { return false; }
        }
        return true;
    }
}

/// <summary>Every loaded effect with a non-empty Config, in definition order. Staged whole.</summary>
internal sealed class EffectConfigsValue {
    internal readonly List<string> Order = new List<string>();
    internal readonly Dictionary<string, EffectConfigSet> Effects = new Dictionary<string, EffectConfigSet>(StringComparer.Ordinal);

    internal EffectConfigSet Get(string type) {
        return type != null && Effects.TryGetValue(type, out EffectConfigSet set) ? set : null;
    }

    internal EffectConfigsValue Clone() {
        EffectConfigsValue clone = new EffectConfigsValue();
        clone.Order.AddRange(Order);
        foreach (KeyValuePair<string, EffectConfigSet> pair in Effects) { clone.Effects[pair.Key] = pair.Value.Clone(); }
        return clone;
    }

    /// <summary>Editable effects sorted by display name, then the read-only ones, also sorted.</summary>
    internal List<EffectConfigSet> PickerOrder() {
        return Effects.Values
            .OrderBy(set => set.Source == EffectSource.ReadOnly ? 1 : 0)
            .ThenBy(set => set.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>True when any effect written to the given file differs from the baseline.</summary>
    internal bool PartChanged(EffectConfigsValue before, EffectSource source) {
        foreach (EffectConfigSet set in Effects.Values) {
            if (set.Source != source) { continue; }
            if (EffectConfigSet.SameEntries(set, before?.Get(set.Type)) == false) { return true; }
        }
        return false;
    }

    internal static bool Same(EffectConfigsValue a, EffectConfigsValue b) {
        if (ReferenceEquals(a, b)) { return true; }
        if (a == null || b == null || a.Order.Count != b.Order.Count) { return false; }
        foreach (string type in a.Order) {
            if (EffectConfigSet.SameEntries(a.Get(type), b.Get(type)) == false) { return false; }
        }
        return true;
    }
}

/// <summary>
/// Reads the loaded effect definitions into an EffectConfigsValue and decides each effect's write
/// target from the two files on disk: a shardstones.json slot entry carrying the effect with a Config
/// wins (the grid overlays the overhaul definition on load, so that is where the live value comes
/// from), then the magiceffects.json element, else read-only.
/// </summary>
internal static class EffectConfigTables {
    private sealed class FileTypes {
        internal string Path;
        internal DateTime Written;
        internal long Length;
        internal HashSet<string> Types = new HashSet<string>(StringComparer.Ordinal);
    }

    private static FileTypes overhaulCache;
    private static FileTypes shardCache;

    internal static EffectConfigsValue Read() {
        Dictionary<string, MagicItemEffectDefinition> definitions = MagicItemEffectDefinitions.AllDefinitions;
        if (definitions == null || definitions.Count == 0) { return null; }

        HashSet<string> overhaulTypes = OverhaulTypes();
        HashSet<string> shardTypes = ShardTypesWithConfig();
        EffectConfigsValue value = new EffectConfigsValue();
        foreach (MagicItemEffectDefinition definition in definitions.Values) {
            if (definition?.Config == null || definition.Config.Count == 0 || string.IsNullOrEmpty(definition.Type)) { continue; }
            if (value.Effects.ContainsKey(definition.Type)) { continue; }
            EffectConfigSet set = new EffectConfigSet {
                Type = definition.Type,
                DisplayName = DisplayName(definition),
                Source = shardTypes.Contains(definition.Type) ? EffectSource.Shard
                    : overhaulTypes.Contains(definition.Type) ? EffectSource.Overhaul
                    : EffectSource.ReadOnly,
                OpenKeys = definition.Type == MagicEffectType.Riches
            };
            foreach (KeyValuePair<string, float> pair in definition.Config) {
                set.Entries.Add(new EffectConfigEntry { Key = pair.Key, Value = pair.Value });
            }
            value.Order.Add(definition.Type);
            value.Effects[definition.Type] = set;
        }
        return value;
    }

    /// <summary>
    /// The shipped Config of every editable effect in <paramref name="current"/>: an overhaul effect's
    /// block from the staged template's magiceffects.json, a shard effect's grid block from
    /// shardstones.json merged over its code defaults (as a load builds it). An effect the shipped files
    /// do not carry, and every read-only one, keeps its entries.
    /// </summary>
    internal static EffectConfigsValue Shipped(EffectConfigsValue current, ShippedDefaults shipped) {
        EffectConfigsValue value = current.Clone();
        JObject overhaul = shipped.Root(BalancePreset.MagicEffectsFile);
        JObject grid = shipped.Root("shardstones.json");
        foreach (EffectConfigSet set in value.Effects.Values) {
            Dictionary<string, float> config = null;
            if (set.Source == EffectSource.Overhaul && overhaul != null) {
                config = (JsonConfigEdits.FindEffect(overhaul, set.Type)?["Config"] as JObject)?.ToObject<Dictionary<string, float>>();
            } else if (set.Source == EffectSource.Shard && grid != null) {
                JObject entry = ShardSlotEntries(grid).FirstOrDefault(slot => (string)slot["EffectType"] == set.Type && slot["Config"] is JObject);
                if (entry != null) {
                    config = ShardEffectDefinitions.ConfigWith(set.Type, entry["Config"].ToObject<Dictionary<string, float>>());
                }
            }
            if (config == null || config.Count == 0) { continue; }
            set.Entries = config.Select(pair => new EffectConfigEntry { Key = pair.Key, Value = pair.Value }).ToList();
        }
        return value;
    }

    /// <summary>The per-key label the Shift tooltip shows (per-effect token, generic token, then the raw key).</summary>
    internal static string KeyLabel(string type, string key) {
        try {
            if (type != null && MagicItemEffectDefinitions.AllDefinitions.TryGetValue(type, out MagicItemEffectDefinition definition)) {
                return definition.GetConfigLabel(key ?? "");
            }
        } catch (Exception) {
            // Localization not ready: the raw key still identifies the row.
        }
        return key ?? "";
    }

    private static string DisplayName(MagicItemEffectDefinition definition) {
        string localized = string.IsNullOrEmpty(definition.DisplayText) ? "" : QuickConfigureTool.L(definition.DisplayText);
        if (string.IsNullOrWhiteSpace(localized) || localized.StartsWith("[") || localized == definition.Type) { return definition.Type; }
        return $"{localized} ({definition.Type})";
    }

    // --- the two files, re-parsed only when they change on disk ---

    private static HashSet<string> OverhaulTypes() {
        return CachedTypes(ref overhaulCache, BalancePreset.MagicEffectsFile, root => {
            HashSet<string> types = new HashSet<string>(StringComparer.Ordinal);
            if (root["MagicItemEffects"] is JArray effects) {
                foreach (JToken token in effects) {
                    if (token is JObject effect && effect["Type"] is JValue type && type.Type == JTokenType.String) { types.Add((string)type); }
                }
            }
            return types;
        });
    }

    private static HashSet<string> ShardTypesWithConfig() {
        return CachedTypes(ref shardCache, "shardstones.json", root => {
            HashSet<string> types = new HashSet<string>(StringComparer.Ordinal);
            foreach (JObject entry in ShardSlotEntries(root)) {
                if (entry["Config"] is JObject && entry["EffectType"] is JValue type && type.Type == JTokenType.String) { types.Add((string)type); }
            }
            return types;
        });
    }

    /// <summary>Every Shards.&lt;Color&gt;.TypeEffects.&lt;Slot&gt; entry and UniformEffect of a shardstones.json root.</summary>
    internal static IEnumerable<JObject> ShardSlotEntries(JObject root) {
        if (root["Shards"] is JObject shards == false) { yield break; }
        foreach (JProperty color in shards.Properties()) {
            if (color.Value is JObject definition == false) { continue; }
            if (definition["TypeEffects"] is JObject typeEffects) {
                foreach (JProperty slot in typeEffects.Properties()) {
                    if (slot.Value is JObject entry) { yield return entry; }
                }
            }
            if (definition["UniformEffect"] is JObject uniform) { yield return uniform; }
        }
    }

    private static HashSet<string> CachedTypes(ref FileTypes cache, string fileName, Func<JObject, HashSet<string>> collect) {
        try {
            string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), fileName);
            FileInfo info = new FileInfo(path);
            if (info.Exists == false) { return new HashSet<string>(StringComparer.Ordinal); }
            if (cache != null && cache.Path == path && cache.Written == info.LastWriteTimeUtc && cache.Length == info.Length) {
                return cache.Types;
            }
            HashSet<string> types = collect(JObject.Parse(File.ReadAllText(path)));
            cache = new FileTypes { Path = path, Written = info.LastWriteTimeUtc, Length = info.Length, Types = types };
            return types;
        } catch (Exception e) {
            EpicLoot.LogWarning($"Quick Configure could not read {fileName} to classify effect configs: {e.Message}");
            return new HashSet<string>(StringComparer.Ordinal);
        }
    }
}
