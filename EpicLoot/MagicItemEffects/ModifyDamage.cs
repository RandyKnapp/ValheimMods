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
            var magicItemType = __instance.m_shared.m_itemType;
            var magicItemskillType = __instance.m_shared.m_skillType;

            // Add damages first
            __result.m_blunt += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddBluntDamage);
            __result.m_slash += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddSlashingDamage);
            __result.m_pierce += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddPiercingDamage);
            __result.m_fire += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddFireDamage);
            __result.m_frost += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddFrostDamage);
            __result.m_lightning += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddLightningDamage);
            __result.m_poison += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddPoisonDamage);
            __result.m_spirit += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddSpiritDamage);
            
             // If item is an Axe add slash value to chop type damage
            if (magicItemskillType == Skills.SkillType.Axes)
            {
                __result.m_chop += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddSlashingDamage);
            };

            // If item is a Pickaxe add pierce value to pickaxe type damage
            if (magicItemskillType == Skills.SkillType.Pickaxes)
            {
                __result.m_pickaxe += GetAddedDamageType(magicItem, magicItemType, magicItemskillType, MagicEffectType.AddPiercingDamage);
            };


            // Then modify
            if (magicItem.HasEffect(MagicEffectType.ModifyPhysicalDamage))
            {
                var totalDamageMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyPhysicalDamage, 0.01f);
                var modifier = 1.0f + totalDamageMod;

                __result.m_blunt *= modifier;
                __result.m_slash *= modifier;
                __result.m_pierce *= modifier;
                __result.m_chop *= modifier;
                __result.m_pickaxe *= modifier;
            }

            if (magicItem.HasEffect(MagicEffectType.ModifyElementalDamage))
            {
                var totalDamageMod = magicItem.GetTotalEffectValue(MagicEffectType.ModifyElementalDamage, 0.01f);
                var modifier = 1.0f + totalDamageMod;

                __result.m_fire *= modifier;
                __result.m_frost *= modifier;
                __result.m_lightning *= modifier;
                __result.m_poison *= modifier;
                __result.m_spirit *= modifier;
            }

            var damageMod = 0f;
            ModifyWithLowHealth.Apply(Player.m_localPlayer, MagicEffectType.ModifyDamage, effect =>
            {
	            if (magicItem.HasEffect(effect))
	            {
		            damageMod += magicItem.GetTotalEffectValue(effect, 0.01f);
	            }
            });
			__result.Modify(1.0f + damageMod);
        }

        private static float GetAddedDamageType(MagicItem magicItem, ItemDrop.ItemData.ItemType magicItemType, Skills.SkillType magicItemSkillType, string effectType)
        {
            if (magicItem.HasEffect(effectType))
            {
                return magicItem.GetTotalEffectValue(effectType);
            }

            return 0;
        }
    }
}
