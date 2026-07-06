using System;
using HarmonyLib;
using UnityEngine;

namespace ItsJustWood
{
    [HarmonyPatch(typeof(Fireplace), nameof(Fireplace.Interact))]
    public static class Fireplace_Interact_Patch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(Fireplace __instance, Humanoid user, ref ItemDrop __state)
        {
            if (!ItsJustWood.modEnabled.Value)
                return;

            if (__instance.m_fuelItem != ItsJustWood.wood)
                return;

            Inventory inventory = user.GetInventory();
            if (inventory.HaveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name))
                return;

            ItemDrop itemFuelReplacement = ItsJustWood.GetReplacementFuelItem(inventory, __instance.m_fuelItem);
            if (itemFuelReplacement == null)
                return;

            __state = __instance.m_fuelItem;

            __instance.m_fuelItem = itemFuelReplacement;
        }

        [HarmonyPriority(Priority.First)]
        private static void Postfix(Fireplace __instance, ItemDrop __state)
        {
            if (!ItsJustWood.modEnabled.Value)
                return;

            if (__state == null)
                return;

            __instance.m_fuelItem = __state;
        }
    }
}