using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class IncreaseHeatResistance
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentHeatResistanceModifier))]
    private static class IncreaseHeatResistance_Player_GetEquipmentHeatResistanceModifier_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null)
            {
                return;
            }

            __result += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseHeatResistance, 0.01f);
        }
    }

    /// <summary>
    /// Helper function primarily for the tooltip.
    /// </summary>
    public static float GetHeatResistanceValue(ItemDrop.ItemData item)
    {
        if (item.HasMagicEffect(MagicEffectType.IncreaseHeatResistance))
        {
            return item.m_shared.m_heatResistanceModifier +
                item.GetMagicItem().GetTotalEffectValue(MagicEffectType.IncreaseHeatResistance, 0.01f);
        }

        return item.m_shared.m_heatResistanceModifier;
    }
}