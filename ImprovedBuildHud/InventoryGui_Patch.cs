using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedBuildHud
{
    [HarmonyPatch(typeof(InventoryGui), "SetupRequirement", new Type[] { typeof(Transform), typeof(Piece.Requirement), typeof(Player), typeof(bool), typeof(int) })]
    public static class InventoryGui_SetupRequirement_Patch
    {
        static void Postfix(Transform elementRoot, Piece.Requirement req, Player player, bool craft, int quality)
        {
            if (string.IsNullOrEmpty(ImprovedBuildHudConfig.InventoryAmountFormat.Value))
            {
                return;
            }

            Text amountText = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            if (amountText.isActiveAndEnabled)
            {
                int num = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
                int amount = req.GetAmount(quality);
                amountText.supportRichText = true;
                var inventoryAmount = string.Format(ImprovedBuildHudConfig.InventoryAmountFormat.Value, num);
                if (!string.IsNullOrEmpty(ImprovedBuildHudConfig.InventoryAmountColor.Value))
                {
                    inventoryAmount = $"<color={ImprovedBuildHudConfig.InventoryAmountColor.Value}>{inventoryAmount}</color>";
                }
                amountText.text = $"{amount} {inventoryAmount}";
            }
        }
    }
}
