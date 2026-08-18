using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Reduces the stamina cost of fishing by a percentage
    public static class ReduceFishingStaminaCost {
        private static bool _inFishingUpdate;

        [HarmonyPatch(typeof(FishingFloat), "FixedUpdate")]
        private static class FishingFloat_FixedUpdate_Patch {
            [UsedImplicitly]
            private static void Prefix() => _inFishingUpdate = true;

            // Finalizer clears the flag even if FixedUpdate throws, so a fishing exception can't leave every
            // later UseStamina discounted.
            [UsedImplicitly]
            private static void Finalizer() => _inFishingUpdate = false;
        }

        // Priority.First so this discount lands before SharedCharacterUseStaminaPatch covers the shortfall
        // from adrenaline/health. The other way round, the discount would apply to the already-reduced
        // remainder -- under-applying it and over-spending those pools.
        [HarmonyPatch(typeof(Player), nameof(Player.UseStamina))]
        [HarmonyPriority(Priority.First)]
        private static class Player_UseStamina_Patch {
            [UsedImplicitly]
            private static void Prefix(Player __instance, ref float v) {
                if (!_inFishingUpdate || v <= 0f || __instance != Player.m_localPlayer) {
                    return;
                }

                var reduction = __instance.GetTotalActiveMagicEffectValue(
                    MagicEffectType.ReduceFishingStaminaCost, 0.01f);
                if (reduction > 0f) {
                    v *= Mathf.Max(0f, 1f - reduction);
                }
            }
        }
    }
}
