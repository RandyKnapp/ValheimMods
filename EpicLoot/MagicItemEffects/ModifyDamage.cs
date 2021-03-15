using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public HitData.DamageTypes GetDamage(int quality)
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetDamage", typeof(int))]
    class ModifyDamage_ItemData_GetDamage_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (!__instance.IsMagic())
            {
                return;
            }

            var magicItem = __instance.GetMagicItem();

            // Add damages first
            __result.m_blunt += GetAddedDamageType(magicItem, MagicEffectType.AddBluntDamage);
            __result.m_slash += GetAddedDamageType(magicItem, MagicEffectType.AddSlashingDamage);
            __result.m_pierce += GetAddedDamageType(magicItem, MagicEffectType.AddPiercingDamage);
            __result.m_fire += GetAddedDamageType(magicItem, MagicEffectType.AddFireDamage);
            __result.m_frost += GetAddedDamageType(magicItem, MagicEffectType.AddFrostDamage);
            __result.m_lightning += GetAddedDamageType(magicItem, MagicEffectType.AddLightningDamage);
            __result.m_poison += GetAddedDamageType(magicItem, MagicEffectType.AddPoisonDamage);
            __result.m_spirit += GetAddedDamageType(magicItem, MagicEffectType.AddSpiritDamage);

            // Then modify
            if (magicItem.HasEffect(MagicEffectType.ModifyPhysicalDamage))
            {
                var totalDamageMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyPhysicalDamage, 0.01f);
                var modifier = 1.0f + totalDamageMod;

                __result.m_blunt *= modifier;
                __result.m_slash *= modifier;
                __result.m_pierce *= modifier;
            }

            if (magicItem.HasEffect(MagicEffectType.ModifyElementalDamage))
            {
                var totalDamageMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyElementalDamage, 0.01f);
                var modifier = 1.0f + totalDamageMod;

                __result.m_fire *= modifier;
                __result.m_frost *= modifier;
                __result.m_lightning *= modifier;
            }

            if (magicItem.HasEffect(MagicEffectType.ModifyDamage))
            {
                var totalDamageMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyDamage, 0.01f);
                __result.Modify(1.0f + totalDamageMod);
            }
        }

        private static float GetAddedDamageType(MagicItem magicItem, string effectType)
        {
            if (magicItem.HasEffect(effectType))
            {
                return magicItem.GetTotalEffectValue(effectType);
            }

            return 0;
        }
    }
}
