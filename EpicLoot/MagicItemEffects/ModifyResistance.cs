using System.Collections.Generic;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
    [HarmonyPatch(typeof(Player), "ApplyArmorDamageMods")]
    public static class ModifyResistance_Player_ApplyArmorDamageMods_Patch
    {
        public static void Postfix(Player __instance, ref HitData.DamageModifiers mods)
        {
            var damageMods = new List<HitData.DamageModPair>();

            if (__instance.HasMagicEquipmentWithEffect(MagicEffectType.AddFireResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Fire, m_modifier = HitData.DamageModifier.Resistant});
            }
            if (__instance.HasMagicEquipmentWithEffect(MagicEffectType.AddFrostResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Frost, m_modifier = HitData.DamageModifier.Resistant });
            }
            if (__instance.HasMagicEquipmentWithEffect(MagicEffectType.AddLightningResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Lightning, m_modifier = HitData.DamageModifier.Resistant });
            }
            if (__instance.HasMagicEquipmentWithEffect(MagicEffectType.AddPoisonResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Poison, m_modifier = HitData.DamageModifier.Resistant });
            }
            if (__instance.HasMagicEquipmentWithEffect(MagicEffectType.AddSpiritResistance))
            {
                damageMods.Add(new HitData.DamageModPair() { m_type = HitData.DamageType.Spirit, m_modifier = HitData.DamageModifier.Resistant });
            }

            mods.Apply(damageMods);
        }
    }
}
