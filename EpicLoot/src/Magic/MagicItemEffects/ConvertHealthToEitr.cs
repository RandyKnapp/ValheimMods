using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string ConvertHealthToEitr = nameof(ConvertHealthToEitr);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // ConvertHealthToEitr (2-piece Mimir's Well set bonus, NoRoll). value% of the finished max health pool is taken
    // away and added to max eitr, one for one. Unlike HeartyEitr it is a real trade, and it works with no eitr food at
    // all. Called from IncreasePlayerBaseStats.PostfixPercentOfPool right after PercentHealth, so it converts the final
    // health pool and the percent-of-eitr effects that follow (PercentEitr, DeepWell) scale what it adds.
    //
    // No GetBaseFoodHP patch: the food bar's base segment cannot show a cut to every food at once, and the health bar
    // itself already shows the real maximum.
    public static class ConvertHealthToEitr
    {
        private const float MaxFraction = 0.9f;        // stacked sources never take the whole pool
        private const float MinResultingMaxHealth = 1f;

        public static void Apply(Player player, ref float hp, ref float eitr)
        {
            if (player != Player.m_localPlayer)
            {
                return;
            }

            float fraction = Mathf.Min(player.GetTotalActiveMagicEffectValue(MagicEffectType.ConvertHealthToEitr, 0.01f),
                MaxFraction);
            if (fraction <= 0f)
            {
                return;
            }

            float moved = Mathf.Min(hp * fraction, hp - MinResultingMaxHealth);
            if (moved <= 0f)
            {
                return;
            }

            hp -= moved;
            eitr += moved;
        }
    }
}
