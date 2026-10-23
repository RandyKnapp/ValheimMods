using BepInEx;
using BepInEx.Bootstrap;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EpicLoot
{
    /// <summary>
    /// What each location prefab's spawners make, kept on disk so the creature sorter loads a location
    /// prefab once instead of on every world load (seconds with a large modpack). Entries are keyed by the
    /// prefab's AssetID. The whole file is dropped when the game build, any plugin's version or the format
    /// changes, since a mod update can change a location's spawners and keep its AssetID (Jotunn derives a
    /// custom location's from its name). The location's biome is not stored: it is read live.
    /// </summary>
    [Serializable]
    public class CreatureSorterLocationCache
    {
        // Bump when what the runner records for a location changes.
        private const int Format = 1;

        public string Key;
        public Dictionary<string, LocationSpawns> Prefabs;

        public static string FilePath => Path.Combine(Paths.ConfigPath, "EpicLoot", "creaturesorter-locations.json");

        /// <summary>The file's entries when it was written for this game build and plugin set, else none.</summary>
        public static CreatureSorterLocationCache Load()
        {
            string key = CurrentKey();
            try
            {
                if (File.Exists(FilePath))
                {
                    CreatureSorterLocationCache cache =
                        JsonConvert.DeserializeObject<CreatureSorterLocationCache>(File.ReadAllText(FilePath));
                    if (cache?.Key == key && cache.Prefabs != null)
                    {
                        return cache;
                    }
                }
            }
            catch (Exception e)
            {
                EpicLoot.Log($"Could not read {FilePath}, the location prefabs will be scanned again: {e.Message}");
            }

            return new CreatureSorterLocationCache { Key = key, Prefabs = new Dictionary<string, LocationSpawns>() };
        }

        public void Save()
        {
            try
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception e)
            {
                EpicLoot.Log($"Could not write {FilePath}: {e.Message}");
            }
        }

        private static string CurrentKey()
        {
            StringBuilder text = new StringBuilder();
            text.Append(Format).Append('\n').Append(global::Version.GetVersionString(true)).Append('\n');
            foreach (PluginInfo plugin in Chainloader.PluginInfos.Values
                .Where(p => p?.Metadata != null)
                .OrderBy(p => p.Metadata.GUID, StringComparer.Ordinal))
            {
                text.Append(plugin.Metadata.GUID).Append('@').Append(plugin.Metadata.Version).Append('\n');
            }

            using (SHA256 sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-", "");
            }
        }
    }

    [Serializable]
    public class LocationSpawns
    {
        // The prefab's name, so the file reads sensibly by hand.
        public string Name;
        public List<SpawnedPrefab> Spawns;
    }

    [Serializable]
    public class SpawnedPrefab
    {
        public string Prefab;

        // What the prefab spawns when it is itself a CreatureSpawner.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Creature;
    }
}
