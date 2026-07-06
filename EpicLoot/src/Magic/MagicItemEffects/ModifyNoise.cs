using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class ModifyNoise
{
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyNoise))]
    public static class ModifyNoise_SEMan_ModifyNoise_Patch
    {
        public static void Postfix(SEMan __instance, ref float noise)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var noiseValue = 0f;
                ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyNoise, effect =>
                {
                    noiseValue += player.GetTotalActiveMagicEffectValue(effect, 0.01f);
                });
                noise += noiseValue;
            }
        }
    }
}