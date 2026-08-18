using EpicLoot.MagicItemEffects.Shards;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // HUD indicator for the Eikthyr shard's ShockingCharge counter. It carries no gameplay of its own --
    // it reads the live charge count straight from ShockingCharge so the small icon text (e.g. "3/5")
    // updates every frame (Hud.UpdateStatusEffects re-queries GetIconText each render). The prototype is
    // built in ShockingCharge.GetOrCreateIndicator (icon = Eikthyr trophy, m_ttl = 0 so it never expires
    // on its own); removal is driven entirely by IsDone below.
    public class SE_ShockingChargeIndicator : StatusEffect
    {
        public override string GetIconText()
        {
            int charges = EikthyrShockingCharge.CurrentCharges;
            return charges > 0 ? $"{charges}/{EikthyrShockingCharge.MaxChargeCount}" : "";
        }

        // Self-remove once fully discharged (charges reset to 0) or the shard is no longer equipped. The
        // unequip check covers dropping the effect without hitting anything again, which is the only path
        // that wouldn't otherwise zero the counter.
        public override bool IsDone()
        {
            return EikthyrShockingCharge.CurrentCharges <= 0 ||
                Player.m_localPlayer == null ||
                !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ShockingCharge);
        }
    }
}
