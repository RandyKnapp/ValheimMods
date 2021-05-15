using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch]
	public class FreeBuildGuiDisplay_Recipe_GetRequiredStation_Patch
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
			if (piece == null || Player.m_localPlayer == null)
			{
				return;
			}

			__state = piece.m_craftingStation;
			if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.FreeBuild))
			{
				piece.m_craftingStation = null;
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
	}
}