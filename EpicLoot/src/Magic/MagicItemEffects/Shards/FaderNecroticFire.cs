using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class NecroticFire {
        // The shard value is a fraction of the stripped physical pool paid out as EACH element, shown as a
        // percentage rather than a bare 0.2.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.NecroticFire,
                value => new object[] { value * 100f });
        }

        // GetDamage postfix handler invoked by ModifyDamage (per-weapon modifier).
        public static void ModifyWeaponDamage(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result) {
            // Only when the local player has this weapon equipped (also gates the weapon tooltip).
            if (!ModifyDamage.RunGetDamagePatch(__instance)) {
                return;
            }

            float factor = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.NecroticFire, 1f);
            if (factor <= 0f) {
                return;
            }

            float physical = __result.m_blunt + __result.m_slash + __result.m_pierce;
            if (physical <= 0f) {
                return;
            }

            // All physical is consumed regardless of factor; the factor only sets the payout.
            DamageConversionHelper.RemovePhysicalShare(ref __result, 1f);

            __result.m_fire += physical * factor;
            __result.m_poison += physical * factor;
        }
    }
}
