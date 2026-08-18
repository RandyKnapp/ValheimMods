using SkillType = Skills.SkillType;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to health regeneration based on the player's Blood Magic skill level.
    public static class BloodMagicLevelIncreasesHealthRegen {
        public static void Apply(SEMan seman, ref float regenMultiplier) {
            if (seman.m_character != Player.m_localPlayer) {
                return;
            }

            var perFullSkill = Player.m_localPlayer.GetTotalActiveMagicEffectValue(
                MagicEffectType.BloodMagicLevelIncreasesHealthRegen, 0.01f);
            if (perFullSkill <= 0f) {
                return;
            }

            var skills = Player.m_localPlayer.GetSkills();
            var skillFactor = skills != null ? skills.GetSkillFactor(SkillType.BloodMagic) : 0f;
            regenMultiplier += perFullSkill * skillFactor;
        }
    }
}
