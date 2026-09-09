using EpicLoot.src.Magic.MagicItemEffects.Helpers;
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

        // GetAttackEitr() is now a thin wrapper over GetAttackEitr(Character, ItemData); patching
        // the unqualified name throws AmbiguousMatchException. Its siblings above still have a
        // single overload each, so only this one needs the argument types.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
        private static void GetAttackEitr_Postfix(Character character, ItemDrop.ItemData weapon, ref float __result)
        {
            DoPostfix(character, weapon, MagicEffectType.ModifyAttackEitrUse, ref __result);
        }
    }

    private static void DoPostfix(Attack __instance, string magicEffect, ref float __result)
    {
        DoPostfix(__instance.m_character, __instance.m_weapon, magicEffect, ref __result);
    }

    private static void DoPostfix(Character character, ItemDrop.ItemData weapon, string magicEffect,
        ref float __result)
    {
        if (__result == 0f || character != Player.m_localPlayer)
        {
            return;
        }

        __result *= GetModifyAttackValue(Player.m_localPlayer, weapon, magicEffect);
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
