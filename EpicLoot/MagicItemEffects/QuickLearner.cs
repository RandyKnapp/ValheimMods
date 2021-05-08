using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Skills), "RaiseSkill")]
	public class QuickLearner_Skills_RaiseSkill_Patch
	{
		[UsedImplicitly]
		private static void Prefix(Skills __instance, ref float factor)
		{
			var items = __instance.m_player.GetMagicEquipmentWithEffect(MagicEffectType.QuickLearner);
			var skillBonus = 1f;
			foreach (var item in items)
			{
				skillBonus += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.QuickLearner, 0.01f);
			}

			factor *= skillBonus;
		}
	}
}