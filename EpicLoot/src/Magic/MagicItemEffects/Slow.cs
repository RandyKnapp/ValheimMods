using HarmonyLib;
using JetBrains.Annotations;
using System;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    public sealed class Slow : MonoBehaviour
    {
        public const string RPCKey = "epic loot slow";

        // Slow values sum across every equipped item and can reach 100% or more, which would freeze the target.
        // A const rather than a config value: the multiplier is re-clamped on receipt, so every client has to
        // agree on the bound.
        public const float MinMultiplier = 0.1f;

        public static float ClampMultiplier(float multiplier) => Mathf.Clamp(multiplier, MinMultiplier, 1f);

        public float Multiplier = 1f;
        public float TimeToLive;

        private Character _character;
        private bool _applied;
        private float _acceleration;
        private float _runSpeed;
        private float _flyFastSpeed;
        private float _swimSpeed;

        public void Start()
        {
            _character = GetComponent<Character>();
            if (_character == null)
            {
                Destroy(this);
                return;
            }

            // Clamp here as well as at the call site: RPC_Slow takes the multiplier off the wire, so an out
            // of range value can arrive from a mismatched or modified client.
            Multiplier = ClampMultiplier(Multiplier);

            // Restored verbatim rather than divided back out, which drifts over repeated slows. Vanilla never
            // writes these four fields at runtime, so nothing else competes for them.
            _acceleration = _character.m_acceleration;
            _runSpeed = _character.m_runSpeed;
            _flyFastSpeed = _character.m_flyFastSpeed;
            _swimSpeed = _character.m_swimSpeed;
            _applied = true;

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

            // OnDestroy performs the restore, so it runs exactly once however the component goes away.
            Destroy(this);
        }

        public void OnDestroy()
        {
            // _applied is false when Start never ran (destroyed the same frame it was added), in which case
            // the speeds were never scaled and there is nothing to put back.
            if (!_applied || _character == null)
            {
                return;
            }

            _applied = false;

            _character.m_acceleration = _acceleration;
            _character.m_runSpeed = _runSpeed;
            _character.m_flyFastSpeed = _flyFastSpeed;
            _character.m_swimSpeed = _swimSpeed;
        }
    }

    [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
    public static class SlowAddRPC_Character_Awake_Patch
    {
        [UsedImplicitly]
        private static void Postfix(Character __instance)
        {
            __instance.m_nview?.Register<float>(Slow.RPCKey, (s, multiplier) => RPC_Slow(__instance, multiplier));
        }

        private static void RPC_Slow(Character character, float multiplier)
        {
            if (character == null)
            {
                return;
            }

            multiplier = Slow.ClampMultiplier(multiplier);
            if (Mathf.Approximately(multiplier, 1f))
            {
                return;
            }

            if (!character.TryGetComponent(out Slow slow))
            {
                slow = character.gameObject.AddComponent<Slow>();
                slow.Multiplier = multiplier;
            }

            slow.TimeToLive = 2;
        }
    }

    public static class ApplySlow_Character_Damage_Patch
    {
        // Called from SharedCharacterDamagePatch's postfix. The Slow value is only readable on the attacker's own
        // client, so it is checked here and the slow is applied on the target's owner through the RPC.
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker)
        {
            // IsValid() (not just a null check) is required: Character.Damage routes RPC_Damage synchronously
            // when the local client owns the target, so the target can already have died and been destroyed by
            // the time this postfix runs. ZNetScene.Destroy nulls the ZDO immediately while the ZNetView
            // component itself only compares null at the end of the frame -- InvokeRPC would then dereference
            // the null m_zdo.
            if (__instance == null || __instance.m_nview == null || !__instance.m_nview.IsValid()
                || attacker != Player.m_localPlayer || __instance.IsBoss())
            {
                return;
            }

            if (!Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.Slow, out float effectValue, 0.01f))
            {
                return;
            }

            // Clamp before sending so the value on the wire is already the one every receiver will use.
            float slowMultiplier = Slow.ClampMultiplier(1 - effectValue);
            if (!Mathf.Approximately(slowMultiplier, 1))
            {
                __instance.m_nview.InvokeRPC(ZRoutedRpc.Everybody, Slow.RPCKey, slowMultiplier);
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
        
        private static bool _appliedAttackSpeed;

        [UsedImplicitly]
        private static void Postfix(Game __instance)
        {
            // Game.Awake runs once per world load; registering again would compound the slow multiplier.
            if (_appliedAttackSpeed)
            {
                return;
            }

            _appliedAttackSpeed = true;
            AnimationSpeedManager.Add(ModifyAttackSpeed);
        }
    }
}