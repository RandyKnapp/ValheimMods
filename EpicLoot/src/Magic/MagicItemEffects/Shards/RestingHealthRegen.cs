namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to health regeneration while the player is actively resting: vanilla's "Resting"
    // status (by a fire, sitting or sheltered), not the "Rested" buff it leaves behind.
    public static class RestingHealthRegen {
        public static void Apply(SEMan seman, ref float regenMultiplier) {
            var player = Player.m_localPlayer;
            if (seman.m_character != player || !seman.HaveStatusEffect(SEMan.s_statusEffectResting)) {
                return;
            }

            regenMultiplier += player.GetTotalActiveMagicEffectValue(MagicEffectType.RestingHealthRegen, 0.01f);
        }
    }
}
