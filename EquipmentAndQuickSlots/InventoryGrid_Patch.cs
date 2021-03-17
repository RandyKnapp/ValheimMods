using Common;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EquipmentAndQuickSlots
{
    //private void UpdateGui(Player player, ItemDrop.ItemData dragItem)
    [HarmonyPatch(typeof(GuiBar), "Awake")]
    public static class GuiBar_Awake_Patch
    {
        private static bool Prefix(GuiBar __instance)
        {
            // I have no idea why this bar is set to zero initially
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (__instance.name == "durability" && __instance.m_bar.sizeDelta.x != 54)
            {
                __instance.m_bar.sizeDelta = new Vector2(54, 0);
            }

            return true;
        }
    }

    //public InventoryGrid.Element GetElement(int x, int y, int width) => this.m_elements[y * width + x];
    [HarmonyPatch(typeof(InventoryGrid), "GetElement", typeof(int), typeof(int), typeof(int))]
    public static class InventoryGrid_GetElement_Patch
    {
        private static bool Prefix(InventoryGrid __instance, ref InventoryGrid.Element __result, int x, int y, int width)
        {
            var index = y * width + x;
            if (index < 0 || index >= __instance.m_elements.Count)
            {
                EquipmentAndQuickSlots.LogError($"Tried to get element for item ({x}, {y}) in inventory ({__instance.m_inventory.m_name}) but that element is outside the bounds!");
                __result = null;
            }
            else
            {
                __result = __instance.m_elements[index];
            }

            return false;
        }
    }

    //private void UpdateGui(Player player, ItemDrop.ItemData dragItem)
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui", typeof(Player), typeof(ItemDrop.ItemData))]
    public static class InventoryGrid_UpdateGui_Patch
    {
        private static void Postfix(InventoryGrid __instance)
        {
            if (__instance.name == "QuickSlotGrid")
            {
                for (var i = 0; i < EquipmentAndQuickSlots.QuickSlotCount; ++i)
                {
                    var element = __instance.m_elements[i];
                    var bindingText = element.m_go.transform.Find("binding").GetComponent<Text>();
                    bindingText.enabled = true;
                    bindingText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    bindingText.text = EquipmentAndQuickSlots.GetBindingLabel(i);
                }
            }
            else if (__instance.name == "EquipmentSlotGrid")
            {
                var horizontalSpacing = __instance.m_elementSpace + 10;
                var verticalSpacing = __instance.m_elementSpace + 10;
                string[] equipNames = { "Head", "Chest", "Legs", "Shoulders", "Utility 1", "Utility 2" };
                Vector2[] equipPositions = {
                    new Vector2(), // Head
                    new Vector2(0, -verticalSpacing), // Chest
                    new Vector2(0, -2 * verticalSpacing), // Legs
                    new Vector2(horizontalSpacing, -0.5f * verticalSpacing), // Shoulders
                    new Vector2(horizontalSpacing, -1.5f * verticalSpacing), // Utility 1
                    //new Vector2(horizontalSpacing, -2 * verticalSpacing), // Utility 2
                };

                for (var i = 0; i < EquipmentAndQuickSlots.EquipSlotCount; ++i)
                {
                    var element = __instance.m_elements[i];
                    var bindingText = element.m_go.transform.Find("binding").GetComponent<Text>();
                    bindingText.enabled = true;
                    bindingText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    bindingText.text = equipNames[i];
                    bindingText.rectTransform.anchoredPosition = new Vector2(32, 5);

                    var offset = new Vector2(-20, 79);
                    element.m_go.RectTransform().anchoredPosition = offset + equipPositions[i];
                }
            }
        }
    }
}
