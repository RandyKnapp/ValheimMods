namespace EpicLoot.MagicItemEffects.Shards {
    public static class PercentHealth {
        public static void Apply(Player player, ref float hp) {
            hp += hp * player.GetTotalActiveMagicEffectValue(MagicEffectType.PercentHealth, 0.01f);
        }
    }
}
