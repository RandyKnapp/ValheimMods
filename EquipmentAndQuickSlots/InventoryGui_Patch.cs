using System;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    // private void OnSelectedItem(
    [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem", new Type[] { typeof(InventoryGrid), typeof(ItemDrop.ItemData), typeof(Vector2i), typeof(InventoryGrid.Modifier) })]
    public static class InventoryGui_OnSelectedItem_Patch
    {
        public static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod, GameObject ___m_dragGo, ItemDrop.ItemData ___m_dragItem)
        {
            if (EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value && EquipmentAndQuickSlots.IsEquipmentSlot(pos))
            {
                if (___m_dragItem != null && EquipmentAndQuickSlots.IsSlotEquippable(___m_dragItem) && EquipmentAndQuickSlots.GetEquipmentTypeForSlot(pos) == ___m_dragItem.m_shared.m_itemType)
                {
                    var player = Player.m_localPlayer;
                    player.UseItem(player.GetInventory(), ___m_dragItem, true);
                    __instance.SetupDragItem(null, null, 1);
                }
                return false;
            }

            return true;
        }
    }
}
