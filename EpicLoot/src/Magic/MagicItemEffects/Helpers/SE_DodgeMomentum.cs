using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // PerfectDodge stamps Stacks and RegenPerStack on the live instance and refreshes m_ttl on each perfect dodge.
    // The bonus is added straight to the base multiplier of 1 the Player seeds each tick, as SE_QueenEverflow does.
    public class SE_DodgeMomentum : StatusEffect
    {
        public int Stacks = 1;
        public float RegenPerStack;

        private float TotalRegenBonus => RegenPerStack * Stacks;

        public override void ModifyStaminaRegen(ref float staminaRegen) => staminaRegen += TotalRegenBonus;

        public override string GetIconText()
        {
            var time = base.GetIconText();
            var stacks = $"×{Stacks}";
            return string.IsNullOrEmpty(time) ? stacks : $"{time}\n{stacks}";
        }

        // Returned with the label left as a token (the callers localize), matching SE_QueenEverflow.
        public override string GetTooltipString()
        {
            return $"$se_staminaregen: <color=orange>+{Mathf.RoundToInt(TotalRegenBonus * 100f)}%</color>";
        }
    }
}
