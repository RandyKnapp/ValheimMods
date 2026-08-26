using BepInEx;
using EpicLoot.Patching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EpicLoot.Config;

/// <summary>
/// Keeps the baseconfig files in step with the version of the mod that is running.
///
/// Background: a baseconfig file with no patches targeting it is only ever written when it is
/// missing (see FilePatching.LoadPatchedJSON). A player who installed an older version therefore
/// keeps that version's loot tables, effects and balance forever, with nothing to indicate it.
/// Files that *do* have patches are rebuilt from the current embedded default every launch and are
/// never out of date.
///
/// Two fingerprints are recorded per file, and both are needed:
///   SourceHash  - the embedded default that produced the file. Answers "is there anything new to apply?"
///   WrittenHash - the content the mod actually wrote. Answers "is it safe to apply it?"
/// A file still matching its WrittenHash has not been touched by the player, so it is refreshed
/// silently. Anything else is the player's own work and is only ever changed if they say so.
/// </summary>
public static class ConfigVersionManager
{
    /// <summary>Player-modified configs whose shipped default has changed. These drive the prompt.</summary>
    public static readonly List<string> OutdatedConfigs = new List<string>();

    private static ConfigVersionState _state;
    private static readonly Dictionary<string, string> _pendingSourceHashes = new Dictionary<string, string>();
    private static readonly List<string> _stampAfterInit = new List<string>();
    private static bool _enabled;
    private static bool _detectionRan;

    public static bool HasOutdatedConfigs => OutdatedConfigs.Count > 0;

    /// <summary>Guards the prompt against running before detection has had a chance to populate.</summary>
    public static bool DetectionRan => _detectionRan;

    /// <summary>
    /// Snapshotted during Awake because both this feature and WelcomeMessage postfix
    /// FejdStartup.Start, and Harmony does not guarantee which of the two runs first. Reading the
    /// config entry from the prompt itself would race with WelcomeMessage clearing it.
    /// </summary>
    public static bool WelcomeMessageWillShow { get; private set; }

    // A sibling of baseconfig/, not a child, so the directory the player browses stays clean.
    private static string BackupDirPath => Path.Combine(Paths.ConfigPath, "EpicLoot", "baseconfig-backup");

    /// <summary>
    /// Refreshes every baseconfig file the player has not edited, and collects the ones they have.
    ///
    /// Must run *before* ELConfig.InitializeConfig so the refreshed contents are what gets
    /// deserialized. Writing afterwards would rely on the per-file FileSystemWatcher firing part way
    /// through Awake, which is both asynchronous and unnecessary.
    /// </summary>
    public static void RefreshUnmodifiedConfigs()
    {
        OutdatedConfigs.Clear();
        _pendingSourceHashes.Clear();
        _stampAfterInit.Clear();

        WelcomeMessageWillShow = ELConfig.AlwaysShowWelcomeMessage.Value;
        _enabled = !ELConfig.AlwaysRefreshCoreConfigs.Value;

        if (!_enabled)
        {
            // The player has opted into unconditional overwrite, so nothing can ever be out of date.
            EpicLoot.Log("'Always Refresh Core Configs' is enabled, skipping config version checks.");
            return;
        }

        _state = ConfigVersionState.Load();

        string baseConfigDir;
        try
        {
            baseConfigDir = ELConfig.GetOverhaulDirectoryPath();
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Could not open the baseconfig directory, " +
                $"skipping config version checks.\n{e.Message}");
            _enabled = false;
            return;
        }

        bool stateChanged = false;

        foreach (string name in FilePatching.ConfigFileNames)
        {
            string embeddedHash = GetEmbeddedHash(name, out string variant, out string embedded);
            if (string.IsNullOrEmpty(embeddedHash))
            {
                // Could not read the embedded default (see GetEmbeddedHash). Nothing meaningful to
                // compare against, so leave any existing stamp alone rather than guessing.
                continue;
            }

            string configPath = Path.Combine(baseConfigDir, $"{name}.json");

            // Missing files are written by InitializeConfig, and patched files are rebuilt from the
            // current embedded default every launch. Both are current by construction, but neither
            // exists in its final form yet, so stamp them once InitializeConfig has run.
            if (!File.Exists(configPath) || FilePatching.PatchesPerFile.GetValues(name, true).Count > 0)
            {
                _stampAfterInit.Add(name);
                continue;
            }

            ConfigVersionEntry entry = _state.Get(name);

            if (entry != null && entry.SourceHash == embeddedHash && entry.Variant == (variant ?? ""))
            {
                // The shipped default has not changed since this file was written, so there is
                // nothing to apply no matter what the player has done to it.
                if (entry.Version != EpicLoot.Version)
                {
                    _state.TouchVersion(name);
                    stateChanged = true;
                }

                continue;
            }

            string diskHash = TryHashFile(configPath);
            if (string.IsNullOrEmpty(diskHash))
            {
                continue;
            }

            if (diskHash == embeddedHash)
            {
                // Already identical to the new default; only the bookkeeping was behind.
                _state.Stamp(name, embeddedHash, diskHash, variant);
                stateChanged = true;
                continue;
            }

            if (entry != null && diskHash == entry.WrittenHash)
            {
                // Untouched since we wrote it, so replacing it cannot lose any of the player's work.
                if (TryWriteDefault(name, configPath, embedded))
                {
                    _state.Stamp(name, embeddedHash, embeddedHash, variant);
                    stateChanged = true;
                    EpicLoot.Log($"Refreshed unmodified config {name}.json to version {EpicLoot.Version}.");
                }

                continue;
            }

            // Either the player edited it, or it predates version tracking and its origin is unknown.
            // Either way it is theirs; only offer, never take.
            if (entry != null && entry.DeclinedSourceHash == embeddedHash)
            {
                EpicLoot.Log($"Config {name}.json differs from the current default, " +
                    $"but an update was already declined for this version.");
                continue;
            }

            EpicLoot.Log($"Config {name}.json was written by version " +
                $"{(entry == null || string.IsNullOrEmpty(entry.Version) ? "<unknown>" : entry.Version)}, " +
                $"has local changes, and its shipped default has changed since.");
            OutdatedConfigs.Add(name);
            _pendingSourceHashes[name] = embeddedHash;
        }

        if (stateChanged)
        {
            _state.Save();
        }
    }

    /// <summary>
    /// Stamps the files that InitializeConfig created or rebuilt, then reports anything still
    /// awaiting the player's decision. Must run after ELConfig.InitializeConfig.
    /// </summary>
    public static void StampInitializedConfigs()
    {
        _detectionRan = true;

        if (!_enabled || _state == null)
        {
            return;
        }

        bool stateChanged = false;
        string baseConfigDir = ELConfig.GetOverhaulDirectoryPath();

        foreach (string name in _stampAfterInit)
        {
            string embeddedHash = GetEmbeddedHash(name, out string variant, out _);
            if (string.IsNullOrEmpty(embeddedHash))
            {
                continue;
            }

            // Read back what was actually written: for a patched file that is the embedded default
            // plus its patches, which is what a later launch will need to compare against.
            string writtenHash = TryHashFile(Path.Combine(baseConfigDir, $"{name}.json"));
            if (string.IsNullOrEmpty(writtenHash))
            {
                continue;
            }

            _state.Stamp(name, embeddedHash, writtenHash, variant);
            stateChanged = true;
        }

        if (stateChanged)
        {
            _state.Save();
        }

        if (HasOutdatedConfigs)
        {
            EpicLoot.LogWarningForce($"{OutdatedConfigs.Count} Epic Loot config file(s) have local changes and " +
                $"were not written by version {EpicLoot.Version}: {string.Join(", ", OutdatedConfigs)}. " +
                $"They will not pick up new content or balance changes until they are updated.");
        }
    }

    /// <summary>
    /// Records content the mod itself wrote to a baseconfig file, so that its own output is not
    /// mistaken for a player edit. Callers must pass exactly what they wrote to disk, plus the
    /// content that was on disk BEFORE the rewrite (null when the file did not exist).
    ///
    /// The before-content matters: runtime rewriters (AutoAddEnchantableItems) merge on top of
    /// whatever is on disk, player edits included. Stamping that merged output as "our own" told
    /// the next launch's RefreshUnmodifiedConfigs the file was untouched, and a mod update then
    /// silently replaced it -- destroying the player's edits with no backup and no prompt.
    /// </summary>
    public static void RecordWrittenContent(string configName, string contents, string previousContents)
    {
        if (!_enabled || _state == null)
        {
            return;
        }

        try
        {
            string embeddedHash = GetEmbeddedHash(configName, out string variant, out _);
            if (string.IsNullOrEmpty(embeddedHash))
            {
                return;
            }

            if (previousContents != null && _state.Files.TryGetValue(configName, out ConfigVersionEntry entry))
            {
                bool baselineWasPlayerModified = string.IsNullOrEmpty(entry.WrittenHash) ||
                    ConfigVersionState.HashConfigText(previousContents) != entry.WrittenHash;
                if (baselineWasPlayerModified)
                {
                    EpicLoot.Log($"Config {configName}.json was rewritten on top of a player-modified " +
                        "baseline; keeping it flagged as player-owned.");
                    return;
                }
            }
            else if (previousContents != null)
            {
                // No bookkeeping entry: the file predates version tracking, so its origin is
                // unknown -- treat it as the player's rather than claiming it.
                return;
            }

            _state.Stamp(configName, embeddedHash, ConfigVersionState.HashConfigText(contents), variant);
            _state.Save();
        }
        catch (Exception e)
        {
            EpicLoot.LogWarning($"Could not record written content for {configName}.json.\n{e.Message}");
        }
    }

    /// <summary>
    /// Remembers that the player declined, per file, against the default they declined for. A later
    /// release that changes that default will ask again; nothing else is suppressed.
    /// </summary>
    public static void DeclineOutdatedConfigs()
    {
        if (_state == null)
        {
            return;
        }

        foreach (string name in OutdatedConfigs)
        {
            _state.RecordDecline(name, _pendingSourceHashes.TryGetValue(name, out string hash) ? hash : "");
        }

        _state.Save();
        EpicLoot.LogForce($"Epic Loot config update declined for {OutdatedConfigs.Count} file(s); " +
            $"you will be asked again if their defaults change in a future update.");
        OutdatedConfigs.Clear();
    }

    /// <summary>
    /// Backs up every outdated config to a timestamped folder, then rewrites it from the embedded
    /// default. Writing each file trips its FileSystemWatcher, so the in-memory config reloads
    /// without a restart (the same mechanism FilePatching.LoadPatchedJSON relies on).
    /// </summary>
    public static void BackupAndResetOutdatedConfigs()
    {
        if (_state == null || !HasOutdatedConfigs)
        {
            return;
        }

        // Named for the version being upgraded *to*: the files inside predate it, and they may not
        // all have come from the same older version, so naming it after one of them would mislead.
        string backupDir = Path.Combine(BackupDirPath, $"pre-{EpicLoot.Version}_{DateTime.Now:yyyyMMdd-HHmmss}");
        string baseConfigDir = ELConfig.GetOverhaulDirectoryPath();
        List<string> updated = new List<string>();

        foreach (string name in OutdatedConfigs.ToList())
        {
            string fileName = $"{name}.json";
            string configPath = Path.Combine(baseConfigDir, fileName);

            try
            {
                if (File.Exists(configPath))
                {
                    Directory.CreateDirectory(backupDir);
                    File.Copy(configPath, Path.Combine(backupDir, fileName), true);
                }

                string embeddedHash = GetEmbeddedHash(name, out string variant, out string embedded);
                ELConfig.CreateBaseConfigurations(configPath, fileName);
                _state.Stamp(name, embeddedHash, embeddedHash, variant);
                updated.Add(name);
            }
            catch (Exception e)
            {
                // Leave this file marked outdated so the player is asked again next launch.
                EpicLoot.LogErrorForce($"Failed to update config {fileName}, it has been left unchanged.\n{e}");
            }
        }

        foreach (string name in updated)
        {
            OutdatedConfigs.Remove(name);
        }

        _state.Save();
        EpicLoot.LogForce($"Updated {updated.Count} Epic Loot config file(s) to version {EpicLoot.Version}. " +
            $"The previous files were backed up to {backupDir}");
    }

    /// <summary>Writes the embedded default over a config, reporting failure rather than throwing.</summary>
    private static bool TryWriteDefault(string configName, string configPath, string embedded)
    {
        try
        {
            File.WriteAllText(configPath, embedded);
            return true;
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Could not refresh {configName}.json, it has been left unchanged.\n{e.Message}");
            return false;
        }
    }

    /// <summary>Hashes a config on disk, returning empty on any read failure so callers can skip it.</summary>
    private static string TryHashFile(string path)
    {
        try
        {
            return ConfigVersionState.HashConfigText(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            EpicLoot.LogWarning($"Could not read {path} for a config version check.\n{e.Message}");
            return "";
        }
    }

    /// <summary>
    /// Fingerprints the embedded default for a config, also handing back its text so callers can
    /// write it without a second read. Returns an empty string when the resource cannot be read:
    /// ReadEmbeddedResourceFile throws on a missing resource, and this runs inside ELConfig's
    /// constructor during Awake, where an escaping exception would take down the mod.
    /// </summary>
    private static string GetEmbeddedHash(string configName, out string variant, out string embedded)
    {
        variant = configName == "magiceffects" ? ELConfig.BalanceConfigurationType.Value : "";
        embedded = null;

        try
        {
            embedded = EpicLoot.ReadEmbeddedResourceFile(
                ELConfig.GetDefaultEmbeddedFileLocation($"{configName}.json"));
            return ConfigVersionState.HashConfigText(embedded);
        }
        catch (Exception e)
        {
            EpicLoot.LogWarning($"Could not read the embedded default for {configName}.json, " +
                $"skipping its version check.\n{e.Message}");
            return "";
        }
    }
}
