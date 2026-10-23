using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EpicLoot
{
    /// <summary>What the game data says about one creature prefab, gathered by CreatureSorterRunner.</summary>
    public sealed class CreatureFacts
    {
        public string Name;
        public string Token;
        public string Faction;
        public float Health;
        public bool Boss;
        public bool HasCharacterDrop;
        public bool AchievementExcluded;
        public bool StartsTamed;
        public bool IsOffspring;
        public bool IsPassive;
        public List<string> DropItems = new List<string>();

        // Biome evidence, strongest first. Every list holds registry biome names.
        public List<string> SpawnBiomes = new List<string>();       // ungated world spawns
        public List<string> AltSpawnBiomes = new List<string>();    // alternate-biome spawns (their parent biome)
        public List<string> RaidBiomes = new List<string>();        // raids that stop once a boss dies: that boss's biome
        public List<string> GatedSpawnBiomes = new List<string>();  // key-gated world spawns
        public List<string> GatedRaidBiomes = new List<string>();   // raids that only need bosses dead
        public List<string> LocationBiomes = new List<string>();    // locations holding a spawner for it
    }

    /// <summary>The biome registry in progression order, as the sorter needs it.</summary>
    public sealed class CreatureBiomeRegistry
    {
        private readonly List<string> _names = new List<string>();
        private readonly List<List<string>> _bossKeys = new List<List<string>>();

        public CreatureBiomeRegistry(IEnumerable<KeyValuePair<string, IEnumerable<string>>> biomesInOrder)
        {
            foreach (KeyValuePair<string, IEnumerable<string>> biome in biomesInOrder)
            {
                if (string.IsNullOrEmpty(biome.Key) || Canonical(biome.Key) != null)
                {
                    continue;
                }

                _names.Add(biome.Key);
                _bossKeys.Add((biome.Value ?? Enumerable.Empty<string>()).Where(k => !string.IsNullOrEmpty(k)).ToList());
            }
        }

        public IReadOnlyList<string> Names => _names;

        public int Order(string biome)
        {
            for (int i = 0; i < _names.Count; i++)
            {
                if (string.Equals(_names[i], biome, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return int.MaxValue;
        }

        /// <summary>The registry's spelling of a biome name, or null when it is not a known biome.</summary>
        public string Canonical(string name)
        {
            return _names.FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsBossKey(string key)
        {
            return BiomeOfBossKey(key) != null;
        }

        /// <summary>The first biome (by progression) whose boss sets this key.</summary>
        public string BiomeOfBossKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            for (int i = 0; i < _names.Count; i++)
            {
                if (_bossKeys[i].Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)))
                {
                    return _names[i];
                }
            }

            return null;
        }

        /// <summary>The earliest known biome among these, or null.</summary>
        public string Earliest(IEnumerable<string> biomes)
        {
            return biomes?.Where(b => Order(b) != int.MaxValue).OrderBy(Order).FirstOrDefault();
        }
    }

    public enum CreatureAction
    {
        OwnTable,
        Excluded,
        Skipped,
        Unresolved,
        NoTemplate,
        Assign
    }

    public sealed class CreatureDecision
    {
        public string Name;
        public CreatureFacts Facts;
        public string Biome;
        public string BiomeSource;
        public float HealthRatio;
        public CreatureClass Class;
        public string Template;
        public CreatureClassExtras Extras;
        public CreatureAction Action;
        public string Reason;

        // An explicit entry that names a different template than the rules would pick for it.
        public bool Disagrees;

        public bool IsCandidate => Action != CreatureAction.OwnTable && Action != CreatureAction.Excluded;
    }

    /// <summary>
    /// Decides a loot table template for every creature that has none: its biome from the strongest
    /// evidence available, its class from its health against the biome's, and the template from the
    /// biome's ladder. Pure -- the game data comes in as CreatureFacts -- so it can be tested offline.
    /// </summary>
    public static class CreatureSorter
    {
        /// <summary>
        /// One decision per creature, candidates or not: the biome is worked out for every creature so a
        /// variant can inherit it from a base that has its own table. <paramref name="useFallbacks"/>
        /// false stops before the drops and faction guesses, to find out whether a location scan could
        /// still place anything.
        /// </summary>
        public static List<CreatureDecision> Classify(IList<CreatureFacts> creatures, CreatureSorterSettings settings,
            CreatureBiomeRegistry registry, IList<KeyValuePair<string, HashSet<string>>> biomeMaterials,
            Func<string, bool> hasOwnTable, Func<string, bool> templateExists, bool useFallbacks)
        {
            Dictionary<string, CreatureDecision> byName = new Dictionary<string, CreatureDecision>(StringComparer.OrdinalIgnoreCase);
            List<CreatureDecision> decisions = new List<CreatureDecision>();
            foreach (CreatureFacts facts in creatures)
            {
                if (facts == null || string.IsNullOrEmpty(facts.Name) || byName.ContainsKey(facts.Name))
                {
                    continue;
                }

                CreatureDecision decision = new CreatureDecision { Name = facts.Name, Facts = facts };
                settings.Overrides.TryGetValue(facts.Name, out CreatureOverride pin);
                if (hasOwnTable(facts.Name))
                {
                    decision.Action = CreatureAction.OwnTable;
                    decision.Reason = "has its own loot table";
                }
                else if (ExclusionReason(facts, settings, pin) is string reason)
                {
                    decision.Action = CreatureAction.Excluded;
                    decision.Reason = reason;
                }
                else if (pin?.Skip == true)
                {
                    decision.Action = CreatureAction.Skipped;
                    decision.Reason = "skipped by Overrides";
                }
                else
                {
                    decision.Action = CreatureAction.Unresolved;
                }

                if (pin != null && !string.IsNullOrEmpty(pin.Biome))
                {
                    decision.Biome = registry.Canonical(pin.Biome) ?? pin.Biome;
                    decision.BiomeSource = "Overrides";
                }

                byName[facts.Name] = decision;
                decisions.Add(decision);
            }

            Pass(decisions, "spawn list", d => registry.Earliest(d.Facts.SpawnBiomes));
            Pass(decisions, "alternate biome spawn", d => registry.Earliest(d.Facts.AltSpawnBiomes));
            Pass(decisions, "raid", d => registry.Earliest(d.Facts.RaidBiomes));
            Variants(decisions, byName, settings);
            Pass(decisions, "gated spawn", d => registry.Earliest(d.Facts.GatedSpawnBiomes));
            Pass(decisions, "boss-gated raid", d => registry.Earliest(d.Facts.GatedRaidBiomes));
            Variants(decisions, byName, settings);
            Pass(decisions, "location", d => registry.Earliest(d.Facts.LocationBiomes));
            Variants(decisions, byName, settings);
            if (useFallbacks)
            {
                Pass(decisions, "drops", d => DropBiome(d.Facts, biomeMaterials));
                Pass(decisions, "faction", d => d.Facts.Faction != null && settings.FactionBiomes.TryGetValue(d.Facts.Faction, out string biome)
                    ? registry.Canonical(biome) ?? biome
                    : null);
                Variants(decisions, byName, settings);
            }

            Dictionary<string, float> referenceHealth = ReferenceHealth(decisions, settings);
            foreach (CreatureDecision decision in decisions)
            {
                // A creature with its own table is classified too, without changing anything, so the
                // report can show where the rules and an explicit entry disagree.
                bool ownTable = decision.Action == CreatureAction.OwnTable;
                if (decision.Action != CreatureAction.Unresolved && !ownTable)
                {
                    continue;
                }

                settings.Overrides.TryGetValue(decision.Name, out CreatureOverride pin);
                if (decision.Biome == null && string.IsNullOrEmpty(pin?.Template))
                {
                    if (!ownTable)
                    {
                        decision.Reason = "no biome evidence";
                    }

                    continue;
                }

                decision.Class = ClassOf(decision, referenceHealth, settings, out decision.HealthRatio);
                if (pin != null && !string.IsNullOrEmpty(pin.Class) && Enum.TryParse(pin.Class, true, out CreatureClass pinnedClass))
                {
                    decision.Class = pinnedClass;
                }

                string template = pin?.Template;
                if (string.IsNullOrEmpty(template))
                {
                    if (!settings.Ladders.TryGetValue(decision.Biome, out CreatureLadder ladder) || ladder == null)
                    {
                        if (!ownTable)
                        {
                            decision.Action = CreatureAction.NoTemplate;
                            decision.Reason = $"biome {decision.Biome} has no Creatures ladder";
                        }

                        continue;
                    }

                    template = ladder.TemplateFor(decision.Class);
                }

                if (string.IsNullOrEmpty(template) || !templateExists(template))
                {
                    if (!ownTable)
                    {
                        decision.Action = CreatureAction.NoTemplate;
                        decision.Reason = $"template '{template}' is not a loot table";
                    }

                    continue;
                }

                decision.Template = template;
                if (ownTable)
                {
                    continue;
                }

                decision.Extras = settings.ClassExtras.TryGetValue(decision.Class, out CreatureClassExtras extras) ? extras : null;
                decision.Action = CreatureAction.Assign;
                decision.Reason = null;
            }

            return decisions;
        }

        /// <summary>
        /// Flags every creature whose explicit RefObject entry names a different template than the rules
        /// would pick for it. <paramref name="refObjectOf"/> returns a creature's explicit RefObject, or
        /// null when its table is not a RefObject entry (a boss, a chest, an API table). Returns the count.
        /// </summary>
        public static int MarkDisagreements(IEnumerable<CreatureDecision> decisions, Func<string, string> refObjectOf)
        {
            int count = 0;
            foreach (CreatureDecision decision in decisions)
            {
                if (decision.Action != CreatureAction.OwnTable || decision.Template == null)
                {
                    continue;
                }

                string current = refObjectOf(decision.Name);
                if (current == null || current == decision.Template)
                {
                    continue;
                }

                decision.Disagrees = true;
                decision.Reason = $"has its own entry ({current}); the rules would pick {decision.Template}";
                count++;
            }

            return count;
        }

        // An override that places the creature (anything but Skip) lets it past every soft exclusion; a
        // creature with no CharacterDrop can never roll loot, override or not.
        private static string ExclusionReason(CreatureFacts facts, CreatureSorterSettings settings, CreatureOverride pin)
        {
            if (!facts.HasCharacterDrop)
            {
                return "no CharacterDrop, so it never rolls loot";
            }

            if (pin != null && pin.Skip != true)
            {
                return null;
            }

            if (facts.AchievementExcluded) return "excluded from kill achievements (test or summon)";
            if (facts.StartsTamed) return "starts tamed";
            if (facts.IsOffspring) return "offspring that grows up";
            if (settings.SkipPassive && facts.IsPassive) return "passive animal";
            if (facts.Faction != null && settings.ExcludeFactions.Contains(facts.Faction)) return $"faction {facts.Faction}";

            string pattern = settings.ExcludeNames.FirstOrDefault(p => GlobMatch(facts.Name, p));
            return pattern != null ? $"name matches {pattern}" : null;
        }

        private static void Pass(List<CreatureDecision> decisions, string source, Func<CreatureDecision, string> biomeOf)
        {
            foreach (CreatureDecision decision in decisions)
            {
                if (decision.Biome != null)
                {
                    continue;
                }

                string biome = biomeOf(decision);
                if (!string.IsNullOrEmpty(biome))
                {
                    decision.Biome = biome;
                    decision.BiomeSource = source;
                }
            }
        }

        // A variant (Draugr_sleeping, Skeleton_Swamps_noarcher) lives where its base does, and a base with
        // no evidence of its own (Morgen) where its variant (Morgen_NonSleeping) does.
        private static void Variants(List<CreatureDecision> decisions, Dictionary<string, CreatureDecision> byName,
            CreatureSorterSettings settings)
        {
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (CreatureDecision decision in decisions)
                {
                    if (decision.Biome != null)
                    {
                        continue;
                    }

                    foreach (string suffix in settings.VariantSuffixes)
                    {
                        if (string.IsNullOrEmpty(suffix))
                        {
                            continue;
                        }

                        CreatureDecision related = null;
                        if (decision.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
                            decision.Name.Length > suffix.Length)
                        {
                            byName.TryGetValue(decision.Name.Substring(0, decision.Name.Length - suffix.Length), out related);
                        }

                        if (related?.Biome == null)
                        {
                            byName.TryGetValue(decision.Name + suffix, out related);
                        }

                        if (related?.Biome != null)
                        {
                            decision.Biome = related.Biome;
                            decision.BiomeSource = $"variant of {related.Name}";
                            changed = true;
                            break;
                        }
                    }
                }
            }
        }

        private static string DropBiome(CreatureFacts facts, IList<KeyValuePair<string, HashSet<string>>> biomeMaterials)
        {
            if (biomeMaterials == null || facts.DropItems.Count == 0)
            {
                return null;
            }

            // Highest biome first, the same rule the equipment sorter applies to recipe materials.
            for (int i = biomeMaterials.Count - 1; i >= 0; i--)
            {
                if (facts.DropItems.Any(item => biomeMaterials[i].Value.Contains(item)))
                {
                    return biomeMaterials[i].Key;
                }
            }

            return null;
        }

        // The ladder's ReferenceHealth, or the median of every non-boss creature placed in that biome.
        private static Dictionary<string, float> ReferenceHealth(List<CreatureDecision> decisions, CreatureSorterSettings settings)
        {
            Dictionary<string, float> result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            foreach (IGrouping<string, CreatureDecision> biome in decisions
                .Where(d => d.Biome != null && d.Action != CreatureAction.Excluded && !d.Facts.Boss && d.Facts.Health > 0)
                .GroupBy(d => d.Biome, StringComparer.OrdinalIgnoreCase))
            {
                List<float> health = biome.Select(d => d.Facts.Health).OrderBy(h => h).ToList();
                result[biome.Key] = health.Count % 2 == 1
                    ? health[health.Count / 2]
                    : (health[health.Count / 2 - 1] + health[health.Count / 2]) / 2;
            }

            foreach (KeyValuePair<string, CreatureLadder> ladder in settings.Ladders)
            {
                if (ladder.Value?.ReferenceHealth is float reference && reference > 0)
                {
                    result[ladder.Key] = reference;
                }
            }

            return result;
        }

        private static CreatureClass ClassOf(CreatureDecision decision, Dictionary<string, float> referenceHealth,
            CreatureSorterSettings settings, out float ratio)
        {
            ratio = decision.Biome != null && referenceHealth.TryGetValue(decision.Biome, out float reference) && reference > 0
                ? decision.Facts.Health / reference
                : 1f;

            if (decision.Facts.Boss) return CreatureClass.Boss;
            if (ratio >= settings.EliteRatio) return CreatureClass.Elite;
            if (ratio >= settings.StrongRatio) return CreatureClass.Strong;
            if (ratio < settings.WeakRatio) return CreatureClass.Weak;
            return CreatureClass.Normal;
        }

        /// <summary>'*' matches any run of characters; case-insensitive.</summary>
        public static bool GlobMatch(string text, string pattern)
        {
            if (text == null || string.IsNullOrEmpty(pattern))
            {
                return false;
            }

            string[] parts = pattern.Split('*');
            int position = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i];
                if (part.Length == 0)
                {
                    continue;
                }

                if (i == 0)
                {
                    if (!text.StartsWith(part, StringComparison.OrdinalIgnoreCase)) return false;
                    position = part.Length;
                }
                else if (i == parts.Length - 1)
                {
                    return text.Length - part.Length >= position &&
                        text.EndsWith(part, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    int found = text.IndexOf(part, position, StringComparison.OrdinalIgnoreCase);
                    if (found < 0) return false;
                    position = found + part.Length;
                }
            }

            return parts.Length > 1 || string.Equals(text, pattern, StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------------------------------------------
        //  Merging the decisions into LootTables
        // ------------------------------------------------------------------------------------------------

        public struct MergeResult
        {
            public LootTable[] Tables;
            public List<string> Added;
            public List<string> Updated;
            public List<string> Removed;

            public bool Changed => Added.Count + Updated.Count + Removed.Count > 0;
        }

        /// <summary>
        /// Adds an "Auto" entry for every assigned creature without one, re-sorts the existing "Auto"
        /// entries and drops the ones whose creature is gone or no longer assigned. Entries without the
        /// mark are returned untouched, in place.
        /// </summary>
        public static MergeResult Merge(LootTable[] tables, IEnumerable<CreatureDecision> decisions)
        {
            tables ??= Array.Empty<LootTable>();
            Dictionary<string, CreatureDecision> assigned = decisions
                .Where(d => d.Action == CreatureAction.Assign)
                .GroupBy(d => d.Name, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            HashSet<string> ownNames = new HashSet<string>(
                tables.Where(t => t != null && t.Auto != true && !string.IsNullOrEmpty(t.Object)).Select(t => t.Object),
                StringComparer.Ordinal);

            MergeResult result = new MergeResult
            {
                Added = new List<string>(), Updated = new List<string>(), Removed = new List<string>()
            };
            List<LootTable> merged = new List<LootTable>(tables.Length + assigned.Count);
            HashSet<string> handled = new HashSet<string>(StringComparer.Ordinal);
            foreach (LootTable table in tables)
            {
                if (table?.Auto != true)
                {
                    merged.Add(table);
                    continue;
                }

                if (table.Object != null && assigned.TryGetValue(table.Object, out CreatureDecision decision) &&
                    !ownNames.Contains(table.Object) && handled.Add(table.Object))
                {
                    LootTable stub = MakeStub(decision);
                    if (SameStub(table, stub))
                    {
                        merged.Add(table);
                    }
                    else
                    {
                        merged.Add(stub);
                        result.Updated.Add(table.Object);
                    }
                }
                else
                {
                    result.Removed.Add(table.Object ?? "<unnamed>");
                }
            }

            foreach (CreatureDecision decision in assigned.Values.OrderBy(d => d.Name, StringComparer.Ordinal))
            {
                if (!handled.Contains(decision.Name) && !ownNames.Contains(decision.Name))
                {
                    merged.Add(MakeStub(decision));
                    result.Added.Add(decision.Name);
                }
            }

            result.Tables = merged.ToArray();
            return result;
        }

        public static LootTable MakeStub(CreatureDecision decision)
        {
            LootModifiers modifiers = decision.Extras?.Modifiers;
            return new LootTable
            {
                Object = decision.Name,
                RefObject = decision.Template,
                Auto = true,
                StarMultiplier = decision.Extras?.StarMultiplier,
                Modifiers = modifiers == null
                    ? null
                    : new LootModifiers { DropRate = modifiers.DropRate, BonusDrops = modifiers.BonusDrops, RarityShift = modifiers.RarityShift }
            };
        }

        private static bool SameStub(LootTable a, LootTable b)
        {
            return a.RefObject == b.RefObject && a.StarMultiplier == b.StarMultiplier && a.StarScaling == null &&
                a.Modifiers?.DropRate == b.Modifiers?.DropRate &&
                a.Modifiers?.BonusDrops == b.Modifiers?.BonusDrops &&
                a.Modifiers?.RarityShift == b.Modifiers?.RarityShift &&
                (a.LeveledLoot == null || a.LeveledLoot.Count == 0);
        }

        // ------------------------------------------------------------------------------------------------
        //  Report
        // ------------------------------------------------------------------------------------------------

        public static string FormatReport(IEnumerable<CreatureDecision> decisions, MergeResult? merge, string header)
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine(header);
            if (merge is MergeResult m)
            {
                report.AppendLine($"Added {m.Added.Count}, updated {m.Updated.Count}, removed {m.Removed.Count} generated entries.");
                if (m.Removed.Count > 0)
                {
                    report.AppendLine("Removed: " + string.Join(", ", m.Removed));
                }
            }

            report.AppendLine();
            report.AppendLine("Creature | HP | Faction | Biome (evidence) | HP ratio | Class | Template | Result");
            foreach (CreatureDecision d in decisions
                .OrderBy(d => d.Action)
                .ThenBy(d => d.Biome ?? "~", StringComparer.OrdinalIgnoreCase)
                .ThenBy(d => d.Facts.Health)
                .ThenBy(d => d.Name, StringComparer.Ordinal))
            {
                bool classified = d.Action == CreatureAction.Assign || d.Action == CreatureAction.NoTemplate ||
                    (d.Action == CreatureAction.OwnTable && d.Template != null);
                report.Append(d.Name).Append(" | ")
                    .Append(d.Facts.Health.ToString("0", CultureInfo.InvariantCulture)).Append(" | ")
                    .Append(d.Facts.Faction ?? "-").Append(" | ")
                    .Append(d.Biome != null ? $"{d.Biome} ({d.BiomeSource})" : "-").Append(" | ")
                    .Append(classified ? d.HealthRatio.ToString("0.##", CultureInfo.InvariantCulture) : "-").Append(" | ")
                    .Append(classified ? d.Class.ToString() : "-").Append(" | ")
                    .Append(d.Template ?? "-").Append(" | ")
                    .Append(d.Action).Append(d.Reason != null ? $": {d.Reason}" : "")
                    .AppendLine();
            }

            return report.ToString();
        }
    }
}
