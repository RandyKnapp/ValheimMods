using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    // m_shared.m_backstabBonus is shared by every copy of the weapon, so the original is kept in a per-call
    // __state (these methods can nest: block counters re-enter DoMeleeAttack from BlockAttack) and put back
    // by a finalizer, even on exception.
    [HarmonyPatch(typeof(Attack), nameof(Attack.DoAreaAttack))]
    public static class ModifyBackstab_Attack_DoAreaAttack_Patch
    {
        private static void Prefix(Attack __instance, ref float __state) { __state = ModifyBackstabPatchHelper.DoPrefix(__instance); }
        private static void Finalizer(Attack __instance, float __state) { ModifyBackstabPatchHelper.Restore(__instance, __state); }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
    public static class ModifyBackstab_Attack_DoMeleeAttack_Patch
    {
        private static void Prefix(Attack __instance, ref float __state) { __state = ModifyBackstabPatchHelper.DoPrefix(__instance); }
        private static void Finalizer(Attack __instance, float __state) { ModifyBackstabPatchHelper.Restore(__instance, __state); }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
    public static class ModifyBackstab_Attack_FireProjectileBurst_Patch
    {
        private static void Prefix(Attack __instance, ref float __state) { __state = ModifyBackstabPatchHelper.DoPrefix(__instance); }
        private static void Finalizer(Attack __instance, float __state) { ModifyBackstabPatchHelper.Restore(__instance, __state); }
    }

    public static class ModifyBackstabPatchHelper
    {
        // Returns the original backstab bonus when it modified the shared value, or -1 when nothing
        // was changed (the caller passes the value back to Restore via Harmony __state).
        public static float DoPrefix(Attack __instance)
        {
            var weapon = __instance.m_weapon;
            if (weapon == null)
            {
                return -1f;
            }

            if (__instance.m_character is Player player && MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                player, __instance.m_weapon, MagicEffectType.ModifyBackstab, out float effectValue, 0.01f))
            {
                float original = weapon.m_shared.m_backstabBonus;
                weapon.m_shared.m_backstabBonus *= 1.0f + effectValue;
                return original;
            }

            return -1f;
        }

        public static void Restore(Attack __instance, float original)
        {
            var weapon = __instance.m_weapon;
            if (weapon != null && original >= 0f)
            {
                weapon.m_shared.m_backstabBonus = original;
            }
        }
    }
}
