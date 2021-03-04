using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Attack), "DoAreaAttack")]
    public static class ModifyBackstab_Attack_DoAreaAttack_Patch
    {
        private static bool Prefix(Attack __instance) { return ModifyBackstabPatchHelper.DoPrefix(__instance); }
        private static void Postfix(Attack __instance) { ModifyBackstabPatchHelper.DoPostfix(__instance); }
    }

    [HarmonyPatch(typeof(Attack), "DoMeleeAttack")]
    public static class ModifyBackstab_Attack_DoMeleeAttack_Patch
    {
        private static bool Prefix(Attack __instance) { return ModifyBackstabPatchHelper.DoPrefix(__instance); }
        private static void Postfix(Attack __instance) { ModifyBackstabPatchHelper.DoPostfix(__instance); }
    }

    [HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
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

            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (weapon == null)
            {
                return true;
            }

            if (weapon.HasMagicEffect(MagicEffectType.ModifyBackstab))
            {
                Override = true;
                OriginalValue = weapon.m_shared.m_backstabBonus;

                var totalBackstabMod = weapon.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyBackstab, 0.01f);
                weapon.m_shared.m_backstabBonus *= 1.0f + totalBackstabMod;
            }

            return true;
        }

        public static void DoPostfix(Attack __instance)
        {
            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (weapon != null && Override)
            {
                weapon.m_shared.m_backstabBonus = OriginalValue;
            }

            Override = false;
            OriginalValue = -1;
        }
    }
}
