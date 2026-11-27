using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class AdrenalineFrostWave {
        // Movement drops to SpeedFloor at application and eases back to normal over the duration, following
        // vanilla's frost curve.
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

        // Seed ttl for the prototype only; the per-cast duration comes from the shard value.
        private const float PrototypeSeedDuration = 2f;
        private const string NovaTemplateName = "EL_FrostWaveNova";
        private const float NovaSpeed = 1.5f;

        private static SE_Frost _prototype;
        private static bool _prototypeMissingLogged;

        public static void RegisterDisplayValues() {
            MagicItem.RegisterDisplayValues(MagicEffectType.AdrenalineFrostWave,
                value => new object[] { value, GetRadius() });
        }

        public static void RegisterNovaPrefab() {
            FrostNovaFx.RegisterPrefab(NovaTemplateName, NovaSpeed);
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class AddRpc_Character_Awake_Patch {
            [UsedImplicitly]
            private static void Postfix(Character __instance) {
                __instance.m_nview?.Register<float>(RpcKey, (sender, duration) => RPC_FrostWave(__instance, duration));
            }
        }

        // SharedPlayerAddAdrenalinePatch has already checked the local player and the adrenaline source.
        public static void OnAdrenalineActivated(Player player) {
            var duration = player.GetTotalActiveMagicEffectValue(MagicEffectType.AdrenalineFrostWave);
            if (duration <= 0f) {
                return;
            }

            // Just above the player's feet, so the nova is visible but close to the ground.
            var novaPosition = player.transform.position;
            novaPosition.y += 0.6f;
            FrostNovaFx.Spawn(NovaTemplateName, novaPosition);

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

                character.m_nview.InvokeRPC(RpcKey, duration);
            }
        }

        // Sent to the target's owner only: vanilla ticks status effects on the owner alone, so a copy on any other
        // client would never expire and would slow the creature again once ownership moved there.
        private static void RPC_FrostWave(Character character, float duration) {
            if (character == null || character.m_seman == null || duration <= 0f ||
                character.m_nview == null || !character.m_nview.IsOwner()) {
                return;
            }

            var prototype = GetOrCreatePrototype();
            if (prototype == null) {
                return;
            }

            // Stamped per instance because the cached prototype would not follow a retune.
            var speedFloor = GetSpeedFloor();

            // Already chilled: extend to the longer of the two and restart the decay curve.
            if (character.m_seman.GetStatusEffect(prototype.NameHash()) is SE_Frost existing) {
                existing.m_ttl = Mathf.Max(duration, existing.GetRemaningTime());
                existing.m_minSpeedFactor = speedFloor;
                existing.ResetTime();
                return;
            }

            // AddStatusEffect(prototype) clones via MemberwiseClone (keeps NameHash) and triggers the frost
            // start effects.
            if (character.m_seman.AddStatusEffect(prototype) is SE_Frost added) {
                added.m_ttl = duration;
                added.m_minSpeedFactor = speedFloor;
                added.ResetTime();
            }
        }

        // Copying public fields off the vanilla prototype brings its start/stop effects, icon and freeze times across;
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
