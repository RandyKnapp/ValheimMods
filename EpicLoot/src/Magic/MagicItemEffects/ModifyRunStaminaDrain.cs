using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class ModifyRunStaminaDrain
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentRunStaminaModifier))]
    private static class ModifyRunStaminaDrain_Player_GetEquipmentDodgeStaminaModifier_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null)
            {
                return;
            }

            __result -= __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ModifySprintStaminaUse, 0.01f);
        }
    }

    /// <summary>
    /// Helper function primarily for the tooltip.
    /// </summary>
    public static float GetModifySprintStaminaValue(ItemDrop.ItemData item)
    {
        if (item.HasMagicEffect(MagicEffectType.ModifySprintStaminaUse))
        {
            return item.m_shared.m_runStaminaModifier -
                item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifySprintStaminaUse, 0.01f);
        }

        return item.m_shared.m_runStaminaModifier;
    }
}