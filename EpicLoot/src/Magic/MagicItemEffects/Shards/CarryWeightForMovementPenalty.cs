using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to carry weight based on the player's movement penalty.
    public static class CarryWeightForMovementPenalty {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
        private static class ModifyMaxCarryWeight_Patch {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, float baseLimit, ref float limit) {
                var player = Player.m_localPlayer;
                if (__instance.m_character != player) {
                    return;
                }

                limit += baseLimit * player.GetTotalActiveMagicEffectValue(
                    MagicEffectType.CarryWeightForMovementPenalty, 0.01f) * PenaltyScaling.MovementPenaltyFactor(player);
            }
        }
    }
}
