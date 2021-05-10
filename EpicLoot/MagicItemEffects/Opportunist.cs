using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
	public class Opportunist_Character_RPC_Damage_Patch
	{
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			Character attacker = hit.GetAttacker();
			if (attacker is Player player && player.HasMagicEquipmentWithEffect(MagicEffectType.Opportunist) && __instance.IsStaggering())
            {
                var chance = 0f;
                var items = player.GetMagicEquipmentWithEffect(MagicEffectType.Opportunist);
                foreach (var item in items)
                {
                    chance += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.Opportunist, 0.01f);
                }

                if (Random.Range(0f, 1f) < chance)
                {
                    __instance.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, __instance.transform);
                    hit.ApplyModifier(hit.m_backstabBonus);
                }
			}
		}
	}
}