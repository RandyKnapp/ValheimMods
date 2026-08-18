namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Helper class to handle gaining resources (health, stamina, eitr) when the player successfully blocks an attack.
    public static class GainOnBlockResource {
        public static void GainOnBlock(Humanoid blocker, bool IsBlocked)
        {
            var player = Player.m_localPlayer;

            // The dispatcher already gates on the local player, but guard here too so the helper stays
            // safe from any future call site: only the local player's own block pays out, and never on a
            // machine with no local player (dedicated server).
            if (!IsBlocked || player == null || blocker != player || player.IsDead()) return;

            player.Heal(player.GetTotalActiveMagicEffectValue(MagicEffectType.LifeGainOnBlock, 1f));
            player.AddStamina(player.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaGainOnBlock, 1f));
            player.AddEitr(player.GetTotalActiveMagicEffectValue(MagicEffectType.EitrGainOnBlock, 1f));
        }
    }
}
