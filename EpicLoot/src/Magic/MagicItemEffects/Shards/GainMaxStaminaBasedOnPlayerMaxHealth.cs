namespace EpicLoot.MagicItemEffects.Shards {
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
