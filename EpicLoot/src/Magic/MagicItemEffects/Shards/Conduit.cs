namespace EpicLoot.MagicItemEffects.Shards {
    // Lightning damage done by the player restores eitr to the player, at damage thresholds.
    public static class Conduit {
        // Lightning damage dealt per eitr trigger. Tunable; higher = a slower trickle.
        private const float LightningDamagePerTrigger = 200f;

        // Tooltip: "Restore {0} Eitr per {1} Lightning Damage Dealt" -- {1} is the LightningDamagePerTrigger
        // const so the shown threshold stays in sync with the code rather than a baked-in literal.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.Conduit,
                value => new object[] { value, LightningDamagePerTrigger });
        }

        // Cumulative lightning damage the local player has dealt with the effect active but not yet paid out
        // as eitr. Carries the sub-threshold remainder across hits.
        private static float _accumulatedLightningDamage;

        // Postfix handler invoked by CharacterDamageDispatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || hit.m_damage.m_lightning <= 0f || attacker != player
                || __instance == player || __instance.IsPlayer() || __instance.IsTamed()) {
                return;
            }

            var eitrPerTrigger = player.GetTotalActiveMagicEffectValue(MagicEffectType.Conduit);
            if (eitrPerTrigger <= 0f) {
                return;
            }

            _accumulatedLightningDamage += hit.m_damage.m_lightning;
            if (_accumulatedLightningDamage < LightningDamagePerTrigger) {
                return;
            }

            var triggers = (int)(_accumulatedLightningDamage / LightningDamagePerTrigger);
            _accumulatedLightningDamage -= triggers * LightningDamagePerTrigger;
            player.AddEitr(triggers * eitrPerTrigger);
        }
    }
}
