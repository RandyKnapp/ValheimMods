using EpicLoot.MagicItemEffects.Helpers;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to XP gain based on the player's movement penalty
    public static class IncreaseXPGainFromMovementPenalty {
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        private static class RaiseSkill_Patch {
            [UsedImplicitly]
            private static void Prefix(Skills __instance, ref float factor) {
                // Inspiration grants an exact number of accumulator points; multiplying them would
                // overshoot each level boundary and vanilla would discard the excess. See SkillXpGrant.
                if (SkillXpGrant.InProgress) {
                    return;
                }

                var player = __instance.m_player;
                if (player == null) {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.IncreaseXPGainFromMovementPenalty, 0.01f) * PenaltyScaling.MovementPenaltyFactor(player);
                factor *= 1f + bonus;
            }
        }
    }
}
