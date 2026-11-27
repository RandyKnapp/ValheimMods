using SkillType = Skills.SkillType;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Interconnected = nameof(Interconnected);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Added to AddSkillLevel's SkillIncrease, so it raises the skill factor, the gameplay skill level and the skills
    // panel's bonus like any other skill effect. Only the held weapon's skill gains anything, so the bonus shows on
    // that one row and moves when the weapon is swapped. A partner is read through its full skill factor, gear
    // bonuses included; it is never the held skill, so the read cannot feed back into itself.
    public static class Interconnected
    {
        private static readonly SkillType[] BowsPartners = { SkillType.Axes, SkillType.Polearms };
        private static readonly SkillType[] BloodMagicPartners = { SkillType.Clubs, SkillType.Unarmed };
        private static readonly SkillType[] ElementalMagicPartners = { SkillType.Swords, SkillType.Spears };
        private static readonly SkillType[] BowsOnly = { SkillType.Bows };
        private static readonly SkillType[] BloodMagicOnly = { SkillType.BloodMagic };
        private static readonly SkillType[] ElementalMagicOnly = { SkillType.ElementalMagic };
        private static readonly SkillType[] CrossbowsOnly = { SkillType.Crossbows };
        private static readonly SkillType[] KnivesOnly = { SkillType.Knives };

        private static bool _computing;

        // A switch rather than a dictionary: an enum key boxes on Mono, and this is read on every skill factor.
        public static SkillType[] GetPartners(SkillType skill)
        {
            switch (skill)
            {
                case SkillType.Bows: return BowsPartners;
                case SkillType.Axes:
                case SkillType.Polearms: return BowsOnly;
                case SkillType.BloodMagic: return BloodMagicPartners;
                case SkillType.Clubs:
                case SkillType.Unarmed: return BloodMagicOnly;
                case SkillType.ElementalMagic: return ElementalMagicPartners;
                case SkillType.Swords:
                case SkillType.Spears: return ElementalMagicOnly;
                case SkillType.Crossbows: return KnivesOnly;
                case SkillType.Knives: return CrossbowsOnly;
                default: return null;
            }
        }

        public static int GetBonusLevels(Player player, SkillType skillType)
        {
            var partners = GetPartners(skillType);
            if (partners == null || player == null || _computing)
            {
                return 0;
            }

            float percent = player.GetTotalActiveMagicEffectValue(MagicEffectType.Interconnected);
            if (percent <= 0f || HeldSkill(player) != skillType)
            {
                return 0;
            }

            _computing = true;
            try
            {
                float best = 0f;
                foreach (var partner in partners)
                {
                    float factor = player.m_skills.GetSkillFactor(partner);
                    if (factor > best)
                    {
                        best = factor;
                    }
                }

                // The factor is the level over 100, so factor x percent is percent% of the level.
                return (int)(best * percent);
            }
            finally
            {
                _computing = false;
            }
        }

        // Empty hands are the unarmed weapon (Unarmed); a bow or Dead Raiser in the left hand counts too. Inside a
        // Heiðr bow read the bow is Elemental Magic, as ElementalArchery has already turned the read itself.
        private static SkillType HeldSkill(Player player)
        {
            var weapon = player.GetCurrentWeapon();
            if (weapon == null)
            {
                return SkillType.None;
            }

            var skill = weapon.m_shared.m_skillType;
            if (skill == SkillType.Bows && ElementalArchery.IsRemapping)
            {
                return SkillType.ElementalMagic;
            }
            return skill;
        }
    }
}
