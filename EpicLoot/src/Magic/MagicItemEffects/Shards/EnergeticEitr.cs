namespace EpicLoot.MagicItemEffects.Shards {
    // Called from IncreasePlayerBaseStats' Priority.Last postfix.
    public static class EnergeticEitr {
        public static void Apply(Player player, float maxStamina, ref float eitr) {
            if (eitr <= 0f) {
                return;
            }

            eitr += maxStamina * player.GetTotalActiveMagicEffectValue(MagicEffectType.EnergeticEitr, 0.01f);
        }
    }
}
