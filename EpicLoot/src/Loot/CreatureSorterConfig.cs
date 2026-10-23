using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot
{
    /// <summary>How tough a creature is within its biome, which picks the rung of the biome's ladder.</summary>
    public enum CreatureClass
    {
        Weak,
        Normal,
        Strong,
        Elite,
        Boss
    }

    /// <summary>
    /// A biome's loot table template per creature class (itemsorter.json BiomeSorterData.*.Creatures).
    /// A rung left out falls back to Normal.
    /// </summary>
    [Serializable]
    public class CreatureLadder
    {
        // Base health that counts as 1.0 for this biome's health ratios. Without it, the median of the
        // biome's creatures found this run is used.
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? ReferenceHealth;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Weak;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Normal;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Strong;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Elite;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Boss;

        public string Get(CreatureClass creatureClass)
        {
            return creatureClass switch
            {
                CreatureClass.Weak => Weak,
                CreatureClass.Normal => Normal,
                CreatureClass.Strong => Strong,
                CreatureClass.Elite => Elite,
                CreatureClass.Boss => Boss,
                _ => null
            };
        }

        /// <summary>The template for a class, falling back to Normal.</summary>
        public string TemplateFor(CreatureClass creatureClass)
        {
            string template = Get(creatureClass);
            return string.IsNullOrEmpty(template) ? Normal : template;
        }
    }

    /// <summary>What a generated entry carries on top of its RefObject, per creature class.</summary>
    [Serializable]
    public class CreatureClassExtras
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public float? StarMultiplier;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public LootModifiers Modifiers;
    }

    /// <summary>An admin's decision for one creature, which wins over everything the sorter would work out.</summary>
    [Serializable]
    public class CreatureOverride
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Biome;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Class;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public string Template;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public bool? Skip;
    }

    /// <summary>
    /// itemsorter.json CreatureSorter. Every member is optional; <see cref="CreatureSorterSettings"/>
    /// fills whatever is missing from the code-side defaults, so an older file keeps working.
    /// Collections are left null rather than initialized: Newtonsoft appends to a pre-filled collection.
    /// </summary>
    [Serializable]
    public class CreatureSorterConfig
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, float> HealthRatios;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? SkipPassive;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ExcludeFactions;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> ExcludeNames;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<string> VariantSuffixes;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> FactionBiomes;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? ScanLocations;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, CreatureClassExtras> ClassExtras;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, CreatureOverride> Overrides;
    }

    /// <summary>The creature sorter's rules with every default filled in.</summary>
    public sealed class CreatureSorterSettings
    {
        public float WeakRatio = 0.55f;
        public float StrongRatio = 1.5f;
        public float EliteRatio = 2.5f;
        public bool SkipPassive = true;
        public bool ScanLocations = true;
        public HashSet<string> ExcludeFactions;
        public List<string> ExcludeNames;
        public List<string> VariantSuffixes;
        public Dictionary<string, string> FactionBiomes;
        public Dictionary<CreatureClass, CreatureClassExtras> ClassExtras;
        public Dictionary<string, CreatureOverride> Overrides;

        // Keyed by biome name as the sorter's registry spells it.
        public Dictionary<string, CreatureLadder> Ladders;

        public static readonly string[] DefaultExcludeFactions = { "Players", "PlayerSpawned", "TrainingDummy" };

        public static readonly string[] DefaultExcludeNames =
            { "*_Summoned", "*_spiritcaller", "Aspect_*", "*_Fader", "staff_*", "*Test" };

        public static readonly string[] DefaultVariantSuffixes =
            { "_sleeping", "_noarcher", "_cave", "_nochest", "_NonSleeping" };

        public static readonly Dictionary<string, string> DefaultFactionBiomes = new Dictionary<string, string>
        {
            { "ForestMonsters", "BlackForest" },
            { "Undead", "Swamp" },
            { "MountainMonsters", "Mountain" },
            { "PlainsMonsters", "Plains" },
            { "SeaMonsters", "Ocean" },
            { "MistlandsMonsters", "Mistlands" },
            { "Dverger", "Mistlands" },
            { "Demon", "AshLands" },
            { "DeepNorth", "DeepNorth" },
        };

        // Mirrors the Creatures ladders shipped in config/itemsorter.json, for a file written before they
        // existed. Keep the two in sync.
        public static readonly Dictionary<string, CreatureLadder> DefaultLadders = new Dictionary<string, CreatureLadder>
        {
            { "Meadows",     Ladder(15,   "Tier0Mob", "Tier0Mob", "Tier1Mob", "Tier1Mob",      "Tier2Mob") },
            { "BlackForest", Ladder(80,   "Tier1Mob", "Tier2Mob", "Tier2Mob", "Tier3EliteMob", "Tier3EliteMob") },
            { "Ocean",       Ladder(400,  "Tier3Mob", "Tier5Mob", "Tier5Mob", "Tier5EliteMob", "Tier5EliteMob") },
            { "Swamp",       Ladder(100,  "Tier3Mob", "Tier3Mob", "Tier4Mob", "Tier4EliteMob", "Tier4EliteMob") },
            { "Mountain",    Ladder(90,   "Tier4Mob", "Tier4Mob", "Tier5Mob", "Tier5EliteMob", "Tier5EliteMob") },
            { "Plains",      Ladder(175,  "Tier5Mob", "Tier5Mob", "Tier6Mob", "Tier6EliteMob", "Tier6EliteMob") },
            { "Mistlands",   Ladder(350,  "Tier6Mob", "Tier7Mob", "Tier7Mob", "Tier7EliteMob", "Tier7EliteMob") },
            { "AshLands",    Ladder(600,  "Tier7Mob", "Tier8Mob", "Tier8Mob", "Tier8EliteMob", "Tier8EliteMob") },
            { "DeepNorth",   Ladder(400,  "Tier8Mob", "Tier8Mob", "Tier9Mob", "Tier9EliteMob", "Tier9EliteMob") },
        };

        // Vanilla creatures the rules would place but that should not drop loot (farmable, player-made,
        // unused, or a boss phase that is not the one that drops). Mirrors CreatureSorter.Overrides in
        // config/itemsorter.json.
        public static readonly string[] DefaultSkips =
            { "Hen", "BlobFrost", "SeekerBrood", "Ghost_Void", "Ghost_old", "FrozenKing", "DvergerTest" };

        public static readonly CreatureClassExtras DefaultBossExtras = new CreatureClassExtras
        {
            Modifiers = new LootModifiers { DropRate = 10, BonusDrops = 1, RarityShift = 0.2f }
        };

        private static CreatureLadder Ladder(float health, string weak, string normal, string strong, string elite, string boss)
        {
            return new CreatureLadder
            {
                ReferenceHealth = health, Weak = weak, Normal = normal, Strong = strong, Elite = elite, Boss = boss
            };
        }

        /// <summary>
        /// Resolves the file's rules against the defaults. <paramref name="ladders"/> are the Creatures
        /// ladders read from BiomeSorterData, already keyed by registry biome name; a biome the file gives
        /// no ladder keeps the default one.
        /// </summary>
        public static CreatureSorterSettings From(CreatureSorterConfig config, Dictionary<string, CreatureLadder> ladders)
        {
            CreatureSorterSettings settings = new CreatureSorterSettings();
            if (config?.HealthRatios != null)
            {
                settings.WeakRatio = Ratio(config.HealthRatios, "Weak", settings.WeakRatio);
                settings.StrongRatio = Ratio(config.HealthRatios, "Strong", settings.StrongRatio);
                settings.EliteRatio = Ratio(config.HealthRatios, "Elite", settings.EliteRatio);
            }

            settings.SkipPassive = config?.SkipPassive ?? true;
            settings.ScanLocations = config?.ScanLocations ?? true;
            settings.ExcludeFactions = new HashSet<string>(config?.ExcludeFactions ?? new List<string>(DefaultExcludeFactions),
                StringComparer.OrdinalIgnoreCase);
            settings.ExcludeNames = config?.ExcludeNames ?? new List<string>(DefaultExcludeNames);
            settings.VariantSuffixes = config?.VariantSuffixes ?? new List<string>(DefaultVariantSuffixes);
            settings.FactionBiomes = new Dictionary<string, string>(config?.FactionBiomes ?? DefaultFactionBiomes,
                StringComparer.OrdinalIgnoreCase);
            settings.Overrides = new Dictionary<string, CreatureOverride>(
                config?.Overrides ?? DefaultSkips.ToDictionary(name => name, _ => new CreatureOverride { Skip = true }),
                StringComparer.OrdinalIgnoreCase);

            settings.ClassExtras = new Dictionary<CreatureClass, CreatureClassExtras>();
            if (config?.ClassExtras != null)
            {
                foreach (KeyValuePair<string, CreatureClassExtras> pair in config.ClassExtras)
                {
                    if (Enum.TryParse(pair.Key, true, out CreatureClass creatureClass) && pair.Value != null)
                    {
                        settings.ClassExtras[creatureClass] = pair.Value;
                    }
                }
            }
            else
            {
                settings.ClassExtras[CreatureClass.Boss] = DefaultBossExtras;
            }

            settings.Ladders = new Dictionary<string, CreatureLadder>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, CreatureLadder> pair in DefaultLadders)
            {
                settings.Ladders[pair.Key] = pair.Value;
            }

            if (ladders != null)
            {
                foreach (KeyValuePair<string, CreatureLadder> pair in ladders)
                {
                    if (pair.Value != null)
                    {
                        settings.Ladders[pair.Key] = pair.Value;
                    }
                }
            }

            return settings;
        }

        private static float Ratio(Dictionary<string, float> ratios, string key, float fallback)
        {
            foreach (KeyValuePair<string, float> pair in ratios)
            {
                if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase) && pair.Value > 0)
                {
                    return pair.Value;
                }
            }

            return fallback;
        }
    }
}
