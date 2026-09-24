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
                !player.HasActiveMagicEffect(MagicEffectType.Immovable, out float magicEffect) ||
                !player.IsBlocking();
        }
    }
    
    // Skipping Stagger alone left the guard broken: vanilla BlockAttack asks AddStaggerDamage whether the block
    // staggered, and a full stagger bar answers yes -- so the block let the whole hit through, with no stagger
    // to show for it, and the bar stayed full (re-pinned by every hit) until it drained. While blocking with
    // Immovable, stagger damage doesn't build at all. Stamina still runs out as usual.
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
                !player.HasActiveMagicEffect(MagicEffectType.Immovable, out float magicEffect) ||
                !player.IsBlocking();
        }
    }
}