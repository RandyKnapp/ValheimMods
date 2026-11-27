using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ArmorFromMovementPenalty {
        // Called from ModifyArmor's GetArmor postfix. Measuring the penalty runs the whole status-effect speed
        // pipeline, so it stays behind the memoized effect lookup.
        public static void ModifyArmor(Player player, ref float armor) {
            var pct = player.GetTotalActiveMagicEffectValue(MagicEffectType.ArmorFromMovementPenalty, 0.01f);
            if (pct == 0f) {
                return;
            }

            var bonus = pct * PenaltyScaling.MovementPenaltyFactor(player);
            if (bonus != 0f) {
                armor *= 1f + bonus;
            }
        }
    }
}
