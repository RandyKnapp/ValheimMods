using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EquipmentAndQuickSlots
{
    /*[HarmonyPatch(typeof(InventoryGui), "OnSelectedItem", new Type[] { typeof(InventoryGrid), typeof(ItemDrop.ItemData), typeof(Vector2i), typeof(InventoryGrid.Modifier) })]
    public static class InventoryGui_OnSelectedItem_Patch
    {
        public static bool Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod, GameObject ___m_dragGo, ItemDrop.ItemData ___m_dragItem)
        {
            if (grid.m_inventory.m_name.Equals("Inventory") && EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value && EquipmentAndQuickSlots.IsEquipmentSlot(pos))
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
    }*/

    public static class InventoryGui_Patch
    {
        public static InventoryGrid QuickSlotGrid;

        [HarmonyPatch(typeof(InventoryGui), "Awake")]
        public static class InventoryGui_Awake_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (QuickSlotGrid != null)
                {
                    Object.Destroy(QuickSlotGrid);
                    QuickSlotGrid = null;
                }

                var go = new GameObject("QuickSlotGrid", typeof(RectTransform));
                go.transform.SetParent(__instance.m_player, false);

                var highlight = new GameObject("SelectedFrame", typeof(RectTransform));
                highlight.transform.SetParent(go.transform, false);
                highlight.AddComponent<Image>().color = Color.magenta;

                QuickSlotGrid = go.AddComponent<InventoryGrid>();
                var root = new GameObject("Root", typeof(RectTransform));
                root.transform.SetParent(go.transform, false);

                var rect = go.transform as RectTransform;
                rect.anchoredPosition = new Vector2(500, -140);

                QuickSlotGrid.m_elementPrefab = __instance.m_playerGrid.m_elementPrefab;
                QuickSlotGrid.m_gridRoot = root.transform as RectTransform;
                //QuickSlotGrid.m_uiGroup = __instance.m_playerGrid.m_uiGroup; // TODO, figure this out
                QuickSlotGrid.m_elementSpace = __instance.m_playerGrid.m_elementSpace;
                QuickSlotGrid.ResetView();

                QuickSlotGrid.m_onSelected += __instance.OnSelectedItem;
                QuickSlotGrid.m_onRightClick += __instance.OnRightClickItem;

                QuickSlotGrid.m_uiGroup = QuickSlotGrid.gameObject.AddComponent<UIGroupHandler>();
                QuickSlotGrid.m_uiGroup.m_groupPriority = 1;
                QuickSlotGrid.m_uiGroup.m_active = true;
                QuickSlotGrid.m_uiGroup.m_enableWhenActiveAndGamepad = highlight;

                var list = __instance.m_uiGroups.ToList();
                list.Insert(2, QuickSlotGrid.m_uiGroup);
                __instance.m_uiGroups = list.ToArray();
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Update")]
        public static class InventoryGui_Update_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                var player = Player.m_localPlayer;
                if (QuickSlotGrid == null || player == null)
                {
                    return;
                }

                var quickSlotInventory = player.GetQuickSlotInventory();
                if (quickSlotInventory != null)
                {
                    QuickSlotGrid.UpdateInventory(quickSlotInventory, player, __instance.m_dragItem);
                }
            }
        }
    }
}
