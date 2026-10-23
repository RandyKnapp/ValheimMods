using EpicLoot.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;

namespace EpicLoot
{
    /// <summary>
    /// The runtime writers of baseconfig/loottables.json: the equipment auto-add and the creature sorter
    /// rewrite it from the live config, and the legacy conversion upgrades a flat file in place. Every
    /// one of them records the result with ConfigVersionManager the same way, so a file that was the
    /// mod's own stays the mod's own and a player's stays the player's.
    /// </summary>
    internal static class LootConfigFile
    {
        private const string ConfigName = "loottables";

        private static string FilePath => Path.Combine(ELConfig.GetOverhaulDirectoryPath(), $"{ConfigName}.json");

        /// <summary>Serializes a whole loot config over the on-disk file. False when the write failed.</summary>
        internal static bool Write(LootConfig config, string reason)
        {
            try
            {
                string contents = JsonConvert.SerializeObject(config, Formatting.Indented);
                string path = FilePath;
                string previousContents = File.Exists(path) ? File.ReadAllText(path) : null;
                File.WriteAllText(path, contents);
                // Claim this as the mod's own output ONLY when the baseline it merged over was also
                // ours -- a player-edited baseline must stay flagged as the player's.
                ConfigVersionManager.RecordWrittenContent(ConfigName, contents, previousContents);
                return true;
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"Failed to write loottables.json ({reason}): {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upgrades an on-disk loottables.json that still holds flat Drops/Loot tables: backs it up, then
        /// rewrites it with those tables converted to LeveledLoot. Tables already in the new form keep
        /// their content. Runs at startup before the file is read and watched, and only for a file no
        /// patch rebuilds (a patched file is converted as it is rebuilt).
        /// </summary>
        internal static void ConvertLegacyFileOnDisk()
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                string previousContents = File.ReadAllText(path);
                JObject root;
                try
                {
                    root = JObject.Parse(previousContents);
                }
                catch (JsonException)
                {
                    // SychronizeConfig reports an unreadable file and falls back to the default itself.
                    return;
                }

                List<string> converted = LootTableMigration.NormalizeJson(root);
                if (converted.Count == 0)
                {
                    return;
                }

                string backupDir = ConfigVersionManager.BackupConfigFile(ConfigName);
                string contents = root.ToString(Formatting.Indented);
                File.WriteAllText(path, contents);
                ConfigVersionManager.RecordWrittenContent(ConfigName, contents, previousContents);

                EpicLoot.LogWarningForce($"loottables.json held {converted.Count} loot table(s) in the deprecated " +
                    $"flat Drops/Loot form ({Summarize(converted)}). They were converted to LeveledLoot level 1; " +
                    $"the original was backed up to {backupDir}.");
            }
            catch (Exception e)
            {
                EpicLoot.LogErrorForce($"Converting the flat loot tables in loottables.json failed: {e.Message}");
            }
        }

        internal static string Summarize(List<string> names, int max = 8)
        {
            if (names.Count <= max)
            {
                return string.Join(", ", names);
            }

            return string.Join(", ", names.GetRange(0, max)) + $", +{names.Count - max} more";
        }
    }
}
