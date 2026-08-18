using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to block based on the player's carried weight
    public class BurdenedBlock 
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

            float carriedWeight = player.GetInventory().GetTotalWeight();
            float burdenedBlockBonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.BurdenedBlock, 1f);
            float burdenedBlockIncrement = Math.Max(0, (int)((carriedWeight - 300f) / 50f));

            baseBlock += (burdenedBlockIncrement * burdenedBlockBonus);

        }
    }
}
