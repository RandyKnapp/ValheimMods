namespace EpicLoot.MagicItemEffects.Shards {
    // Restores a percentage of the player's maximum stamina when they kill an enemy
    public static class StaminaOnKill {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || __instance == player || attacker != player
                || __instance.IsPlayer() || __instance.IsTamed() || __instance.GetHealth() > 0f) {
                return;
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaOnKill, 0.01f);
            if (fraction > 0f) {
                player.AddStamina(player.GetMaxStamina() * fraction);
            }
        }
    }
}
