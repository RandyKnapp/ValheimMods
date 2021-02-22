using System;
using HarmonyLib;
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
                var player = Player.m_localPlayer;
                var displayName = Localization.instance.Localize(piece.m_name);

                int fewestPossible = Int32.MaxValue;
                for (int index = 0; index < piece.m_resources.Length; ++index)
                {
                    Piece.Requirement requirement = piece.m_resources[index];
                    int currentAmount = player.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                    int canMake = currentAmount / requirement.m_amount;
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
