using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
	public class ModifyParryWindow_Humanoid_BlockAttack_Patch
	{
		[UsedImplicitly]
		private static void Prefix(Humanoid __instance, ref float ___m_blockTimer, ref float __state)
		{
			if (__instance is Player player && player.HasActiveMagicEffect(MagicEffectType.ModifyParryWindow) && ___m_blockTimer > -1)
			{
				var parryFrameIncrease = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyParryWindow);
                ___m_blockTimer -= parryFrameIncrease / 1000;
				__state = parryFrameIncrease;
			}
		}
		
		[UsedImplicitly]
		private static void Postfix(Humanoid __instance, ref float ___m_blockTimer, float __state)
		{
			if (__instance is Player player && player.HasActiveMagicEffect(MagicEffectType.ModifyParryWindow) && ___m_blockTimer > -1)
			{
				___m_blockTimer += __state / 1000;
			}
		}
	}
}