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
                var noiseValue = 0f;
                ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyNoise, effect =>
                {
                    noiseValue += player.GetTotalActiveMagicEffectValue(effect, 0.01f);
                });
                // A reduction by that share of the base noise, the same way vanilla's SE_Stats.m_noiseModifier
                // applies; the value used to be added flat, which made the player slightly louder.
                noise -= baseNoise * Mathf.Clamp01(noiseValue);
            }
        }
    }
}
