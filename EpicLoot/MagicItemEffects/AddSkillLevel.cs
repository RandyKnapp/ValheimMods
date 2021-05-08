using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using SkillType = Skills.SkillType;

namespace EpicLoot.MagicItemEffects
{
	[HarmonyPatch(typeof(Skills), "GetSkillFactor")]
	public class AddSkillLevel_Skills_GetSkillFactor_Patch
	{
		[UsedImplicitly]
		private static void Postfix(Skills __instance, SkillType skillType, ref float __result) => __result = Math.Min(1, __result + skillIncrease(__instance.m_player, skillType) / 100f);

		public static int skillIncrease(Player player, SkillType skillType)
		{
			int increase = 0;
			
			void check(string effect, params SkillType[] type)
            {
            	if (type.Contains(skillType))
            	{
            		increase += (int) player.GetMagicEquipmentWithEffect(effect).Sum(item => item.GetMagicItem().GetTotalEffectValue(effect));
            	}
            }

            check(MagicEffectType.AddSwordsSkill, SkillType.Swords);
            check(MagicEffectType.AddKnivesSkill, SkillType.Knives);
            check(MagicEffectType.AddClubsSkill, SkillType.Clubs);
            check(MagicEffectType.AddPolearmsSkill, SkillType.Polearms);
            check(MagicEffectType.AddSpearsSkill, SkillType.Spears);
            check(MagicEffectType.AddBlockingSkill, SkillType.Blocking);
            check(MagicEffectType.AddAxesSkill, SkillType.Axes);
            check(MagicEffectType.AddBowsSkill, SkillType.Bows);
            check(MagicEffectType.AddUnarmedSkill, SkillType.Unarmed);
            check(MagicEffectType.AddPickaxesSkill, SkillType.Pickaxes);
            check(MagicEffectType.AddMovementSkills, SkillType.Run, SkillType.Jump, SkillType.Swim, SkillType.Sneak);

            return increase;
		}
	}

	[HarmonyPatch(typeof(SkillsDialog), "Setup")]
	class DisplayExtraSkillLevels_SkillsDialog_Setup_Patch
	{
		[UsedImplicitly]
		private static void Postfix(SkillsDialog __instance, Player player)
		{
			List<Skills.Skill> allSkills = player.m_skills.GetSkillList();
			foreach (var element in __instance.m_elements)
			{
				Skills.Skill skill = allSkills.Find(s => s.m_info.m_description == element.GetComponentInChildren<UITooltip>().m_text);
				int extraSkill = AddSkillLevel_Skills_GetSkillFactor_Patch.skillIncrease(player, skill.m_info.m_skill);
				extraSkill = Math.Min(extraSkill, 100 - (int) skill.m_level);
				if (extraSkill > 0)
				{
					Transform levelbar = Utils.FindChild(element.transform, "bar");
					GameObject extraLevelbar = Object.Instantiate(levelbar.gameObject, levelbar.parent);
					RectTransform rect = extraLevelbar.GetComponent<RectTransform>();
					rect.sizeDelta = new Vector2(Math.Min(160f, (skill.m_level + extraSkill) * 1.6f), rect.sizeDelta.y);
					extraLevelbar.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.9f);
					extraLevelbar.transform.SetSiblingIndex(levelbar.GetSiblingIndex());
					Transform levelText = Utils.FindChild(element.transform, "leveltext");
					levelText.GetComponent<Text>().text += " +" + extraSkill;
				}
			}
		}
	}
}