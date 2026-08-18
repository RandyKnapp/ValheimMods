using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Converts a portion of physical damage to lightning damage.
    public static class ConvertPhysicalDamageToLightning {
        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            // Only when the local player has this weapon equipped (also gates the weapon tooltip).
            if (!ModifyDamage.RunGetDamagePatch(__instance)) {
                return;
            }

            float fraction = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.ConvertPhysicalDamageToLightning, 0.01f);
            if (fraction <= 0f) {
                return;
            }

            fraction = Mathf.Clamp01(fraction);
            float physical = __result.m_blunt + __result.m_slash + __result.m_pierce;
            if (physical <= 0f) {
                return;
            }

            __result.m_lightning += physical * fraction;
            DamageConversionHelper.RemovePhysicalShare(ref __result, fraction);
        }
    }
}
