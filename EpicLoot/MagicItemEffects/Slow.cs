using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	public class Slow : MonoBehaviour
	{
		public float multiplier;
		public float ttl;

		private void Start()
		{
			Character character = GetComponent<Character>();

			character.m_acceleration *= multiplier;
			character.m_runSpeed *= multiplier;
			character.m_flyFastSpeed *= multiplier;
			character.m_swimSpeed *= multiplier;
		}

		private void FixedUpdate()
		{
			ttl -= Time.fixedDeltaTime;
			if (ttl > 0)
			{
				return;
			}

			Character character = GetComponent<Character>();

			character.m_acceleration /= multiplier;
			character.m_runSpeed /= multiplier;
			character.m_flyFastSpeed /= multiplier;
			character.m_swimSpeed /= multiplier;

			Destroy(this);
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.Awake))]
	public static class SlowAddRPC_Character_Awake_Patch
	{
		[UsedImplicitly]
		private static void Postfix(Character __instance) => __instance.m_nview.Register<float>("epic loot slow", (s, multiplier) => RPC_Slow(__instance, multiplier));

		private static void RPC_Slow(Character character, float multiplier)
		{
			if (!(character.GetComponent<Slow>() is Slow slow))
			{
				slow = character.gameObject.AddComponent<Slow>();
				slow.multiplier = multiplier;
			}

			slow.ttl = 2;
		}
	}

	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public static class ApplySlow_Character_RPC_Damage_Patch
	{
		[UsedImplicitly]
		private static void Postfix(Character __instance, HitData hit)
		{
			if (!__instance.IsBoss())
			{
				var slowMultiplier = 1f;
				if ((hit.GetAttacker() as Player)?.GetMagicEquipmentWithEffect(MagicEffectType.Slow) is List<ItemDrop.ItemData> items)
				{
					foreach (var item in items)
					{
						slowMultiplier -= item.GetMagicItem().GetTotalEffectValue(MagicEffectType.Slow, 0.01f);
					}

					if (slowMultiplier != 1f)
					{
						__instance.m_nview.InvokeRPC(ZRoutedRpc.Everybody, "epic loot slow", slowMultiplier);
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.FixedUpdate))]
	public static class ModifyEnemyAttackSpeed_CharacterAnimEvent_FixedUpdate_Patch
	{
		[UsedImplicitly]
		[HarmonyPriority(Priority.LowerThanNormal)]
		private static void Prefix(Character ___m_character, ref Animator ___m_animator)
		{
			if (___m_character.InAttack() && ___m_character.GetComponent<Slow>()?.multiplier is float slowMultiplier)
			{
				if (___m_animator.speed > 0.001f && (___m_animator.speed * 1e4f % 10 > 3 || ___m_animator.speed * 1e4f % 10 < 1))
				{
					___m_animator.speed = (float) Math.Round(___m_animator.speed * slowMultiplier, 3) + ___m_animator.speed % 1e-4f + 2e-4f;
				}
			}
		}
	}
}