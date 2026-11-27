using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Character), nameof(Character.Stagger))]
    public class Immovable_Character_Stagger_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Character __instance)
        {
            return !(__instance is Player player) ||
                !player.HasActiveMagicEffect(MagicEffectType.Immovable, out float _) ||
                !player.IsBlocking();
        }
    }

    // Skipping Stagger alone is not enough: vanilla BlockAttack asks AddStaggerDamage whether the block
    // staggered, and a full bar answers yes, letting the whole hit through. So stagger damage does not build
    // while blocking with Immovable.
    [HarmonyPatch(typeof(Character), nameof(Character.AddStaggerDamage))]
    public class Immovable_Character_AddStaggerDamage_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Character __instance, ref bool __result)
        {
            if (__instance is Player player && player.IsBlocking() &&
                player.HasActiveMagicEffect(MagicEffectType.Immovable, out float _))
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.ApplyPushback), typeof(Vector3), typeof(float))]
    public class Immovable_Character_ApplyPushback_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Character __instance)
        {
            return !(__instance is Player player) ||
                !player.HasActiveMagicEffect(MagicEffectType.Immovable, out float _) ||
                !player.IsBlocking();
        }
    }
}