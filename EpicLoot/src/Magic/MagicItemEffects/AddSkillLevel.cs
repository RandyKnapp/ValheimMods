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

        // Runs from every GetSkillFactor (the HUD reads one per frame while a bow is drawn, and the Interconnected and
        // skill-as-skill effects read more inside it), so it allocates nothing: a switch for the one-skill effects and
        // the shards' own static arrays for the rest.
        public static int SkillIncrease(Player player, SkillType skillType)
        {
            int increase = 0;

            string single = null;
            switch (skillType)
            {
                case SkillType.Swords: single = MagicEffectType.AddSwordsSkill; break;
                case SkillType.Knives: single = MagicEffectType.AddKnivesSkill; break;
                case SkillType.Clubs: single = MagicEffectType.AddClubsSkill; break;
                case SkillType.Polearms: single = MagicEffectType.AddPolearmsSkill; break;
                case SkillType.Spears: single = MagicEffectType.AddSpearsSkill; break;
                case SkillType.Blocking: single = MagicEffectType.AddBlockingSkill; break;
                case SkillType.Axes:
                case SkillType.WoodCutting: single = MagicEffectType.AddAxesSkill; break;
                case SkillType.Bows: single = MagicEffectType.AddBowsSkill; break;
                case SkillType.Crossbows: single = MagicEffectType.AddCrossbowsSkill; break;
                case SkillType.Unarmed: single = MagicEffectType.AddUnarmedSkill; break;
                case SkillType.Pickaxes: single = MagicEffectType.AddPickaxesSkill; break;
                case SkillType.Fishing: single = MagicEffectType.AddFishingSkill; break;
                case SkillType.ElementalMagic: single = MagicEffectType.AddElementalMagicSkill; break;
                case SkillType.BloodMagic: single = MagicEffectType.AddBloodMagicSkill; break;
                case SkillType.Run:
                case SkillType.Jump:
                case SkillType.Swim:
                case SkillType.Sneak: single = MagicEffectType.AddMovementSkills; break;
                case SkillType.Crafting:
                case SkillType.Cooking: single = MagicEffectType.AddCrafterSkills; break;
            }
            if (single != null)
            {
                increase += (int) player.GetTotalActiveMagicEffectValue(single);
            }

            if (Contains(Shards.IncreaseMeleeSkills.MeleeSkills, skillType))
            {
                increase += (int) player.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseMeleeSkills);
            }
            if (Contains(src.Magic.MagicItemEffects.Shards.IncreaseRangedSkills.RangedSkills, skillType))
            {
                increase += (int) player.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseRangedSkills);
            }

            increase += SkillsAsSkills(player, skillType, MagicEffectType.BlockAsDodgeAsBlock,
                Shards.BlockAsDodgeAsBlock.type, Shards.BlockAsDodgeAsBlock.asType);
            increase += SkillsAsSkills(player, skillType, MagicEffectType.BlockAsWoodCuttingAndPickaxes,
                Shards.BlockAsWoodCuttingAndPickaxes.type, Shards.BlockAsWoodCuttingAndPickaxes.asType);

            if (skillType != SkillType.None && skillType != SkillType.All)
            {
                increase += (int) player.GetTotalActiveMagicEffectValue(MagicEffectType.AddAllSkills);
            }
            increase += Interconnected.GetBonusLevels(player, skillType);

            return increase;
        }

        private static bool Contains(SkillType[] types, SkillType skillType)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i] == skillType)
                {
                    return true;
                }
            }
            return false;
        }

        private static int SkillsAsSkills(Player player, SkillType skillType, string effect, SkillType[] type, SkillType[] asType)
        {
            if (_inSkillsAsSkills || !Contains(type, skillType))
            {
                return 0;
            }

            int increase = 0;
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
                if (tooltipComponent == null)
                    continue;
                
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

                    if (levelbar == null)
                        continue;
                    
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
                        // Auga's skill element (SkillsPanelSkillController) rewrites its level text every second
                        // and then calls SkillsDialog.Setup, so this is appended again after each rewrite.
                        levelText = Utils.FindChild(element.transform, "SkillLevel");
                        tooltipComponent.m_topic = $" <color={EpicLoot.GetRarityColor(ItemRarity.Magic)}>+{extraSkill}</color>";
                        var augaLevelText = levelText != null ? levelText.GetComponent<TMP_Text>() : null;
                        if (augaLevelText != null)
                            augaLevelText.text += tooltipComponent.m_topic;
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
 
 