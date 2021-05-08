using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Minimap), "Explore", typeof(Vector3), typeof(float))]
	public class DiscoveryRadiusIncrease_Minimap_Explore_Patch
	{
		[UsedImplicitly]
		private static void Prefix(Minimap __instance, ref float radius)
		{
			var items = Player.m_localPlayer.GetMagicEquipmentWithEffect(MagicEffectType.ModifyDiscoveryRadius);
			var skillBonus = 1f;
			foreach (var item in items)
			{
				skillBonus += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyDiscoveryRadius, 0.01f);
			}

			radius *= skillBonus;
		}
	}
}