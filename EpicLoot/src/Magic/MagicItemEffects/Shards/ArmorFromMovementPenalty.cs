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

                // GetArmor is hot -- every equipped piece, on every damage calculation and every HUD
                // refresh -- and measuring the penalty runs the whole status-effect speed pipeline, so
                // it stays behind the memoized effect lookup.
                var pct = player.GetTotalActiveMagicEffectValue(MagicEffectType.ArmorFromMovementPenalty, 0.01f);
                if (pct == 0f) {
                    return;
                }

                var bonus = pct * PenaltyScaling.MovementPenaltyFactor(player);
                if (bonus != 0f) {
                    __result *= 1f + bonus;
                }
            }
        }
    }
}
