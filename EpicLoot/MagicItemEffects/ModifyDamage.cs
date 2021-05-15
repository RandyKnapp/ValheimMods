using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public HitData.DamageTypes GetDamage(int quality)
    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDamage), typeof(int))]
    public class ModifyDamage_ItemData_GetDamage_Patch
    {
        public static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (!__instance.IsMagic())
            {
                return;
            }

            var magicItem = __instance.GetMagicItem();
            var magicItemskillType = __instance.m_shared.m_skillType;

            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);

            // Add damages first
            __result.m_blunt += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddBluntDamage);
            __result.m_slash += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddSlashingDamage);
            __result.m_pierce += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddPiercingDamage);
            __result.m_fire += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddFireDamage);
            __result.m_frost += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddFrostDamage);
            __result.m_lightning += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddLightningDamage);
            __result.m_poison += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddPoisonDamage);
            __result.m_spirit += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddSpiritDamage);
            
            if (magicItemskillType == Skills.SkillType.Axes)
            {
                __result.m_chop += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddSlashingDamage);
            }
            else if (magicItemskillType == Skills.SkillType.Pickaxes)
            {
                __result.m_pickaxe += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.AddPiercingDamage);
            }

            // Then modify
            if (MagicEffectsHelper.HasActiveMagicEffect(player, magicItem, MagicEffectType.ModifyPhysicalDamage))
            {
                var totalDamageMod = MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.ModifyPhysicalDamage, 0.01f);
                var modifier = 1.0f + totalDamageMod;

                __result.m_blunt *= modifier;
                __result.m_slash *= modifier;
                __result.m_pierce *= modifier;
                __result.m_chop *= modifier;
                __result.m_pickaxe *= modifier;
            }

            if (MagicEffectsHelper.HasActiveMagicEffect(player, magicItem, MagicEffectType.ModifyElementalDamage))
            {
                var totalDamageMod = MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, MagicEffectType.ModifyElementalDamage, 0.01f);
                var modifier = 1.0f + totalDamageMod;

                __result.m_fire *= modifier;
                __result.m_frost *= modifier;
                __result.m_lightning *= modifier;
                __result.m_poison *= modifier;
                __result.m_spirit *= modifier;
            }

            var damageMod = 0f;
            ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyDamage, effect =>
            {
                if (MagicEffectsHelper.HasActiveMagicEffect(player, magicItem, effect))
                {
                    damageMod += MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, magicItem, effect, 0.01f);
                }
            });
            __result.Modify(1.0f + damageMod);
        }
    }
}
