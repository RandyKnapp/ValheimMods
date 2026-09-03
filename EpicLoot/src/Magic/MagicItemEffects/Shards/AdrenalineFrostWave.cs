using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // On adrenaline activation, applies a frost wave to nearby enemies. The wave slows movement by 60% at application and eases back to normal over the duration, following vanilla's frost curve.
    public static class AdrenalineFrostWave {
        // Enemy search radius, and the speed floor while chilled -- movement drops to this fraction at
        // application and eases back to normal over the duration, following vanilla's frost curve. Both
        // tunable in this effect's Config block in config/shardstones.json, under these key names.
        public const float DefaultRadius = 10f;
        public const float DefaultSpeedFloor = 0.4f;

        private const string RadiusKey = "Radius";
        private const string SpeedFloorKey = "SpeedFloor";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { RadiusKey, DefaultRadius },
            { SpeedFloorKey, DefaultSpeedFloor },
        };

        private static float GetRadius() {
            return Mathf.Max(0f,
                EffectConfig.Get(MagicEffectType.AdrenalineFrostWave, RadiusKey, DefaultRadius));
        }

        // Clamped to 0..1: above 1 the "slow" would be a speed boost, below 0 it would invert movement.
        private static float GetSpeedFloor() {
            return EffectConfig.GetClamped(MagicEffectType.AdrenalineFrostWave,
                SpeedFloorKey, DefaultSpeedFloor, 0f, 1f);
        }

        private const string RpcKey = "EL_FrostWave";

        // Unity object name of the SE prototype -- NameHash() hashes this (GetStableHashCode), so it must be
        // identical on every client for the add/refresh lookup to line up.
        private const string SeName = "EL_FrostWave";

        // Seed ttl for the prototype only; the per-cast duration comes from the shard value and is stamped
        // on the instance, so this is not a balance knob and is not exposed in the effect Config.
        private const float PrototypeSeedDuration = 2f;
        private const string NovaTemplateName = "EL_FrostWaveNova";
        private const float NovaSpeed = 1.5f;

        private static SE_Frost _prototype;
        private static bool _prototypeMissingLogged;

        // Tooltip: "Frost Wave: Slow Enemies within {1}m for {0}s" -- {1} is the configured radius, so the
        // shown number follows a retune instead of the baked-in default.
        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.AdrenalineFrostWave,
                value => new object[] { value, GetRadius() });
        }

        // Registers the chill RPC on every character so a remote-owned target can receive it. Mirrors
        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class AddRpc_Character_Awake_Patch {
            [UsedImplicitly]
            private static void Postfix(Character __instance) {
                __instance.m_nview?.Register<float>(RpcKey, (sender, duration) => RPC_FrostWave(__instance, duration));
            }
        }

        // Called by SharedPlayerAddAdrenalinePatch, which owns the Player.AddAdrenaline patch and the
        // fill/pop detection (including the local-player and no-adrenaline-source guards).
        public static void OnAdrenalineActivated(Player player) {
            var duration = player.GetTotalActiveMagicEffectValue(MagicEffectType.AdrenalineFrostWave);
            if (duration <= 0f) {
                return;
            }

            // Spawn the nova visibly above the player's feet, but close to the ground.
            var novaPosition = player.transform.position;
            novaPosition.y += 0.6f;
            FrostNovaFx.Spawn(NovaTemplateName, novaPosition, NovaSpeed);

            var center = player.transform.position;
            var radius = GetRadius();
            var radiusSqr = radius * radius;

            foreach (var character in Character.GetAllCharacters()) {
                if (character == null || character.IsPlayer() || character.IsTamed() || character.IsDead()) {
                    continue;
                }

                if ((character.transform.position - center).sqrMagnitude > radiusSqr) {
                    continue;
                }

                if (character.m_nview == null || !character.m_nview.IsValid()) {
                    continue;
                }

                // Broadcast so the target's owner (and everyone else, harmlessly) adds/refreshes the chill.
                character.m_nview.InvokeRPC(ZRoutedRpc.Everybody, RpcKey, duration);
            }
        }

        // Adds or refreshes the chill on the character. Runs on every client; only the owner's copy drives
        // movement, so this is what actually slows a remote-owned target.
        private static void RPC_FrostWave(Character character, float duration) {
            if (character == null || character.m_seman == null || duration <= 0f) {
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype == null) {
                return;
            }

            // The speed floor is stamped per instance rather than left on the cached prototype, so a retune
            // reaches chills applied from here on without waiting for a new game session.
            var speedFloor = GetSpeedFloor();

            // Already chilled: extend to the longer of the two and restart the decay curve.
            if (character.m_seman.GetStatusEffect(prototype.NameHash()) is SE_Frost existing) {
                existing.m_ttl = Mathf.Max(duration, existing.GetRemaningTime());
                existing.m_minSpeedFactor = speedFloor;
                existing.ResetTime();
                return;
            }

            // AddStatusEffect(prototype) clones via MemberwiseClone (keeps NameHash) and triggers the frost
            // start effects, then we stamp this cast's duration onto the added instance.
            if (character.m_seman.AddStatusEffect(prototype) is SE_Frost added) {
                added.m_ttl = duration;
                added.m_minSpeedFactor = speedFloor;
                added.ResetTime();
            }
        }

        // Lazily builds our clone of the vanilla Frost effect. Runs on a proc, so ObjectDB is loaded. Copying
        // public fields off the vanilla prototype brings its start/stop effects, icon and freeze times across;
        // the private cached name hash is NOT copied (CopyFields binds public instance fields only), so this
        // fresh ScriptableObject hashes its own name and stays a distinct effect from vanilla Frost.
        private static SE_Frost GetOrCreatePrototype() {
            if (_prototype != null) {
                return _prototype;
            }

            var frost = ObjectDB.instance?.GetStatusEffect(SEMan.s_statusEffectFrost) as SE_Frost;
            if (frost == null) {
                if (!_prototypeMissingLogged) {
                    EpicLoot.LogWarning("AdrenalineFrostWave: could not find the vanilla 'Frost' status effect; enemies will not be chilled.");
                    _prototypeMissingLogged = true;
                }
                return null;
            }

            var wave = ScriptableObject.CreateInstance<SE_Frost>();
            Common.Utils.CopyFields(frost, wave, typeof(SE_Frost));
            wave.name = SeName;
            wave.m_ttl = PrototypeSeedDuration;
            wave.m_minSpeedFactor = GetSpeedFloor();

            _prototype = wave;
            return _prototype;
        }
    }
}
