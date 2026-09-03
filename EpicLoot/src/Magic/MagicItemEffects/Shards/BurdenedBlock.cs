using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Provides a bonus to block based on the player's carried weight
    public class BurdenedBlock
    {
        // Carried weight the bonus starts paying out at, and how much further weight buys each additional
        // step of it. Tunable as "WeightThreshold" and "WeightPerBonus" in this effect's Config block in
        // config/shardstones.json.
        public const float DefaultWeightThreshold = 300f;
        public const float DefaultWeightPerBonus = 50f;

        private const string WeightThresholdKey = "WeightThreshold";
        private const string WeightPerBonusKey = "WeightPerBonus";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { WeightThresholdKey, DefaultWeightThreshold },
            { WeightPerBonusKey, DefaultWeightPerBonus },
        };

        private static float GetWeightThreshold()
        {
            return Mathf.Max(0f, EffectConfig.Get(MagicEffectType.BurdenedBlock,
                WeightThresholdKey, DefaultWeightThreshold));
        }

        // Floored at 1 because the step count divides by this: a configured 0 would make the bonus infinite.
        private static float GetWeightPerBonus()
        {
            return Mathf.Max(1f, EffectConfig.Get(MagicEffectType.BurdenedBlock,
                WeightPerBonusKey, DefaultWeightPerBonus));
        }

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

            // Memoized effect value first: GetBaseBlockPower is a hot path, and the inventory weight walk
            // and config lookups below are not free.
            float burdenedBlockBonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.BurdenedBlock, 1f);
            if (burdenedBlockBonus <= 0f)
            {
                return;
            }

            float carriedWeight = player.GetInventory().GetTotalWeight();
            float burdenedBlockIncrement = Math.Max(0,
                (int)((carriedWeight - GetWeightThreshold()) / GetWeightPerBonus()));

            baseBlock += (burdenedBlockIncrement * burdenedBlockBonus);

        }
    }
}
