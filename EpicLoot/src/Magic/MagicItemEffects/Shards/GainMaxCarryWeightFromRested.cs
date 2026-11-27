namespace EpicLoot.MagicItemEffects.Shards {
    public static class GainMaxCarryWeightFromRested {
        // The comfort belongs to a particular status-effect instance, not to the process.
        // SEMan.OnDestroy does not call Stop, so a bare static comfort could leak to a
        // different character after logout or a player replacement.
        internal static SE_Rested RestedEffect = null!;
        internal static int RestedComfort;

        // ModifyMaxCarryWeight handler invoked by SharedSEManModifyMaxCarryWeightPatch.
        public static void ModifyMaxCarryWeight(Player player, SEMan seman, ref float limit) {
            var perComfort = player.GetTotalActiveMagicEffectValue(
                MagicEffectType.GainMaxCarryWeightFromRested);
            if (perComfort == 0f) {
                return;
            }

            // An old character's recorded comfort must never apply to a new Rested instance.
            if (RestedEffect == null || RestedComfort <= 0 ||
                !ReferenceEquals(seman.GetStatusEffect(SEMan.s_statusEffectRested), RestedEffect)) {
                return;
            }

            limit += perComfort * RestedComfort;
        }
    }
}
