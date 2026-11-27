using System.Text;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // QueenEverflow stamps Stacks and RegenPerStack on the live instance and refreshes m_ttl on each kill.
    public class SE_QueenEverflow : StatusEffect
    {
        public int Stacks = 1;
        public float RegenPerStack;

        // Added straight to the base multiplier of 1 the Player seeds each tick, rather than vanilla's
        // (multiplier - 1) convention in SE_Stats.
        private float TotalRegenBonus => RegenPerStack * Stacks;

        public override void ModifyHealthRegen(ref float regenMultiplier) => regenMultiplier += TotalRegenBonus;
        public override void ModifyStaminaRegen(ref float staminaRegen) => staminaRegen += TotalRegenBonus;
        public override void ModifyEitrRegen(ref float eitrRegen) => eitrRegen += TotalRegenBonus;

        public override string GetIconText()
        {
            var time = base.GetIconText();
            var stacks = $"×{Stacks}";
            return string.IsNullOrEmpty(time) ? stacks : $"{time}\n{stacks}";
        }

        public override string GetTooltipString()
        {
            var percent = Mathf.RoundToInt(TotalRegenBonus * 100f).ToString();
            var sb = new StringBuilder();
            sb.AppendFormat("$se_healthregen: <color=orange>+{0}%</color>\n", percent);
            sb.AppendFormat("$se_staminaregen: <color=orange>+{0}%</color>\n", percent);
            sb.AppendFormat("$se_eitrregen: <color=orange>+{0}%</color>\n", percent);
            return sb.ToString();
        }
    }
}
