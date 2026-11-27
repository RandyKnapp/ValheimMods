using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string ReduceStaggerTaken = nameof(ReduceStaggerTaken);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // AddStaggerDamage is where both kinds of stagger land: unblocked hits from ApplyDamage, and blocked ones from
    // BlockAttack (which asks it whether the guard broke), so the guard holds longer too.
    public static class ReduceStaggerTaken
    {
        private const float MaxReduction = 0.9f;   // so the bar still fills

        [HarmonyPatch(typeof(Character), nameof(Character.AddStaggerDamage))]
        private static class Character_AddStaggerDamage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, ref float damage)
            {
                // AddStaggerDamage runs on every character's owner, enemies included, and on a dedicated server there is
                // no local player at all: only the local player's own stagger is reduced.
                if (__instance != Player.m_localPlayer)
                {
                    return;
                }

                if (Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ReduceStaggerTaken, out float reduction, 0.01f))
                {
                    damage *= 1f - Mathf.Clamp(reduction, 0f, MaxReduction);
                }
            }
        }
    }
}
