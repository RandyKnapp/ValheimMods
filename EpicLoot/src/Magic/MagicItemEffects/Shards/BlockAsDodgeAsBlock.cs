using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Skills;

namespace EpicLoot.MagicItemEffects.Shards 
        {
    // uses SkillsAsSkills in AddSkillLevel
    public static class BlockAsDodgeAsBlock 
    {
        public static readonly SkillType[] type = // modify these
        {
            SkillType.Blocking,
            SkillType.Dodge
        };

        public static readonly SkillType[] asType = // from these
        {
            SkillType.Blocking,
            SkillType.Dodge
        };

        public static void RegisterDisplayValues() {
            // Always three float args: the display text uses {1} and {2}, so a shorter array (or int
            // boxes, which the range/generic formatters don't treat as values) would make string.Format
            // throw in menu/compendium contexts where there is no local player.
            MagicItem.RegisterDisplayValues(MagicEffectType.BlockAsDodgeAsBlock,
                value => {
                    var player = Player.m_localPlayer;
                    if (player == null) return new object[] { value, 0f, 0f };
                    var blockSkill = player.m_skills.GetSkillFactor(SkillType.Blocking);
                    var dodgeSkill = player.m_skills.GetSkillFactor(SkillType.Dodge);
                    var blockBonus = (float)(int)(dodgeSkill * value);
                    var dodgeBonus = (float)(int)(blockSkill * value);
                    return new object[] { value, blockBonus, dodgeBonus };
                });
        }
    }
}
