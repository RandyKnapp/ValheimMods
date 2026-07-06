using BepInEx;
using EpicLoot.Config;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Reflection;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
    public static class FreeBuildGuiDisplay_Recipe_GetRequiredStation_Patch
    {
        [UsedImplicitly]
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.HaveRequirements), new[] {typeof(Piece), typeof(Player.RequirementMode)});
            yield return AccessTools.DeclaredMethod(typeof(Player), nameof(Player.CheckCanRemovePiece));
            yield return AccessTools.DeclaredMethod(typeof(Hud), nameof(Hud.SetupPieceInfo));
        }

        [UsedImplicitly]
        private static void Prefix(ref CraftingStation __state, Piece piece)
        {
            if (piece == null || Player.m_localPlayer == null || ZoneSystem.instance == null)
            {
                return;
            }

            __state = piece.m_craftingStation;

            if (piece.m_craftingStation != null && piece.m_craftingStation.name != null &&
                Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.FreeBuild,  out float magicEffect))
            {
                if (CanBeFreeBuilt(piece))
                {
                    piece.m_craftingStation = null;
                }
            }
        }

        [UsedImplicitly]
        private static void Postfix(CraftingStation __state, Piece piece)
        {
            if (piece != null && Player.m_localPlayer != null)
            {
                piece.m_craftingStation = __state;
            }
        }

        private static bool CanBeFreeBuilt(Piece piece)
        {
            if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.Unlimited)
            {
                return true;
            }

            string requiredKey = "";

            switch (piece.m_craftingStation.name)
            {
                case "forge":
                    if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.BossKillUnlocksCurrentBiomePieces)
                    {
                        requiredKey = "defeated_eikthyr";
                    }
                    else if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.BossKillUnlocksNextBiomePieces)
                    {
                        requiredKey = "";
                    }
                    break;
                case "piece_stonecutter":
                    if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.BossKillUnlocksCurrentBiomePieces)
                    {
                        requiredKey = "defeated_gdking";
                    }
                    else if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.BossKillUnlocksNextBiomePieces)
                    {
                        requiredKey = "defeated_eikthyr";
                    }
                    break;
                case "piece_artisanstation":
                    if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.BossKillUnlocksCurrentBiomePieces)
                    {
                        requiredKey = "defeated_dragon";
                    }
                    else if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.BossKillUnlocksNextBiomePieces)
                    {
                        requiredKey = "defeated_bonemass";
                    }
                    break;
                case "blackforge":
                case "piece_magetable":
                    if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.BossKillUnlocksCurrentBiomePieces)
                    {
                        requiredKey = "defeated_goblinking";
                    }
                    else if (ELConfig.GatedFreebuildMode.Value == GatedItemType.GatedPieceTypeMode.BossKillUnlocksNextBiomePieces)
                    {
                        requiredKey = "defeated_dragon";
                    }
                    break;
                default:
                    return true;
            }

            if (!requiredKey.IsNullOrWhiteSpace())
            {
                return ZoneSystem.instance.GetGlobalKey(requiredKey);
            }

            return true;
        }
    }
}