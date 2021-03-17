using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EquipmentAndQuickSlots
{
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
                BuildInventoryGrid(ref QuickSlotGrid, "QuickSlotGrid", new Vector2(500, -160), new Vector2((74 * EquipmentAndQuickSlots.QuickSlotCount) + 10, 90), inventoryGui);
            }

            private static void BuildEquipmentSlotGrid(InventoryGui inventoryGui)
            {
                BuildInventoryGrid(ref EquipmentSlotGrid, "EquipmentSlotGrid", new Vector2(500, 20), new Vector2(210, 270), inventoryGui);
            }

            private static void BuildInventoryGrid(ref InventoryGrid grid, string name, Vector2 position, Vector2 size, InventoryGui inventoryGui)
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
                highlight.AddComponent<Image>().color = Color.yellow;
                var highlightRT = highlight.transform as RectTransform;
                highlightRT.anchoredPosition = new Vector2(0, 0);
                highlightRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x + 2);
                highlightRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y + 2);
                highlightRT.localScale = new Vector3(1, 1, 1);

                var bkg = inventoryGui.m_player.Find("Bkg").gameObject;
                var background = Object.Instantiate(bkg, go.transform);
                background.name = name + "Bkg";
                var backgroundRT = background.transform as RectTransform;
                backgroundRT.anchoredPosition = new Vector2(0, 0);
                backgroundRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                backgroundRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                backgroundRT.localScale = new Vector3(1, 1, 1);

                grid = go.AddComponent<InventoryGrid>();
                var root = new GameObject("Root", typeof(RectTransform));
                root.transform.SetParent(go.transform, false);

                var rect = go.transform as RectTransform;
                rect.anchoredPosition = position;

                grid.m_elementPrefab = inventoryGui.m_playerGrid.m_elementPrefab;
                grid.m_gridRoot = root.transform as RectTransform;
                grid.m_elementSpace = inventoryGui.m_playerGrid.m_elementSpace;
                grid.ResetView();

                if (name == "EquipmentSlotGrid")
                {
                    grid.m_onSelected += OnEquipmentSelected(inventoryGui);
                    grid.m_onRightClick += OnEquipmentRightClicked(inventoryGui);
                }
                else
                {
                    grid.m_onSelected += OnSelected(inventoryGui);
                    grid.m_onRightClick += OnRightClicked(inventoryGui);
                }

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
                    EquipmentAndQuickSlots.Log($"OnSelected: inventoryGrid={inventoryGrid}, item={item?.m_shared.m_name}, pos={pos}, mod={mod}");
                    inventoryGui.OnSelectedItem(inventoryGrid, item, pos, mod);
                };
            }

            private static Action<InventoryGrid, ItemDrop.ItemData, Vector2i> OnRightClicked(InventoryGui inventoryGui)
            {
                return (InventoryGrid inventoryGrid, ItemDrop.ItemData item, Vector2i pos) =>
                {
                    EquipmentAndQuickSlots.Log($"OnRightClicked: inventoryGrid={inventoryGrid}, item={item?.m_shared.m_name}, pos={pos}");
                    if (item == null || Player.m_localPlayer == null)
                    {
                        return;
                    }
                    Player.m_localPlayer.UseItem(Player.m_localPlayer.m_inventory.Extended(), item, true);
                };
            }

            private static Action<InventoryGrid, ItemDrop.ItemData, Vector2i, InventoryGrid.Modifier> OnEquipmentSelected(InventoryGui inventoryGui)
            {
                return (InventoryGrid inventoryGrid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod) =>
                {
                    var player = Player.m_localPlayer;
                    EquipmentAndQuickSlots.Log($"OnEquipmentSelected: inventoryGrid={inventoryGrid}, item={item?.m_shared.m_name}, pos={pos}, mod={mod}");

                    if (player != null 
                        && inventoryGui.m_dragItem != null 
                        && EquipmentAndQuickSlots.IsSlotEquippable(inventoryGui.m_dragItem)
                        && pos == EquipmentAndQuickSlots.GetEquipmentSlotForType(inventoryGui.m_dragItem.m_shared.m_itemType))
                    {
                        player.QueueEquipItem(inventoryGui.m_dragItem);
                        inventoryGui.SetupDragItem(null, null, 0);
                    }
                };
            }

            private static Action<InventoryGrid, ItemDrop.ItemData, Vector2i> OnEquipmentRightClicked(InventoryGui inventoryGui)
            {
                return (InventoryGrid inventoryGrid, ItemDrop.ItemData item, Vector2i pos) =>
                {
                    var player = Player.m_localPlayer;
                    EquipmentAndQuickSlots.Log($"OnEquipmentRightClicked: inventoryGrid={inventoryGrid}, item={item?.m_shared.m_name}, pos={pos}");
                    if (item != null 
                        && player != null 
                        && item.m_equiped 
                        && player.IsItemEquiped(item)
                        && inventoryGui.m_dragItem == null)
                    {
                        Player.m_localPlayer.QueueUnequipItem(item);
                    }
                };
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateInventory")]
        public static class InventoryGui_UpdateInventory_Patch
        {
            public static bool Prefix(InventoryGui __instance, Player player)
            {
                player.m_inventory.Extended().CallBase = true;
                __instance.m_playerGrid.UpdateInventory(player.m_inventory, player, __instance.m_dragItem);
                player.m_inventory.Extended().CallBase = false;

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

                return false;
            }
        }
    }
}
