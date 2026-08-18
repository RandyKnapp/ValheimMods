namespace EpicLoot.MagicItemEffects.Shards {
    // On adrenaline activation provides a cooldown reduction to the forsaken power. The reduction is a fraction of the current cooldown, so it is more effective when the cooldown is high.
    public static class AdrenalineCharge {
        // Below this the remaining cooldown is not worth tracking; vanilla's per-frame decrement in
        // Player.UpdateGuardianPower would clear it within a second anyway. Cosmetic.
        private const float CooldownFloor = 1f;

        public static void OnAdrenalineActivated(Player player) {
            if (player.m_guardianSE == null || player.m_guardianPowerCooldown <= 0f) {
                return; // no forsaken power equipped, or it is already off cooldown
            }

            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.AdrenalineCharge, 0.01f);
            if (fraction <= 0f) {
                return;
            }

            player.m_guardianPowerCooldown *= 1f - fraction;
            if (player.m_guardianPowerCooldown < CooldownFloor) {
                player.m_guardianPowerCooldown = 0f;
            }
        }
    }
}
