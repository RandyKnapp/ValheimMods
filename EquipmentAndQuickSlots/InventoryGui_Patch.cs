using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EquipmentAndQuickSlots
{
    [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem", new Type[] { typeof(InventoryGrid), typeof(ItemDrop.ItemData), typeof(Vector2i), typeof(InventoryGrid.Modifier) })]
    public static class InventoryGui_OnSelectedItem_Patch
    {
        public static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
        {
            if (grid.m_inventory.m_name.Equals("EquipmentSlotInventory") && EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value)
            {
                if (__instance.m_dragItem != null 
                    && EquipmentAndQuickSlots.IsSlotEquippable(__instance.m_dragItem) 
                    && EquipmentAndQuickSlots.GetEquipmentTypeForSlot(pos.x) == __instance.m_dragItem.m_shared.m_itemType)
                {
                    var player = Player.m_localPlayer;
                    player.UseItem(player.GetInventory(), __instance.m_dragItem, true);
                    __instance.SetupDragItem(null, null, 1);
                }
                return false;
            }

            return true;
        }
    }

    public static class InventoryGui_Patch
    {
        public static InventoryGrid QuickSlotGrid;
        public static InventoryGrid EquipmentSlotGrid;

        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class InventoryGui_Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                BuildQuickSlotGrid(__instance);
                BuildEquipmentSlotGrid(__instance);
            }

            private static void BuildQuickSlotGrid(InventoryGui inventoryGui)
            {
                BuildInventoryGrid(ref QuickSlotGrid, "QuickSlotGrid", new Vector2(500, -140), inventoryGui);
            }

            private static void BuildEquipmentSlotGrid(InventoryGui inventoryGui)
            {
                BuildInventoryGrid(ref EquipmentSlotGrid, "EquipmentSlotGrid", new Vector2(500, 100), inventoryGui);
            }

            private static void BuildInventoryGrid(ref InventoryGrid grid, string name, Vector2 position, InventoryGui inventoryGui)
            {
                if (grid != null)
                {
                    Object.Destroy(grid);
                    grid = null;
                }

                var go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(inventoryGui.m_player, false);

                var highlight = new GameObject("SelectedFrame", typeof(RectTransform));
                highlight.transform.SetParent(go.transform, false);
                highlight.AddComponent<Image>().color = Color.magenta;

                grid = go.AddComponent<InventoryGrid>();
                var root = new GameObject("Root", typeof(RectTransform));
                root.transform.SetParent(go.transform, false);

                var rect = go.transform as RectTransform;
                rect.anchoredPosition = position;

                grid.m_elementPrefab = inventoryGui.m_playerGrid.m_elementPrefab;
                grid.m_gridRoot = root.transform as RectTransform;
                grid.m_elementSpace = inventoryGui.m_playerGrid.m_elementSpace;
                grid.ResetView();

                grid.m_onSelected += OnSelected(inventoryGui);
                grid.m_onRightClick += inventoryGui.OnRightClickItem;

                grid.m_uiGroup = QuickSlotGrid.gameObject.AddComponent<UIGroupHandler>();
                grid.m_uiGroup.m_groupPriority = 1;
                grid.m_uiGroup.m_active = true;
                grid.m_uiGroup.m_enableWhenActiveAndGamepad = highlight;

                var list = inventoryGui.m_uiGroups.ToList();
                list.Insert(2, grid.m_uiGroup);
                inventoryGui.m_uiGroups = list.ToArray();
            }

            private static Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier> OnSelected(InventoryGui inventoryGui)
            {
                return (InventoryGrid inventoryGrid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod) =>
                {
                    if (mod == InventoryGrid.Modifier.Move)
                    {
                        mod = InventoryGrid.Modifier.Select;
                    }
                    inventoryGui.OnSelectedItem(inventoryGrid, item, pos, mod);
                };
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                var player = Player.m_localPlayer;
                if (player == null)
                {
                    return;
                }

                if (QuickSlotGrid != null)
                {
                    var quickSlotInventory = player.GetQuickSlotInventory();
                    if (quickSlotInventory != null)
                    {
                        QuickSlotGrid.UpdateInventory(quickSlotInventory, player, __instance.m_dragItem);
                    }
                }

                if (EquipmentSlotGrid != null)
                {
                    var equipmentSlotInventory = player.GetEquipmentSlotInventory();
                    if (equipmentSlotInventory != null)
                    {
                        EquipmentSlotGrid.UpdateInventory(equipmentSlotInventory, player, __instance.m_dragItem);
                    }
                }
            }
        }
    }
}
