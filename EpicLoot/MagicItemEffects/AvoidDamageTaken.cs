using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), "Damage")]
	public class AvoidDamageTaken_Character_Damage_Patch
	{
		[UsedImplicitly]
		private static bool Prefix(Character __instance, HitData hit)
		{
			Character attacker = hit.GetAttacker();
			if (__instance is Player player && player.HasMagicEquipmentWithEffect(MagicEffectType.AvoidDamageTaken) && attacker != null && attacker != __instance)
			{
				var avoidanceChance = 0f;
				var items = player.GetMagicEquipmentWithEffect(MagicEffectType.AvoidDamageTaken);
				foreach (var item in items)
				{
					avoidanceChance += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.AvoidDamageTaken, 0.01f);
				}

				return !(Random.Range(0f, 1f) < avoidanceChance);
			}

			return true;
		}
	}
}