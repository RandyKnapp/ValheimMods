using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EpicLoot
{
    /// <summary>
    /// Converts the deprecated flat loot table form (a table-level Drops/Loot pair) into LeveledLoot.
    /// Every path a table takes into the game runs through <see cref="Normalize"/> -- the config file,
    /// patches, a server push and the API -- so nothing downstream reads the flat fields any more.
    ///
    /// The flat pair used to answer creature levels 1-3 (winning over any LeveledLoot entry there) and
    /// nothing above that; the conversion keeps what every level used to roll, and gives level 4+ the
    /// flat data instead of the null that used to throw.
    /// </summary>
    public static class LootTableMigration
    {
        // The auto-add rewrite writes "Drops": null and "Loot": [] onto every table; those are not legacy.
        public static bool IsLegacy(LootTable table)
        {
#pragma warning disable CS0618 // the obsolete flat fields are exactly what this reads
            return table != null && (!IsNullOrEmpty(table.Drops) || !IsNullOrEmpty(table.Loot));
#pragma warning restore CS0618
        }

        /// <summary>
        /// Folds a table's flat Drops/Loot into LeveledLoot, in place. Returns true when the table was in
        /// the flat form. Idempotent; empty flat fields are cleared either way.
        /// </summary>
        public static bool Normalize(LootTable table)
        {
            if (table == null)
            {
                return false;
            }

#pragma warning disable CS0618
            bool hasDrops = !IsNullOrEmpty(table.Drops);
            bool hasLoot = !IsNullOrEmpty(table.Loot);
            float[][] flatDrops = table.Drops;
            LootDrop[] flatLoot = table.Loot;
            table.Drops = null;
            table.Loot = null;
#pragma warning restore CS0618

            if (!hasDrops && !hasLoot)
            {
                return false;
            }

            table.LeveledLoot ??= new List<LeveledLootDef>();
            if (hasDrops)
            {
                Fold(table.LeveledLoot, def => def.Drops, (def, value) => def.Drops = value, flatDrops);
            }

            if (hasLoot)
            {
                Fold(table.LeveledLoot, def => def.Loot, (def, value) => def.Loot = value, flatLoot);
            }

            // Levels 1-3 whose data the flat pair used to shadow are now empty shells.
            table.LeveledLoot.RemoveAll(def => def == null ||
                (def.Level != 1 && def.Drops == null && def.Loot == null &&
                 (def.ExtraData == null || def.ExtraData.Count == 0)));
            table.LeveledLoot.Sort((a, b) => a.Level.CompareTo(b.Level));
            return true;
        }

        private static void Fold<T>(List<LeveledLootDef> levels, Func<LeveledLootDef, T[]> get,
            Action<LeveledLootDef, T[]> set, T[] flat)
        {
            // What the old walk-down handed level 4: the first entry at each level from 4 down, taking the
            // first one whose field is non-empty.
            T[] levelFour = null;
            int levelFourFrom = 0;
            for (int level = 4; level >= 1; level--)
            {
                LeveledLootDef found = levels.Find(def => def != null && def.Level == level);
                if (found != null && !IsNullOrEmpty(get(found)))
                {
                    levelFour = get(found);
                    levelFourFrom = level;
                    break;
                }
            }

            // The flat pair owned levels 1-3.
            foreach (LeveledLootDef def in levels)
            {
                if (def != null && def.Level >= 1 && def.Level <= 3)
                {
                    set(def, null);
                }
            }

            LeveledLootDef levelOne = levels.Find(def => def != null && def.Level == 1);
            if (levelOne == null)
            {
                levelOne = new LeveledLootDef { Level = 1 };
                levels.Add(levelOne);
            }

            set(levelOne, flat);

            // Level 4 and up used to fall through to leveled data below 4 that the flat pair hid from
            // levels 1-3. Pin it at level 4 so those levels keep rolling it.
            if (levelFour != null && levelFourFrom < 4)
            {
                LeveledLootDef levelFourDef = levels.Find(def => def != null && def.Level == 4);
                if (levelFourDef == null)
                {
                    levelFourDef = new LeveledLootDef { Level = 4 };
                    levels.Add(levelFourDef);
                }

                set(levelFourDef, levelFour);
            }
        }

        // ------------------------------------------------------------------------------------------------
        //  JSON side: the patch pipeline and the on-disk file work on a JObject.
        // ------------------------------------------------------------------------------------------------

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });

        /// <summary>True when a LootTables element still carries a non-empty flat Drops or Loot array.</summary>
        public static bool IsLegacyToken(JToken tableToken)
        {
            return tableToken is JObject table &&
                (table["Drops"] is JArray { Count: > 0 } || table["Loot"] is JArray { Count: > 0 });
        }

        /// <summary>
        /// Converts every flat table in a loottables.json root in place and returns the Object names it
        /// converted (empty when the file was already clean). Tables that are not flat are left
        /// byte-for-byte alone.
        /// </summary>
        public static List<string> NormalizeJson(JObject root)
        {
            List<string> converted = new List<string>();
            if (root?["LootTables"] is not JArray tables)
            {
                return converted;
            }

            for (int i = 0; i < tables.Count; i++)
            {
                if (!IsLegacyToken(tables[i]))
                {
                    continue;
                }

                LootTable table = tables[i].ToObject<LootTable>(Serializer);
                Normalize(table);
                tables[i] = JObject.FromObject(table, Serializer);
                converted.Add(table?.Object ?? "<unnamed>");
            }

            return converted;
        }

        /// <summary>
        /// Rewrites a JSONPath aimed at a loot table's flat Loot or Drops onto its level 1 entry:
        /// <c>$.LootTables[?(@.Object == 'X')].Loot</c> becomes
        /// <c>$.LootTables[?(@.Object == 'X')].LeveledLoot[?(@.Level == 1)].Loot</c>. Bracket notation
        /// (<c>['Loot']</c>) is recognized too. Paths into ItemSets, or anywhere else, are not touched.
        /// </summary>
        public static bool TryRedirectLegacyPath(string path, out string redirected)
        {
            redirected = path;
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            int index;
            if (path.StartsWith("$.LootTables", StringComparison.Ordinal))
            {
                index = "$.LootTables".Length;
            }
            else if (path.StartsWith("$['LootTables']", StringComparison.Ordinal) ||
                     path.StartsWith("$[\"LootTables\"]", StringComparison.Ordinal))
            {
                index = "$['LootTables']".Length;
            }
            else
            {
                return false;
            }

            // The element selector: one bracketed group, or ".*".
            if (index < path.Length && path[index] == '[')
            {
                int close = FindClosingBracket(path, index);
                if (close < 0)
                {
                    return false;
                }

                index = close + 1;
            }
            else if (string.CompareOrdinal(path, index, ".*", 0, 2) == 0)
            {
                index += 2;
            }
            else
            {
                return false;
            }

            if (!TryReadMember(path, index, out string member, out int afterMember) ||
                (member != "Loot" && member != "Drops"))
            {
                return false;
            }

            redirected = path.Substring(0, index) + ".LeveledLoot[?(@.Level == 1)]." + member +
                path.Substring(afterMember);
            return true;
        }

        private static readonly Regex LevelFilter = new Regex(
            @"^(?<tables>.*?)\.LeveledLoot\[\?\(\s*@\.Level\s*==\s*(?<level>\d+)\s*\)\]", RegexOptions.Compiled);

        /// <summary>
        /// For a path that picks one level of a table's LeveledLoot, the part that selects the tables and
        /// the level it asks for -- so a patch that no longer matches can say which levels exist.
        /// </summary>
        public static bool TryGetLevelFilter(string path, out string tablesPath, out int level)
        {
            tablesPath = null;
            level = 0;
            Match match = LevelFilter.Match(path ?? "");
            if (!match.Success)
            {
                return false;
            }

            tablesPath = match.Groups["tables"].Value;
            return int.TryParse(match.Groups["level"].Value, out level);
        }

        // Reads ".Name" or "['Name']" / "[\"Name\"]" at index; the member must end the path or be
        // followed by another segment.
        private static bool TryReadMember(string path, int index, out string member, out int after)
        {
            member = null;
            after = index;
            if (index >= path.Length)
            {
                return false;
            }

            if (path[index] == '.')
            {
                int start = index + 1;
                int end = start;
                while (end < path.Length && (char.IsLetterOrDigit(path[end]) || path[end] == '_'))
                {
                    end++;
                }

                if (end == start || (end < path.Length && path[end] != '.' && path[end] != '['))
                {
                    return false;
                }

                member = path.Substring(start, end - start);
                after = end;
                return true;
            }

            if (path[index] == '[' && index + 1 < path.Length && (path[index + 1] == '\'' || path[index + 1] == '"'))
            {
                char quote = path[index + 1];
                int closeQuote = path.IndexOf(quote, index + 2);
                if (closeQuote < 0 || closeQuote + 1 >= path.Length || path[closeQuote + 1] != ']')
                {
                    return false;
                }

                member = path.Substring(index + 2, closeQuote - index - 2);
                after = closeQuote + 2;
                return true;
            }

            return false;
        }

        // Matching ']' for the '[' at open, skipping quoted strings and nested brackets.
        private static int FindClosingBracket(string path, int open)
        {
            int depth = 0;
            char quote = '\0';
            for (int i = open; i < path.Length; i++)
            {
                char c = path[i];
                if (quote != '\0')
                {
                    if (c == quote)
                    {
                        quote = '\0';
                    }

                    continue;
                }

                switch (c)
                {
                    case '\'':
                    case '"':
                        quote = c;
                        break;
                    case '[':
                        depth++;
                        break;
                    case ']':
                        depth--;
                        if (depth == 0)
                        {
                            return i;
                        }

                        break;
                }
            }

            return -1;
        }

        private static bool IsNullOrEmpty<T>(T[] array)
        {
            return array == null || array.Length == 0;
        }
    }
}
