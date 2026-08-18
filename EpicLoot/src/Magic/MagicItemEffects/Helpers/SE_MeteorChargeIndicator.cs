using EpicLoot.MagicItemEffects.Shards;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // HUD indicator for the Yagluth shard's MeteorSummoner counter. It carries no gameplay of its own --
    // it reads the live charge count straight from MeteorSummoner so the small icon text (e.g. "3/25")
    // updates every frame (Hud.UpdateStatusEffects re-queries GetIconText each render). The prototype is
    // built in MeteorSummoner.GetOrCreateIndicator (icon = Yagluth trophy, m_ttl = 0 so it never expires
    // on its own); removal is driven entirely by IsDone below.
    public class SE_MeteorChargeIndicator : StatusEffect
    {
        public override string GetIconText()
        {
            int charges = MeteorSummoner.CurrentCharges;
            return charges > 0 ? $"{charges}/{MeteorSummoner.MaxChargeCount}" : "";
        }

        // Self-remove once the meteor has been spent (charges reset to 0) or the shard is no longer equipped.
        // The unequip check covers dropping the effect without hitting anything again, which is the only path
        // that wouldn't otherwise zero the counter.
        public override bool IsDone()
        {
            return MeteorSummoner.CurrentCharges <= 0 ||
                Player.m_localPlayer == null ||
                !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.MeteorSummoner);
        }
    }
}
