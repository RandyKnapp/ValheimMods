using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // SpeedBonus is stamped on the live instance by PerfectDodgeGivesSpeed. It is added as a fraction of baseSpeed,
    // as SE_Stats applies m_speedModifier, so it stacks additively with other speed effects instead of compounding.
    public class SE_DodgeAgility : StatusEffect
    {
        public float SpeedBonus;

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
        {
            speed += baseSpeed * SpeedBonus;
        }

        // Returned with the label left as a token (the callers localize), matching SE_QueenEverflow. Vanilla
        // has no $se_ token for movement speed -- SE_Stats never prints m_speedModifier -- so this uses a
        // mod-owned key.
        public override string GetTooltipString()
        {
            return $"$mod_epicloot_se_dodgeagility_speed: <color=orange>+{Mathf.RoundToInt(SpeedBonus * 100f)}%</color>";
        }
    }
}
