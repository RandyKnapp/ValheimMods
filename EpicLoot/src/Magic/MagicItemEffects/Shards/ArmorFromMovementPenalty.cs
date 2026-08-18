using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to armor based on the player's movement penalty.
    public static class ArmorFromMovementPenalty {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
        private static class GetArmor_Patch {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result) {
                var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
                if (player == null) {
                    return;
                }

                var bonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.ArmorFromMovementPenalty, 0.01f)
                    * PenaltyScaling.MovementPenaltyFactor(player);
                if (bonus != 0f) {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
