namespace EpicLoot.MagicItemEffects.Shards {
    public static class DayStaminaRegen {
        public static void Apply(SEMan seman, ref float staminaMultiplier) {
            if (seman.m_character != Player.m_localPlayer || !EnvMan.IsDay()) {
                return;
            }

            staminaMultiplier += Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.DayStaminaRegen, 0.01f);
        }
    }
}
