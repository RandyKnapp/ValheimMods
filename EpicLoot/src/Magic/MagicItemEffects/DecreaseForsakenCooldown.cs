using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class DecreaseForsakenCooldown
{
    [HarmonyPatch(typeof(Player), nameof(Player.ActivateGuardianPower))]
    private class DecreaseForsakenCooldown_Player_ActivateGuardianPower_Patch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance.m_guardianSE != null && __instance.m_guardianPowerCooldown == __instance.m_guardianSE.m_cooldown &&
                __instance.HasActiveMagicEffect(MagicEffectType.DecreaseForsakenCooldown, out float magicEffectValue, 0.01f))
            {
                __instance.m_guardianPowerCooldown *= 1 - magicEffectValue;
            }
        }
    }
}