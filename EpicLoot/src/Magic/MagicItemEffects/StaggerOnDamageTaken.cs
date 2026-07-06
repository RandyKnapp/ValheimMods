using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects;

[HarmonyPatch(typeof(Character), nameof(Character.Damage))]
public static class StaggerOnDamageTaken_Character_Damage_Patch
{
    [UsedImplicitly]
    private static void Postfix(Character __instance, HitData hit)
    {
        if (hit == null || __instance == null)
        {
            return;
        }

        Character attacker = hit.GetAttacker();

        if (attacker == Player.m_localPlayer &&
            __instance != null && attacker != __instance &&
            Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.StaggerOnDamageTaken, out float effectValue, 0.01f))
        {
            // Don't stagger friendly players, only PvP enabled ones
            if (__instance is Player && __instance.IsPVPEnabled() == false)
            {
                return;
            }

            if (UnityEngine.Random.Range(0f, 1f) <= effectValue)
            {
                __instance.Stagger(-__instance.transform.forward);
            }
        }
    }
}