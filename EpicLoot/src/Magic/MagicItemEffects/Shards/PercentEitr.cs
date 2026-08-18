namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a percent bonus to eitr
    public static class PercentEitr {
        public static void Apply(Player player, ref float eitr) {
            eitr += eitr * player.GetTotalActiveMagicEffectValue(MagicEffectType.PercentEitr, 0.01f);
        }
    }
}
