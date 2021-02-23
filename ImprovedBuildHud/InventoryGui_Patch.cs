using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedBuildHud
{
    [HarmonyPatch(typeof(InventoryGui), "SetupRequirement", new Type[] { typeof(Transform), typeof(Piece.Requirement), typeof(Player), typeof(bool), typeof(int) })]
    public static class InventoryGui_SetupRequirement_Patch
    {
        static bool Prefix(ref bool __result, Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
        {
            Image icon = elementRoot.transform.Find("res_icon").GetComponent<Image>();
            Text nameText = elementRoot.transform.Find("res_name").GetComponent<Text>();
            Text amountText = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            UITooltip tooltip = elementRoot.GetComponent<UITooltip>();
            if (req.m_resItem != null)
            {
                icon.gameObject.SetActive(true);
                nameText.gameObject.SetActive(true);
                amountText.gameObject.SetActive(true);
                icon.sprite = req.m_resItem.m_itemData.GetIcon();
                icon.color = Color.white;
                tooltip.m_text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
                nameText.text = Localization.instance.Localize(req.m_resItem.m_itemData.m_shared.m_name);
                int num = ImprovedBuildHud.GetAvailableItems(req.m_resItem.m_itemData.m_shared.m_name);
                int amount = req.GetAmount(quality);
                if (amount <= 0)
                {
                    InventoryGui.HideRequirement(elementRoot);
                    __result = false;
                    return false;
                }

                amountText.supportRichText = true;
                amountText.horizontalOverflow = HorizontalWrapMode.Overflow;
                var inventoryAmount = string.Format(ImprovedBuildHudConfig.InventoryAmountFormat.Value, num);
                if (!string.IsNullOrEmpty(ImprovedBuildHudConfig.InventoryAmountColor.Value))
                {
                    inventoryAmount = $"<color={ImprovedBuildHudConfig.InventoryAmountColor.Value}>{inventoryAmount}</color>";
                }
                amountText.text = $"{amount} {inventoryAmount}";

                if (num < amount)
                {
                    amountText.color = (double)Mathf.Sin(Time.time * 10f) > 0.0 ? Color.red : Color.white;
                }
                else
                {
                    amountText.color = Color.white;
                }
            }
            __result = true;
            return false;
        }
    }
}
