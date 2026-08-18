using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Reduces the armor of the player, but increases the damage they deal
    public static class ReduceArmorIncreaseDamage {
        [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetArmor), typeof(int), typeof(float))]
        private static class GetArmor_Patch {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData __instance, ref float __result) {
                var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
                if (player == null) {
                    return;
                }

                var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.ReduceArmorIncreaseDamage, 0.01f);
                if (value > 0f) {
                    __result *= Mathf.Max(0f, 1f - value);
                }
            }
        }

        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            var player = Player.m_localPlayer;
            if (player == null || !player.IsItemEquiped(__instance)) {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.ReduceArmorIncreaseDamage, 0.01f);
            if (value > 0f) {
                __result.Modify(1f + value);
            }
        }
    }
}
