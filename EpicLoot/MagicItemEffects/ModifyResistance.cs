using System;
using System.Collections.Generic;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
    [HarmonyPatch(typeof(Player), nameof(Player.ApplyArmorDamageMods))]
    public static class ModifyResistance_Player_ApplyArmorDamageMods_Patch
    {
        public static void Postfix(Player __instance, ref HitData.DamageModifiers mods)
        {
            var damageMods = new List<HitData.DamageModPair>();

            if (__instance.HasActiveMagicEffect(MagicEffectType.AddFireResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Fire, m_modifier = HitData.DamageModifier.Resistant});
            }
            if (__instance.HasActiveMagicEffect(MagicEffectType.AddFrostResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Resistant });
            }
            if (__instance.HasActiveMagicEffect(MagicEffectType.AddLightningResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Lightning, m_modifier = HitData.DamageModifier.Resistant });
            }
            if (__instance.HasActiveMagicEffect(MagicEffectType.AddPoisonResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Poison, m_modifier = HitData.DamageModifier.Resistant });
            }
            if (__instance.HasActiveMagicEffect(MagicEffectType.AddSpiritResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Spirit, m_modifier = HitData.DamageModifier.Resistant });
            }

            mods.Apply(damageMods);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public static class ModifyResistance_Character_RPC_Damage_Patch
    {
	    public static void Prefix(Character __instance, HitData hit)
	    {
		    if (!(__instance is Player player))
		    {
			    return;
		    }

		    float sum(params string[] effects)
		    {
			    float value = 1;
			    foreach (var effect in effects)
			    {
				    value -= player.GetTotalActiveMagicEffectValue(effect, 0.01f);
			    }

			    return Math.Max(value, 0);
		    }

		    // elemental resistances
		    hit.m_damage.m_fire *= sum(MagicEffectType.AddFireResistancePercentage, MagicEffectType.AddElementalResistancePercentage);
		    hit.m_damage.m_frost *= sum(MagicEffectType.AddFrostResistancePercentage, MagicEffectType.AddElementalResistancePercentage);
		    hit.m_damage.m_lightning *= sum(MagicEffectType.AddLightningResistancePercentage, MagicEffectType.AddElementalResistancePercentage);
		    hit.m_damage.m_poison *= sum(MagicEffectType.AddPoisonResistancePercentage, MagicEffectType.AddElementalResistancePercentage);
		    hit.m_damage.m_spirit *= sum(MagicEffectType.AddSpiritResistancePercentage, MagicEffectType.AddElementalResistancePercentage);
		    
		    // physical resistances
		    hit.m_damage.m_blunt *= sum(MagicEffectType.AddBluntResistancePercentage, MagicEffectType.AddPhysicalResistancePercentage);
		    hit.m_damage.m_slash *= sum(MagicEffectType.AddSlashingResistancePercentage, MagicEffectType.AddPhysicalResistancePercentage);
		    hit.m_damage.m_pierce *= sum(MagicEffectType.AddPiercingResistancePercentage, MagicEffectType.AddPhysicalResistancePercentage);
		    hit.m_damage.m_chop *= sum(MagicEffectType.AddChoppingResistancePercentage, MagicEffectType.AddPhysicalResistancePercentage);
	    }
    }
}
