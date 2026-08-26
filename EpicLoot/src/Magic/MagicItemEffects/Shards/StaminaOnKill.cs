namespace EpicLoot.MagicItemEffects.Shards {
    // Restores a percentage of the player's maximum stamina when they kill an enemy
    public static class StaminaOnKill {
        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            // Kill check by predicted lethality: against a remote-owned victim the RPC carrying this
            // hit has not executed yet, so GetHealth() still reads pre-hit health and a plain <= 0
            // check never fired in multiplayer.
            if (hit == null || player == null || __instance == player || attacker != player
                || __instance.IsPlayer() || __instance.IsTamed()
                || __instance.GetHealth() - hit.GetTotalDamage() > 0f) {
                return;
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaOnKill, 0.01f);
            if (fraction > 0f) {
                player.AddStamina(player.GetMaxStamina() * fraction);
            }
        }
    }
}
