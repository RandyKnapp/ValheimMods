using HarmonyLib;
using TMPro;

namespace ImprovedBuildHud
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.SetupPieceInfo), typeof(Piece))]
    public static class Hud_Patch
    {
        private static void Postfix(Piece piece, TMP_Text ___m_buildSelection)
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
                    // Vanilla tolerates a null m_resItem and a zero amount (mis-configured pieces from
                    // other mods produce both); this runs per frame, so guard rather than throw.
                    if (requirement == null || requirement.m_resItem == null || requirement.m_amount <= 0)
                    {
                        continue;
                    }
                    var currentAmount = ImprovedBuildHud.GetAvailableItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                    var canMake = currentAmount / requirement.m_amount;
                    if (canMake < fewestPossible)
                    {
                        fewestPossible = canMake;
                    }
                }
                if (fewestPossible == int.MaxValue)
                {
                    return;
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
