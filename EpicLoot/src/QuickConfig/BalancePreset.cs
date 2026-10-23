using EpicLoot.Config;
using EpicLoot.Patching;
using System;
using System.Collections.Generic;
using System.IO;

namespace EpicLoot.QuickConfig;

/// <summary>
/// The three balance presets the old welcome panel offered, applied to the STAGED values only; the
/// overhaul rewrite of magiceffects.json happens on Save, after the .cfg batch, on the host or (for a
/// server admin) on the server.
/// </summary>
internal static class BalancePreset {
    internal const string MagicEffectsFile = "magiceffects.json";
    internal const string MagicEffectsConfigName = "magiceffects";

    internal static readonly string[] Names = { "balanced", "minimal", "legendary" };

    private struct DropMix {
        internal float Item;
        internal float Unidentified;
        internal float Materials;
        internal float ShardStone;
    }

    // The four drop ratios are relative weights competing for each loot drop (LootRoller.SelectDropType),
    // so each preset sets all four: leaving one alone would let a value from another preset, or from a
    // hand-edited config, skew the mix the preset is meant to describe.
    private static readonly Dictionary<string, DropMix> Mixes = new Dictionary<string, DropMix>(StringComparer.Ordinal) {
        { "balanced", new DropMix { Item = 0.6f, Unidentified = 0.3f, Materials = 0.15f, ShardStone = 0.1f } },
        { "minimal", new DropMix { Item = 0.1f, Unidentified = 0.5f, Materials = 0.25f, ShardStone = 0.1f } },
        { "legendary", new DropMix { Item = 1.0f, Unidentified = 0.1f, Materials = 0.0f, ShardStone = 0.1f } }
    };

    /// <summary>A template name as the .cfg may hold it ("Default", odd casing) reduced to one of the three.</summary>
    internal static string Normalize(string value) {
        string trimmed = (value ?? "").Trim().ToLowerInvariant();
        return Array.IndexOf(Names, trimmed) >= 0 ? trimmed : "balanced";
    }

    /// <summary>Stages a preset: template, drop mix, and a reset of any staged Effect Tuning edits.</summary>
    internal static string ApplyToStaged(StagedConfig staged, string name) {
        if (staged == null || Mixes.TryGetValue(name, out DropMix mix) == false) { return null; }
        staged.Set("BalanceConfigurationType", name);
        staged.Set("ItemDropRatio", mix.Item);
        staged.Set("ItemsUnidentifiedDropRatio", mix.Unidentified);
        staged.Set("MaterialsDropRatio", mix.Materials);
        staged.Set("ShardStoneDropRatio", mix.ShardStone);
        staged.PresetPressed = name;
        // The rewrite on Save replaces the whole file, so edits staged against the old one are dropped
        // now rather than written over the fresh template.
        ReloadMagicEffects(staged);
        return QuickConfigureTool.L("$mod_epicloot_cfg_preset_resets_effects");
    }

    /// <summary>
    /// Re-reads the magiceffects.json values of <paramref name="config"/> from the live config. Effect
    /// Tuning is one value over two files, and re-reading it brings back every effect, so its shard
    /// effects keep what <paramref name="config"/> held: they live in shardstones.json, which neither a
    /// preset nor the rewrite touches.
    /// </summary>
    internal static void ReloadMagicEffects(StagedConfig config) {
        EffectConfigsValue before = config.Get<EffectConfigsValue>(QuickConfigBindings.EffectConfigsKey);
        config.Reload(slot => slot is JsonSlot json && json.File == MagicEffectsFile);
        EffectConfigsValue after = config.Get<EffectConfigsValue>(QuickConfigBindings.EffectConfigsKey);
        if (before == null || after == null || ReferenceEquals(before, after)) { return; }

        foreach (EffectConfigSet set in before.Effects.Values) {
            if (set.Source == EffectSource.Shard && after.Effects.ContainsKey(set.Type)) {
                after.Effects[set.Type] = set.Clone();
            }
        }
    }

    /// <summary>True when a patch file targets this config ("magiceffects", no extension).</summary>
    internal static bool IsPatched(string configName) {
        return FilePatching.PatchesPerFile.GetValues(configName, true).Count > 0;
    }

    /// <summary>True when Save must rewrite magiceffects.json: a preset was pressed, or the template changed (host or server admin).</summary>
    internal static bool NeedsOverhaulRewrite(StagedConfig staged, StagedConfig baseline) {
        if (staged == null || baseline == null || QuickConfigBindings.CanEditFiles() == false) { return false; }
        if (staged.PresetPressed != null) { return true; }
        return staged.Get("BalanceConfigurationType", "balanced") != baseline.Get("BalanceConfigurationType", "balanced");
    }

    /// <summary>
    /// Rewrites baseconfig/magiceffects.json from the embedded overhaul the (already saved) Balance
    /// Template names, records it as the mod's own content, and reloads it. With backupFirst the
    /// current file is copied to baseconfig-backup/ first; the host has asked the player by then, and a
    /// server backs up a hand-edited file for a remote admin without asking.
    /// </summary>
    internal static bool ApplyOverhaulRewrite(bool backupFirst, out string message) {
        message = "";
        string template = ELConfig.BalanceConfigurationType.Value;
        try {
            string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), MagicEffectsFile);
            string previousText = File.Exists(path) ? File.ReadAllText(path) : null;
            string backupDir = null;
            if (backupFirst && previousText != null) {
                backupDir = ConfigVersionManager.BackupConfigFile(MagicEffectsConfigName);
            }

            // A file that patches target is the template plus those patches, rebuilt the way every launch
            // rebuilds it; the bare template would drop patch-added effects until the next restart.
            if (IsPatched(MagicEffectsConfigName)) {
                FilePatching.LoadPatchedJSON(MagicEffectsConfigName);
            } else {
                ELConfig.CreateBaseConfigurations(path, MagicEffectsFile);
            }
            string newText = File.ReadAllText(path);
            // This IS the mod's own content. A backed-up file was the player's, and the version
            // manager would keep it flagged as theirs if it saw that baseline; they chose to replace
            // it, so the fresh template is recorded as written from scratch.
            ConfigVersionManager.RecordWrittenContent(MagicEffectsConfigName, newText, backupDir != null ? null : previousText);
            ELConfig.ReloadBaseConfigsFromDisk(new[] { MagicEffectsFile });

            message = $"magiceffects.json rewritten from the {template} template" +
                (backupDir != null ? $"; the previous file was backed up to {backupDir}" : "") + ".";
            EpicLoot.LogForce($"Quick Configure: {message}");
            return true;
        } catch (Exception e) {
            EpicLoot.LogErrorForce($"Quick Configure could not rewrite magiceffects.json from the {template} template: {e}");
            message = $"magiceffects.json was not rewritten: {e.Message}";
            return false;
        }
    }
}
