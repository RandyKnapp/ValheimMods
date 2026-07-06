using HarmonyLib;
using System;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public static class ModifyMovementSpeed
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
    private static class ModifyMovementSpeed_Player_GetEquipmentMovementModifier_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance == null)
            {
                return;
            }

            float modify = GetModifyMovementSpeedAmount(__instance);
            __result += modify;
        }
    }

    private static float GetModifyMovementSpeedAmount(Player __instance)
    {
        float movementSpeed = 0f;
        foreach (var itemData in __instance.GetMagicEquipment())
        {
            // Negate previous penalties
            movementSpeed -= GetSpeedPenaltyAmount(itemData);
        }

        ModifyWithLowHealth.Apply(__instance, MagicEffectType.ModifyMovementSpeed, effect =>
        {
            movementSpeed += __instance.GetTotalActiveMagicEffectValue(effect, 0.01f);
        });

        return movementSpeed;
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
