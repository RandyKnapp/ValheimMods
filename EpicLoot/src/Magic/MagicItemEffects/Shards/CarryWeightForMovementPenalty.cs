using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to carry weight based on the player's movement penalty.
    public static class CarryWeightForMovementPenalty {
        // ModifyMaxCarryWeight handler invoked by SharedSEManModifyMaxCarryWeightPatch.
        public static void ModifyMaxCarryWeight(Player player, float baseLimit, ref float limit) {
            var pct = player.GetTotalActiveMagicEffectValue(
                MagicEffectType.CarryWeightForMovementPenalty, 0.01f);
            if (pct == 0f) {
                return;
            }

            // Measuring the penalty runs the whole status-effect speed pipeline, so it stays behind the
            // memoized effect lookup: a player without this shard contributes 0 either way, and this is
            // reached on every fixed tick through Player.UpdateStats -> IsEncumbered.
            limit += baseLimit * pct * PenaltyScaling.MovementPenaltyFactor(player);
        }
    }
}
