using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EpicLoot_UnityLib
{
    /// <summary>
    /// The search box above every enchanting table item list. Plain text shows a row whose name contains any
    /// of its words, or which carries every comma-separated phrase as an enchantment. A leading @ searches
    /// enchantments only. Each phrase is matched whole against one enchantment name, so "attack speed" does
    /// not match "Movement Speed".
    /// Kept free of Unity types: the row hands over its name and enchantment names as plain strings.
    /// </summary>
    public static class ItemSearchFilter
    {
        public const char EnchantOnlyPrefix = '@';

        private static readonly Regex RichTextRegex = new Regex(@"<[^>]*>", RegexOptions.Compiled);
        private static readonly Regex PlaceholderRegex = new Regex(@"\{[^}]*\}", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);
        private static readonly char[] NameWordSeparators = { ' ', ',' };

        public class Query
        {
            public bool EnchantOnly;
            public string[] NameWords = Array.Empty<string>();
            public readonly List<string> Phrases = new List<string>();

            // An empty box, a bare @ or nothing but commas: every row shows.
            public bool IsEmpty => Phrases.Count == 0;
        }

        public static Query Parse(string text)
        {
            Query query = new Query();
            string trimmed = text?.Trim() ?? string.Empty;
            if (trimmed.Length > 0 && trimmed[0] == EnchantOnlyPrefix)
            {
                query.EnchantOnly = true;
                trimmed = trimmed.Substring(1);
            }

            foreach (string part in trimmed.Split(','))
            {
                string phrase = CollapseWhitespace(part);
                if (phrase.Length > 0)
                {
                    query.Phrases.Add(phrase);
                }
            }

            if (!query.EnchantOnly)
            {
                query.NameWords = trimmed.Split(NameWordSeparators, StringSplitOptions.RemoveEmptyEntries);
            }

            return query;
        }

        /// <param name="name">The row's name with rich text stripped, or null for a row with no name label.</param>
        /// <param name="getEnchantmentNames">Only called when the name alone does not decide the row.</param>
        public static bool Matches(Query query, string name, Func<IReadOnlyList<string>> getEnchantmentNames)
        {
            if (query.IsEmpty)
            {
                return true;
            }

            if (!query.EnchantOnly)
            {
                // A row with no name label can't be matched by name, so plain text never hides it.
                if (name == null)
                {
                    return true;
                }

                foreach (string word in query.NameWords)
                {
                    if (name.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
            }

            IReadOnlyList<string> enchantmentNames = getEnchantmentNames?.Invoke();
            if (enchantmentNames == null || enchantmentNames.Count == 0)
            {
                return false;
            }

            foreach (string phrase in query.Phrases)
            {
                bool found = false;
                foreach (string enchantmentName in enchantmentNames)
                {
                    if (enchantmentName.IndexOf(phrase, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        public static string StripRichText(string text)
        {
            return RichTextRegex.Replace(text ?? string.Empty, string.Empty);
        }

        /// <summary>
        /// A localized effect DisplayText reduced to the words a player would search for: no rich text, no
        /// {0} value slots, single spaces. "Attack Speed +{0:0.#}%" becomes "Attack Speed +%".
        /// </summary>
        public static string NormalizeEffectName(string localizedDisplayText)
        {
            return CollapseWhitespace(PlaceholderRegex.Replace(StripRichText(localizedDisplayText), string.Empty));
        }

        private static string CollapseWhitespace(string text)
        {
            return WhitespaceRegex.Replace(text, " ").Trim();
        }
    }
}
