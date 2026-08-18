namespace EpicLoot.MagicItemEffects.Shards {
    // Add a percentage of the player's max Stamina to their max Eitr. Invoked from IncreasePlayerBaseStats' Priority.Last
    public static class EnergeticEitr {
        public static void Apply(Player player, float maxStamina, ref float eitr) {
            if (eitr <= 0f) {
                return;
            }

            eitr += maxStamina * player.GetTotalActiveMagicEffectValue(MagicEffectType.EnergeticEitr, 0.01f);
        }
    }
}
