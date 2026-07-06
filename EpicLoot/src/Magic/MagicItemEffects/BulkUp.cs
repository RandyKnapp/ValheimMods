using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

[HarmonyPatch]
public static class BulkupEffect
{
    [HarmonyPriority(Priority.LowerThanNormal)]
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyHealthRegen))]
    public static class ModifyHealthRegen_Patch
    {
        public static void Postfix(SEMan __instance, ref float regenMultiplier)
        {
            if (__instance.m_character != Player.m_localPlayer)
            {
                return;
            }

            regenMultiplier = GetHealthRegenWithBulkUp(regenMultiplier);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    public static class Player_GetTotalFoodValue_Patch
    {
        public static void Postfix(Player __instance, ref float hp)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            hp += GetAdditionalHealthWithBulkUp(hp);
        }
    }

    /// <summary>
    /// Sets the hud display to the correct size
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetBaseFoodHP))]
    public static class Player_GetBaseFoodHP_Patch
    {
        public static void Postfix(Player __instance, ref float __result)
        {
            __result += GetAdditionalHealthWithBulkUp(__result);
        }
    }

    private static float GetHealthRegenWithBulkUp(float regen)
    {
        if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.BulkUp, out float bulkupValue, 0.01f))
        {
            bulkupValue = Mathf.Clamp01(bulkupValue);
            // Reduce regen by bulk up percent
            regen -= bulkupValue;
        }

        return regen;
    }

    private static float GetAdditionalHealthWithBulkUp(float health)
    {
        if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.BulkUp, out float bulkupValue, 0.01f))
        {
            bulkupValue = Mathf.Clamp01(bulkupValue);
            // Increase health by bulk up percent
            return health * bulkupValue;
        }

        return 0f;
    }
}