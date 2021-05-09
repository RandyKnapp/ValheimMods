using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Projectile), "Awake")]
	public class RPC_ExplodingArrow_Projectile_Awake_Patch
	{
        [UsedImplicitly]
		private static void Postfix(Projectile __instance)
		{
			__instance.m_nview.Register<Vector3, float>("epic loot exploding arrow", RPC_ExplodingArrow);
		}
			
		private static void RPC_ExplodingArrow(long sender, Vector3 position, float explodingArrowStrength)
		{
			var poisonPrefab = ZNetScene.instance.GetPrefab("vfx_blob_attack");
			var poisonCloud = Object.Instantiate(poisonPrefab, position, Quaternion.identity);
			var particles = poisonCloud.transform.Find("particles");
			var cloudParticles = particles.Find("ooz (1)").GetComponent<ParticleSystem>();
			var main = cloudParticles.main;
			main.startColor = new Color(0.9f, 0.3f, 0, 0.5f);
			main.simulationSpeed = 7;
			var splashParticles = particles.Find("wetsplsh").GetComponent<ParticleSystem>();
			main = splashParticles.main;
			main.startColor = new Color(1, 0.14f, 0.1f, 1);
			main.simulationSpeed = 3;

			var characters = new List<Character>();
			Character.GetCharactersInRange(poisonCloud.transform.localPosition, 4f, characters);
			foreach (var c in characters)
			{
				if (c.IsOwner())
				{
					var fireHit = new HitData {m_damage = {m_fire = explodingArrowStrength}};
					c.Damage(fireHit);
				}
			}
		}
	}

	
	[HarmonyPatch(typeof(Projectile), "OnHit")]
	public class ExplodingArrowHit_Projectile_OnHit_Patch
	{
        [UsedImplicitly]
		private static void Prefix(out bool __state, Projectile __instance)
		{
			__state = __instance.m_stayAfterHitStatic;
			__instance.m_stayAfterHitStatic = true;
		}
		
        [UsedImplicitly]
		private static void Postfix(bool __state, Vector3 hitPoint, Projectile __instance)
		{
			if (__instance.m_didHit)
			{
				var explodingArrow = __instance.m_nview.GetZDO().GetFloat("epic loot exploding arrow", float.NaN);
				if (!float.IsNaN(explodingArrow))
				{
					var explodingArrowStrength = explodingArrow * __instance.m_damage.GetTotalDamage();
					__instance.m_nview.InvokeRPC(ZRoutedRpc.Everybody, "epic loot exploding arrow", hitPoint, explodingArrowStrength);
				}

				if (!__state)
				{
					ZNetScene.instance.Destroy(__instance.gameObject);
				}
			}

			__instance.m_stayAfterHitStatic = __state;
		}
	}

	[HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
	public class ExplodingArrowInstantiation_Attack_FireProjectileBurst_Patch
	{
		private static GameObject ChooseAttackProjectile(GameObject defaultAttackProjectile, Attack attack)
		{
			if (attack.m_character == Player.m_localPlayer && Player.m_localPlayer.HasMagicEquipmentWithEffect(MagicEffectType.ExplosiveArrows))
			{
				return ObjectDB.instance.GetItemPrefab("ArrowFire").GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_attackProjectile;
			}

			return defaultAttackProjectile;
		}

		private static GameObject MarkAttackProjectile(GameObject attackProjectile, Attack attack)
		{
			if (attack.m_character == Player.m_localPlayer && Player.m_localPlayer.GetEquipmentOfType(ItemDrop.ItemData.ItemType.Bow)?.GetMagicItem()?.GetTotalEffectValue(MagicEffectType.ExplosiveArrows, 0.01f) is float explosiveStrength && explosiveStrength > 0)
			{
				attackProjectile.GetComponent<ZNetView>().GetZDO().Set("epic loot exploding arrow", explosiveStrength);
			}

			return attackProjectile;
		}

		private static readonly MethodInfo AttackProjectileMarker = AccessTools.DeclaredMethod(typeof(ExplodingArrowInstantiation_Attack_FireProjectileBurst_Patch), nameof(MarkAttackProjectile));
		private static readonly MethodInfo AttackProjectileChooser = AccessTools.DeclaredMethod(typeof(ExplodingArrowInstantiation_Attack_FireProjectileBurst_Patch), nameof(ChooseAttackProjectile));
		private static readonly MethodInfo Instantiator = AccessTools.GetDeclaredMethods(typeof(Object)).Where(m => m.Name == "Instantiate" && m.GetGenericArguments().Length == 1).Select(m => m.MakeGenericMethod(typeof(GameObject))).First(m => m.GetParameters().Length == 3 && m.GetParameters()[1].ParameterType == typeof(Vector3));

        [UsedImplicitly]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var result = new List<CodeInstruction>();
			var searchLdLoc = -1;
			OpCode[] ldLocOps = {OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3, OpCodes.Ldloc_S};
			foreach (var instruction in instructions.Reverse())
			{
				if (instruction.opcode == OpCodes.Call && instruction.OperandIs(Instantiator))
				{
					result.Add(new CodeInstruction(OpCodes.Call, AttackProjectileMarker));
					result.Add(new CodeInstruction(OpCodes.Ldarg_0)); // this
					searchLdLoc = 3;
				}

				if (ldLocOps.Contains(instruction.opcode) && --searchLdLoc == 0)
				{
					result.Add(new CodeInstruction(OpCodes.Call, AttackProjectileChooser));
					result.Add(new CodeInstruction(OpCodes.Ldarg_0)); // this
				}

				result.Add(instruction);
			}

			return ((IEnumerable<CodeInstruction>) result).Reverse();
		}
	}
}