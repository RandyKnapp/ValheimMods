using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Projectile), nameof(Projectile.SpawnOnHit))]
	public class RecallWeapon_Projectile_SpawnOnHit_Patch
	{
		private static ItemDrop IssueItemRecall(ItemDrop item, Projectile projectile)
		{
			if (item && item.m_itemData.HasMagicEffect(MagicEffectType.RecallWeapon))
            {
            	projectile.m_owner?.m_nview.InvokeRPC("epic loot weapon recall", item.m_nview.GetZDO().m_uid);
            }

			return item;
		}

		private static readonly MethodInfo ItemDropper = AccessTools.DeclaredMethod(typeof(ItemDrop), nameof(ItemDrop.DropItem));
		private static readonly MethodInfo ItemRecaller = AccessTools.DeclaredMethod(typeof(RecallWeapon_Projectile_SpawnOnHit_Patch), nameof(IssueItemRecall));
		
		[UsedImplicitly]
		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var instruction in instructions)
			{
				yield return instruction;
				if (instruction.opcode == OpCodes.Call && instruction.OperandIs(ItemDropper))
				{
					yield return new CodeInstruction(OpCodes.Ldarg_0); // this
					yield return new CodeInstruction(OpCodes.Call, ItemRecaller);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Player), nameof(Player.Awake))]
	public class RPC_WeaponRecall_Player_Awake_Patch
	{
		[UsedImplicitly]
		private static void Postfix(Player __instance)
		{
			__instance.m_nview.Register<ZDOID>("epic loot weapon recall", (s, item) => RPC_WeaponRecall(__instance, item));
		}

		private static void RPC_WeaponRecall(Player player, ZDOID item)
		{
			ZNetScene.instance.FindInstance(item)?.GetComponent<ItemDrop>()?.Pickup(player);
		}
	}
}