using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public HitData.DamageModifiers GetDamageModifiers()
    [HarmonyPatch(typeof(Character), "GetDamageModifiers")]
    public static class ModifyResistance_Character_GetDamageModifiers_Patch
    {
        public static void Postfix(Character __instance, HitData.DamageModifiers __result)
        {
            if (__instance.IsPlayer())
            {
                Player player = __instance as Player;

                var damageMods = new List<HitData.DamageModPair>();

                if (player.HasMagicEquipmentWithEffect(MagicEffectType.AddFireResistance))
                {
                    damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Fire, m_modifier = HitData.DamageModifier.Resistant});
                }
                if (player.HasMagicEquipmentWithEffect(MagicEffectType.AddFrostResistance))
                {
                    damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Resistant });
                }
                if (player.HasMagicEquipmentWithEffect(MagicEffectType.AddLightningResistance))
                {
                    damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Lightning, m_modifier = HitData.DamageModifier.Resistant });
                }
                if (player.HasMagicEquipmentWithEffect(MagicEffectType.AddPoisonResistance))
                {
                    damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Poison, m_modifier = HitData.DamageModifier.Resistant });
                }
                if (player.HasMagicEquipmentWithEffect(MagicEffectType.AddSpiritResistance))
                {
                    damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Spirit, m_modifier = HitData.DamageModifier.Resistant });
                }

                __result.Apply(damageMods);
            }
        }
    }
}
