namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    public static class GainOnBlockResource {
        public static void GainOnBlock(Humanoid blocker, bool IsBlocked)
        {
            var player = Player.m_localPlayer;

            // The dispatcher gates on the local player too; this keeps the helper safe on a dedicated server
            // and from any other call site.
            if (!IsBlocked || player == null || blocker != player || player.IsDead()) return;

            player.Heal(player.GetTotalActiveMagicEffectValue(MagicEffectType.LifeGainOnBlock, 1f));
            player.AddStamina(player.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaGainOnBlock, 1f));
            player.AddEitr(player.GetTotalActiveMagicEffectValue(MagicEffectType.EitrGainOnBlock, 1f));
        }
    }
}
