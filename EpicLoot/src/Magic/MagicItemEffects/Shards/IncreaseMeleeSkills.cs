using SkillType = Skills.SkillType;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to all offensive melee weapon skills (Swords, Knives, Clubs, Polearms, Spears, Axes, Unarmed)
    public static class IncreaseMeleeSkills {
        public static readonly SkillType[] MeleeSkills = {
            SkillType.Swords,
            SkillType.Knives,
            SkillType.Clubs,
            SkillType.Polearms,
            SkillType.Spears,
            SkillType.Axes,
            SkillType.Unarmed
        };
    }
}
