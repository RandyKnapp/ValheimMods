using EpicLoot.General;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to adrenaline gain when the player takes damage.
    public static class DamageTakenGivesAdrenaline {
        // Postfix handler invoked by CharacterRpcDamageDispatch (on-damage-taken reaction).
        public static void OnDamageTaken(Character __instance, HitData hit) {
            if (hit == null || __instance != Player.m_localPlayer) {
                return;
            }

            var amount = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.DamageTakenGivesAdrenaline);
            if (amount <= 0f) {
                return;
            }

            // Flat grant, but only for hits that actually landed damage -- a fully mitigated or
            // zero-damage hit shouldn't build adrenaline.
            if (hit.m_damage.EpicLootGetTotalDamageAgainstPlayer() > 0f) {
                Player.m_localPlayer.AddAdrenaline(amount);
            }
        }
    }
}
