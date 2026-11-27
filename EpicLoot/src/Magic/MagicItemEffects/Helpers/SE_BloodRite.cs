using EpicLoot.MagicItemEffects.Shards;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // HUD readout of KillsReduceNextBloodCost's banked discount; the discount itself is applied by that effect's
    // UseHealth patch. Stop forfeits whatever is still banked, so the icon and the discount never disagree.
    public class SE_BloodRite : StatusEffect
    {
        private static string DiscountText =>
            $"-{Mathf.RoundToInt(KillsReduceNextBloodCost.BankedReduction * 100f)}%";

        public override string GetIconText()
        {
            var time = base.GetIconText();
            return string.IsNullOrEmpty(time) ? DiscountText : $"{time}\n{DiscountText}";
        }

        public override string GetTooltipString()
        {
            return $"$mod_epicloot_se_bloodrite_discount: <color=orange>{DiscountText}</color>";
        }

        public override bool IsDone()
        {
            return KillsReduceNextBloodCost.BankedReduction <= 0f ||
                Player.m_localPlayer == null ||
                !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.KillsReduceNextBloodCost) ||
                base.IsDone();
        }

        // Any removal path -- expiry, unequipping the shard, death, a status-effect wipe -- forfeits the bank.
        // Guarded on the local player because the bank is local-player state.
        public override void Stop()
        {
            base.Stop();
            if (m_character == Player.m_localPlayer)
            {
                KillsReduceNextBloodCost.ClearBank();
            }
        }
    }
}
