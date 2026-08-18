namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to maximum stamina based on the player's maximum health.
    public static class GainMaxStaminaBasedOnPlayerMaxHealth {
        public static void Apply(Player player, float hp, ref float stamina) {
            if (player != Player.m_localPlayer) {
                return;
            }

            stamina += hp * player.GetTotalActiveMagicEffectValue(
                MagicEffectType.GainMaxStaminaBasedOnPlayerMaxHealth, 0.01f);
        }
    }
}
