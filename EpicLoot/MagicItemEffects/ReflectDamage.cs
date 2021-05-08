using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), "Damage")]
	public class ReflectiveDamage_Character_Damage_Patch
	{
		private static bool IsApplyingReflectiveDmg = false;

		[UsedImplicitly]
		[HarmonyPriority(Priority.Last)]
		private static void Prefix(Character __instance, HitData hit)
		{
			Character attacker = hit.GetAttacker();
			if (__instance is Player player && player.HasMagicEquipmentWithEffect(MagicEffectType.ReflectDamage) && attacker != null && attacker != __instance && !IsApplyingReflectiveDmg)
			{
				var reflectiveDamage = 0f;
				var items = player.GetMagicEquipmentWithEffect(MagicEffectType.ReflectDamage);
				foreach (var item in items)
				{
					reflectiveDamage += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ReflectDamage, 0.01f);
				}

				if (reflectiveDamage > 0)
				{
					HitData hitData = new HitData()
					{
						m_attacker = __instance.GetZDOID(),
						m_dir = hit.m_dir * -1,
						m_point = attacker.transform.localPosition,
						m_damage = {m_pierce = hit.GetTotalDamage() * reflectiveDamage}
					};
					try
					{
						IsApplyingReflectiveDmg = true;
						attacker.Damage(hitData);
					}
					finally
					{
						IsApplyingReflectiveDmg = false;
					}
				}
			}
		}
	}
}