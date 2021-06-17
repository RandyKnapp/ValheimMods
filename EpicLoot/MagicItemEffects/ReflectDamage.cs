using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
	public class ReflectiveDamage_Character_Damage_Patch
	{
		private static bool _isApplyingReflectiveDmg;

		[UsedImplicitly]
		[HarmonyPriority(Priority.Last)]
		private static void Prefix(Character __instance, HitData hit)
		{
			var attacker = hit.GetAttacker();
			if (__instance is Player player && player.HasActiveMagicEffect(MagicEffectType.ReflectDamage) && attacker != null && attacker != __instance && !_isApplyingReflectiveDmg)
			{
				var reflectiveDamage = player.GetTotalActiveMagicEffectValue(MagicEffectType.ReflectDamage, 0.01f);
                if (reflectiveDamage > 0)
				{
					var hitData = new HitData()
					{
						m_attacker = __instance.GetZDOID(),
						m_dir = hit.m_dir * -1,
						m_point = attacker.transform.localPosition,
						m_damage = {m_pierce = (hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage()) * reflectiveDamage}
					};
					try
					{
						_isApplyingReflectiveDmg = true;
						attacker.Damage(hitData);
					}
					finally
					{
						_isApplyingReflectiveDmg = false;
					}
				}
			}
		}
	}
}