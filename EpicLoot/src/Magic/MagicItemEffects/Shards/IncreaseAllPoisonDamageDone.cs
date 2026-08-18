namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to all poison damage done by the player.
    public static class IncreaseAllPoisonDamageDone {
        // Prefix handler invoked by CharacterDamageDispatch (attacker-side outgoing modifier).
        public static void ModifyOutgoingHit(HitData hit, Character attacker) {
            if (hit == null || hit.m_damage.m_poison <= 0f || attacker != Player.m_localPlayer) {
                return;
            }

            var bonus = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.IncreaseAllPoisonDamageDone, 0.01f);
            if (bonus > 0f) {
                hit.m_damage.m_poison *= 1f + bonus;
            }
        }
    }
}
