namespace EpicLoot.MagicItemEffects.Helpers {
    // Re-entrancy latch for effects that grant skill XP by calling Skills.RaiseSkill themselves
    // (currently only Inspiration).
    //
    // Every Skills.RaiseSkill patch in the assembly must bail while this is set, for two reasons:
    //
    //  * Recursion. Inspiration's own postfix would re-roll on the XP it just granted, without bound.
    //  * Grant accuracy. The other RaiseSkill prefixes (QuickLearner, IncreaseHarvestXPGain,
    //    IncreaseXPGainFromMovementPenalty, IncreasedXPGainFromBlockDayNight) all multiply `factor`.
    //    Inspiration grants a fixed number of accumulator points, walking the skill to each level
    //    boundary one call at a time because vanilla discards accumulator overflow on level-up
    //    (Skills.Skill.Raise). An outside multiplier makes each of those calls overshoot the boundary,
    //    the excess is thrown away, and the player ends up with *less* total XP for having an XP-gain
    //    effect equipped. Suppressing the multipliers for the duration of the grant keeps the packet
    //    exactly the size Inspiration intends.
    public static class SkillXpGrant {
        public static bool InProgress;
    }
}
