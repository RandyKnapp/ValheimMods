using System;
using System.Collections.Generic;

namespace EpicLoot
{
    /// <summary>
    /// The star-scaling arithmetic for loot tables, kept free of UnityEngine and of any random source
    /// (callers pass their roll in) so it can be exercised outside the game.
    ///
    /// A creature at level L with an aggressiveness multiplier k rolls at effective level
    /// Le = 1 + (L - 1) * k. Each table picks, independently for Drops and for Loot, the highest authored
    /// level at or below Le (its anchor) and extrapolates the difference Le - anchor with StarScaling.
    /// </summary>
    public static class LootScalingMath
    {
        // Rounding slack so an effective level of 2.9999994 still reaches a level 3 anchor.
        private const float LevelEpsilon = 0.0001f;

        /// <summary>
        /// Stands in for loottables.json's DefaultStarScaling when a file predates it; mirrors the shipped
        /// value (the average of the fitted Tier{N}Mob curves), so keep the two in sync. In such an older
        /// file every template authors levels 1-6 by hand, so this only reaches past level 6 and into
        /// single-level tables.
        /// </summary>
        public static readonly LootScaling CodeDefaultStarScaling = new LootScaling
        {
            DropChance = 0.149f,
            BonusDrops = 0.016f,
            RarityShift = 0.28f,
            RarityFalloff = 1f
        };

        /// <summary>A StarScaling block with every field resolved.</summary>
        public struct ScalingParams
        {
            public float DropChance;
            public float BonusDrops;
            public float RarityShift;
            public float RarityFalloff;
            public ItemRarity MaxRarity;
            public int MaxDrops;

            public bool IsNone => DropChance == 0 && BonusDrops == 0 && RarityShift == 0;
        }

        /// <summary>
        /// Field-by-field merge, most specific layer first: the first layer that sets a field decides it.
        /// Null layers are skipped. A field no layer sets is 0 (no scaling), except RarityFalloff (1) and
        /// MaxRarity (the highest rarity).
        /// </summary>
        public static ScalingParams MergeScaling(params LootScaling[] layers)
        {
            float? dropChance = null, bonusDrops = null, rarityShift = null, rarityFalloff = null;
            ItemRarity? maxRarity = null;
            int? maxDrops = null;

            foreach (LootScaling layer in layers)
            {
                if (layer == null)
                {
                    continue;
                }

                dropChance ??= layer.DropChance;
                bonusDrops ??= layer.BonusDrops;
                rarityShift ??= layer.RarityShift;
                rarityFalloff ??= layer.RarityFalloff;
                maxRarity ??= layer.MaxRarity;
                maxDrops ??= layer.MaxDrops;
            }

            return new ScalingParams
            {
                DropChance = dropChance ?? 0f,
                BonusDrops = bonusDrops ?? 0f,
                RarityShift = Math.Max(0f, rarityShift ?? 0f),
                RarityFalloff = Math.Max(0f, rarityFalloff ?? 1f),
                MaxRarity = maxRarity ?? Rarities.Highest,
                MaxDrops = Math.Max(0, maxDrops ?? 0)
            };
        }

        /// <summary>
        /// Every star counts as <paramref name="multiplier"/> stars. A multiplier of 0 makes every
        /// creature loot as if it had none; levels below 1 are returned unchanged (they roll nothing).
        /// </summary>
        public static float EffectiveLevel(int level, float multiplier)
        {
            if (level <= 1)
            {
                return level;
            }

            return 1f + (level - 1) * Math.Max(0f, multiplier);
        }

        /// <summary>
        /// The highest level at or below <paramref name="effectiveLevel"/> that <paramref name="has"/>
        /// accepts, or null when the table defines nothing that low.
        /// </summary>
        public static LeveledLootDef FindAnchor(List<LeveledLootDef> levels, float effectiveLevel,
            Func<LeveledLootDef, bool> has)
        {
            if (levels == null)
            {
                return null;
            }

            int ceiling = (int)Math.Floor(effectiveLevel + LevelEpsilon);
            LeveledLootDef best = null;
            foreach (LeveledLootDef def in levels)
            {
                if (def == null || def.Level > ceiling || !has(def))
                {
                    continue;
                }

                // Several entries at one level: the first wins, the same as the old List.Find walk.
                if (best == null || def.Level > best.Level)
                {
                    best = def;
                }
            }

            return best;
        }

        /// <summary>How far past its anchor a roll is extrapolating; never negative.</summary>
        public static float Steps(float effectiveLevel, int anchorLevel)
        {
            float steps = effectiveLevel - anchorLevel;
            return steps < LevelEpsilon ? 0f : steps;
        }

        /// <summary>
        /// Adds <paramref name="dropChanceAdd"/> to the chance of dropping anything while keeping the
        /// shape of the counts that do drop. A table whose anchor never drops anything starts from one
        /// item. The input is returned untouched when there is nothing to add, so an unscaled roll keeps
        /// its authored weights exactly.
        /// </summary>
        public static List<KeyValuePair<int, float>> ScaleDropChance(List<KeyValuePair<int, float>> drops,
            float dropChanceAdd)
        {
            if (drops == null || Math.Abs(dropChanceAdd) < 1e-6f)
            {
                return drops;
            }

            float total = 0, dropping = 0;
            foreach (KeyValuePair<int, float> pair in drops)
            {
                float weight = Math.Max(0f, pair.Value);
                total += weight;
                if (pair.Key > 0)
                {
                    dropping += weight;
                }
            }

            float chance = total > 0 ? dropping / total : 0f;
            float scaled = Clamp01(chance + dropChanceAdd);

            List<KeyValuePair<int, float>> result = new List<KeyValuePair<int, float>>
            {
                new KeyValuePair<int, float>(0, 1f - scaled)
            };

            if (dropping > 0)
            {
                foreach (KeyValuePair<int, float> pair in drops)
                {
                    if (pair.Key > 0 && pair.Value > 0)
                    {
                        result.Add(new KeyValuePair<int, float>(pair.Key, scaled * pair.Value / dropping));
                    }
                }
            }
            else
            {
                result.Add(new KeyValuePair<int, float>(1, scaled));
            }

            return result;
        }

        /// <summary>
        /// Global Drop Rate Modifier's maths, shared with per-creature DropRate: the weight of dropping
        /// nothing is divided by the rate and every other weight multiplied by it. A rate of 0 or less
        /// means nothing drops at all.
        /// </summary>
        public static List<KeyValuePair<int, float>> ApplyDropRate(List<KeyValuePair<int, float>> drops, float rate)
        {
            if (drops == null || Math.Abs(rate - 1f) < 1e-6f)
            {
                return drops;
            }

            if (rate <= 0f)
            {
                return new List<KeyValuePair<int, float>> { new KeyValuePair<int, float>(0, 1f) };
            }

            List<KeyValuePair<int, float>> result = new List<KeyValuePair<int, float>>(drops.Count);
            foreach (KeyValuePair<int, float> pair in drops)
            {
                result.Add(new KeyValuePair<int, float>(pair.Key,
                    pair.Key == 0 ? pair.Value / rate : pair.Value * rate));
            }

            return result;
        }

        /// <summary>
        /// Adds a (possibly fractional, possibly negative) bonus to a rolled count: the whole part always
        /// applies and <paramref name="roll01"/> decides the fraction. A roll that dropped nothing stays at
        /// nothing, and the result is capped by <paramref name="maxDrops"/> when that is above 0.
        /// </summary>
        public static int AddBonusDrops(int count, float bonus, float roll01, int maxDrops)
        {
            if (count <= 0)
            {
                return count;
            }

            if (Math.Abs(bonus) > 1e-6f)
            {
                float total = count + bonus;
                if (total <= 0)
                {
                    return 0;
                }

                int whole = (int)Math.Floor(total);
                count = whole + (roll01 < total - whole ? 1 : 0);
            }

            return maxDrops > 0 ? Math.Min(count, maxDrops) : count;
        }

        /// <summary>A loot table entry's weight <paramref name="steps"/> stars past its anchor.</summary>
        public static float ScaledWeight(LootDrop drop, float steps)
        {
            if (drop == null)
            {
                return 0f;
            }

            if (steps <= 0 || drop.WeightPerStar == null)
            {
                return drop.Weight;
            }

            return Math.Max(0f, drop.Weight + drop.WeightPerStar.Value * steps);
        }

        /// <summary>
        /// Promotes rarity weight upward. Each whole step moves rate * falloff^(t - t0) of tier t's weight
        /// to tier t + 1, where t0 is the lowest tier the original array rolls; a fractional last step
        /// moves that share of it. <paramref name="extraRate"/> is one more step after those. Weight never
        /// moves into a tier above <paramref name="maxRarity"/>, weight already above it is left where it
        /// is, and the total is preserved.
        ///
        /// An empty array stays empty -- no Rarity means "drop this as a plain item" -- and an array that
        /// needs no promotion is returned as is.
        /// </summary>
        public static float[] PromoteRarity(float[] rarity, float rate, float falloff, float steps,
            float extraRate, ItemRarity maxRarity)
        {
            if (rarity == null || rarity.Length == 0)
            {
                return rarity;
            }

            bool starSteps = rate > 0 && steps > 0;
            bool extraStep = extraRate > 0;
            if (!starSteps && !extraStep)
            {
                return rarity;
            }

            float[] weights = new float[Math.Max(Rarities.Count, rarity.Length)];
            Array.Copy(rarity, weights, rarity.Length);

            int lowest = -1;
            for (int i = 0; i < weights.Length; i++)
            {
                if (weights[i] > 0)
                {
                    lowest = i;
                    break;
                }
            }

            if (lowest < 0)
            {
                return rarity;
            }

            int cap = Math.Min((int)maxRarity, weights.Length - 1);
            if (starSteps)
            {
                int whole = (int)Math.Floor(steps);
                for (int i = 0; i < whole; i++)
                {
                    PromoteOnce(weights, rate, falloff, lowest, cap, 1f);
                }

                float fraction = steps - whole;
                if (fraction > LevelEpsilon)
                {
                    PromoteOnce(weights, rate, falloff, lowest, cap, fraction);
                }
            }

            if (extraStep)
            {
                PromoteOnce(weights, extraRate, falloff, lowest, cap, 1f);
            }

            return weights;
        }

        // Top-down, so weight a step has just moved up is not moved again in the same step.
        private static void PromoteOnce(float[] weights, float rate, float falloff, int lowest, int cap, float share)
        {
            for (int tier = cap - 1; tier >= lowest; tier--)
            {
                float tierRate = Clamp01(rate * (float)Math.Pow(falloff, tier - lowest)) * share;
                float moved = weights[tier] * tierRate;
                weights[tier] -= moved;
                weights[tier + 1] += moved;
            }
        }

        private static float Clamp01(float value)
        {
            return value < 0 ? 0 : value > 1 ? 1 : value;
        }
    }
}
