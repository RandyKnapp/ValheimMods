using System;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    //public bool EquipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
    [HarmonyPatch(typeof(Humanoid), "EquipItem", typeof(ItemDrop.ItemData), typeof(bool))]
    public static class Humanoid_EquipItem_Patch
    {
        public static void Postfix(Humanoid __instance, ItemDrop.ItemData item, bool __result)
        {
            if (!__result || __instance == null || item == null || !__instance.IsPlayer())
            {
                return;
            }

            Debug.Log($"Equipped item {item.m_shared.m_setName} at slot <{item.m_gridPos}>");
            var player = __instance as Player;
            if (EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value && EquipmentAndQuickSlots.IsSlotEquippable(item) && player.GetEquipmentSlotInventory() != null)
            {
                var currentInventorySlot = item.m_gridPos;
                var correctInventorySlot = EquipmentAndQuickSlots.GetEquipmentSlotForType(item.m_shared.m_itemType);
                if (currentInventorySlot.x != correctInventorySlot)
                {
                    //player.GetEquipmentSlotInventory().MoveItemToThis(__instance.m_inventory, item, item.m_stack, correctInventorySlot, 0);
                }
            }

            return;
        }
    }

    //public void UnequipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
    [HarmonyPatch(typeof(Humanoid), "UnequipItem", typeof(ItemDrop.ItemData), typeof(bool))]
    public static class Humanoid_UnequipItem_Patch
    {
        private static bool _shouldDrop;
        private static bool _dropping;

        public static bool Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (!EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value)
            {
                return true;
            }

            if (_dropping)
            {
                return false;
            }

            if (!__instance.IsPlayer())
            {
                return true;
            }

            _shouldDrop = false;
            if (item != null && EquipmentAndQuickSlots.IsSlotEquippable(item))
            {
                if (__instance.m_inventory.HaveEmptySlot())
                {
                    var correctInventorySlot = __instance.m_inventory.FindEmptySlot(false);
                    //__instance.m_inventory.MoveItemToThis((__instance as Player).GetEquipmentSlotInventory(), item, item.m_stack, correctInventorySlot.x, correctInventorySlot.y);
                }
                else
                {
                    _shouldDrop = true;
                }
            }

            return true;
        }

        public static void Postfix(Humanoid __instance, ItemDrop.ItemData item, Inventory ___m_inventory)
        {
            if (!EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value)
            {
                return;
            }

            if (_shouldDrop)
            {
                _dropping = true;
                _shouldDrop = false;

                __instance.DropItem(___m_inventory, item, 1);
                _dropping = false;
            }
        }
    }
}
