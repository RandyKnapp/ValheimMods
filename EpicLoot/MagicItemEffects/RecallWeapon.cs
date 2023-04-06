using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch]
	public class RecallWeapon_Projectile_SpawnOnHit_Patch
    {
        public const float AutoRecallDistance = 80;

		private static ItemDrop IssueItemRecall(ItemDrop item, Projectile projectile)
		{
			if (item && item.m_itemData.HasMagicEffect(MagicEffectType.RecallWeapon))
            {
            	projectile.m_owner?.m_nview.InvokeRPC("el-wr", item.m_nview.GetZDO().m_uid);
            }

			return item;
		}

		private static readonly MethodInfo ItemDropper = AccessTools.DeclaredMethod(typeof(ItemDrop), nameof(ItemDrop.DropItem));
		private static readonly MethodInfo ItemRecaller = AccessTools.DeclaredMethod(typeof(RecallWeapon_Projectile_SpawnOnHit_Patch), nameof(IssueItemRecall));
		
		[UsedImplicitly]
	    [HarmonyPatch(typeof(Projectile), nameof(Projectile.SpawnOnHit))]
		[HarmonyTranspiler]
		private static IEnumerable<CodeInstruction> Projectile_SpawnOnHit_Transpiler(IEnumerable<CodeInstruction> instructions)
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

		[HarmonyPatch(typeof(Projectile), nameof(Projectile.LateUpdate))]
		[HarmonyPostfix]
        public static void Projectile_LateUpdate_Postfix(Projectile __instance)
        {
            var item = __instance.m_spawnItem;
            var player = Player.m_localPlayer;
            if (player != null && item != null && item.HasMagicEffect(MagicEffectType.RecallWeapon))
            {
                var v = player.transform.position - __instance.transform.position;
                var distSq = v.sqrMagnitude;
                EpicLoot.Log($"Distance from player: {v.magnitude:F1}");
                if (distSq > AutoRecallDistance * AutoRecallDistance)
                {
					EpicLoot.LogWarning($"[RecallWeapon] Destroying projectile and recalling weapon when over {AutoRecallDistance} meters away from the player.");
                    var itemDrop = ItemDrop.DropItem(__instance.m_spawnItem, 0, __instance.transform.position, __instance.transform.rotation);
                    IssueItemRecall(itemDrop, __instance);
                    __instance.m_spawnItem = null;
                    ZNetScene.instance.Destroy(__instance.gameObject);
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
			__instance.m_nview.Register<ZDOID>("el-wr", (s, item) => RPC_WeaponRecall(__instance, item));
		}

		private static void RPC_WeaponRecall(Player player, ZDOID item)
		{
			ZNetScene.instance.FindInstance(item)?.GetComponent<ItemDrop>()?.Pickup(player);
		}
	}
}