using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public static class ModifyMovementSpeed
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
    private static class ModifyMovementSpeed_Player_GetEquipmentMovementModifier_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            float modify = GetModifyMovementSpeedAmount(__instance);
            __result += modify;
        }
    }

    // Not an effect type: a key of its own in the per-player effect cache, which equipping and any magic data
    // write reset. Vanilla asks for the movement modifier several times per tick.
    private const string PenaltyRefundCacheKey = "~RemoveSpeedPenaltyRefund";

    private static float GetModifyMovementSpeedAmount(Player __instance)
    {
        if (!EquipmentEffectCache.TryGetValue(__instance, PenaltyRefundCacheKey, out float? refund) || !refund.HasValue)
        {
            float sum = 0f;
            foreach (var itemData in __instance.GetMagicEquipment())
            {
                sum -= GetSpeedPenaltyAmount(itemData);
            }

            refund = sum;
            EquipmentEffectCache.Store(__instance, PenaltyRefundCacheKey, refund);
        }

        return refund.Value + ModifyWithLowHealth.Sum(__instance, MagicEffectType.ModifyMovementSpeed);
    }

    private static float GetSpeedPenaltyAmount(ItemDrop.ItemData item)
    {
        if (item != null && item.HasMagicEffect(MagicEffectType.RemoveSpeedPenalty))
        {
            // Do not return a positve value
            return Mathf.Clamp(item.m_shared.m_movementModifier, float.MinValue, 0f);
        }

        return 0f;
    }

    /// <summary>
    /// Helper function primarily for the tooltip.
    /// </summary>
    public static float GetItemMovementModifier(ItemDrop.ItemData item)
    {
        if (item != null && item.HasMagicEffect(MagicEffectType.RemoveSpeedPenalty))
        {
            return Mathf.Max(0f, item.m_shared.m_movementModifier);
        }
        else if (item.HasMagicEffect(MagicEffectType.ModifyMovementSpeed))
        {
            return item.m_shared.m_movementModifier +
                item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyMovementSpeed, 0.01f);
        }

        return item.m_shared.m_movementModifier;
    }
}
