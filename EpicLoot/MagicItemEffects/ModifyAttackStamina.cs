using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetStaminaUsage()
    [HarmonyPatch(typeof(Attack), "GetStaminaUsage")]
    class ModifyAttackStamina_Attack_GetStaminaUsage_Patch
    {
        public static void Postfix(Attack __instance, ref float __result)
        {
            if (__instance.m_weapon != null && __instance.m_weapon.IsMagic())
            {
                if (__instance.m_weapon.GetMagicItem().HasEffect(MagicEffectType.ModifyAttackStaminaUse))
                {
                    __result *= 1 - __instance.m_weapon.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f);
                }
            }
        }
    }
}
