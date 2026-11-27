namespace EpicLoot.MagicItemEffects.Shards {
    public static class PercentStamina {
        public static void Apply(Player player, ref float stamina) {
            stamina += stamina * player.GetTotalActiveMagicEffectValue(MagicEffectType.PercentStamina, 0.01f);
        }
    }
}
