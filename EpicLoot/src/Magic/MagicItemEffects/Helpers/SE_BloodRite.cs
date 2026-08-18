using EpicLoot.MagicItemEffects.Shards;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // "Blood Rite" -- the HUD buff for the DarkPurple head shard's banked blood discount (see
    // KillsReduceNextBloodCost). It carries no gameplay of its own: the discount itself is applied by that
    // effect's UseHealth patch. This reads the live bank straight from it so the icon text (e.g. "0:24\n-8%")
    // updates every frame, since Hud.UpdateStatusEffects re-queries GetIconText each render.
    //
    // The prototype is built in KillsReduceNextBloodCost.GetOrCreatePrototype (icon = DarkPurple shardstone,
    // m_ttl = BuffDuration); each kill refreshes the countdown. Removal is driven by IsDone below, and Stop
    // forfeits whatever is still banked so the icon and the discount can never disagree.
    public class SE_BloodRite : StatusEffect
    {
        private static string DiscountText =>
            $"-{Mathf.RoundToInt(KillsReduceNextBloodCost.BankedReduction * 100f)}%";

        // Standard remaining-duration string with the banked discount appended (e.g. "0:24\n-8%").
        public override string GetIconText()
        {
            var time = base.GetIconText();
            return string.IsNullOrEmpty(time) ? DiscountText : $"{time}\n{DiscountText}";
        }

        public override string GetTooltipString()
        {
            return $"$mod_epicloot_se_bloodrite_discount: <color=orange>{DiscountText}</color>";
        }

        // Self-remove once the discount has been spent (bank back to 0), once the shard is no longer
        // equipped, or when the standard TTL runs out (base.IsDone).
        public override bool IsDone()
        {
            return KillsReduceNextBloodCost.BankedReduction <= 0f ||
                Player.m_localPlayer == null ||
                !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.KillsReduceNextBloodCost) ||
                base.IsDone();
        }

        // Any removal path -- expiry, unequipping the shard, death, a status-effect wipe -- forfeits the
        // banked discount, so the bank never outlives its icon. A no-op on the spend path (already 0).
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
