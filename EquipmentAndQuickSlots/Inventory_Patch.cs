using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    //public bool ContainsItem(ItemDrop.ItemData item) => this.m_inventory.Contains(item);
    [HarmonyPatch(typeof(Inventory), "ContainsItem")]
    public static class Inventory_ContainsItem_Patch
    {
        public static bool Prefix(Inventory __instance, ref bool __result, ItemDrop.ItemData item)
        {
            if (Player.m_localPlayer != null && Player.m_localPlayer.m_inventory == __instance)
            {
                __result = Player.m_localPlayer.InventoryContainsItem(item);
                return false;
            }

            return true;
        }
    }

}
