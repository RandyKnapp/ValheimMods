using System;
using HarmonyLib;

namespace QuickUseSlots
{
    public static class EquipUtil
    {
        public static void MoveItemToSlot(Inventory inventory, ItemDrop.ItemData item, Vector2i dest)
        {
            var originalPosition = item.m_gridPos;
            var itemInDest = inventory.GetItemAt(dest.x, dest.y);
            if (itemInDest != null)
            {
                itemInDest.m_gridPos = originalPosition;
            }

            item.m_gridPos = dest;
            inventory.Changed();
        }
    }

    //public bool EquipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
    [HarmonyPatch(typeof(Humanoid), "EquipItem", new Type[] { typeof(ItemDrop.ItemData), typeof(bool) })]
    public static class Humanoid_EquipItem_Patch
    {
        public static bool Prefix(ItemDrop.ItemData item, Inventory ___m_inventory)
        {
            if (item != null && EquipmentAndQuickSlots.IsSlotEquippable(item))
            {
                var currentInventorySlot = item.m_gridPos;
                var correctInventorySlot = EquipmentAndQuickSlots.GetEquipmentSlotForType(item.m_shared.m_itemType);
                if (currentInventorySlot != correctInventorySlot)
                {
                    EquipUtil.MoveItemToSlot(___m_inventory, item, correctInventorySlot);
                }
            }

            return true;
        }
    }

    //public bool EquipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
    [HarmonyPatch(typeof(Humanoid), "UnequipItem", new Type[] { typeof(ItemDrop.ItemData), typeof(bool) })]
    public static class Humanoid_UnequipItem_Patch
    {
        private static bool _shouldDrop;
        private static bool _dropping;

        public static bool Prefix(ItemDrop.ItemData item, Inventory ___m_inventory)
        {
            if (_dropping)
            {
                return false;
            }

            _shouldDrop = false;
            if (item != null && EquipmentAndQuickSlots.IsSlotEquippable(item) && EquipmentAndQuickSlots.IsEquipmentSlot(item.m_gridPos))
            {
                if (___m_inventory.HaveEmptySlot())
                {
                    var correctInventorySlot = ___m_inventory.FindEmptySlot(false);
                    EquipUtil.MoveItemToSlot(___m_inventory, item, correctInventorySlot);
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
