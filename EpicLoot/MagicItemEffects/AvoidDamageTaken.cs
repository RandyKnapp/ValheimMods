using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public class AvoidDamageTaken_Character_RPC_Damage_Patch
	{
		[UsedImplicitly]
		private static bool Prefix(Character __instance, HitData hit)
		{
			var attacker = hit.GetAttacker();
			if (__instance is Player player && attacker != null && attacker != __instance)
			{
				var avoidanceChance = 0f;
				ModifyWithLowHealth.Apply(player, MagicEffectType.AvoidDamageTaken, effect =>
				{
                    avoidanceChance += player.GetTotalActiveMagicEffectValue(effect, 0.01f);
                });

				return !(Random.Range(0f, 1f) < avoidanceChance);
			}

			return true;
		}
	}
}