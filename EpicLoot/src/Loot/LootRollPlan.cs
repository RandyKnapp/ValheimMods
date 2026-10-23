namespace EpicLoot
{
    /// <summary>
    /// The scaling state a loot entry is resolved under: the effective level its table rolled at, how far
    /// past its Loot anchor that is, and the StarScaling that applies. ResolveLootDrop promotes an entry's
    /// Rarity with the context of whichever table supplied that Rarity -- the rolling table for its own
    /// entries and the ItemSets they name, a referenced table for what a "Table.Level" entry resolves to.
    /// </summary>
    public sealed class LootScalingContext
    {
        public float EffectiveLevel { get; internal set; }
        public float LootSteps { get; internal set; }

        // Modifiers.RarityShift of the creature and table being rolled; carried into references so a
        // creature's own tweak still applies to gear that comes through its template's reference.
        public float RarityExtra { get; internal set; }

        public LootScalingMath.ScalingParams Scaling { get; internal set; }

        public float[] Promote(float[] rarity)
        {
            return LootScalingMath.PromoteRarity(rarity, Scaling.RarityShift, Scaling.RarityFalloff, LootSteps,
                RarityExtra, Scaling.MaxRarity);
        }
    }

    /// <summary>Everything one table roll needs, worked out before any random number is drawn.</summary>
    internal struct LootRollPlan
    {
        public LeveledLootDef DropsAnchor;
        public LeveledLootDef LootAnchor;
        public float DropChanceAdd;
        public float DropRate;
        public float BonusDrops;
        public LootScalingContext Scaling;
    }
}
