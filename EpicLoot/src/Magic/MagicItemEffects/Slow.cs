using HarmonyLib;
using JetBrains.Annotations;
using System;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public sealed class Slow : MonoBehaviour
    {
        public const string RPCKey = "epic loot slow";

        public float Multiplier;
        public float TimeToLive;

        private Character _character;

        public void Start()
        {
            _character = GetComponent<Character>();

            _character.m_acceleration *= Multiplier;
            _character.m_runSpeed *= Multiplier;
            _character.m_flyFastSpeed *= Multiplier;
            _character.m_swimSpeed *= Multiplier;
        }

        public void FixedUpdate()
        {
            TimeToLive -= Time.fixedDeltaTime;

            if (TimeToLive > 0)
            {
                return;
            }

            _character.m_acceleration /= Multiplier;
            _character.m_runSpeed /= Multiplier;
            _character.m_flyFastSpeed /= Multiplier;
            _character.m_swimSpeed /= Multiplier;

            Destroy(this);
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    public static class SlowAddRPC_Character_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Character __instance)
        {
            __instance.m_nview.Register<float>(Slow.RPCKey, (s, multiplier) => RPC_Slow(__instance, multiplier));
        }

        private static void RPC_Slow(Character character, float multiplier)
        {
            if (!character.TryGetComponent(out Slow slow))
            {
                slow = character.gameObject.AddComponent<Slow>();
                slow.Multiplier = multiplier;
            }

            slow.TimeToLive = 2;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.RPC_Damage))]
    public static class ApplySlow_Character_RPC_Damage_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Character __instance, HitData hit)
        {
            if (!__instance.IsBoss()
                && hit.GetAttacker() is Player player
                && player.HasActiveMagicEffect(MagicEffectType.Slow, out float effectValue, 0.01f))
            {
                float slowMultiplier = 1 - effectValue;

                if (!Mathf.Approximately(slowMultiplier, 1))
                {
                    __instance.m_nview.InvokeRPC(ZRoutedRpc.Everybody, Slow.RPCKey, slowMultiplier);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
    public static class ModifyEnemyAttackSpeed_AnimationHandler_Patch
    {
        public static double ModifyAttackSpeed(Character character, double speed)
        {
            if (character.InAttack() && character.TryGetComponent(out Slow slow))
            {
                if (speed > 0.001f && (speed * 1e4f % 10 > 3 || speed * 1e4f % 10 < 1))
                {
                    speed = (float) Math.Round(speed * slow.Multiplier, 3) + speed % 1e-4f + 2e-4f;
                }
            }

            return speed;
        }
        
        [UsedImplicitly]
        private static void Postfix(Game __instance)
        {
            AnimationSpeedManager.Add(ModifyAttackSpeed);
        }
    }
}