using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), "Stagger")]
	public class Immovable_Character_Stagger_Patch
	{
		[UsedImplicitly]
		private static bool Prefix(Character __instance)
		{
			return !(__instance is Player player) || !player.HasMagicEquipmentWithEffect(MagicEffectType.Immovable) || !player.IsBlocking();
		}
	}
	
	[HarmonyPatch(typeof(Character), "ApplyPushback")]
	public class Immovable_Character_ApplyPushback_Patch
	{
		[UsedImplicitly]
		private static bool Prefix(Character __instance)
		{
			return !(__instance is Player player) || !player.HasMagicEquipmentWithEffect(MagicEffectType.Immovable) || !player.IsBlocking();
		}
	}
}