using BepInEx;
using EpicLoot.Biomes;
using EpicLoot.Config;
using EpicLoot.Magic;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// Runs the creature sorter against the live game: gathers CreatureFacts from ZNetScene, the spawn
    /// lists, raids and (only when something is still unplaced) the location prefabs, remembered between
    /// loads in CreatureSorterLocationCache, then writes an "Auto" loottables.json entry for every creature
    /// that has no table of its own.
    ///
    /// It runs where the loot config is owned -- a dedicated server, a host, single player -- once the
    /// world's locations are set up, which a headless server reaches too. A connected client takes the
    /// server's tables, "Auto" entries included, from the config push.
    /// </summary>
    public static class CreatureSorterRunner
    {
        // The last run's decisions, for Quick Configure's template grouping and the console command.
        internal static List<CreatureDecision> LastDecisions;

        // Masks covering more biomes than this say "anywhere", not where a creature comes from.
        private const int MaxBiomesInInformativeMask = 3;

        private static bool _running;

        [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SetupLocations))]
        public static class ZoneSystem_SetupLocations_Patch
        {
            // Last, so locations and raids other mods add in their own postfixes are in.
            [HarmonyPriority(Priority.Last)]
            public static void Postfix()
            {
                Run("world load", true);
                // The equipment pass normally runs on the minimap hook, which a headless server never reaches.
                AutoAddEnchantableItems.RunOnHeadlessWorldLoad();
            }
        }

        /// <summary>Re-sorts when a world is loaded; does nothing otherwise (the world load will).</summary>
        public static void RunIfWorldLoaded(string reason)
        {
            if (ZNetScene.instance == null || ZoneSystem.instance == null)
            {
                return;
            }

            Run(reason, true);
        }

        /// <summary>
        /// Sorts and, when <paramref name="write"/> is set and this machine owns the config, writes the
        /// result. Returns the decisions and the merge, or null when the sorter could not run.
        /// </summary>
        public static (List<CreatureDecision> Decisions, CreatureSorter.MergeResult Merge)? Run(string reason, bool write)
        {
            if (_running || LootRoller.Config == null || ZNetScene.instance == null)
            {
                return null;
            }

            if (write && (ELConfig.AutoAddCreaturesToLootTables?.Value != true || !OwnsLootConfig()))
            {
                return null;
            }

            _running = true;
            try
            {
                Stopwatch timer = Stopwatch.StartNew();
                (List<CreatureDecision> decisions, (int Loaded, int Cached)? locations) = Classify();
                LastDecisions = decisions;
                CreatureSorter.MergeResult merge = CreatureSorter.Merge(LootRoller.Config.LootTables, decisions);

                // Where an explicit creature entry names another template than the rules would: nothing is
                // changed, but it shows in the report (the shipped vanilla entries should agree).
                Dictionary<string, string> explicitRefs = LootRoller.Config.LootTables
                    .Where(t => t != null && t.Auto != true && !string.IsNullOrEmpty(t.Object) && !string.IsNullOrEmpty(t.RefObject))
                    .GroupBy(t => t.Object, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First().RefObject, StringComparer.Ordinal);
                int disagreements = CreatureSorter.MarkDisagreements(decisions,
                    name => explicitRefs.TryGetValue(name, out string refObject) ? refObject : null);

                int unresolved = decisions.Count(d => d.Action == CreatureAction.Unresolved || d.Action == CreatureAction.NoTemplate);
                string locationNote = locations is (int loaded, int cached)
                    ? $" (location prefabs: {loaded} scanned, {cached} from cache)"
                    : "";
                string summary = $"Creature sorter ({reason}): {decisions.Count(d => d.IsCandidate)} creature(s) without " +
                    $"a loot table; {merge.Added.Count} added, {merge.Updated.Count} updated, {merge.Removed.Count} removed, " +
                    $"{unresolved} could not be placed{locationNote}, " +
                    $"{disagreements} explicit entr{(disagreements == 1 ? "y differs" : "ies differ")} from the rules; " +
                    $"{timer.ElapsedMilliseconds} ms.";

                if (write)
                {
                    WriteReport(decisions, merge, summary);
                    if (merge.Changed)
                    {
                        LootConfig updated = LootRoller.Config.ShallowCopy();
                        updated.LootTables = merge.Tables;
                        if (LootConfigFile.Write(updated, "creature sorter"))
                        {
                            ELConfig.ReloadBaseConfigsFromDisk(new[] { "loottables.json" });
                        }
                    }

                    if (merge.Changed || unresolved > 0)
                    {
                        EpicLoot.LogForce(summary + " See creaturesorter-report.txt.");
                    }
                    else
                    {
                        EpicLoot.Log(summary);
                    }
                }

                return (decisions, merge);
            }
            catch (Exception e)
            {
                EpicLoot.LogErrorForce($"Creature sorter ({reason}) failed: {e}");
                return null;
            }
            finally
            {
                _running = false;
            }
        }

        private static bool OwnsLootConfig()
        {
            return ZNet.instance != null && ZNet.instance.IsServer();
        }

        // The decisions, and how many location prefabs were loaded or read from the cache when the location
        // pass ran.
        private static (List<CreatureDecision>, (int Loaded, int Cached)?) Classify()
        {
            CreatureBiomeRegistry registry = new CreatureBiomeRegistry(BiomeDataManager.BiomesInOrder
                .Select(def => new KeyValuePair<string, IEnumerable<string>>(def.Name, def.BossDefeatedKeys)));
            List<KeyValuePair<string, HashSet<string>>> biomeMaterials = new List<KeyValuePair<string, HashSet<string>>>();
            Dictionary<string, CreatureLadder> ladders = new Dictionary<string, CreatureLadder>(StringComparer.OrdinalIgnoreCase);
            AutoAddEnchantableItems.AutoSorterConfiguration sorterConfig = AutoAddEnchantableItems.Config;
            if (sorterConfig?.BiomeSorterData != null)
            {
                foreach (KeyValuePair<string, AutoAddEnchantableItems.SortingData> entry in sorterConfig.BiomeSorterData)
                {
                    string biome = CanonicalBiome(registry, entry.Key);
                    if (biome == null || entry.Value == null)
                    {
                        continue;
                    }

                    if (entry.Value.BiomeMaterials != null && biomeMaterials.All(pair => pair.Key != biome))
                    {
                        biomeMaterials.Add(new KeyValuePair<string, HashSet<string>>(biome,
                            new HashSet<string>(entry.Value.BiomeMaterials)));
                    }

                    if (entry.Value.Creatures != null && !ladders.ContainsKey(biome))
                    {
                        ladders[biome] = entry.Value.Creatures;
                    }
                }
            }

            biomeMaterials.Sort((a, b) => registry.Order(a.Key).CompareTo(registry.Order(b.Key)));
            CreatureSorterSettings settings = CreatureSorterSettings.From(sorterConfig?.CreatureSorter, ladders);

            HashSet<string> autoNames = new HashSet<string>(LootRoller.Config.LootTables
                .Where(t => t?.Auto == true && t.Object != null).Select(t => t.Object), StringComparer.Ordinal);
            HashSet<string> ownNames = new HashSet<string>(LootRoller.Config.LootTables
                .Where(t => t != null && t.Auto != true && t.Object != null).Select(t => t.Object), StringComparer.Ordinal);
            // An Auto entry's key holds its template's list; a table in it named after the creature itself
            // came from somewhere else (the API) and is the creature's own.
            bool HasOwnTable(string name) => ownNames.Contains(name) ||
                (LootRoller.LootTables.TryGetValue(name, out List<LootTable> own) &&
                 (!autoNames.Contains(name) || own.Any(t => t != null && t.Object == name && string.IsNullOrEmpty(t.RefObject))));
            bool TemplateExists(string name) => !autoNames.Contains(name) &&
                LootRoller.LootTables.TryGetValue(name, out List<LootTable> tables) && tables.Count > 0;

            Dictionary<string, CreatureFacts> facts = GatherCreatures();
            GatherSpawnLists(facts, registry);
            GatherRaids(facts, registry);

            List<CreatureDecision> decisions = CreatureSorter.Classify(facts.Values.ToList(), settings, registry,
                biomeMaterials, HasOwnTable, TemplateExists, false);
            (int Loaded, int Cached)? locations = null;
            if (settings.ScanLocations && decisions.Any(d => d.Action == CreatureAction.Unresolved && d.Biome == null))
            {
                locations = GatherLocations(facts, registry);
            }

            decisions = CreatureSorter.Classify(facts.Values.ToList(), settings, registry, biomeMaterials,
                HasOwnTable, TemplateExists, true);
            return (decisions, locations);
        }

        private static string CanonicalBiome(CreatureBiomeRegistry registry, string name)
        {
            if (registry.Canonical(name) is string canonical)
            {
                return canonical;
            }

            return BiomeDataManager.TryResolve(name, out Heightmap.Biome biome) && biome != Heightmap.Biome.None &&
                BiomeDataManager.IsKnown(biome)
                ? registry.Canonical(BiomeDataManager.GetName(biome))
                : null;
        }

        // ------------------------------------------------------------------------------------------------
        //  Gathering
        // ------------------------------------------------------------------------------------------------

        private static Dictionary<string, CreatureFacts> GatherCreatures()
        {
            Dictionary<string, CreatureFacts> facts = new Dictionary<string, CreatureFacts>(StringComparer.Ordinal);
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab == null || facts.ContainsKey(prefab.name))
                {
                    continue;
                }

                // Prefab-time reads only: GetMaxHealth()/IsTamed() go through a ZNetView a prefab never
                // initialized.
                Character character = prefab.GetComponent<Character>();
                if (character == null || prefab.GetComponent<Player>() != null)
                {
                    continue;
                }

                CharacterDrop characterDrop = prefab.GetComponent<CharacterDrop>();
                Tameable tameable = prefab.GetComponent<Tameable>();
                facts[prefab.name] = new CreatureFacts
                {
                    Name = prefab.name,
                    Token = character.m_name,
                    Faction = character.m_faction.ToString(),
                    Health = character.m_health,
                    Boss = character.m_boss,
                    HasCharacterDrop = characterDrop != null,
                    AchievementExcluded = character.m_killedForAchievements == global::Utils.AchievementInclusion.Excluded,
                    StartsTamed = tameable != null && tameable.m_startsTamed,
                    IsOffspring = prefab.GetComponent<Growup>() != null,
                    IsPassive = prefab.GetComponent<AnimalAI>() != null,
                    DropItems = characterDrop?.m_drops?
                        .Where(drop => drop?.m_prefab != null)
                        .Select(drop => drop.m_prefab.name)
                        .ToList() ?? new List<string>()
                };
            }

            return facts;
        }

        private static void GatherSpawnLists(Dictionary<string, CreatureFacts> facts, CreatureBiomeRegistry registry)
        {
            // The zone control prefab's own lists, plus every other SpawnSystemList that is loaded --
            // which is how Jotunn's list of mod creature spawns (added to spawn systems at runtime) is
            // seen without reflection.
            HashSet<SpawnSystemList> lists = new HashSet<SpawnSystemList>();
            GameObject zoneCtrl = ZoneSystem.instance != null ? ZoneSystem.instance.m_zoneCtrlPrefab : null;
            SpawnSystem spawnSystem = zoneCtrl != null ? zoneCtrl.GetComponent<SpawnSystem>() : null;
            if (spawnSystem?.m_spawnLists != null)
            {
                lists.UnionWith(spawnSystem.m_spawnLists.Where(list => list != null));
            }

            lists.UnionWith(Resources.FindObjectsOfTypeAll<SpawnSystemList>());

            foreach (SpawnSystemList list in lists)
            {
                foreach (SpawnSystem.SpawnData spawn in list.m_spawners ?? new List<SpawnSystem.SpawnData>())
                {
                    if (spawn == null || !spawn.m_enabled || spawn.m_devDisabled || spawn.m_prefab == null ||
                        !string.IsNullOrEmpty(spawn.m_requiredPersistentEvent))
                    {
                        continue;
                    }

                    CreatureFacts creature = CreatureFor(facts, spawn.m_prefab);
                    if (creature == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(spawn.m_requiredGlobalKey))
                    {
                        AddBiome(creature.SpawnBiomes, EarliestInMask(spawn.m_biome, registry));
                    }
                    else
                    {
                        // Behind a boss key it is that boss's biome invading; behind any other key the
                        // spawn's own biome is still where it lives.
                        AddBiome(creature.GatedSpawnBiomes, registry.BiomeOfBossKey(spawn.m_requiredGlobalKey) ??
                            EarliestInMask(spawn.m_biome, registry));
                    }
                }
            }

            foreach (AltBiome altBiome in AltBiomeList.m_altBiomes)
            {
                if (altBiome == null || !altBiome.m_enabled || altBiome.m_spawn == null)
                {
                    continue;
                }

                string parent = EarliestInMask(altBiome.m_biome, registry);
                foreach (SpawnSystem.SpawnData spawn in altBiome.m_spawn)
                {
                    if (spawn != null && spawn.m_enabled && spawn.m_prefab != null)
                    {
                        AddBiome(CreatureFor(facts, spawn.m_prefab)?.AltSpawnBiomes, parent);
                    }
                }
            }
        }

        private static void GatherRaids(Dictionary<string, CreatureFacts> facts, CreatureBiomeRegistry registry)
        {
            if (RandEventSystem.instance?.m_events == null)
            {
                return;
            }

            foreach (RandomEvent raid in RandEventSystem.instance.m_events)
            {
                if (raid == null || !raid.m_enabled || raid.m_spawn == null)
                {
                    continue;
                }

                // A raid that stops once a boss dies previews that boss's biome (army_seekers: queen ->
                // Mistlands). One that only needs bosses dead comes from the latest of them.
                string previewBiome = registry.Earliest(raid.m_notRequiredGlobalKeys?.Select(registry.BiomeOfBossKey));
                string gatedBiome = raid.m_requiredGlobalKeys?
                    .Select(registry.BiomeOfBossKey)
                    .Where(biome => biome != null)
                    .OrderByDescending(registry.Order)
                    .FirstOrDefault();

                foreach (SpawnSystem.SpawnData spawn in raid.m_spawn)
                {
                    CreatureFacts creature = spawn?.m_prefab != null ? CreatureFor(facts, spawn.m_prefab) : null;
                    if (creature == null)
                    {
                        continue;
                    }

                    if (previewBiome != null)
                    {
                        AddBiome(creature.RaidBiomes, previewBiome);
                    }
                    else
                    {
                        AddBiome(creature.GatedRaidBiomes, gatedBiome);
                    }
                }
            }
        }

        // Which creatures each location's spawners make. Reading that means loading the location prefab, so
        // only a prefab the cache does not know yet is loaded, and what it held is added to the cache. Only
        // reached when a creature is still unplaced after everything cheaper.
        private static (int Loaded, int Cached) GatherLocations(Dictionary<string, CreatureFacts> facts, CreatureBiomeRegistry registry)
        {
            if (ZoneSystem.instance?.m_locations == null)
            {
                return (0, 0);
            }

            CreatureSorterLocationCache cache = CreatureSorterLocationCache.Load();
            int loaded = 0;
            int cached = 0;
            bool added = false;
            foreach (ZoneSystem.ZoneLocation location in ZoneSystem.instance.m_locations)
            {
                if (location == null || !location.m_enable || !location.m_prefab.IsValid)
                {
                    continue;
                }

                string biome = EarliestInMask(location.m_biome, registry);
                if (biome == null)
                {
                    continue;
                }

                string key = location.m_prefab.m_assetID.ToString();
                if (cache.Prefabs.TryGetValue(key, out LocationSpawns spawns))
                {
                    cached++;
                }
                else
                {
                    loaded++;
                    // A prefab that could not be read is left out of the cache, to be tried again next time.
                    spawns = ReadLocationSpawns(location);
                    if (spawns == null)
                    {
                        continue;
                    }

                    cache.Prefabs[key] = spawns;
                    added = true;
                }

                foreach (SpawnedPrefab spawned in spawns.Spawns ?? new List<SpawnedPrefab>())
                {
                    AddBiome(CreatureFor(facts, spawned)?.LocationBiomes, biome);
                }
            }

            if (added)
            {
                cache.Save();
            }

            return (loaded, cached);
        }

        // Loads a location prefab just long enough to note every prefab its spawners make; null when it
        // cannot be read.
        private static LocationSpawns ReadLocationSpawns(ZoneSystem.ZoneLocation location)
        {
            try
            {
                location.m_prefab.Load();
                GameObject asset = location.m_prefab.Asset;
                if (asset == null)
                {
                    return null;
                }

                List<GameObject> prefabs = new List<GameObject>();
                foreach (CreatureSpawner spawner in asset.GetComponentsInChildren<CreatureSpawner>(true))
                {
                    prefabs.Add(spawner.m_creaturePrefab);
                }

                foreach (SpawnArea area in asset.GetComponentsInChildren<SpawnArea>(true))
                {
                    foreach (SpawnArea.SpawnData spawn in area.m_prefabs ?? new List<SpawnArea.SpawnData>())
                    {
                        prefabs.Add(spawn?.m_prefab);
                    }
                }

                foreach (OfferingBowl bowl in asset.GetComponentsInChildren<OfferingBowl>(true))
                {
                    prefabs.Add(bowl.m_bossPrefab);
                }

                foreach (TriggerSpawner trigger in asset.GetComponentsInChildren<TriggerSpawner>(true))
                {
                    prefabs.AddRange(trigger.m_creaturePrefabs ?? Array.Empty<GameObject>());
                }

                // Both names are kept: which one is a creature is decided against the creatures of each run.
                List<SpawnedPrefab> spawns = new List<SpawnedPrefab>();
                HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (GameObject prefab in prefabs)
                {
                    if (prefab == null)
                    {
                        continue;
                    }

                    CreatureSpawner spawner = prefab.GetComponent<CreatureSpawner>();
                    string creature = spawner != null && spawner.m_creaturePrefab != null ? spawner.m_creaturePrefab.name : null;
                    if (seen.Add(prefab.name + "\n" + creature))
                    {
                        spawns.Add(new SpawnedPrefab { Prefab = prefab.name, Creature = creature });
                    }
                }

                return new LocationSpawns { Name = asset.name, Spawns = spawns };
            }
            catch (Exception e)
            {
                EpicLoot.Log($"Creature sorter could not inspect location {location.m_name}: {e.Message}");
                return null;
            }
            finally
            {
                location.m_prefab.Release();
            }
        }

        // The creature a spawn entry makes: the prefab itself, or what a CreatureSpawner prefab spawns.
        private static CreatureFacts CreatureFor(Dictionary<string, CreatureFacts> facts, GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            if (facts.TryGetValue(prefab.name, out CreatureFacts creature))
            {
                return creature;
            }

            CreatureSpawner spawner = prefab.GetComponent<CreatureSpawner>();
            return spawner != null && spawner.m_creaturePrefab != null &&
                facts.TryGetValue(spawner.m_creaturePrefab.name, out creature)
                ? creature
                : null;
        }

        // The same, for a location's spawn as the cache holds it.
        private static CreatureFacts CreatureFor(Dictionary<string, CreatureFacts> facts, SpawnedPrefab spawned)
        {
            if (spawned?.Prefab != null && facts.TryGetValue(spawned.Prefab, out CreatureFacts creature))
            {
                return creature;
            }

            return spawned?.Creature != null && facts.TryGetValue(spawned.Creature, out creature) ? creature : null;
        }

        private static string EarliestInMask(Heightmap.Biome mask, CreatureBiomeRegistry registry)
        {
            if (mask == Heightmap.Biome.None)
            {
                return null;
            }

            List<string> inMask = BiomeDataManager.BiomesInOrder
                .Where(def => def.Biome != Heightmap.Biome.None && (mask & def.Biome) != 0)
                .Select(def => def.Name)
                .ToList();
            return inMask.Count > 0 && inMask.Count <= MaxBiomesInInformativeMask ? registry.Earliest(inMask) : null;
        }

        private static void AddBiome(List<string> biomes, string biome)
        {
            if (biomes != null && !string.IsNullOrEmpty(biome) && !biomes.Contains(biome))
            {
                biomes.Add(biome);
            }
        }

        // ------------------------------------------------------------------------------------------------
        //  Report
        // ------------------------------------------------------------------------------------------------

        private static string ReportPath => Path.Combine(Paths.ConfigPath, "EpicLoot", "creaturesorter-report.txt");

        private static void WriteReport(List<CreatureDecision> decisions, CreatureSorter.MergeResult merge, string summary)
        {
            try
            {
                File.WriteAllText(ReportPath, CreatureSorter.FormatReport(decisions, merge,
                    $"{summary}\nWritten {DateTime.Now:yyyy-MM-dd HH:mm:ss}. Rules: itemsorter.json CreatureSorter " +
                    "and BiomeSorterData.*.Creatures. Pin a creature under CreatureSorter.Overrides."));
            }
            catch (Exception e)
            {
                EpicLoot.Log($"Could not write {ReportPath}: {e.Message}");
            }
        }
    }
}
