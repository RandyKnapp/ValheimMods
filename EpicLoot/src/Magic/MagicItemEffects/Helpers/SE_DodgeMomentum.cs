using EpicLoot.MagicItemEffects.Shards;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // "Dodge Momentum" -- the stacking stamina-regen buff a perfect dodge grants (see PerfectDodge). Each
    // stack adds RegenPerStack (a fraction, e.g. 0.08 at Epic) to the player's stamina regen multiplier;
    // PerfectDodge stamps the value on the live instance, so the buff always reflects the rarity of the shard
    // that granted it and a re-proc mid-buff can raise or lower it.
    //
    // ModifyStaminaRegen is re-queried every frame (SEMan.ModifyStaminaRegen), so the bonus always tracks the
    // live stack count. Vanilla adds (multiplier - 1) when a multiplier is > 1 (see SE_Stats); here the
    // accumulated per-stack bonus is added directly to the base multiplier of 1 the Player seeds each tick --
    // the same approach SE_QueenEverflow takes.
    //
    // PerfectDodge owns the lifetime: it stamps Stacks/MaxStacks/RegenPerStack on the live instance and
    // refreshes m_ttl on each perfect dodge, and the buff self-expires via m_ttl like any timed status effect.
    public class SE_DodgeMomentum : StatusEffect
    {
        public int Stacks = 1;
        public int MaxStacks = PerfectDodge.DefaultMaxStacks;
        public float RegenPerStack; // fraction added to the stamina regen multiplier per stack (0.08 at Epic)

        private float TotalRegenBonus => RegenPerStack * Stacks;

        public override void ModifyStaminaRegen(ref float staminaRegen) => staminaRegen += TotalRegenBonus;

        // Standard remaining-duration string with the current stack count appended (e.g. "0:07\n×3"). The
        // HUD re-queries this every render, so the countdown and stack count stay live.
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
