using System;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
	//public float GetDeflectionForce(int quality)
	[HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDeflectionForce), typeof(int))]
	public static class ModifyParry_ItemData_GetDeflectionForce_Patch
	{
		public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
            var totalParryMod = 0f;
			ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyParry, effect =>
            {
                totalParryMod += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, __instance, effect, 0.01f);
            });

			__result *= 1.0f + totalParryMod;
            if (player != null && player.m_leftItem == null && player.HasActiveMagicEffect(MagicEffectType.Duelist))
			{
				__result += __instance.GetDamage().GetTotalDamage() / 2 * player.GetTotalActiveMagicEffectValue(MagicEffectType.Duelist, 0.01f);
			}

			__result = (float) Math.Round(__result, 1);
		}
	}

	//public override bool BlockAttack(HitData hit, Character attacker)
	[HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
	public static class ModifyParry_Humanoid_BlockAttack_Patch
	{
		public static bool Override;
		public static float OriginalValue;

		public static bool Prefix(Humanoid __instance, HitData hit, Character attacker)
		{
			Override = false;
			OriginalValue = -1;

			var currentBlocker = __instance.GetCurrentBlocker();
			if (currentBlocker == null || !(__instance is Player player))
			{
				return true;
			}

			var totalParryBonusMod = 0f;
			ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyParry, effect =>
			{
				if (player.HasActiveMagicEffect(effect))
				{
					if (!Override)
					{
						Override = true;
						OriginalValue = currentBlocker.m_shared.m_timedBlockBonus;
					}

					totalParryBonusMod += player.GetTotalActiveMagicEffectValue(effect, 0.01f);
				}
			});

			currentBlocker.m_shared.m_timedBlockBonus *= 1.0f + totalParryBonusMod;
			
			return true;
		}

		public static void Postfix(Humanoid __instance, HitData hit, Character attacker)
		{
			var currentBlocker = __instance.GetCurrentBlocker();
			if (currentBlocker != null && Override)
			{
				currentBlocker.m_shared.m_timedBlockBonus = OriginalValue;
			}

			Override = false;
			OriginalValue = -1;
		}
	}
}