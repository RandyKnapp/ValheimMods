using System;
using HarmonyLib;
using UnityEngine;

namespace ItsJustWood
{
    // NOTE: this patch currently never substitutes anything -- vanilla cooking stations that take fuel
    // burn Coal, and GetReplacementFuelItem only substitutes when the built-in fuel is Wood. Kept
    // deliberately (see also Smelter_OnAddFuel_Patch); the gate lives in GetReplacementFuelItem.
    [HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnAddFuelSwitch))]
    public static class CookingStation_OnAddFuelSwitch_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(CookingStation __instance, Humanoid user, ItemDrop.ItemData item, ref ItemDrop __state)
        {
            if (!ItsJustWood.modEnabled.Value)
                return;

            if (item != null && item.m_shared.m_name == __instance.m_fuelItem.m_itemData.m_shared.m_name)
                return;

            ItemDrop itemFuelReplacement = ItsJustWood.GetReplacementFuelItem(user.GetInventory(), __instance.m_fuelItem);
            if (itemFuelReplacement == null)
                return;

            __state = __instance.m_fuelItem;

            __instance.m_fuelItem = itemFuelReplacement;
        }

        // Finalizer, not postfix: restore must run even when the original throws (see Fireplace patch).
        private static void Finalizer(CookingStation __instance, ItemDrop __state)
        {
            if (__state == null)
                return;

            __instance.m_fuelItem = __state;
        }

    }
}