using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects.Shards
{
    // Reduces stagger damage taken while blocking, and applies a small self-damage when blocking.
    public static class BloodStaggerBlock
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateBlock))]
        private class BlockState_Patch {
            private static void Postfix(Humanoid __instance) {
                BloodBlockSelfDamage.OnBlockStart(__instance, MagicEffectType.BloodStaggerBlock);
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.AddStaggerDamage))]
        public class BloodStaggerBlock_StaggerReduction_Patch
        {
            private static void Prefix(Character __instance, ref float damage)
            {
                // AddStaggerDamage runs for every character on its owner (enemies included, and on a
                // dedicated server with no local player at all), so only the local player's OWN stagger
                // accumulation is reduced -- not the stagger the player deals to enemies while blocking.
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                if (Player.m_localPlayer.IsBlocking())
                {
                    damage *= (1f - Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.BloodStaggerBlock, .01f));
                }
            }
        }
    }
}
