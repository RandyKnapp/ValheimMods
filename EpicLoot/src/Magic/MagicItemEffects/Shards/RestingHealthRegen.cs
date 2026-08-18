namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to health regeneration while the player is resting
    public static class RestingHealthRegen {
        public static void Apply(SEMan seman, ref float regenMultiplier) {
            var player = Player.m_localPlayer;
            if (seman.m_character != player || !seman.HaveStatusEffect(SEMan.s_statusEffectRested)) {
                return;
            }

            regenMultiplier += player.GetTotalActiveMagicEffectValue(MagicEffectType.RestingHealthRegen, 0.01f);
        }
    }
}
