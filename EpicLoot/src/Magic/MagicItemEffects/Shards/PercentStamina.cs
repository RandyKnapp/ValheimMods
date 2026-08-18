namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a percent bonus to player stamina
    public static class PercentStamina {
        public static void Apply(Player player, ref float stamina) {
            stamina += stamina * player.GetTotalActiveMagicEffectValue(MagicEffectType.PercentStamina, 0.01f);
        }
    }
}
