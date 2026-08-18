namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to health regeneration during the day
    public static class DayHealthRegen {
        public static void Apply(SEMan seman, ref float regenMultiplier) {
            if (seman.m_character != Player.m_localPlayer || !EnvMan.IsDay()) {
                return;
            }

            regenMultiplier += Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.DayHealthRegen, 0.01f);
        }
    }
}
