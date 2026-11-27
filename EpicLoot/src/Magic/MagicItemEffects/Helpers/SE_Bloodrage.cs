using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // Bloodrage stamps Stacks and DamagePerStack on the live instance and refreshes m_ttl on each hit taken.
    //
    // Deliberately skill-agnostic -- vanilla SE_Stats gates m_damageModifier behind m_modifyAttackSkill,
    // but Bloodrage is meant to be a flat rage regardless of what you are swinging. SEMan.ModifyAttack is
    // called from Attack.DoMeleeAttack, DoAreaAttack and FireProjectileBurst, so this covers melee, AoE,
    // bows and staves alike.
    //
    // Known quirk: HitData.DamageTypes.Modify(float) also scales m_chop and m_pickaxe, so raging speeds
    // up tree-felling and mining slightly. Vanilla's own SE_Stats.ModifyAttack behaves identically, so it is
    // accepted rather than worked around with a per-type multiplier.
    public class SE_Bloodrage : StatusEffect
    {
        public int Stacks = 1;
        public float DamagePerStack;

        private float TotalBonus => DamagePerStack * Stacks;

        public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
        {
            hitData.m_damage.Modify(1f + TotalBonus);
        }

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
