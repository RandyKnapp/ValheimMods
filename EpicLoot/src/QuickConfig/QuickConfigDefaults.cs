using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EpicLoot.QuickConfig;

/// <summary>
/// The config files the mod ships (the embedded baseconfig defaults), read once per page reset.
/// magiceffects.json is the overhaul of the Balance Template that is staged, since that is the file
/// Save would write.
/// </summary>
internal sealed class ShippedDefaults {
    private readonly string template;
    private readonly Dictionary<string, string> texts = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, JObject> roots = new Dictionary<string, JObject>(StringComparer.Ordinal);

    internal ShippedDefaults(string balanceTemplate) {
        template = BalancePreset.Normalize(balanceTemplate);
    }

    /// <summary>The shipped file as a JObject, or null when it cannot be read.</summary>
    internal JObject Root(string file) {
        if (roots.TryGetValue(file, out JObject root)) { return root; }
        string text = Text(file);
        try {
            root = text != null ? JObject.Parse(text) : null;
        } catch (Exception e) {
            EpicLoot.LogWarning($"Quick Configure could not parse the shipped {file}: {e.Message}");
            root = null;
        }
        roots[file] = root;
        return root;
    }

    /// <summary>The shipped file deserialized as the mod loads it, or null.</summary>
    internal T Typed<T>(string file) where T : class {
        string text = Text(file);
        if (text == null) { return null; }
        try {
            return JsonConvert.DeserializeObject<T>(text);
        } catch (Exception e) {
            EpicLoot.LogWarning($"Quick Configure could not read the shipped {file}: {e.Message}");
            return null;
        }
    }

    /// <summary>The token at a dotted path of the shipped file ("Gamble.GamblesCount"), or null.</summary>
    internal JToken Token(string file, string path) => Root(file)?.SelectToken(path);

    private string Text(string file) {
        if (texts.TryGetValue(file, out string text)) { return text; }
        string resource = file == BalancePreset.MagicEffectsFile
            ? $"EpicLoot.config.overhauls.{template}.{file}"
            : $"EpicLoot.config.{file}";
        try {
            text = EpicLoot.ReadEmbeddedResourceFile(resource);
        } catch (Exception e) {
            EpicLoot.LogWarning($"Quick Configure could not read the shipped {file}: {e.Message}");
            text = null;
        }
        texts[file] = text;
        return text;
    }
}

/// <summary>Stages the shipped defaults of a page's settings (the Reset Page button).</summary>
internal static class QuickConfigDefaults {
    /// <summary>
    /// Stages the default of every slot the keys name, leaving anything without a known default alone.
    /// Nothing is written: Save applies it like any other edit. Returns how many values changed.
    /// </summary>
    internal static int Reset(StagedConfig staged, IEnumerable<string> keys) {
        ShippedDefaults shipped = new ShippedDefaults(staged.Get("BalanceConfigurationType", "balanced"));
        HashSet<string> done = new HashSet<string>(StringComparer.Ordinal);
        int changed = 0;
        foreach (string key in keys) {
            if (string.IsNullOrEmpty(key) || done.Add(key) == false || staged.IsAvailable(key) == false) { continue; }
            object current = staged.GetRaw(key);
            // Several slots may share a key (the effect configs feed two files); the first with a default decides.
            foreach (ConfigSlot slot in QuickConfigBindings.Slots) {
                if (slot.Key != key) { continue; }
                object value;
                try {
                    value = slot.DefaultFor(current, shipped);
                } catch (Exception e) {
                    EpicLoot.LogWarning($"Quick Configure could not find the default of {key}: {e.Message}");
                    value = null;
                }
                if (value == null) { continue; }
                if (StagedConfig.ValuesEqual(value, current) == false) {
                    staged.Set(key, value);
                    changed++;
                }
                break;
            }
        }
        return changed;
    }
}
