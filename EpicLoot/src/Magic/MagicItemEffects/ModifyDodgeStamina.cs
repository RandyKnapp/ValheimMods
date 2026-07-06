using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class ModifyDodgeStamina
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentDodgeStaminaModifier))]
    private static class IncreaseHeatResistance_Player_GetEquipmentDodgeStaminaModifier_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null)
            {
                return;
            }

            __result -= __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyDodgeStaminaUse, 0.01f);
        }
    }

    /// <summary>
    /// Helper function primarily for the tooltip.
    /// </summary>
    public static float GetModifyDodgeStaminaValue(ItemDrop.ItemData item)
    {
        if (item.HasMagicEffect(MagicEffectType.ModifyDodgeStaminaUse))
        {
            return item.m_shared.m_dodgeStaminaModifier -
                item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyDodgeStaminaUse, 0.01f);
        }

        return item.m_shared.m_dodgeStaminaModifier;
    }
}