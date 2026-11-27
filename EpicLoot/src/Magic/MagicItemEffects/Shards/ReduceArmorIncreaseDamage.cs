using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class ReduceArmorIncreaseDamage {
        // Called from ModifyArmor's GetArmor postfix.
        public static void ModifyArmor(Player player, ref float armor) {
            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.ReduceArmorIncreaseDamage, 0.01f);
            if (value > 0f) {
                armor *= Mathf.Max(0f, 1f - value);
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
