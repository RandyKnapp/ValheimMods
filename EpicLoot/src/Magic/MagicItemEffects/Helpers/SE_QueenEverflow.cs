using EpicLoot.MagicItemEffects.Shards;
using System.Text;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // The stacking regen buff granted by the Queen boss shard (see QueenEverflow). Each stack adds
    // RegenPerStack (a fraction, e.g. 0.10 at Legendary) to the player's health, stamina AND eitr regen
    // multipliers; the three Modify*Regen overrides are re-queried every frame (SEMan.Modify*Regen), so the
    // bonus always tracks the live stack count and rarity. The HUD icon text shows the standard remaining-
    // duration string plus the current stack count.
    //
    // QueenEverflow owns the lifetime: it stamps Stacks/MaxStacks/RegenPerStack on the live instance and
    // refreshes m_ttl on each kill, and the buff self-expires via m_ttl like any timed status effect.
    public class SE_QueenEverflow : StatusEffect
    {
        public int Stacks = 1;
        public int MaxStacks = QueenEverflow.DefaultMaxStacks;
        public float RegenPerStack; // fraction added to each regen rate per stack (e.g. 0.10 at Legendary)

        // Vanilla adds (multiplier - 1) to the regen multiplier when the multiplier is > 1 (see
        // SE_Stats.ModifyHealthRegen); here we add the accumulated per-stack bonus directly to the base
        // multiplier of 1 that the Player seeds each tick, giving +TotalRegenBonus across all three pools.
        private float TotalRegenBonus => RegenPerStack * Stacks;

        public override void ModifyHealthRegen(ref float regenMultiplier) => regenMultiplier += TotalRegenBonus;
        public override void ModifyStaminaRegen(ref float staminaRegen) => staminaRegen += TotalRegenBonus;
        public override void ModifyEitrRegen(ref float eitrRegen) => eitrRegen += TotalRegenBonus;

        // Standard remaining-duration string with the current stack count appended (e.g. "0:27\n×3"). The
        // HUD re-queries this every render, so the countdown and stack count stay live.
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
