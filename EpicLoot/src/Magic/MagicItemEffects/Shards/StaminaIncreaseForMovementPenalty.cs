using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Green (Movement) legs shard: +% max stamina scaled by the movement-speed penalty of the player's
    // gear. Invoked from IncreasePlayerBaseStats' GetTotalFoodValue postfix (like its IncreaseStamina) so it
    // feeds both the stamina pool and its HUD bar; the percent is of the food-derived max stamina. Shard
    // values are authored as whole-number percents, hence the 0.01f.
    public static class StaminaIncreaseForMovementPenalty
    {
        public static void Apply(Player player, ref float stamina)
        {
            if (player != Player.m_localPlayer)
            {
                return;
            }

            stamina += stamina * player.GetTotalActiveMagicEffectValue(
                MagicEffectType.StaminaIncreaseForMovementPenalty, 0.01f) * PenaltyScaling.MovementPenaltyFactor(player);
        }
    }
}
