using EpicLoot.MagicItemEffects.Helpers;
using HarmonyLib;

namespace EpicLoot.src.Magic.MagicItemEffects.Shards {
    // Provides a bonus to XP gain from blocking based on the time of day (day or night)
    public static class IncreasedXPGainFromBlockDayNight {
        [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
        public class DayNightBlockXP_Patch {
            [HarmonyPrefix]
            public static void GainDayNightXPOnBlock(Skills.SkillType skillType, ref float factor) {
                // Inspiration grants an exact number of accumulator points; multiplying them would
                // overshoot each level boundary and vanilla would discard the excess. See SkillXpGrant.
                if (SkillXpGrant.InProgress) {
                    return;
                }

                // not exactly a fan guarding by SkillType.Blocking as its not on block but just at night all blocking skill increased + X%
                // will change to on block only if future interactions appear

                if (skillType == Skills.SkillType.Blocking && EnvMan.IsDay()) {
                    var dayEffectBonus = 1f + Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.DayBlocker, .01f);
                    factor *= dayEffectBonus;
                }
                if (skillType == Skills.SkillType.Blocking && EnvMan.IsNight()) {
                    var nightEffectBonus = 1f + Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.NightBlocker, .01f);
                    factor *= nightEffectBonus;
                }
            }
        }
    }
}
