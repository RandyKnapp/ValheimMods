using System;
using HarmonyLib;
using UnityEngine;

namespace ItsJustWood
{
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

        [HarmonyPriority(Priority.First)]
        private static void Postfix(CookingStation __instance, ItemDrop __state)
        {
            if (!ItsJustWood.modEnabled.Value)
                return;

            if (__state == null)
                return;

            __instance.m_fuelItem = __state;
        }

    }
}