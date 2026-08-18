namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to adrenaline based on the fire damage dealt by the player.
    public static class BurningAdrenaline {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(HitData hit, Character attacker) {
            if (hit == null || hit.m_damage.m_fire <= 0f || attacker != Player.m_localPlayer) {
                return;
            }

            var fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BurningAdrenaline, 0.01f);
            if (fraction > 0f) {
                Player.m_localPlayer.AddAdrenaline(hit.m_damage.m_fire * fraction);
            }
        }
    }
}
