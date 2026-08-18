using static Skills;

namespace EpicLoot.src.Magic.MagicItemEffects.Shards {
    // Provides a bonus to ranged skills (bows and crossbows).
    public static class IncreaseRangedSkills {
        public static readonly SkillType[] RangedSkills =
        {
            SkillType.Bows,
            SkillType.Crossbows,
        };
    }
}
