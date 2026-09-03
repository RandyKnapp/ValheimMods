using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to stamina regeneration based on the player's weight
    public static class StaminaRegenBonusFromPlayerWeight {
        public static void Apply(SEMan seman, ref float staminaMultiplier) {
            var player = Player.m_localPlayer;
            if (seman.m_character != player) {
                return;
            }

            var pct = player.GetTotalActiveMagicEffectValue(
                MagicEffectType.StaminaRegenBonusFromPlayerWeight, 0.01f);
            if (pct == 0f) {
                return;
            }

            // WeightFactor calls GetMaxCarryWeight, which re-enters the whole ModifyMaxCarryWeight
            // handler chain. ModifyStaminaRegen runs every tick, so it stays behind the memoized lookup.
            staminaMultiplier += pct * PenaltyScaling.WeightFactor(player);
        }
    }
}
