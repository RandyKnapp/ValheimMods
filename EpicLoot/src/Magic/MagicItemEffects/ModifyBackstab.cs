using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    // TODO: Investigate if these patches can be refactored
    [HarmonyPatch(typeof(Attack), nameof(Attack.DoAreaAttack))]
    public static class ModifyBackstab_Attack_DoAreaAttack_Patch
    {
        private static bool Prefix(Attack __instance) { return ModifyBackstabPatchHelper.DoPrefix(__instance); }
        private static void Postfix(Attack __instance) { ModifyBackstabPatchHelper.DoPostfix(__instance); }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
    public static class ModifyBackstab_Attack_DoMeleeAttack_Patch
    {
        private static bool Prefix(Attack __instance) { return ModifyBackstabPatchHelper.DoPrefix(__instance); }
        private static void Postfix(Attack __instance) { ModifyBackstabPatchHelper.DoPostfix(__instance); }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.FireProjectileBurst))]
    public static class ModifyBackstab_Attack_FireProjectileBurst_Patch
    {
        private static bool Prefix(Attack __instance) { return ModifyBackstabPatchHelper.DoPrefix(__instance); }
        private static void Postfix(Attack __instance) { ModifyBackstabPatchHelper.DoPostfix(__instance); }
    }

    public static class ModifyBackstabPatchHelper
    {
        public static bool Override;
        public static float OriginalValue;

        public static bool DoPrefix(Attack __instance)
        {
            Override = false;
            OriginalValue = -1;

            var weapon = __instance.m_weapon;
            if (weapon == null)
            {
                return true;
            }

            if (__instance.m_character is Player player && MagicEffectsHelper.HasActiveMagicEffectOnWeapon(
                player, __instance.m_weapon, MagicEffectType.ModifyBackstab, out float effectValue, 0.01f))
            {
                Override = true;
                OriginalValue = weapon.m_shared.m_backstabBonus;

                weapon.m_shared.m_backstabBonus *= 1.0f + effectValue;
            }

            return true;
        }

        public static void DoPostfix(Attack __instance)
        {
            var weapon = __instance.m_weapon;
            if (weapon != null && Override)
            {
                weapon.m_shared.m_backstabBonus = OriginalValue;
            }

            Override = false;
            OriginalValue = -1;
        }
    }
}
