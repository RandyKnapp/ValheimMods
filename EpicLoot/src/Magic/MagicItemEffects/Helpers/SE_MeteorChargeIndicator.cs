using EpicLoot.MagicItemEffects.Shards;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // HUD readout of MeteorSummoner's charges, e.g. "3/25". Hud.UpdateStatusEffects re-queries GetIconText each
    // render. The prototype has m_ttl 0, so IsDone is the only thing that removes it.
    public class SE_MeteorChargeIndicator : StatusEffect
    {
        public override string GetIconText()
        {
            int charges = MeteorSummoner.CurrentCharges;
            return charges > 0 ? $"{charges}/{MeteorSummoner.MaxChargeCount}" : "";
        }

        // The unequip check covers dropping the effect without hitting anything again, the one path that would
        // not otherwise zero the counter.
        public override bool IsDone()
        {
            return MeteorSummoner.CurrentCharges <= 0 ||
                Player.m_localPlayer == null ||
                !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.MeteorSummoner);
        }
    }
}
