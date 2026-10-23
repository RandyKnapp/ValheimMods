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
    // Reduce Stagger Taken (3-piece Svalinn's Arc set bonus, NoRoll for now). The stagger damage the local player takes
    // is cut by the effect value in percent, at most 90 so the bar still fills. AddStaggerDamage is where both kinds
    // land: unblocked hits from ApplyDamage, and blocked ones from BlockAttack (which asks it whether the guard broke),
    // so the guard holds longer too. Multiplies with Blood Stagger Block's own cut while blocking.
    public static class ReduceStaggerTaken
    {
        private const float MaxReduction = 0.9f;

        [HarmonyPatch(typeof(Character), nameof(Character.AddStaggerDamage))]
        private static class Character_AddStaggerDamage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance, ref float damage)
            {
                // AddStaggerDamage runs on every character's owner, enemies included, and on a dedicated server there is
                // no local player at all: only the local player's own stagger is reduced.
                if (__instance == null || __instance != Player.m_localPlayer)
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
