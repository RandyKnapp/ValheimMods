using System.Linq;
using EpicLoot.Compendium;
using EpicLoot.Crafting;
using EpicLoot.Config;
using TMPro;
using UnityEngine;

namespace EpicLoot
{
    public static class MagicEffectRarity
    {
        public const string LinkIdPrefix = "elrare";
        public const int MaxTier = 3;

        // Escapes, not literals: a CP1252 round-trip corrupted the pip glyphs in
        // EpicLoot.GetMagicEffectPip once already.
        //
        // Tried in order against the real fonts, because guessing loses: U+2726 comes up as an empty box
        // in the item tooltip. The tooltip and the enchanting table between them draw the marker in both
        // Averia faces, each with its own fallback chain, so a candidate has to clear both. U+25C6 is
        // last and is not a guess -- GetMagicEffectPip already draws it on every effect line.
        private static readonly char[] GlyphCandidates = { '\u2605', '\u2726', '\u25C8', '\u25C6' };

        private static char _tierGlyph = '\u25C6';
        private static bool _glyphResolved;

        private static void ResolveGlyph()
        {
            if (_glyphResolved)
            {
                return;
            }

            TMP_FontAsset sans = MagicFontManager.GetTMPFont(MagicFontManager.TMP_FontOptions.AveriaSansLibre)?.font;
            TMP_FontAsset serif = MagicFontManager.GetTMPFont(MagicFontManager.TMP_FontOptions.AveriaSerifLibre)?.font;
            if (sans == null || serif == null)
            {
                return;
            }

            _glyphResolved = true;

            foreach (var candidate in GlyphCandidates)
            {
                // searchFallbacks/tryAddCharacter both true: a dynamic atlas reports a glyph as missing
                // until something asks it to add it, which is exactly what drawing the text would do.
                if (sans.HasCharacter(candidate, true, true) && serif.HasCharacter(candidate, true, true))
                {
                    _tierGlyph = candidate;
                    break;
                }
            }

            EpicLoot.Log($"Effect rarity marker: U+{(int)_tierGlyph:X4}");
        }

        private static readonly float[] TierRatioCeilings = { 0.35f, 0.125f, 0.0625f };
        private static readonly string[] TierColors = { "#8FD8FF", "#C79BFF", "#FFD24A" };

        private static float _anchorWeight = float.NaN;
        private static float[] _rungsBelowAnchor = new float[0];

        public static void InvalidateAnchor()
        {
            _anchorWeight = float.NaN;
        }

        private static void EnsureAnchor()
        {
            if (!float.IsNaN(_anchorWeight))
            {
                return;
            }

            var weights = MagicItemEffectDefinitions.AllDefinitions.Values
                .Where(CanRoll)
                .Select(x => x.SelectionWeight)
                .Where(x => x > 0f)
                .OrderBy(x => x)
                .ToList();

            _anchorWeight = weights.Count > 0 ? weights[weights.Count / 2] : 0f;
            _rungsBelowAnchor = weights.Where(x => x < _anchorWeight).Distinct().OrderBy(x => x).ToArray();
        }

        // Two independent reads of the same weight, and the lower wins.
        //
        // Rank alone cheapens the top tier: the legendary/minimal configs use only four distinct
        // weights, so their lowest rung holds 14-15 effects. Ratio alone is just as bad the other way --
        // it assumes a long tail, and hands out no stars at all once a config's spread is narrow.
        // Taking the min means a tier has to be earned on both counts, so a config with no genuine
        // outlier (legendary) simply has no three-star effects rather than inventing some.
        private static int GetRankTier(float weight)
        {
            for (var i = 0; i < _rungsBelowAnchor.Length && i < MaxTier; i++)
            {
                if (Mathf.Approximately(weight, _rungsBelowAnchor[i]))
                {
                    return MaxTier - i;
                }
            }

            return 0;
        }

        private static int GetRatioTier(float weight)
        {
            var ratio = weight / _anchorWeight;
            for (var i = TierRatioCeilings.Length - 1; i >= 0; i--)
            {
                if (ratio < TierRatioCeilings[i])
                {
                    return i + 1;
                }
            }

            return 0;
        }

        public static float GetAnchorWeight()
        {
            EnsureAnchor();
            return _anchorWeight;
        }

        // Anything that cannot be selected by a roll is excluded from BOTH the anchor and the tiers.
        // ShardEffectDefinitions synthesizes ~114 NoRoll effects into AllDefinitions and never sets
        // SelectionWeight, so they all sit at the field default of 1 -- enough phantom weight to drag
        // the balanced median from 5 to 1 and demote the rarest effect in the config by a whole tier.
        private static bool CanRoll(MagicItemEffectDefinition effectDef)
        {
            return effectDef != null &&
                !(effectDef.Requirements?.NoRoll ?? false) &&
                !EnchantCostsHelper.EffectIsDeprecated(effectDef);
        }

        public static int GetTier(MagicItemEffectDefinition effectDef)
        {
            if (!CanRoll(effectDef) || effectDef.SelectionWeight <= 0f)
            {
                return 0;
            }

            EnsureAnchor();
            if (_anchorWeight <= 0f)
            {
                return 0;
            }

            return Mathf.Min(GetRankTier(effectDef.SelectionWeight), GetRatioTier(effectDef.SelectionWeight));
        }

        // Sockets are excluded on purpose: a socketed effect comes from the shard that was fitted, not
        // from a weighted roll, so its SelectionWeight says nothing about how lucky this item was.
        public static int GetBestTier(MagicItem magicItem)
        {
            var best = 0;
            if (magicItem == null)
            {
                return best;
            }

            foreach (var effect in magicItem.Effects)
            {
                MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effect.EffectType, out var effectDef);
                var tier = GetTier(effectDef);
                if (tier > best)
                {
                    best = tier;
                }
            }

            return best;
        }

        public static string Decorate(MagicItemEffectDefinition effectDef, string effectText, bool allowAnimation)
        {
            return Decorate(GetTier(effectDef), effectText, allowAnimation);
        }

        // TryGetValue rather than MagicItemEffectDefinitions.Get: Get warns and fabricates a stand-in
        // definition for an unknown type, and this runs per effect per tooltip frame.
        public static string Decorate(string effectType, string effectText, bool allowAnimation)
        {
            MagicItemEffectDefinitions.AllDefinitions.TryGetValue(effectType, out var effectDef);
            return Decorate(effectDef, effectText, allowAnimation);
        }

        // allowAnimation is false for any surface backed by UnityEngine.UI.Text: legacy Text has no
        // <link> tag and prints it verbatim instead of ignoring it.
        public static string Decorate(int tier, string effectText, bool allowAnimation)
        {
            if (tier <= 0 || tier > MaxTier || ELConfig.EffectRarityFlareMode == null ||
                ELConfig.EffectRarityFlareMode.Value == EffectRarityFlare.Off)
            {
                return effectText;
            }

            ResolveGlyph();

            var marker = $"<color={TierColors[tier - 1]}>{new string(_tierGlyph, tier)}</color>";
            var decorated = $"{effectText} {marker}";

            if (!allowAnimation || ELConfig.EffectRarityFlareMode.Value != EffectRarityFlare.Animated)
            {
                return decorated;
            }

            return $"<link=\"{LinkIdPrefix}{tier}\">{decorated}</link>";
        }
    }
}
