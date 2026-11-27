using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class ModifyJumpStaminaUsage
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentJumpStaminaModifier))]
    public static class ModifyJumpStaminaUse_Player_GetEquipmentJumpStaminaModifier_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            __result -= GetModifyJumpStaminaUsageAmount(__instance);
        }
    }

    private static float GetModifyJumpStaminaUsageAmount(Player __instance)
    {
        return ModifyWithLowHealth.Sum(__instance, MagicEffectType.ModifyJumpStaminaUse);
    }

    /// <summary>
    /// Helper function primarily for the tooltip.
    /// </summary>
    public static float GetJumpStaminaUsageValue(ItemDrop.ItemData item)
    {
        if (item.HasMagicEffect(MagicEffectType.ModifyJumpStaminaUse))
        {
            return item.m_shared.m_jumpStaminaModifier -
                item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyJumpStaminaUse, 0.01f);
        }

        return item.m_shared.m_jumpStaminaModifier;
    }
}