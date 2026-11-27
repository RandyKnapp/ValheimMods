using System;
using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using EpicLoot.MagicItemEffects.Shards;
using UnityEngine;
using static EpicLoot.Compendium.CompendiumFormat;
using SkillType = Skills.SkillType;

namespace EpicLoot.Compendium;

public static class SkillsTextSection
{
    private static readonly string[] XpExtras =
    {
        MagicEffectType.Inspiration, MagicEffectType.IncreaseHarvestXPGain, MagicEffectType.IncreaseXPGainFromMovementPenalty,
    };

    public static void Build(MagicPages instance)
    {
        Player player = Player.m_localPlayer;
        if (!player || player.m_skills == null)
        {
            return;
        }

        instance.MagicPagesTextArea.Add("");
        instance.MagicPagesTextArea.Add($"<size={MagicPages.LARGE_FONT_SIZE}>$mod_epicloot_skills</size>");
        MagicTextTable table = AddStatTable(instance, "$mod_epicloot_skills_col_skill");

        foreach (Skills.SkillDef def in player.m_skills.m_skills)
        {
            SkillType type = def.m_skill;
            if (type == SkillType.None || type == SkillType.All)
            {
                continue;
            }

            float baseLevel = player.m_skills.m_skillData.TryGetValue(type, out Skills.Skill skill) ? skill.m_level : 0f;
            int bonus = AddSkillLevel_Skills_GetSkillFactor_Patch.SkillIncrease(player, type);
            if (baseLevel <= 0f && bonus == 0)
            {
                continue;
            }

            List<string> detail = new();
            foreach (string effectType in EffectsFor(type))
            {
                detail.AddRange(StatSourceLines.GetMagicSources(player, effectType));
            }

            AddRow(table, $"$skill_{type.ToString().ToLower()}", Whole(baseLevel), bonus, Whole(baseLevel + bonus), detail,
                x => Signed(x));
        }

        float xpBonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.QuickLearner, 0.01f);
        List<string> xpDetail = new(StatSourceLines.GetMagicSources(player, MagicEffectType.QuickLearner));
        foreach (string effectType in XpExtras)
        {
            xpDetail.AddRange(StatSourceLines.GetMagicSources(player, effectType));
        }

        if (xpDetail.Count > 0)
        {
            AddRow(table, "$mod_epicloot_skills_xpgain", Percent(1f), xpBonus, Percent(1f + xpBonus), xpDetail, SignedPercent);
        }

        table.Finish();
    }

    private static IEnumerable<string> EffectsFor(SkillType type)
    {
        string single = null;
        switch (type)
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
            yield return single;
        }

        if (Array.IndexOf(IncreaseMeleeSkills.MeleeSkills, type) >= 0)
        {
            yield return MagicEffectType.IncreaseMeleeSkills;
        }

        if (Array.IndexOf(global::EpicLoot.src.Magic.MagicItemEffects.Shards.IncreaseRangedSkills.RangedSkills, type) >= 0)
        {
            yield return MagicEffectType.IncreaseRangedSkills;
        }

        if (Array.IndexOf(BlockAsDodgeAsBlock.type, type) >= 0)
        {
            yield return MagicEffectType.BlockAsDodgeAsBlock;
        }

        if (Array.IndexOf(BlockAsWoodCuttingAndPickaxes.type, type) >= 0)
        {
            yield return MagicEffectType.BlockAsWoodCuttingAndPickaxes;
        }

        yield return MagicEffectType.AddAllSkills;
        yield return MagicEffectType.Interconnected;
    }

    private static string Whole(float value) => Mathf.FloorToInt(value).ToString();
}
