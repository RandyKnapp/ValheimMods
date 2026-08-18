using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to stamina regeneration based on the player's weight
    public static class StaminaRegenBonusFromPlayerWeight {
        public static void Apply(SEMan seman, ref float staminaMultiplier) {
            var player = Player.m_localPlayer;
            if (seman.m_character != player) {
                return;
            }

            staminaMultiplier += player.GetTotalActiveMagicEffectValue(
                MagicEffectType.StaminaRegenBonusFromPlayerWeight, 0.01f) * PenaltyScaling.WeightFactor(player);
        }
    }
}
