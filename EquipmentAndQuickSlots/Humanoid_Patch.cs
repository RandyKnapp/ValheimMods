using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    public static class EquipmentSlotHelper
    {
        public static bool DoingEquip = false;
        public static bool AllowMove = true;

        public class SwapData
        {
            public Inventory InventoryA;
            public ItemDrop.ItemData Item;
            public Inventory InventoryB;
            public Vector2i SlotB;

            public SwapData(Inventory inventoryA, ItemDrop.ItemData item, Inventory inventoryB, Vector2i slotB)
            {
                InventoryA = inventoryA;
                Item = item;
                InventoryB = inventoryB;
                SlotB = slotB;
            }
        }

        public static void Swap(Inventory inventoryA, ItemDrop.ItemData item, Inventory inventoryB, Vector2i slotB)
        {
            var slotA = item.m_gridPos;
            if (inventoryA == inventoryB && item.m_gridPos == slotB)
            {
                EquipmentAndQuickSlots.Log("Item already in correct slot");
                return;
            }

            var otherItemInSlot = inventoryB.GetItemAt(slotB.x, slotB.y);
            if (otherItemInSlot != null)
            {
                EquipmentAndQuickSlots.Log($"Item exists in other slot ({otherItemInSlot.m_shared.m_name})");
                inventoryB.m_inventory.Remove(otherItemInSlot);
            }

            inventoryA.m_inventory.Remove(item);
            inventoryB.m_inventory.Add(item);
            item.m_gridPos = slotB;

            if (otherItemInSlot != null)
            {
                otherItemInSlot.m_gridPos = slotA;
                inventoryA.m_inventory.Add(otherItemInSlot);
            }

            inventoryA.Changed();
            inventoryB.Changed();
        }

        public static void RefreshEquipmentInSlots(Player player)
        {
            var inventories = player.GetAllInventories();
            var equipSlotInventory = player.GetEquipmentSlotInventory();
            var swaps = new List<SwapData>();
            var drops = new List<SwapData>();

            // Swap in newly equipped items
            foreach (var inventory in inventories)
            {
                if (inventory != equipSlotInventory)
                {
                    foreach (var item in inventory.m_inventory)
                    {
                        if (item.m_equiped && EquipmentAndQuickSlots.IsSlotEquippable(item))
                        {
                            var equipSlot = EquipmentAndQuickSlots.GetEquipmentSlotForType(item.m_shared.m_itemType);
                            if (equipSlot.x < 0 || equipSlot.y < 0)
                            {
                                continue;
                            }
                            swaps.Add(new SwapData(inventory, item, equipSlotInventory, equipSlot));
                            EquipmentAndQuickSlots.LogWarning($"move ({item.m_shared.m_name}) to equip slot");
                        }
                    }
                }
            }

            foreach (var swap in swaps)
            {
                Swap(swap.InventoryA, swap.Item, swap.InventoryB, swap.SlotB);
            }
            swaps.Clear();

            // Swap out unequipped items and incorrectly added
            foreach (var item in equipSlotInventory.m_inventory)
            {
                if (!item.m_equiped || !EquipmentAndQuickSlots.IsSlotEquippable(item))
                {
                    var destInventories = player.GetAllInventories();
                    bool moved = false;
                    foreach (var destInventory in destInventories)
                    {
                        if (!destInventory.CountAsEmptySlots(player))
                        {
                            continue;
                        }

                        var emptySlot = destInventory.FindEmptySlot(false);
                        if (emptySlot.x >= 0 && emptySlot.y >= 0)
                        {
                            moved = true;
                            swaps.Add(new SwapData(equipSlotInventory, item, destInventory, emptySlot));
                            break;
                        }
                    }

                    EquipmentAndQuickSlots.LogWarning($"move ({item.m_shared.m_name}) to main inventory");
                    if (!moved)
                    {
                        if (!CanEquip(item, player))
                        {
                            player.Message(MessageHud.MessageType.Center, "Item force unequipped, inventory full, dropped item");
                            drops.Add(new SwapData(equipSlotInventory, item, null, new Vector2i()));
                        }
                        else
                        {
                            item.m_equiped = true;
                            player.Message(MessageHud.MessageType.Center, "Could not unequip, inventory full");
                        }
                    }
                }
                else
                {
                    var equipSlot = EquipmentAndQuickSlots.GetEquipmentSlotForType(item.m_shared.m_itemType);
                    if ((equipSlot.x >= 0 && equipSlot.y >= 0) && item.m_gridPos != equipSlot)
                    {
                        item.m_gridPos = equipSlot;
                        EquipmentAndQuickSlots.LogWarning($"move ({item.m_shared.m_name}) to correct slot ({equipSlot})");
                    }
                }
            }

            foreach (var drop in drops)
            {
                ItemDrop.DropItem(drop.Item, drop.Item.m_stack, player.transform.position, Quaternion.identity);
                drop.InventoryA.RemoveItem(drop.Item);
            }
            drops.Clear();

            foreach (var swap in swaps)
            {
                Swap(swap.InventoryA, swap.Item, swap.InventoryB, swap.SlotB);
            }
            swaps.Clear();
        }

        public static bool CanEquip(ItemDrop.ItemData item, Player player)
        {
            if (player.IsItemEquiped(item) 
                || (player.InAttack() || player.InDodge()) 
                || player.IsPlayer() 
                && !player.IsDead() 
                && (player.IsSwiming() && !player.IsOnGround()) 
                || item.m_shared.m_useDurability && item.m_durability <= 0.0f)
                return false;
            return true;
        }

        //public bool EquipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        public static class Humanoid_EquipItem_Patch
        {
            public static bool Prefix()
            {
                DoingEquip = true;
                return true;
            }

            public static void Postfix(Humanoid __instance, bool __result, ItemDrop.ItemData item)
            {
                if (AllowMove && __instance != null && __instance.IsPlayer() && __instance.m_nview.IsValid())
                {
                    RefreshEquipmentInSlots(__instance as Player);
                }
                DoingEquip = false;
            }
        }

        //public void UnequipItem(ItemDrop.ItemData item, bool triggerEquipEffects = true)
        [HarmonyPatch(typeof(Humanoid), "UnequipItem")]
        public static class Humanoid_UnequipItem_Patch
        {
            public static void Postfix(Humanoid __instance, ItemDrop.ItemData item)
            {
                if (AllowMove && !DoingEquip && __instance != null && __instance.IsPlayer() && __instance.m_nview.IsValid() && item != null)
                {
                    RefreshEquipmentInSlots(__instance as Player);
                }
            }
        }
    }
}
