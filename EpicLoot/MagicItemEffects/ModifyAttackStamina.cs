using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public float GetStaminaUsage()
    [HarmonyPatch(typeof(Attack), nameof(Attack.GetStaminaUsage))]
    public class ModifyAttackStamina_Attack_GetStaminaUsage_Patch
    {
        public static void Postfix(Attack __instance, ref float __result)
        {
            if (__instance.m_character is Player player)
            {
                __result *= 1 - player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyAttackStaminaUse, 0.01f);
            }
        }
    }
}
