using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Skills;

namespace EpicLoot.MagicItemEffects.Shards {
    // uses SkillsAsSkills in AddSkillLevel
    public static class BlockAsWoodCuttingAndPickaxes {
        public static readonly SkillType[] type = // modify these
        {
            SkillType.Blocking
        };

        public static readonly SkillType[] asType = // from these
        {
            SkillType.WoodCutting,
            SkillType.Pickaxes
        };

        public static void RegisterDisplayValues() {


            MagicItem.RegisterDisplayValues(MagicEffectType.BlockAsWoodCuttingAndPickaxes,
                value => 
                {
                    var player = Player.m_localPlayer;
                    if (player == null) return new object[] { value, 0 };
                    var chopSkill = player.m_skills.GetSkillFactor(SkillType.WoodCutting);
                    var pickSkill = player.m_skills.GetSkillFactor(SkillType.Pickaxes);
                    var blockBonus = (int)((chopSkill + pickSkill) * value);
                    return new object[] { value, blockBonus};
                });
        }
    }
}
