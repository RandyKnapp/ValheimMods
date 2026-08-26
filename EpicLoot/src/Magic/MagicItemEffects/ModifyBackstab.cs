using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    // Per-invocation __state (not statics): m_shared.m_backstabBonus is the descriptor shared by
    // every copy of the weapon, and the old static Override/OriginalValue pair leaked the
    // multiplied value permanently when the original threw or when two of these patched methods
    // nested (max-block-charge counters re-enter DoMeleeAttack from BlockAttack). The finalizer
    // restores even on exception.
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
