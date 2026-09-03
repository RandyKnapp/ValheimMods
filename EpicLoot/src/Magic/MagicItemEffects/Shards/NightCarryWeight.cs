namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to carry weight at night
    public static class NightCarryWeight {
        // ModifyMaxCarryWeight handler invoked by SharedSEManModifyMaxCarryWeightPatch.
        public static void ModifyMaxCarryWeight(Player player, float baseLimit, ref float limit) {
            var pct = player.GetTotalActiveMagicEffectValue(MagicEffectType.NightCarryWeight, 0.01f);
            if (pct == 0f) {
                return;
            }

            if (EnvMan.IsNight()) {
                limit += baseLimit * pct;
            }
        }
    }
}
