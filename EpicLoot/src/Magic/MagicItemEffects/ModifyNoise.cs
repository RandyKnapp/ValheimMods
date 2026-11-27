using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public class ModifyNoise
{
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyNoise))]
    public static class ModifyNoise_SEMan_ModifyNoise_Patch
    {
        public static void Postfix(SEMan __instance, float baseNoise, ref float noise)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var noiseValue = ModifyWithLowHealth.Sum(player, MagicEffectType.ModifyNoise);
                // A reduction by that share of the base noise, the same way vanilla's SE_Stats.m_noiseModifier applies.
                noise -= baseNoise * Mathf.Clamp01(noiseValue);
            }
        }
    }
}
