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
            if (__instance.name == "durability" && __instance.m_bar.sizeDelta.x != 54)
            {
                __instance.m_bar.sizeDelta = new Vector2(54, 0);
            }

            return true;
        }
    }

    //private void UpdateGui(Player player, ItemDrop.ItemData dragItem)
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui", typeof(Player), typeof(ItemDrop.ItemData))]
    public static class InventoryGrid_UpdateGui_Patch
    {
        private static void Postfix(InventoryGrid __instance, Player player, ItemDrop.ItemData dragItem)
        {
            if (__instance.name == "QuickSlotGrid")
            {
                for (int i = 0; i < EquipmentAndQuickSlots.QuickSlotCount; ++i)
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
                float horizontalSpacing = __instance.m_elementSpace + 10;
                float verticalSpacing = __instance.m_elementSpace + 10;
                string[] equipNames = { "Head", "Chest", "Legs", "Shoulders", "Utility 1", "Utility 2" };
                Vector2[] equipPositions = {
                    new Vector2(), // Head
                    new Vector2(0, -verticalSpacing), // Chest
                    new Vector2(0, -2 * verticalSpacing), // Legs
                    new Vector2(horizontalSpacing, 0), // Shoulders
                    new Vector2(horizontalSpacing, -1 * verticalSpacing), // Utility 1
                    new Vector2(horizontalSpacing, -2 * verticalSpacing), // Utility 2
                };

                for (int i = 0; i < EquipmentAndQuickSlots.EquipSlotCount; ++i)
                {
                    var element = __instance.m_elements[i];
                    var bindingText = element.m_go.transform.Find("binding").GetComponent<Text>();
                    bindingText.enabled = true;
                    bindingText.horizontalOverflow = HorizontalWrapMode.Overflow;
                    bindingText.text = equipNames[i];
                    bindingText.rectTransform.anchoredPosition = new Vector2(32, 5);

                    Vector2 offset = new Vector2();// Vector2(692, -20);
                    (element.m_go.transform as RectTransform).anchoredPosition = offset + equipPositions[i];
                }
            }
        }
    }
}
