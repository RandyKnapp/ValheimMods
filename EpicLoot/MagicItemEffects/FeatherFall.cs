using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), "UpdateGroundContact")]
	public class FeatherFallDisableFallDamage_Character_UpdateGroundContact_Patch
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, ref float ___m_maxAirAltitude)
		{
			if (!__instance.m_groundContact)
			{
				return;
			}
			
			if (__instance is Player player && player.HasMagicEquipmentWithEffect(MagicEffectType.FeatherFall))
			{
				___m_maxAirAltitude = Mathf.Min(3.99f + __instance.transform.position.y, ___m_maxAirAltitude);
			}
		}
	}
	
	[HarmonyPatch(typeof(Player), "FixedUpdate")]
	public class FeatherFallReduceFallSpeed_Player_FixedUpdate_Patch
	{
		[UsedImplicitly]
		private static void Postfix(Player __instance)
		{
			if (__instance.HasMagicEquipmentWithEffect(MagicEffectType.FeatherFall) && __instance.m_body)
			{
				Vector3 velocity = __instance.m_body.velocity;
				velocity.y = Math.Max(-2, velocity.y);
				__instance.m_body.velocity = velocity;
			}
		}
	}
}