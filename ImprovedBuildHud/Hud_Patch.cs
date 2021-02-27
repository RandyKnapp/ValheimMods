using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ImprovedBuildHud
{
    [HarmonyPatch(typeof(Hud), "SetupPieceInfo", new Type[] {typeof(Piece)})]
    public static class Hud_Patch
    {
        private static void Postfix(Piece piece, Text ___m_buildSelection)
        {
            if (piece != null && !string.IsNullOrEmpty(ImprovedBuildHudConfig.CanBuildAmountFormat.Value))
            {
                var displayName = Localization.instance.Localize(piece.m_name);
                if (piece.m_resources.Length == 0)
                {
                    return;
                }

                var fewestPossible = int.MaxValue;
                foreach (var requirement in piece.m_resources)
                {
                    var currentAmount = ImprovedBuildHud.GetAvailableItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                    var canMake = currentAmount / requirement.m_amount;
                    if (canMake < fewestPossible)
                    {
                        fewestPossible = canMake;
                    }
                }

                var canBuildDisplay = string.Format(ImprovedBuildHudConfig.CanBuildAmountFormat.Value, fewestPossible);
                if (!string.IsNullOrEmpty(ImprovedBuildHudConfig.CanBuildAmountColor.Value))
                {
                    canBuildDisplay = $"<color={ImprovedBuildHudConfig.CanBuildAmountColor.Value}>{canBuildDisplay}</color>";
                }
                ___m_buildSelection.text = $"{displayName} {canBuildDisplay}";
            }
        }
    }
}
