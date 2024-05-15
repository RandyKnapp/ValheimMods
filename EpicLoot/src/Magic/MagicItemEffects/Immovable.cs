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
            return !(__instance is Player player) || !player.HasActiveMagicEffect(MagicEffectType.Immovable) || !player.IsBlocking();
        }
    }
    
    [HarmonyPatch(typeof(Character), nameof(Character.ApplyPushback), typeof(Vector3), typeof(float))]
    public class Immovable_Character_ApplyPushback_Patch
    {
        [UsedImplicitly]
        private static bool Prefix(Character __instance)
        {
            return !(__instance is Player player) || !player.HasActiveMagicEffect(MagicEffectType.Immovable) || !player.IsBlocking();
        }
    }
}