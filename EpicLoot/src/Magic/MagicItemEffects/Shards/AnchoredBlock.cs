using EpicLoot.src.Magic.MagicItemEffects.Helpers;

namespace EpicLoot.MagicItemEffects.Shards 
{
    public static class AnchoredBlock 
    {
        public static void Apply(ItemDrop.ItemData __instance, ref float baseBlock)
        {
            // Only the item's own wearer gets the bonus: GetBaseBlockPower also runs for blocking
            // enemies' shields and for unequipped-item tooltips, and on a dedicated server there is no
            // local player at all.
            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
            if (player == null)
            {
                return;
            }

            float perPenaltyPoint = player.GetTotalActiveMagicEffectValue(MagicEffectType.AnchoredBlock, 1);
            if (perPenaltyPoint == 0f)
            {
                return;
            }

            // MovementPenalty is a fraction (0.25 at -25% speed), so * 100 pays per percentage point.
            float penaltyBonusBlock = perPenaltyPoint * (PenaltyScaling.MovementPenalty(player) * 100);
            baseBlock += (penaltyBonusBlock);
        }
    }
}

