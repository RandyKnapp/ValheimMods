using HarmonyLib;
using System.Reflection;

namespace EpicLoot.MagicItemEffects;

public static class ModifyAttackCosts
{
    [HarmonyPatch]
    private static class ModifyAttackCost_Patches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackHealth))]
        private static void GetAttackHealth_Postfix(Attack __instance, ref float __result)
        {
            DoPostfix(__instance, MagicEffectType.ModifyAttackHealthUse, ref __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
        private static void GetAttackStamina_Postfix(Attack __instance, ref float __result)
        {
            DoPostfix(__instance, MagicEffectType.ModifyAttackStaminaUse, ref __result);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr))]
        private static void GetAttackEitr_Postfix(Attack __instance, ref float __result)
        {
            DoPostfix(__instance, MagicEffectType.ModifyAttackEitrUse, ref __result);
        }
    }

    private static void DoPostfix(Attack __instance, string magicEffect, ref float __result)
    {
        if (__result == 0f || __instance.m_character != Player.m_localPlayer)
        {
            return;
        }

        __result *= GetModifyAttackValue(Player.m_localPlayer, __instance.m_weapon, magicEffect);
    }

    public static float GetModifyAttackValue(Player player, ItemDrop.ItemData item, string magicEffect)
    {
        return GetEffectPercentage(MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(
            player, item, magicEffect, 0.01f));
    }

    public static float GetEffectPercentage(float effectValue)
    {
        return 1.0f - effectValue;
    }
}
