using EpicLoot.Adventure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EpicLoot.Biomes
{
    /// <summary>
    /// A biome the mod knows about, resolved from biomedata.json or appended from a legacy
    /// adventuredata Bounties.Bosses entry.
    /// </summary>
    public sealed class BiomeDefinition
    {
        public string Name;
        public Heightmap.Biome Biome;
        /// <summary>Position in progression order, 0 first. Not the raw Order value from the file.</summary>
        public int Index;
        public IReadOnlyList<string> BossDefeatedKeys = Array.Empty<string>();
        public string Color = "white";
        public string DisplayName;
        public bool IsVanilla;
        public bool IsLegacy;
    }

    /// <summary>
    /// The biome registry: the one place that maps biome names to Heightmap.Biome values and back,
    /// and that owns biome progression order, boss keys and presentation. Every user-facing config
    /// takes biome names as strings and resolves them here, so a biome another mod adds only has to
    /// be declared once, in biomedata.json.
    /// </summary>
    public static class BiomeDataManager
    {
        public static BiomeDataConfig Config = new BiomeDataConfig();

        #nullable enable
        public static event Action? OnBiomeDataInitialized;
        #nullable disable

        private static readonly Regex NamePattern = new Regex("^[A-Za-z0-9]+$", RegexOptions.Compiled);
        private static readonly Regex DigitsPattern = new Regex("^[0-9]+$", RegexOptions.Compiled);

        private static List<BiomeDefinition> _inOrder = new List<BiomeDefinition>();
        private static Dictionary<string, BiomeDefinition> _byName =
            new Dictionary<string, BiomeDefinition>(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<Heightmap.Biome, BiomeDefinition> _byBiome =
            new Dictionary<Heightmap.Biome, BiomeDefinition>();
        // Flat (biome, key) list in progression order, duplicates kept, so a key shared by two
        // biomes (Ocean and BlackForest both use defeated_gdking) keeps the neighbours it has today.
        private static List<KeyValuePair<Heightmap.Biome, string>> _bossKeysInOrder =
            new List<KeyValuePair<Heightmap.Biome, string>>();
        private static List<BountyBossConfig> _legacyBosses = new List<BountyBossConfig>();
        private static readonly HashSet<string> _warnedUnresolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _legacyNoticeLogged;

        public static void Initialize(BiomeDataConfig config)
        {
            if (config == null)
            {
                EpicLoot.LogWarning("BiomeDataManager.Initialize called with a null config; keeping the currently loaded biome data.");
                return;
            }

            Config = config;
            _warnedUnresolved.Clear();
            _legacyNoticeLogged = false;
            Rebuild();
            EpicLoot.Log($"Biome data loaded, progression order: {string.Join(", ", _inOrder.Select(b => b.Name))}");
            OnBiomeDataInitialized?.Invoke();
        }

        public static BiomeDataConfig GetCFG()
        {
            return Config;
        }

        /// <summary>
        /// Registers the deprecated adventuredata Bounties.Bosses list. Entries for biomes the registry
        /// already defines are ignored; the rest are appended to the end of the order so an un-migrated
        /// file keeps working. Rebuilds the lookups without firing <see cref="OnBiomeDataInitialized"/>,
        /// since it is called from the adventure data handler of that very event.
        /// </summary>
        internal static void SetLegacyBosses(IEnumerable<BountyBossConfig> bosses)
        {
            _legacyBosses = bosses?.Where(b => b != null).ToList() ?? new List<BountyBossConfig>();
            Rebuild();
        }

        public static IReadOnlyList<BiomeDefinition> BiomesInOrder => _inOrder;

        /// <summary>Biomes that are not part of the vanilla Heightmap.Biome enum.</summary>
        public static IEnumerable<BiomeDefinition> CustomBiomes => _inOrder.Where(d => !d.IsVanilla);

        /// <summary>
        /// Resolves a biome name, a vanilla enum name, or a numeric value. Names win over numbers, so
        /// "8" is BlackForest but a biome named "8" cannot exist (names need a letter).
        /// </summary>
        public static bool TryResolve(string nameOrId, out Heightmap.Biome biome)
        {
            biome = Heightmap.Biome.None;
            if (string.IsNullOrWhiteSpace(nameOrId))
            {
                return false;
            }

            string value = nameOrId.Trim();
            if (_byName.TryGetValue(value, out BiomeDefinition def))
            {
                biome = def.Biome;
                return true;
            }

            if (TryParseVanillaName(value, out biome))
            {
                return true;
            }

            if (int.TryParse(value, out int number) && number >= 0)
            {
                biome = (Heightmap.Biome)number;
                return true;
            }

            return false;
        }

        /// <summary>
        /// <see cref="TryResolve"/> with a fallback. An unknown value is reported once per config load,
        /// not once per lookup, because the merchant panel re-resolves every entry on each refresh.
        /// </summary>
        public static Heightmap.Biome Resolve(string nameOrId, Heightmap.Biome fallback = Heightmap.Biome.None)
        {
            if (string.IsNullOrWhiteSpace(nameOrId))
            {
                return fallback;
            }

            if (TryResolve(nameOrId, out Heightmap.Biome biome))
            {
                return biome;
            }

            if (_warnedUnresolved.Add(nameOrId.Trim()))
            {
                EpicLoot.LogWarningForce($"Unknown biome '{nameOrId}'. Use a vanilla biome name, a biome " +
                    $"defined in biomedata.json, or its numeric Heightmap.Biome value.");
            }

            return fallback;
        }

        public static bool TryGetDefinition(Heightmap.Biome biome, out BiomeDefinition definition)
        {
            return _byBiome.TryGetValue(biome, out definition);
        }

        public static bool IsKnown(Heightmap.Biome biome)
        {
            return _byBiome.ContainsKey(biome);
        }

        /// <summary>Registry name, else the enum name, else the number a custom value prints as.</summary>
        public static string GetName(Heightmap.Biome biome)
        {
            return _byBiome.TryGetValue(biome, out BiomeDefinition def) ? def.Name : biome.ToString();
        }

        /// <summary>
        /// Display token for a biome. The fallback is the token vanilla builds for its own biomes and
        /// the one Expand World Data registers for the biomes it adds ("biome_1024"), so custom biomes
        /// display correctly with no DisplayName at all.
        /// </summary>
        public static string GetLocalizationToken(Heightmap.Biome biome)
        {
            if (_byBiome.TryGetValue(biome, out BiomeDefinition def) && !string.IsNullOrEmpty(def.DisplayName))
            {
                return def.DisplayName;
            }

            return $"$biome_{biome.ToString().ToLowerInvariant()}";
        }

        public static string GetColor(Heightmap.Biome biome)
        {
            return _byBiome.TryGetValue(biome, out BiomeDefinition def) ? def.Color : "white";
        }

        /// <summary>Sort key in progression order. None sorts first, unknown biomes last.</summary>
        public static int GetOrder(Heightmap.Biome biome)
        {
            if (biome == Heightmap.Biome.None)
            {
                return -1;
            }

            return _byBiome.TryGetValue(biome, out BiomeDefinition def) ? def.Index : int.MaxValue;
        }

        public static IReadOnlyList<string> GetBossKeys(Heightmap.Biome biome)
        {
            return _byBiome.TryGetValue(biome, out BiomeDefinition def) ? def.BossDefeatedKeys : Array.Empty<string>();
        }

        /// <summary>True when every boss key of the biome is set. A biome with no keys is never gated.</summary>
        public static bool HasAllBossKeys(Heightmap.Biome biome)
        {
            if (ZoneSystem.instance == null)
            {
                return false;
            }

            foreach (string key in GetBossKeys(biome))
            {
                if (!ZoneSystem.instance.GetGlobalKey(key))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Every boss key in progression order; a key shared by several biomes appears once per biome.</summary>
        public static IEnumerable<string> BossKeysInOrder => _bossKeysInOrder.Select(x => x.Value);

        public static Heightmap.Biome GetFirstBiomeForBossKey(string bossKey)
        {
            if (string.IsNullOrEmpty(bossKey))
            {
                return Heightmap.Biome.None;
            }

            foreach (KeyValuePair<Heightmap.Biome, string> entry in _bossKeysInOrder)
            {
                if (entry.Value == bossKey)
                {
                    return entry.Key;
                }
            }

            return Heightmap.Biome.None;
        }

        /// <summary>The boss key one step earlier in progression order, or null for the first key or an unknown one.</summary>
        public static string GetPrevBossKey(string bossKey)
        {
            if (string.IsNullOrEmpty(bossKey))
            {
                return null;
            }

            int index = _bossKeysInOrder.FindIndex(x => x.Value == bossKey);
            return index <= 0 ? null : _bossKeysInOrder[index - 1].Value;
        }

        public static bool IsVanillaValue(Heightmap.Biome biome)
        {
            return biome != Heightmap.Biome.None && biome != Heightmap.Biome.All &&
                Enum.IsDefined(typeof(Heightmap.Biome), biome);
        }

        private static void Rebuild()
        {
            var accepted = new List<(BiomeDefinition def, int order, int position)>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedBiomes = new HashSet<Heightmap.Biome>();

            List<BiomeEntryConfig> entries = Config?.Biomes ?? new List<BiomeEntryConfig>();
            for (int i = 0; i < entries.Count; i++)
            {
                int position = i + 1;
                if (!TryBuildDefinition(entries[i], position, out BiomeDefinition def, out int order))
                {
                    continue;
                }

                if (!usedNames.Add(def.Name))
                {
                    Warn($"biomedata.json entry #{position}: duplicate biome name '{def.Name}', keeping the first one.");
                    continue;
                }

                if (!usedBiomes.Add(def.Biome))
                {
                    Warn($"biomedata.json entry #{position}: duplicate biome ID {(int)def.Biome} ('{def.Name}'), keeping the first one.");
                    continue;
                }

                accepted.Add((def, order, position));
            }

            _inOrder = accepted.OrderBy(x => x.order).ThenBy(x => x.position).Select(x => x.def).ToList();
            _byName = new Dictionary<string, BiomeDefinition>(StringComparer.OrdinalIgnoreCase);
            _byBiome = new Dictionary<Heightmap.Biome, BiomeDefinition>();
            foreach (BiomeDefinition def in _inOrder)
            {
                _byName[def.Name] = def;
                _byBiome[def.Biome] = def;
            }

            MergeLegacyBosses();

            for (int i = 0; i < _inOrder.Count; i++)
            {
                _inOrder[i].Index = i;
            }

            _bossKeysInOrder = _inOrder
                .SelectMany(def => def.BossDefeatedKeys.Select(key => new KeyValuePair<Heightmap.Biome, string>(def.Biome, key)))
                .ToList();
        }

        private static void MergeLegacyBosses()
        {
            var ignored = new List<string>();
            foreach (BountyBossConfig legacy in _legacyBosses)
            {
                string raw = legacy.Biome?.Trim();
                if (string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                if (!TryResolve(raw, out Heightmap.Biome biome) || biome == Heightmap.Biome.None || biome == Heightmap.Biome.All)
                {
                    Warn($"adventuredata.json Bounties.Bosses entry '{raw}' does not name a known biome; skipped.");
                    continue;
                }

                if (_byBiome.ContainsKey(biome))
                {
                    ignored.Add(raw);
                    continue;
                }

                bool isVanilla = IsVanillaValue(biome);
                var def = new BiomeDefinition
                {
                    Name = isVanilla ? biome.ToString() : ((int)biome).ToString(),
                    Biome = biome,
                    BossDefeatedKeys = string.IsNullOrWhiteSpace(legacy.BossDefeatedKey) ?
                        new List<string>() : new List<string> { legacy.BossDefeatedKey.Trim() },
                    IsVanilla = isVanilla,
                    IsLegacy = true
                };
                _inOrder.Add(def);
                _byName[def.Name] = def;
                _byBiome[def.Biome] = def;
                EpicLoot.LogWarning($"adventuredata.json Bounties.Bosses entry '{raw}' is not in biomedata.json and was " +
                    $"appended to the end of the biome order. Bounties.Bosses is deprecated; define the biome in biomedata.json.");
            }

            if (ignored.Count > 0 && !_legacyNoticeLogged)
            {
                _legacyNoticeLogged = true;
                EpicLoot.Log($"adventuredata.json Bounties.Bosses is deprecated; its entries for {string.Join(", ", ignored)} " +
                    $"are ignored because biome order and boss keys now come from biomedata.json.");
            }
        }

        private static bool TryBuildDefinition(BiomeEntryConfig entry, int position, out BiomeDefinition def, out int order)
        {
            def = null;
            order = int.MaxValue;

            if (entry == null)
            {
                Warn($"biomedata.json entry #{position} is empty; skipped.");
                return false;
            }

            string name = entry.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                Warn($"biomedata.json entry #{position} has no Name; skipped.");
                return false;
            }

            if (!NamePattern.IsMatch(name) || DigitsPattern.IsMatch(name))
            {
                Warn($"biomedata.json entry #{position}: biome name '{name}' must be letters and digits only " +
                    $"(no spaces or underscores) with at least one letter; skipped.");
                return false;
            }

            Heightmap.Biome biome;
            bool isVanilla;
            if (TryParseVanillaName(name, out Heightmap.Biome vanilla))
            {
                if (vanilla == Heightmap.Biome.None || vanilla == Heightmap.Biome.All)
                {
                    Warn($"biomedata.json entry #{position}: '{name}' is reserved; skipped.");
                    return false;
                }

                if (entry.ID.HasValue && entry.ID.Value != (int)vanilla)
                {
                    Warn($"biomedata.json entry '{name}': ID {entry.ID.Value} ignored, the vanilla biome {vanilla} is always {(int)vanilla}.");
                }

                biome = vanilla;
                name = vanilla.ToString();
                isVanilla = true;
            }
            else
            {
                if (!entry.ID.HasValue || entry.ID.Value <= 0)
                {
                    Warn($"biomedata.json entry '{name}' is not a vanilla biome and has no ID. A custom biome needs the " +
                        $"Heightmap.Biome value its biome mod assigns (Expand World Data uses 1024, 2048, 4096, ...); skipped.");
                    return false;
                }

                int id = entry.ID.Value;
                if ((id & (id - 1)) != 0)
                {
                    Warn($"biomedata.json entry '{name}': ID {id} is not a power of two. Biome values are single flags; skipped.");
                    return false;
                }

                biome = (Heightmap.Biome)id;
                if (IsVanillaValue(biome) || biome == Heightmap.Biome.All)
                {
                    Warn($"biomedata.json entry '{name}': ID {id} is the vanilla biome {biome}; use the name '{biome}' instead; skipped.");
                    return false;
                }

                isVanilla = false;
            }

            List<string> keys = (entry.BossDefeatedKeys ?? new List<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Distinct()
                .ToList();

            def = new BiomeDefinition
            {
                Name = name,
                Biome = biome,
                BossDefeatedKeys = keys,
                Color = ParseColor(entry.Color, name),
                DisplayName = string.IsNullOrWhiteSpace(entry.DisplayName) ? null : entry.DisplayName.Trim(),
                IsVanilla = isVanilla
            };
            order = entry.Order ?? int.MaxValue;
            return true;
        }

        private static bool TryParseVanillaName(string value, out Heightmap.Biome biome)
        {
            biome = Heightmap.Biome.None;
            if (DigitsPattern.IsMatch(value))
            {
                return false;
            }

            if (Enum.TryParse(value, true, out Heightmap.Biome parsed) && Enum.IsDefined(typeof(Heightmap.Biome), parsed))
            {
                biome = parsed;
                return true;
            }

            return false;
        }

        private static string ParseColor(string color, string biomeName)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                return "white";
            }

            string trimmed = color.Trim();
            if (ColorUtility.TryParseHtmlString(trimmed, out _))
            {
                return trimmed;
            }

            Warn($"biomedata.json entry '{biomeName}': Color '{color}' is not a valid HTML color; using white.");
            return "white";
        }

        private static void Warn(string message)
        {
            EpicLoot.LogWarningForce(message);
        }
    }
}
