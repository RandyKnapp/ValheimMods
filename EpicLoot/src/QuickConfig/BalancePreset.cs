using EpicLoot.Config;
using EpicLoot.Patching;
using System;
using System.Collections.Generic;
using System.IO;

namespace EpicLoot.QuickConfig;

/// <summary>
/// The three balance presets, applied to the STAGED values only; the
/// overhaul rewrite of the balance template files happens on Save, after the .cfg batch, on the host or
/// (for a server admin) on the server.
/// </summary>
internal static class BalancePreset {
    internal const string MagicEffectsFile = "magiceffects.json";

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
        // The rewrite on Save replaces the template files whole, so edits staged against the old ones are
        // dropped now rather than written over the fresh templates.
        ReloadTemplateFiles(staged);
        return QuickConfigureTool.L("$mod_epicloot_cfg_preset_resets_effects");
    }

    /// <summary>Re-reads the values of <paramref name="config"/> held in the balance template files from the live config.</summary>
    internal static void ReloadTemplateFiles(StagedConfig config) {
        config.Reload(slot => slot is JsonSlot json && ELConfig.IsBalanceTemplateFile(json.File));
    }

    /// <summary>True when a balance template file on disk is the player's work, so a rewrite should ask and back it up.</summary>
    internal static bool AnyTemplateFilePlayerModified() {
        foreach (string file in ELConfig.BalanceTemplateFiles) {
            if (ConfigVersionManager.IsPlayerModified(Path.GetFileNameWithoutExtension(file))) { return true; }
        }
        return false;
    }

    /// <summary>True when a patch file targets this config ("magiceffects", no extension).</summary>
    internal static bool IsPatched(string configName) {
        return FilePatching.PatchesPerFile.GetValues(configName, true).Count > 0;
    }

    /// <summary>True when Save must rewrite the template files: a preset was pressed, or the template changed (host or server admin).</summary>
    internal static bool NeedsOverhaulRewrite(StagedConfig staged, StagedConfig baseline) {
        if (staged == null || baseline == null || QuickConfigBindings.CanEditFiles() == false) { return false; }
        if (staged.PresetPressed != null) { return true; }
        return staged.Get("BalanceConfigurationType", "balanced") != baseline.Get("BalanceConfigurationType", "balanced");
    }

    /// <summary>
    /// Rewrites the balance template files in baseconfig/ from the embedded overhaul the (already saved)
    /// Balance Template names, records them as the mod's own content, and reloads them. With backupFirst
    /// each hand-edited file is copied to baseconfig-backup/ first; the host has asked the player by then,
    /// and a server backs them up for a remote admin without asking.
    /// </summary>
    internal static bool ApplyOverhaulRewrite(bool backupFirst, out string message) {
        message = "";
        string template = ELConfig.BalanceConfigurationType.Value;
        string[] names = ELConfig.BalanceTemplateFiles;
        string files = names.Length > 1
            ? string.Join(", ", names, 0, names.Length - 1) + " and " + names[names.Length - 1]
            : names[0];
        try {
            string backupDir = null;
            foreach (string file in ELConfig.BalanceTemplateFiles) {
                string configName = Path.GetFileNameWithoutExtension(file);
                string path = Path.Combine(ELConfig.GetOverhaulDirectoryPath(), file);
                string previousText = File.Exists(path) ? File.ReadAllText(path) : null;
                bool backup = backupFirst && previousText != null && ConfigVersionManager.IsPlayerModified(configName);
                if (backup) {
                    backupDir = ConfigVersionManager.BackupConfigFile(configName, backupDir);
                }

                // A file that patches target is the template plus those patches, rebuilt the way every launch
                // rebuilds it; the bare template would drop patch-added effects until the next restart.
                if (IsPatched(configName)) {
                    FilePatching.LoadPatchedJSON(configName);
                } else {
                    ELConfig.CreateBaseConfigurations(path, file);
                }
                string newText = File.ReadAllText(path);
                // This IS the mod's own content. A backed-up file was the player's, and the version
                // manager would keep it flagged as theirs if it saw that baseline; they chose to replace
                // it, so the fresh template is recorded as written from scratch.
                ConfigVersionManager.RecordWrittenContent(configName, newText, backup ? null : previousText);
            }
            ELConfig.ReloadBaseConfigsFromDisk(ELConfig.BalanceTemplateFiles);

            message = $"{files} rewritten from the {template} template" +
                (backupDir != null ? $"; the edited files were backed up to {backupDir}" : "") + ".";
            EpicLoot.LogForce($"Quick Configure: {message}");
            return true;
        } catch (Exception e) {
            EpicLoot.LogErrorForce($"Quick Configure could not rewrite {files} from the {template} template: {e}");
            message = $"{files} were not rewritten: {e.Message}";
            return false;
        }
    }
}
