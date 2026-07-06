using HarmonyLib;
using System;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

[HarmonyPatch]
public class OffSetAttack
{
    private const int EFFECT_DURATION = 500; // miliseconds
    private static bool _activeOffSet = false;
    private static long _offSetTriggerTime = 0;

    /// <summary>
    /// Records OffSetAttack is triggered.
    /// </summary>
    /// <param name="__instance"></param>
    [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
    [HarmonyPrefix]
    public static void Prefix(Attack __instance)
    {
        if (__instance.m_character != Player.m_localPlayer)
        {
            return;
        }

        if (__instance.m_currentAttackCainLevel == 2)
        {
            _offSetTriggerTime = DateTime.Now.Ticks;
            _activeOffSet = true;
        }
    }

    /// <summary>
    /// Applies any active OffSetAttack buffs when taking damage.
    /// </summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    private static void Prefix(Character __instance, ref HitData hit)
    {
        if (!_activeOffSet ||
            __instance == null ||
            __instance != Player.m_localPlayer ||
            !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.OffSetAttack, out float effectValue, 0.01f))
        {
            return;
        }

        effectValue = Mathf.Clamp01(effectValue);

        if (DateTime.Now.Ticks <= _offSetTriggerTime + (EFFECT_DURATION * TimeSpan.TicksPerMillisecond))
        {
            hit.m_damage.Modify(1f - effectValue);
            hit.m_pushForce = 0f; // knock back immunity
            hit.m_staggerMultiplier = 0f;

            AudioSource.PlayClipAtPoint(EpicAssets.OffSetSFX, Player.m_localPlayer.transform.position);
        }
        else
        {
            _activeOffSet = false;
        }
    }
}