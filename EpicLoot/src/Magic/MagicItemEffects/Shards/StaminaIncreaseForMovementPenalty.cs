using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Called from IncreasePlayerBaseStats' GetTotalFoodValue postfix, so it feeds both the stamina pool and its HUD
    // bar; the percent is of the food-derived max stamina.
    public static class StaminaIncreaseForMovementPenalty
    {
        public static void Apply(Player player, ref float stamina)
        {
            if (player != Player.m_localPlayer)
            {
                return;
            }

            var pct = player.GetTotalActiveMagicEffectValue(
                MagicEffectType.StaminaIncreaseForMovementPenalty, 0.01f);
            if (pct == 0f)
            {
                return;
            }

            stamina += stamina * pct * PenaltyScaling.MovementPenaltyFactor(player);
        }
    }
}
