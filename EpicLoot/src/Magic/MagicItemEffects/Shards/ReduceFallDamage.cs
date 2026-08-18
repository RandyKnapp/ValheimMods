using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Reduces fall damage by a flat amount based on the total value of ReduceFallDamage effects on the player
    public static class ReduceFallDamage {
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyFallDamage))]
        private static class ModifyFallDamage_Patch {
            [UsedImplicitly]
            private static void Postfix(SEMan __instance, ref float damage) {
                if (__instance.m_character != Player.m_localPlayer || damage <= 0f) {
                    return;
                }

                var reduction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                    MagicEffectType.ReduceFallDamage);
                if (reduction > 0f) {
                    damage = Mathf.Max(0f, damage - reduction);
                }
            }
        }
    }
}
