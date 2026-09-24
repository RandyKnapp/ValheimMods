using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using SkillType = Skills.SkillType;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
    public static class AddSkillLevel_Skills_GetSkillFactor_Patch
    {
        private static bool _inSkillsAsSkills = false;

        [UsedImplicitly]
        private static void Postfix(Skills __instance, SkillType skillType, ref float __result)
        {
            __result += SkillIncrease(__instance.m_player, skillType) / 100f;
        }

        public static int SkillIncrease(Player player, SkillType skillType)
        {
            var increase = 0;
            
            void check(string effect, params SkillType[] type)
            {
                if (type.Contains(skillType))
                {
                    increase += (int) player.GetTotalActiveMagicEffectValue(effect);
                }
            }



            void SkillsAsSkills(string effect, SkillType[] type, SkillType[] asType) 
            {
                if (_inSkillsAsSkills) return;
                if (type.Contains(skillType)) 
                {
                    _inSkillsAsSkills = true;

                    float effectValue = player.GetTotalActiveMagicEffectValue(effect);
                    try 
                    {
                        for (int i = 0; i < asType.Length; ++i) 
                        {
                            if (asType[i] == skillType) continue;
                            var asTotal = player.m_skills.GetSkillFactor(asType[i]); // skills total post bonuses
                            increase += (int)(asTotal * effectValue);
                        }
                    } 
                    finally 
                    {
                        _inSkillsAsSkills = false;
                    }
                }
            }

            check(MagicEffectType.AddSwordsSkill, SkillType.Swords);
            check(MagicEffectType.AddKnivesSkill, SkillType.Knives);
            check(MagicEffectType.AddClubsSkill, SkillType.Clubs);
            check(MagicEffectType.AddPolearmsSkill, SkillType.Polearms);
            check(MagicEffectType.AddSpearsSkill, SkillType.Spears);
            check(MagicEffectType.AddBlockingSkill, SkillType.Blocking);
            check(MagicEffectType.AddAxesSkill, SkillType.Axes);
            check(MagicEffectType.AddAxesSkill, SkillType.WoodCutting);
            check(MagicEffectType.AddBowsSkill, SkillType.Bows);
            check(MagicEffectType.AddCrossbowsSkill, SkillType.Crossbows);
            check(MagicEffectType.AddUnarmedSkill, SkillType.Unarmed);
            check(MagicEffectType.AddPickaxesSkill, SkillType.Pickaxes);
            check(MagicEffectType.AddFishingSkill, SkillType.Fishing);
            check(MagicEffectType.AddElementalMagicSkill, SkillType.ElementalMagic);
            check(MagicEffectType.AddBloodMagicSkill, SkillType.BloodMagic);
            check(MagicEffectType.AddMovementSkills, SkillType.Run, SkillType.Jump, SkillType.Swim, SkillType.Sneak);
            check(MagicEffectType.AddCrafterSkills, SkillType.Crafting, SkillType.Cooking);
            check(MagicEffectType.IncreaseMeleeSkills, Shards.IncreaseMeleeSkills.MeleeSkills);
            SkillsAsSkills(MagicEffectType.BlockAsDodgeAsBlock, Shards.BlockAsDodgeAsBlock.type, Shards.BlockAsDodgeAsBlock.asType);
            SkillsAsSkills(MagicEffectType.BlockAsWoodCuttingAndPickaxes, Shards.BlockAsWoodCuttingAndPickaxes.type, Shards.BlockAsWoodCuttingAndPickaxes.asType);

            return increase;
        }
    }

    // The skill *level* vanilla reads for gameplay -- a hit's m_skillLevel, which sets the Staff of Protection
    // bubble's absorb; a summon's damage factor and how many may be out at once (SpawnAbility) -- as opposed to
    // the skill factor, which the patch above already raises. Adding the bonus to Skills.GetSkillLevel outright
    // would count it twice in GetSkillFactor (which reads GetSkillLevel, then gets the bonus above) and in the
    // skills panel, so it is only added for the duration of a gameplay read: Character.GetSkillLevel, which
    // every attack, projectile, area effect and item tooltip goes through, and SpawnAbility's spawn coroutine,
    // which reads Skills.GetSkillLevel directly. A GetSkillFactor inside such a read (SkillIncrease's
    // skill-as-skill effects call it) suspends the read, for the same double-count reason.
    [HarmonyPatch]
    public static class AddSkillLevel_GameplaySkillLevel_Patch
    {
        private static int _gameplayReads;

        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillLevel))]
        [HarmonyPostfix]
        private static void Skills_GetSkillLevel_Postfix(Skills __instance, SkillType skillType, ref float __result)
        {
            if (_gameplayReads > 0 && __instance.m_player != null && skillType != SkillType.None)
            {
                __result += AddSkillLevel_Skills_GetSkillFactor_Patch.SkillIncrease(__instance.m_player, skillType);
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.GetSkillLevel))]
        [HarmonyPrefix]
        private static void Character_GetSkillLevel_Prefix() => _gameplayReads++;

        [HarmonyPatch(typeof(Character), nameof(Character.GetSkillLevel))]
        [HarmonyFinalizer]
        private static void Character_GetSkillLevel_Finalizer() => _gameplayReads--;

        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
        [HarmonyPrefix]
        private static void Skills_GetSkillFactor_Prefix(out int __state)
        {
            __state = _gameplayReads;
            _gameplayReads = 0;
        }

        [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillFactor))]
        [HarmonyFinalizer]
        private static void Skills_GetSkillFactor_Finalizer(int __state) => _gameplayReads = __state;

        [HarmonyPatch]
        private static class SpawnAbility_Spawn_Patch
        {
            [UsedImplicitly]
            private static MethodBase TargetMethod() =>
                AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(SpawnAbility), nameof(SpawnAbility.Spawn)));

            [UsedImplicitly]
            private static void Prefix() => _gameplayReads++;

            [UsedImplicitly]
            private static void Finalizer() => _gameplayReads--;
        }
    }

    // These fix a bug in vanilla where skill factor cannot go over 100
    [HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillRange))]
    public static class Skills_GetRandomSkillRange_Patch
    {
        public static bool Prefix(Skills __instance, out float min, out float max, SkillType skillType)
        {
            // Unclamped: the factor is above 1 when an EpicLoot skill bonus takes a skill past 100, and a
            // clamped Lerp threw that part away, so +skill added no damage at trained 100.
            var skillValue = Mathf.LerpUnclamped(0.4f, 1.0f, __instance.GetSkillFactor(skillType));
            min = Mathf.Max(0, skillValue - 0.15f);
            max = skillValue + 0.15f;
            return false;
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.GetRandomSkillFactor))]
    public static class Skills_GetRandomSkillFactor_Patch
    {
        // ReSharper disable once RedundantAssignment
        public static bool Prefix(Skills __instance, ref float __result, SkillType skillType)
        {
            __instance.GetRandomSkillRange(out var low, out var high, skillType);
            __result = Mathf.Lerp(low, high, Random.value);
            return false;
        }
    }

    [HarmonyPatch(typeof(SkillsDialog), nameof(SkillsDialog.Setup))]
    public static class DisplayExtraSkillLevels_SkillsDialog_Setup_Patch
    {
        [UsedImplicitly]
        private static void Postfix(SkillsDialog __instance, Player player)
        {
            var allSkills = player.m_skills.GetSkillList();
            var elementList = new List<GameObject>();
            if (EpicLoot.HasAuga)
            {
                var inventoryGuiRoot = __instance.gameObject.GetComponentInParent<InventoryGui>();

                if (inventoryGuiRoot == null)
                    return;

                var skillContainer = Utils.FindChild(inventoryGuiRoot.transform, "SkillElementsContainer");

                if (skillContainer == null)
                    return;
                
                for (int i = 0; i < skillContainer.childCount; i++)
                    elementList.Add(skillContainer.GetChild(i).gameObject);
            }
            else
            {
                elementList = __instance.m_elements;
            }
            
            foreach (var element in elementList)
            {
                var tooltipComponent = element.GetComponentInChildren<UITooltip>();
                
                if (EpicLoot.HasAuga)
                    tooltipComponent.m_topic = string.Empty;
                
                var skill = allSkills.Find(s => s.m_info.m_description == tooltipComponent.m_text);
                
                if (skill == null)
                    continue;
                
                var extraSkill = AddSkillLevel_Skills_GetSkillFactor_Patch.SkillIncrease(player, skill.m_info.m_skill);

                if (extraSkill > 0)
                { 
                    var levelbar = Utils.FindChild(element.transform, "bar");
                    
                    if (EpicLoot.HasAuga) 
                        levelbar = Utils.FindChild(element.transform, "ProgressBarLevel");
                    
                    var extraLevelbar = Utils.FindChild(element.transform, "extrabar")?.gameObject;
                    
                    if (extraLevelbar == null)
                    {
                        extraLevelbar = Object.Instantiate(levelbar.gameObject, levelbar.parent);
                        extraLevelbar.transform.SetSiblingIndex(levelbar.GetSiblingIndex());
                        extraLevelbar.name = "extrabar";
                    }
                    
                    extraLevelbar.SetActive(true);
                    
                    if (EpicLoot.HasAuga)
                    {
                        var fillBarImage = extraLevelbar.GetComponent<Image>();
                        fillBarImage.color = EpicLoot.GetRarityColorARGB(ItemRarity.Magic);
                        fillBarImage.fillAmount = Mathf.Lerp(0.0f, 0.75f, (skill.m_level  + extraSkill) / 100f);
                    }
                    else
                    {
                        var rect = extraLevelbar.GetComponent<RectTransform>();
                        rect.sizeDelta = new Vector2((skill.m_level + extraSkill) * 1.6f, rect.sizeDelta.y);
                        extraLevelbar.GetComponent<Image>().color = EpicLoot.GetRarityColorARGB(ItemRarity.Magic);
                    }

                    var levelText = Utils.FindChild(element.transform, "leveltext");

                    if (EpicLoot.HasAuga)
                    {
                        levelText = Utils.FindChild(element.transform, "SkillLevel");
                        tooltipComponent.m_topic = $" <color={EpicLoot.GetRarityColor(ItemRarity.Magic)}>+{extraSkill}</color>";
                        levelText.GetComponent<Text>().text += tooltipComponent.m_topic;
                    }
                    else
                    {
                        levelText.GetComponent<TMP_Text>().text += $" <color={EpicLoot.GetRarityColor(ItemRarity.Magic)}>+{extraSkill}</color>";    
                    }
                }
                else
                {
                    var extralevelbar = Utils.FindChild(element.transform, "extrabar");
                    if (extralevelbar != null)
                        extralevelbar.gameObject.SetActive(false);
                }
            }
        }
    }
}
 
 