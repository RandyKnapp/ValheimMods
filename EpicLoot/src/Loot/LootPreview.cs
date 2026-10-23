using EpicLoot.Config;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EpicLoot
{
    /// <summary>
    /// What a loot table rolls at each creature level once star scaling is applied, worked out from the
    /// same plan a real roll uses: the chance of any drop, the expected item count, the item mix and the
    /// rarity split. Backs the lootpreview console command.
    /// </summary>
    internal static class LootPreview
    {
        private static readonly string[] RarityLabels = { "Mag", "Rar", "Epi", "Leg", "Myt", "Anc" };

        internal static List<string> Describe(string objectName, int maxLevel)
        {
            List<string> lines = new List<string>();
            List<LootTable> tables = LootRoller.GetLootTable(objectName);
            if (tables.Count == 0)
            {
                lines.Add($"No loot table for '{objectName}'.");
                return lines;
            }

            LootTable own = LootRoller.GetCreatureOverride(objectName);
            if (own != null)
            {
                lines.Add($"{objectName}: StarMultiplier {Fmt(own.StarMultiplier ?? 1)}" +
                    (own.Modifiers != null ? $", Modifiers DropRate {Fmt(own.Modifiers.DropRate ?? 1)} " +
                        $"BonusDrops {Fmt(own.Modifiers.BonusDrops ?? 0)} RarityShift {Fmt(own.Modifiers.RarityShift ?? 0)}" : "") +
                    (own.StarScaling != null ? ", own StarScaling" : ""));
            }

            for (int index = 0; index < tables.Count; index++)
            {
                LootTable table = tables[index];
                LootScalingMath.ScalingParams scaling = LootRoller.PlanRollAt(table, 1, own == table ? null : own, 0).Scaling.Scaling;
                lines.Add($"Table {index + 1}/{tables.Count} ({table.Object}): levels " +
                    $"[{string.Join(", ", table.LeveledLoot.Where(d => d != null).Select(d => d.Level))}], " +
                    $"StarScaling DropChance {Fmt(scaling.DropChance)} BonusDrops {Fmt(scaling.BonusDrops)} " +
                    $"RarityShift {Fmt(scaling.RarityShift)} Falloff {Fmt(scaling.RarityFalloff)} " +
                    $"Max {scaling.MaxRarity}{(scaling.MaxDrops > 0 ? $" MaxDrops {scaling.MaxDrops}" : "")}");

                for (int level = 1; level <= maxLevel; level++)
                {
                    lines.AddRange(DescribeLevel(table, level, objectName));
                }
            }

            return lines;
        }

        private static IEnumerable<string> DescribeLevel(LootTable table, int level, string objectName)
        {
            LootRollPlan plan = LootRoller.PlanRoll(table, level, objectName);
            LootScalingContext context = plan.Scaling;
            string stars = level > 1 ? new string('*', level - 1) : "-";
            if (plan.DropsAnchor == null)
            {
                yield return $"  L{level} {stars}: no Drops at or below effective level {Fmt(context.EffectiveLevel)}";
                yield break;
            }

            List<KeyValuePair<int, float>> drops = LootScalingMath.ScaleDropChance(
                plan.DropsAnchor.Drops.Select(x => new KeyValuePair<int, float>((int)x[0], x[1])).ToList(),
                plan.DropChanceAdd);
            drops = LootScalingMath.ApplyDropRate(drops,
                Mathf.Clamp(ELConfig.GlobalDropRateModifier.Value, 0, 4) * plan.DropRate);

            float total = drops.Sum(x => Math.Max(0, x.Value));
            float dropping = drops.Where(x => x.Key > 0).Sum(x => Math.Max(0, x.Value));
            float chance = total > 0 ? dropping / total : 0;
            float expected = dropping > 0
                ? drops.Where(x => x.Key > 0).Sum(x => Math.Max(0, x.Value) *
                    ExpectedCount(x.Key, plan.BonusDrops, context.Scaling.MaxDrops)) / dropping
                : 0;

            LootDrop[] loot = plan.LootAnchor?.Loot ?? Array.Empty<LootDrop>();
            float lootTotal = loot.Sum(x => LootScalingMath.ScaledWeight(x, context.LootSteps));
            float[] rarity = new float[Rarities.Count];
            float unknownShare = 0;
            StringBuilder mix = new StringBuilder();
            foreach (LootDrop entry in loot)
            {
                float share = lootTotal > 0 ? LootScalingMath.ScaledWeight(entry, context.LootSteps) / lootTotal : 0;
                if (share <= 0)
                {
                    continue;
                }

                mix.Append($" {entry.Item} {share * 100:0.#}%");
                float[] entryRarity = RarityOf(entry, context, 0);
                if (entryRarity == null)
                {
                    unknownShare += share;
                    continue;
                }

                for (int i = 0; i < rarity.Length; i++)
                {
                    rarity[i] += share * entryRarity[i];
                }
            }

            float known = 1 - unknownShare;
            string raritySplit = known > 0
                ? string.Join(" ", rarity.Select((w, i) => $"{RarityLabels[Math.Min(i, RarityLabels.Length - 1)]} {w / known * 100:0.#}"))
                : "none";
            float epicPlus = known > 0 ? rarity.Skip((int)ItemRarity.Epic).Sum() / known : 0;

            yield return $"  L{level} {stars} (eff {Fmt(context.EffectiveLevel)}; drops@{plan.DropsAnchor.Level}" +
                $" loot@{plan.LootAnchor?.Level.ToString() ?? "-"}): P(any) {chance * 100:0.#}%, " +
                $"items|drop {expected:0.##}, Epic+/kill {chance * expected * epicPlus:0.###}";
            yield return $"      rarity {raritySplit}{(unknownShare > 0.001f ? $" ({unknownShare * 100:0.#}% plain/unknown)" : "")}";
            yield return $"      mix{mix}";
        }

        // The average count a rolled count becomes once the bonus (whole part plus a chance at one more)
        // and the cap apply.
        private static float ExpectedCount(int count, float bonus, int maxDrops)
        {
            float total = count + bonus;
            if (total <= 0)
            {
                return 0;
            }

            int whole = (int)Math.Floor(total);
            float fraction = total - whole;
            float Capped(int n) => maxDrops > 0 ? Math.Min(n, maxDrops) : n;
            return Math.Abs(bonus) < 1e-6f ? Capped(count) : Capped(whole) * (1 - fraction) + Capped(whole + 1) * fraction;
        }

        // The normalized rarity split an entry drops at, following ItemSets and table references the way
        // ResolveLootDrop does; null for a plain (non-magic) item.
        private static float[] RarityOf(LootDrop entry, LootScalingContext context, int depth)
        {
            if (entry == null || depth > 8)
            {
                return null;
            }

            if (entry.Rarity != null && entry.Rarity.Length > 0)
            {
                return Normalize(context.Promote(entry.Rarity));
            }

            if (LootRoller.ItemSets.TryGetValue(entry.Item ?? "", out LootItemSet set))
            {
                return Average(set.Loot, member => member.Weight, member => RarityOf(member, context, depth + 1));
            }

            if (LootRoller.TryResolveTableReference(entry.Item ?? "", context, out LootDrop[] list, out LootScalingContext reference))
            {
                return Average(list, member => LootScalingMath.ScaledWeight(member, reference.LootSteps),
                    member => RarityOf(member, reference, depth + 1));
            }

            return null;
        }

        private static float[] Average(IEnumerable<LootDrop> entries, Func<LootDrop, float> weight, Func<LootDrop, float[]> rarityOf)
        {
            float[] sum = new float[Rarities.Count];
            float weightSum = 0;
            foreach (LootDrop entry in entries ?? Enumerable.Empty<LootDrop>())
            {
                float w = weight(entry);
                float[] r = w > 0 ? rarityOf(entry) : null;
                if (r == null)
                {
                    continue;
                }

                weightSum += w;
                for (int i = 0; i < sum.Length; i++)
                {
                    sum[i] += w * r[i];
                }
            }

            return weightSum > 0 ? sum.Select(x => x / weightSum).ToArray() : null;
        }

        private static float[] Normalize(float[] weights)
        {
            float[] result = new float[Rarities.Count];
            float total = 0;
            for (int i = 0; i < result.Length && i < weights.Length; i++)
            {
                result[i] = Math.Max(0, weights[i]);
                total += result[i];
            }

            return total > 0 ? result.Select(x => x / total).ToArray() : null;
        }

        private static string Fmt(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
