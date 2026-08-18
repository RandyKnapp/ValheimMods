namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to stamina regeneration during nighttime
    public static class NightStaminaRegenIncrease {
        public static void Apply(SEMan seman, ref float staminaMultiplier) {
            if (seman.m_character != Player.m_localPlayer || !EnvMan.IsNight()) {
                return;
            }

            staminaMultiplier += Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.NightStaminaRegenIncrease, 0.01f);
        }
    }
}
