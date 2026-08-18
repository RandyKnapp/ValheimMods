using EpicLoot.MagicItemEffects.Shards;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // "Bloodrage" -- the stacking damage buff taking a hit grants (see Bloodrage). Each stack adds
    // DamagePerStack (a fraction, e.g. 0.05 at Mythic) to every attack the player lands; Bloodrage stamps
    // the value on the live instance, so the buff always reflects the rarity of the shard that granted it
    // and a re-proc mid-buff can raise or lower it.
    //
    // ModifyAttack is re-queried per attack (SEMan.ModifyAttack), so the bonus always tracks the live
    // stack count. Bloodrage owns the lifetime: it stamps Stacks/MaxStacks/DamagePerStack and refreshes
    // m_ttl on each hit taken, and the buff self-expires via m_ttl like any timed status effect.
    //
    // Deliberately skill-agnostic -- vanilla SE_Stats gates m_damageModifier behind m_modifyAttackSkill,
    // but Bloodrage is meant to be a flat rage regardless of what you are swinging. SEMan.ModifyAttack is
    // called from Attack.DoMeleeAttack, DoAreaAttack and FireProjectileBurst, so this covers melee, AoE,
    // bows and staves alike.
    //
    // Known quirk: HitData.DamageTypes.Modify(float) also scales m_chop and m_pickaxe, so raging speeds
    // up tree-felling and mining slightly. Vanilla's own SE_Stats.ModifyAttack behaves identically and the
    // ceiling here is +25%, so it is accepted rather than worked around with a per-type multiplier.
    public class SE_Bloodrage : StatusEffect
    {
        public int Stacks = 1;
        public int MaxStacks = Bloodrage.DefaultMaxStacks;
        public float DamagePerStack; // fraction added to outgoing damage per stack (0.05 at Mythic)

        private float TotalBonus => DamagePerStack * Stacks;

        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            hitData.m_damage.Modify(1f + TotalBonus);
        }

        // Standard remaining-duration string with the current stack count appended (e.g. "0:07\n×3").
        // The HUD re-queries this every render, so the countdown and stack count stay live.
        public override string GetIconText()
        {
            var time = base.GetIconText();
            var stacks = $"×{Stacks}";
            return string.IsNullOrEmpty(time) ? stacks : $"{time}\n{stacks}";
        }

        // Returned with the label left as a token (the callers localize), matching SE_DodgeMomentum.
        public override string GetTooltipString()
        {
            return $"$inventory_damage: <color=orange>+{Mathf.RoundToInt(TotalBonus * 100f)}%</color>";
        }
    }
}
