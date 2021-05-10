using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Character), "Damage")]
	public class ModifyStaggerDamage_Character_Damage_Patch
	{
		public static bool HandlingProjectileDamage = false;
		
		[UsedImplicitly]
		private static void Prefix(Character __instance, HitData hit)
		{
			Character attacker = hit.GetAttacker();
			if (!HandlingProjectileDamage && attacker is Player player)
			{
				hit.ApplyModifier(ReadStaggerDamageValue(player));
			}
		}

		public static float ReadStaggerDamageValue(Player player)
		{
			var staggerDamage = 1f;
            var items = player.GetMagicEquipmentWithEffect(MagicEffectType.ModifyStaggerDamage);
            foreach (var item in items)
            {
            	staggerDamage += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyStaggerDamage, 0.01f);
            }

            return staggerDamage;
		}
	}

	[HarmonyPatch(typeof(Projectile), "OnHit")]
	public class ModifyStaggerDamageProjectileHit_Projectile_OnHit_Patch
	{
		[UsedImplicitly]
		private static void Prefix() => ModifyStaggerDamage_Character_Damage_Patch.HandlingProjectileDamage = true;

		[UsedImplicitly]
		private static void Postfix() => ModifyStaggerDamage_Character_Damage_Patch.HandlingProjectileDamage = false;
	}

	[HarmonyPatch(typeof(Projectile), "Setup")]
	public class ModifyStaggerDamage_Projectile_Setup_Patch
	{
		[UsedImplicitly]
		private static void Prefix(Character owner, HitData hitData)
		{
			if (owner is Player player)
			{
				hitData.ApplyModifier(ModifyStaggerDamage_Character_Damage_Patch.ReadStaggerDamageValue(player));
			}
		}
	}
}