using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Adds a flat bonus to base block, at the cost of a self-damage hit each time a block is started.
    public static class BloodBaseBlock
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateBlock))]
        private class BlockState_Patch
        {
            private static void Postfix(Humanoid __instance)
            {
                BloodBlockSelfDamage.OnBlockStart(__instance, MagicEffectType.BloodBaseBlock);
            }
        }

        public static void Apply(ItemDrop.ItemData __instance, ref float baseBlock)
        {
            // GetBaseBlockPower is called for every humanoid's blocker (blocking enemies included, via
            // GetBlockPower inside BlockAttack) and for tooltips of unequipped items, so the bonus only
            // applies to the item's own wearer -- never to an enemy's shield, and never on a dedicated
            // server where there is no local player.
            var player = PlayerExtensions.GetPlayerWithEquippedItem(__instance);
            if (player == null)
            {
                return;
            }

            float bloodBaseBlock = player.GetTotalActiveMagicEffectValue(MagicEffectType.BloodBaseBlock, 1f);

            baseBlock += (bloodBaseBlock);
        }
    }
}
