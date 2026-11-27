using BepInEx;
using Newtonsoft.Json;
using SoftReferenceableAssets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// What each location prefab's spawners make, kept on disk so the creature sorter loads a location
    /// prefab once instead of on every world load (seconds with a large modpack). Entries are keyed by the
    /// prefab's AssetID, and each one remembers what it was read from (its Source) and is read again only
    /// when that changes: the size and write time of the prefab's bundle files, plus, for a mod's bundle,
    /// the installed mods, since a mod's code can change what its locations spawn without touching their
    /// files. A vanilla location is therefore not read again after a mod update, only after a game update
    /// that rewrote its bundle. The location's biome is not stored: it is read live.
    /// </summary>
    [Serializable]
    public class CreatureSorterLocationCache
    {
        // Bump when what the runner records for a location changes.
        private const int Format = 2;

        public string Key;
        public Dictionary<string, LocationSpawns> Prefabs;

        [JsonIgnore] private bool _changed;
        [JsonIgnore] private string _modsText;
        [JsonIgnore] private string _streamingAssets;
        [JsonIgnore] private readonly Dictionary<string, string> _fileStamps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        [JsonIgnore] private bool _loaderUnreadable;

        public static string FilePath => Path.Combine(Paths.ConfigPath, "EpicLoot", "creaturesorter-locations.json");

        private static string CurrentKey => $"format {Format}";

        /// <summary>The file's entries when it was written in this format, else none.</summary>
        public static CreatureSorterLocationCache Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    CreatureSorterLocationCache cache =
                        JsonConvert.DeserializeObject<CreatureSorterLocationCache>(File.ReadAllText(FilePath));
                    if (cache?.Key == CurrentKey && cache.Prefabs != null)
                    {
                        return cache;
                    }
                }
            }
            catch (Exception e)
            {
                EpicLoot.Log($"Could not read {FilePath}, the location prefabs will be scanned again: {e.Message}");
            }

            return new CreatureSorterLocationCache { Key = CurrentKey, Prefabs = new Dictionary<string, LocationSpawns>() };
        }

        /// <summary>The entry for this prefab when it was read from the same source, else null.</summary>
        public LocationSpawns Get(string key, string source)
        {
            return Prefabs.TryGetValue(key, out LocationSpawns spawns) && spawns != null && spawns.Source == source
                ? spawns
                : null;
        }

        public void Set(string key, LocationSpawns spawns)
        {
            Prefabs[key] = spawns;
            _changed = true;
        }

        /// <summary>
        /// Drops the entries of prefabs no location uses any more (a mod that was removed), then writes the
        /// file when anything changed.
        /// </summary>
        public void SaveIfChanged(ICollection<string> keysInUse)
        {
            foreach (string key in Prefabs.Keys.Where(key => !keysInUse.Contains(key)).ToList())
            {
                Prefabs.Remove(key);
                _changed = true;
            }

            if (!_changed)
            {
                return;
            }

            try
            {
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
                _changed = false;
            }
            catch (Exception e)
            {
                EpicLoot.Log($"Could not write {FilePath}: {e.Message}");
            }
        }

        /// <summary>
        /// What a location prefab is read from, for <see cref="LocationSpawns.Source"/>, and whether it is the
        /// game's own (every bundle it loads from is in the game's StreamingAssets). When the loader's
        /// internals cannot be read, every prefab stamps as the game build plus the installed mods.
        /// </summary>
        public string SourceOf(AssetID assetID, out bool isVanilla)
        {
            isVanilla = false;
            string files = null;
            if (!_loaderUnreadable)
            {
                try
                {
                    files = BundleFiles(assetID, out isVanilla);
                }
                catch (Exception e)
                {
                    // Thrown when BundleFiles is compiled after a game update renamed the loader's fields, so it
                    // has to be caught here rather than inside it.
                    _loaderUnreadable = true;
                    EpicLoot.LogWarning($"Creature sorter could not read the asset loader ({e.GetType().Name}); " +
                        "location prefabs are cached against the installed mods instead.");
                }
            }

            if (files == null)
            {
                isVanilla = false;
                return "mods " + InstalledMods.Hash(ModsText);
            }

            return isVanilla
                ? "files " + InstalledMods.Hash(files)
                : "files+mods " + InstalledMods.Hash(files + ModsText);
        }

        private string ModsText => _modsText ??= InstalledMods.Describe();

        // The bundle files a prefab loads from (its own and its dependencies), one line each: name, size and
        // write time. Null when the asset is not in a bundle on disk -- Jotunn wraps its in-memory assets in
        // bundles with no path. The SoftReferenceableAssets members are internal, reached through the
        // publicized reference, which is why this is kept out of line (see SourceOf).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private string BundleFiles(AssetID assetID, out bool isVanilla)
        {
            isVanilla = false;
            SoftReferenceableAssets.AssetBundleLoader loader = SoftReferenceableAssets.AssetBundleLoader.Instance;
            if (loader == null || !loader.Initialized || loader.m_assetIDToLoaderIndex == null ||
                !loader.m_assetIDToLoaderIndex.TryGetValue(assetID, out int assetIndex))
            {
                return null;
            }

            int bundleIndex = loader.m_assetLoaders[assetIndex].m_bundleLoaderIndex;
            if (string.IsNullOrEmpty(loader.m_bundleLoaders[bundleIndex].m_bundlePath))
            {
                return null;
            }

            int[] indices = loader.m_bundleLoaders[bundleIndex].BundleLoaderIndices ?? new[] { bundleIndex };
            _streamingAssets ??= Path.GetFullPath(Application.streamingAssetsPath);
            isVanilla = true;
            StringBuilder text = new StringBuilder();
            foreach (int index in indices)
            {
                string path = loader.m_bundleLoaders[index].m_bundlePath;
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }

                if (!_fileStamps.TryGetValue(path, out string stamp))
                {
                    string fullPath = Path.GetFullPath(path);
                    StringBuilder line = new StringBuilder(Path.GetFileName(fullPath));
                    InstalledMods.AppendFileStamp(line, fullPath);
                    stamp = (fullPath.StartsWith(_streamingAssets, StringComparison.OrdinalIgnoreCase) ? "game " : "mod ") + line;
                    _fileStamps[path] = stamp;
                }

                isVanilla &= stamp.StartsWith("game ", StringComparison.Ordinal);
                text.Append(stamp).Append('\n');
            }

            return text.ToString();
        }
    }

    [Serializable]
    public class LocationSpawns
    {
        // The prefab's name, so the file reads sensibly by hand.
        public string Name;

        // What the entry was read from (CreatureSorterLocationCache.SourceOf); a different value reads it again.
        public string Source;

        // The prefab could not be loaded or inspected. Kept, so it is not loaded again on every world load,
        // and tried again when its source changes.
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Unreadable;

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
