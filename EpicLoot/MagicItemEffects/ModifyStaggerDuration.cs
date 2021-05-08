using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(CharacterAnimEvent), "FixedUpdate")]
	public static class ModifyStaggerDuration_CharacterAnimEvent_FixedUpdate_Patch
	{
		private static void Prefix(Character ___m_character, ref Animator ___m_animator)
		{
			if (!(___m_character.m_nview?.GetZDO() is ZDO zdo))
			{
				return;
			}

			float speedupfactor = zdo.GetFloat("epic loot stagger duration");
			if (___m_character.IsStaggering() && speedupfactor > 1f && ___m_animator.speed > 0.001f)
			{
				float speed = ___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.length / speedupfactor * (___m_animator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "stagger2" ? 2 : 1);
				___m_animator.speed = speed;
			}
		}
	}

	[HarmonyPatch(typeof(Character), "RPC_Damage")]
	public static class RPC_TagCharacterOnHit_Character_RPC_Damage_Patch
	{
		private static void Prefix(Character __instance, HitData hit)
		{
			if (!__instance.IsStaggering() && hit.m_skill != Skills.SkillType.Bows && hit.m_skill != Skills.SkillType.None)
			{
				float staggerValue = 1f;
				if (hit.GetAttacker() is Player player)
				{
					var items = player.GetMagicEquipmentWithEffect(MagicEffectType.ModifyStaggerDuration);
					foreach (var item in items)
					{
						staggerValue += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
					}
				}
				__instance.m_nview.GetZDO().Set("epic loot stagger duration", staggerValue);
			}
		}
	}

	[HarmonyPatch(typeof(Humanoid), "BlockAttack")]
	public static class TagCharacterOnBlock_Humanoid_BlockAttack_Patch
	{
		private static void Prefix(Humanoid __instance, Character attacker)
		{
			if (!attacker.IsStaggering())
			{
				float staggerValue = 1f;
				if (__instance is Player player)
				{
					var items = player.GetMagicEquipmentWithEffect(MagicEffectType.ModifyStaggerDuration);
					foreach (var item in items)
					{
						staggerValue += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
					}

				}
				attacker.m_nview.GetZDO().Set("epic loot stagger duration", staggerValue);
			}
		}
	}

	[HarmonyPatch(typeof(Projectile), "OnHit")]
	public class StaggeringProjectileHit_Projectile_OnHit_Patch
	{
		private static void Prefix(Projectile __instance, Collider collider)
		{
			if (collider != null && Projectile.FindHitObject(collider) is GameObject target && target.GetComponent<Character>() is Character character)
			{
				if (__instance.m_nview?.GetZDO() is ZDO zdo)
				{
					float staggerValue = zdo.GetFloat("epic loot stagger duration", 1f);
					character.m_nview.GetZDO().Set("epic loot stagger duration", staggerValue);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
	public class StaggeringProjectileInstantiation_Attack_FireProjectileBurst_Patch
	{
		private static GameObject MarkAttackProjectile(GameObject attackProjectile, Attack attack)
		{
			if (attack.m_character == Player.m_localPlayer)
			{
				float staggerValue = 1f;
				var items = Player.m_localPlayer.GetMagicEquipmentWithEffect(MagicEffectType.ModifyStaggerDuration);
				foreach (var item in items)
				{
					staggerValue += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyStaggerDuration, 0.01f);
				}

				attackProjectile.GetComponent<ZNetView>().GetZDO().Set("epic loot stagger duration", staggerValue);
			}

			return attackProjectile;
		}

		private static readonly MethodInfo AttackProjectileMarker = AccessTools.DeclaredMethod(typeof(StaggeringProjectileInstantiation_Attack_FireProjectileBurst_Patch), nameof(MarkAttackProjectile));
		private static readonly MethodInfo Instantiator = AccessTools.GetDeclaredMethods(typeof(Object)).Where(m => m.Name == "Instantiate" && m.GetGenericArguments().Length == 1).Select(m => m.MakeGenericMethod(typeof(GameObject))).First(m => m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3));

        [UsedImplicitly]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;
				if (instruction.opcode == OpCodes.Call && instruction.OperandIs(Instantiator))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0); // this
					yield return new CodeInstruction(OpCodes.Call, AttackProjectileMarker);
				}
			}
		}
	}
}
