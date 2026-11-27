using EpicLoot.MagicItemEffects.Shards;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // HUD readout of Conduit's eitr pool, e.g. "63/70". It carries no gameplay of its own: Conduit keeps the pool and
    // adds this while the effect is worn, and Hud.UpdateStatusEffects re-queries the text each render.
    public class SE_ConduitPool : StatusEffect
    {
        public override string GetIconText()
        {
            return Conduit.GetPoolText();
        }

        public override bool IsDone()
        {
            return !Conduit.IsActive(Player.m_localPlayer);
        }
    }
}
