using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // "Dodge Agility" -- the brief movement-speed burst a perfect dodge grants (see PerfectDodgeGivesSpeed).
    // SpeedBonus is a fraction (e.g. 0.60 at Epic) stamped on the live instance by that effect, so the buff
    // always reflects the rarity of the shard that granted it, and a re-proc mid-buff can raise or lower it.
    //
    // ModifySpeed is the same hook vanilla's own speed effects use (SEMan.ApplyStatusEffectSpeedMods, called
    // every frame from the character's movement update), and the bonus is applied the way SE_Stats applies
    // m_speedModifier -- added as a fraction of baseSpeed rather than multiplied into the running total, so
    // it stacks additively with other speed effects instead of compounding with them.
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
