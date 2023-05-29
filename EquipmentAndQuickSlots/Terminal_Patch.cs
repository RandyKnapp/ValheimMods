using System;
using System.Collections.Generic;
using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    public static class Terminal_Patch
    {
        public static void Postfix()
        {
            Terminal.ConsoleCommand resetinventory = new Terminal.ConsoleCommand("resetinventory", "Remove everything from every inventory", (args =>
            {
                Player.m_localPlayer.GetAllInventories().ForEach(x =>x.RemoveAll());
            }), true);

            Terminal.ConsoleCommand breakequipment = new Terminal.ConsoleCommand("breakequipment", "Break all the equipment in your inventory", (args =>
            {
                foreach (var inventory in Player.m_localPlayer.GetAllInventories())
                {
                    foreach (var item in inventory.m_inventory)
                    {
                        if (item.m_equipped && item.m_shared.m_useDurability)
                        {
                            item.m_durability = 0;
                        }
                    }
                }
            }));

            Terminal.ConsoleCommand fixMainInventory = new Terminal.ConsoleCommand("fixinventory", "Fix the bug where items are not visible in your inventory (drops them to the ground)", (args =>
            {
                Player.m_localPlayer.GetAllInventories().ForEach(inv =>
                {
                    EquipmentAndQuickSlots.LogWarning($"Fixing inventory: {inv.m_name} ({inv.m_width}, {inv.m_height})");
                    var currentItemPositions = new List<Vector2i>();

                    foreach (var itemData in inv.m_inventory)
                    {
                        var hasOverlap = currentItemPositions.Exists(pos => pos == itemData.m_gridPos);
                        if (hasOverlap)
                        {
                            EquipmentAndQuickSlots.LogWarning($"Found item overlapping other item: {itemData.m_shared.m_name} ({itemData.m_gridPos.x}, {itemData.m_gridPos.y}), dropping to ground...");
                            Player.m_localPlayer.DropItem(inv, itemData, itemData.m_stack);
                        }

                        if (itemData.m_gridPos.x < 0 || itemData.m_gridPos.x >= inv.m_width || itemData.m_gridPos.y < 0 || itemData.m_gridPos.y >= inv.m_height)
                        {
                            EquipmentAndQuickSlots.LogWarning($"Found item outside grid: {itemData.m_shared.m_name} ({itemData.m_gridPos.x}, {itemData.m_gridPos.y}), dropping to ground...");
                            Player.m_localPlayer.DropItem(inv, itemData, itemData.m_stack);
                        }

                        currentItemPositions.Add(itemData.m_gridPos);
                    }
                });
            }));

            Terminal.ConsoleCommand dropAll = new Terminal.ConsoleCommand("dropall", "Drop every item in your inventory", (args =>
            {
                Player.m_localPlayer.GetAllInventories().ForEach(inv =>
                {
                    var items = new List<ItemDrop.ItemData>(inv.m_inventory);
                    foreach (var itemData in items)
                    {
                        EquipmentAndQuickSlots.LogWarning($"Dropping item: {itemData.m_shared.m_name} ({itemData.m_gridPos.x}, {itemData.m_gridPos.y}), dropping to ground...");
                        Player.m_localPlayer.DropItem(inv, itemData, itemData.m_stack);
                    }
                });
            }));

            Terminal.ConsoleCommand invCheck = new Terminal.ConsoleCommand("invcheck", "List every item in your inventory", (args =>
            {
                Player.m_localPlayer.GetAllInventories().ForEach(inv =>
                {
                    EquipmentAndQuickSlots.LogWarning($"inv: {inv.m_name}, ({inv.m_width}, {inv.m_height})");
                    var items = new List<ItemDrop.ItemData>(inv.m_inventory);
                    foreach (var itemData in items)
                    {
                        EquipmentAndQuickSlots.LogWarning($"- {itemData.m_shared.m_name} ({itemData.m_gridPos.x}, {itemData.m_gridPos.y})");
                    }
                });
            }));
        }
    }
}
