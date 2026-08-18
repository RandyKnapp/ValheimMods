using BepInEx;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EpicLoot.Config;

/// <summary>
/// Records which mod version last wrote each baseconfig file, and a hash of the embedded default
/// that produced it. Used to detect base configs left behind by an older version of the mod.
/// </summary>
[Serializable]
public class ConfigVersionEntry
{
    /// <summary>The EpicLoot.Version that last wrote or validated this file.</summary>
    public string Version = "";

    /// <summary>
    /// SHA-256 of the *embedded default* that was current when this entry was stamped. This is not a
    /// copy of the config, just a fingerprint, and it is deliberately taken from the embedded resource
    /// rather than the file on disk: iteminfo/loottables/adventuredata are rewritten at runtime by
    /// AutoAddEnchantableItems, so on-disk content always diverges and would be useless to compare.
    /// </summary>
    public string SourceHash = "";

    /// <summary>
    /// Hash of the exact content the mod last wrote to this file. When the file on disk still hashes
    /// to this, nobody has edited our output and it is safe to refresh silently. Without it we could
    /// not tell "the player changed this" apart from "this file is simply old".
    /// </summary>
    public string WrittenHash = "";

    /// <summary>Only meaningful for magiceffects, whose embedded source depends on BalanceConfigurationType.</summary>
    public string Variant = "";

    /// <summary>
    /// The SourceHash the player declined an update for. The prompt stays quiet while it still
    /// matches, so a later release that changes this config's default moves the hash and asks again.
    /// </summary>
    public string DeclinedSourceHash = "";

    /// <summary>Recorded alongside DeclinedSourceHash purely so the file reads sensibly by hand.</summary>
    public string DeclinedVersion = "";
}

[Serializable]
public class ConfigVersionState
{
    public Dictionary<string, ConfigVersionEntry> Files = new Dictionary<string, ConfigVersionEntry>();

    // Lives outside baseconfig/ so it can never be confused for a config the player should edit, and
    // so it can never trip the per-file FileSystemWatchers registered in ELConfig.SychronizeConfig.
    public static string FilePath => Path.Combine(Paths.ConfigPath, "EpicLoot", "configstate.json");

    public static ConfigVersionState Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                ConfigVersionState loaded =
                    JsonConvert.DeserializeObject<ConfigVersionState>(File.ReadAllText(FilePath));
                if (loaded != null)
                {
                    loaded.Files ??= new Dictionary<string, ConfigVersionEntry>();
                    return loaded;
                }
            }
        }
        catch (Exception e)
        {
            // A corrupt state file must never block startup; treat it as absent. Every config then
            // reads as "unknown origin", which surfaces the update prompt rather than hiding a problem.
            EpicLoot.LogWarningForce($"Could not read {FilePath}, treating config versions as unknown.\n{e.Message}");
        }

        return new ConfigVersionState();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception e)
        {
            EpicLoot.LogWarningForce($"Could not write {FilePath}! Config version tracking will not persist.\n{e.Message}");
        }
    }

    public ConfigVersionEntry Get(string configName)
    {
        return Files.TryGetValue(configName, out ConfigVersionEntry entry) ? entry : null;
    }

    /// <summary>
    /// Records a file as being exactly the mod's own output. Any previous decline is dropped: the
    /// player's edits are no longer present, so there is nothing left to protect.
    /// </summary>
    public void Stamp(string configName, string sourceHash, string writtenHash, string variant)
    {
        Files[configName] = new ConfigVersionEntry
        {
            Version = EpicLoot.Version,
            SourceHash = sourceHash,
            WrittenHash = writtenHash ?? "",
            Variant = variant ?? ""
        };
    }

    /// <summary>
    /// Moves an entry to the current mod version without otherwise disturbing it. Used when the
    /// shipped default has not changed, where the file is still valid and any decline still applies.
    /// </summary>
    public void TouchVersion(string configName)
    {
        if (Files.TryGetValue(configName, out ConfigVersionEntry entry))
        {
            entry.Version = EpicLoot.Version;
        }
    }

    public void RecordDecline(string configName, string sourceHash)
    {
        if (!Files.TryGetValue(configName, out ConfigVersionEntry entry))
        {
            entry = new ConfigVersionEntry();
            Files[configName] = entry;
        }

        entry.DeclinedSourceHash = sourceHash ?? "";
        entry.DeclinedVersion = EpicLoot.Version;
    }

    /// <summary>
    /// Hashes text rather than bytes: the shipped configs have inconsistent UTF-8 BOMs and line
    /// endings, which would otherwise produce spurious mismatches between platforms and checkouts.
    /// A false mismatch here would make an untouched config look edited and prompt forever, so the
    /// BOM is stripped explicitly rather than trusted to the caller's reader - Trim() will not
    /// remove U+FEFF, which .NET does not classify as whitespace.
    /// </summary>
    public static string HashConfigText(string text)
    {
        if (text == null)
        {
            return "";
        }

        string normalized = text.Replace("\uFEFF", "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();

        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            StringBuilder sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}
