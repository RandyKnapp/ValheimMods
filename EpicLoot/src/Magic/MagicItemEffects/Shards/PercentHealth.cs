namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a percent bonus to player health
    public static class PercentHealth {
        public static void Apply(Player player, ref float hp) {
            hp += hp * player.GetTotalActiveMagicEffectValue(MagicEffectType.PercentHealth, 0.01f);
        }
    }
}
