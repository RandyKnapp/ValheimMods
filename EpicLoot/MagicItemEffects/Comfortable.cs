using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(SE_Rested), "CalculateComfortLevel")]
	public class Comfortable_SE_Rested_CalculateComfortLevel_Patch
	{
		[UsedImplicitly]
		private static void Postfix(Player player, ref int __result)
		{
			var items = player.GetMagicEquipmentWithEffect(MagicEffectType.Comfortable);
			var skillBonus = 1;
			foreach (var item in items)
			{
				skillBonus += (int)item.GetMagicItem().GetTotalEffectValue(MagicEffectType.Comfortable);
			}

			__result += skillBonus;
		}
	}
}