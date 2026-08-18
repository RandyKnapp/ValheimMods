using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards
{

    //Battering Ram is commented out due to it not being used, and the patch being mildly expensive
    // This effect allows the player to deal blunt damage to enemies by running into them. The damage is based on the player's weight and speed, and has a cooldown per target.

    //public static class BatteringRam
    //{
    //    private const float MinRamSpeed = 3f;           // horizontal speed required to ram (roughly a jog+)
    //    private const float FullSpeed = 10f;             // speed at which the ram deals its full weight damage
    //    private const float Range = 1.2f;               // how close an enemy must be to be rammed
    //    private const float PerTargetCooldown = 10f;     // seconds before the same enemy can be rammed again
    //    private const float WeightDamageFactor = 0.05f; // blunt per (value * carried-weight) unit

    //    private static readonly Dictionary<Character, float> _nextHit = new Dictionary<Character, float>();
    //    private static readonly List<Character> _inRange = new List<Character>();
    //    private static float _pruneTimer;

    //    [HarmonyPatch(typeof(Player), nameof(Player.Update))]
    //    private static class Update_Patch
    //    {
    //        [UsedImplicitly]
    //        private static void Postfix(Player __instance)
    //        {
    //            if (__instance != Player.m_localPlayer || __instance.IsDead())
    //            {
    //                return;
    //            }

    //            var value = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.BatteringRam);
    //            if (value <= 0f)
    //            {
    //                return;
    //            }

    //            var velocity = __instance.GetVelocity();
    //            velocity.y = 0f;
    //            var speed = velocity.magnitude;
    //            if (speed < MinRamSpeed || !__instance.IsOnGround())
    //            {
    //                return;
    //            }

    //            var weight = __instance.GetInventory().GetTotalWeight();
    //            var speedFactor = Mathf.Clamp01(speed / FullSpeed);
    //            var blunt = value * weight * WeightDamageFactor * speedFactor;
    //            if (blunt <= 0f)
    //            {
    //                return;
    //            }

    //            Prune();

    //            _inRange.Clear();
    //            Character.GetCharactersInRange(__instance.transform.position, Range, _inRange);
    //            foreach (var character in _inRange)
    //            {
    //                if (character == null || character == __instance || character.IsDead() ||
    //                    character.IsPlayer() || character.IsTamed())
    //                {
    //                    continue;
    //                }

    //                if (_nextHit.TryGetValue(character, out var next) && Time.time < next)
    //                {
    //                    continue;
    //                }
    //                _nextHit[character] = Time.time + PerTargetCooldown;

    //                if (__instance.HaveStamina(value * 0.5f) == false) {
    //                    continue;
    //                } else {
    //                    __instance.UseStamina(value * 0.5f);
    //                }

    //                var hit = new HitData();
    //                hit.m_point = character.GetCenterPoint();
    //                hit.m_dir = (character.transform.position - __instance.transform.position).normalized;
    //                hit.m_damage.m_blunt = blunt;
    //                hit.m_hitType = HitData.HitType.PlayerHit;
    //                hit.m_pushForce = 20f * speedFactor;
    //                hit.SetAttacker(__instance);
    //                character.Damage(hit);
    //            }
    //        }
    //    }

    //    // Drop stale cooldown entries occasionally so the dictionary doesn't grow without bound.
    //    private static void Prune()
    //    {
    //        _pruneTimer -= Time.deltaTime;
    //        if (_pruneTimer > 0f)
    //        {
    //            return;
    //        }
    //        _pruneTimer = 5f;

    //        var stale = new List<Character>();
    //        foreach (var pair in _nextHit)
    //        {
    //            if (pair.Key == null || Time.time > pair.Value + 1f)
    //            {
    //                stale.Add(pair.Key);
    //            }
    //        }
    //        foreach (var key in stale)
    //        {
    //            _nextHit.Remove(key);
    //        }
    //    }
    //}
}
