namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to Eitr based on the player's maximum health.
    public static class HeartyEitr {
        public static void Apply(Player player, float maxHealth, ref float eitr) {
            if (eitr <= 0f) {
                return;
            }

            eitr += maxHealth * player.GetTotalActiveMagicEffectValue(MagicEffectType.HeartyEitr, 0.01f);
        }
    }
}
