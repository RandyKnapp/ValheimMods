using HarmonyLib;
using System;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

[HarmonyPatch]
public class OffSetAttack
{
    private const int EFFECT_DURATION = 500; // milliseconds
    private static bool _activeOffSet = false;
    private static bool _activeOffSetSFXPlayed = false;
    private static long _offSetTriggerTime = 0;

    // Opens the window on the third swing of a combo; the sfx plays once per window.
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
            _activeOffSetSFXPlayed = false;
        }
    }

    // Called from SharedCharacterRpcDamagePatch's prefix, on the victim's owner. Hits with no attacker
    // (environment, damage over time) are not reduced.
    public static void ReduceIncomingHit(Character __instance, HitData hit)
    {
        if (!_activeOffSet ||
            __instance != Player.m_localPlayer ||
            !hit.HaveAttacker() ||
            !Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.OffSetAttack, out float effectValue, 0.01f))
        {
            return;
        }

        effectValue = Mathf.Clamp01(effectValue);

        if (DateTime.Now.Ticks <= _offSetTriggerTime + (EFFECT_DURATION * TimeSpan.TicksPerMillisecond))
        {
            hit.m_damage.Modify(1f - effectValue);
            hit.m_pushForce = 0f;
            hit.m_staggerMultiplier = 0f;

            if (!_activeOffSetSFXPlayed) {
                LocalFx.Spawn(EpicAssets.OffSetSFX, Player.m_localPlayer.transform.position, Quaternion.identity, lifetime: 4f, noCameraShake: false);
                _activeOffSetSFXPlayed = true;
            }
        }
        else
        {
            _activeOffSet = false;
        }
    }
}