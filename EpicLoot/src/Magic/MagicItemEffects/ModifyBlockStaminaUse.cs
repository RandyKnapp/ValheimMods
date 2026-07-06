using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class ModifyBlockStaminaUse
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentBlockStaminaModifier))]
    private static class ModifyBlockStaminaUse_Player_GetEquipmentBlockStaminaModifier_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null)
            {
                return;
            }

            __result -= __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyBlockStaminaUse, 0.01f);
        }
    }

    /// <summary>
    /// Helper function primarily for the tooltip.
    /// </summary>
    public static float GetModifyBlockStaminaValue(ItemDrop.ItemData item)
    {
        if (item.HasMagicEffect(MagicEffectType.ModifyBlockStaminaUse))
        {
            return item.m_shared.m_blockStaminaModifier -
                item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyBlockStaminaUse, 0.01f);
        }

        return item.m_shared.m_blockStaminaModifier;
    }
}
