using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Restores a percentage of the player's maximum stamina when they kill an enemy
    public static class StaminaOnKill {
        private static readonly KillCredit _credit = new KillCredit();

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || __instance == player || attacker != player
                || __instance.IsPlayer() || __instance.IsTamed()) {
                return;
            }

            // The memoized total before the kill check: against a remote-owned victim the RPC carrying this hit
            // has not executed yet, so the post-hit health is estimated; against one this client owns, it has.
            // One payout per kill, however many hits land as the victim dies (KillCredit).
            var fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.StaminaOnKill, 0.01f);
            if (fraction <= 0f || !_credit.TryClaim(__instance, hit)) {
                return;
            }

            player.AddStamina(player.GetMaxStamina() * fraction);
        }
    }
}
