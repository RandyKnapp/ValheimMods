using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    public static class StrikeCausesLightning {
        public const float DefaultDamagePerValue = 3.6f;
        public const float DefaultRadius = 1f;

        private const string DamagePerValueKey = "DamagePerValue";
        private const string RadiusKey = "Radius";

        public static readonly Dictionary<string, float> DefaultConfig = new Dictionary<string, float> {
            { DamagePerValueKey, DefaultDamagePerValue },
            { RadiusKey, DefaultRadius },
        };

        // A networked, damage-free clone of the vanilla lightning-rod AOE, registered into ZNetScene on every
        // client each world load so the ZDO of each spawned strike resolves everywhere.
        private const string VisualSourcePrefab = "lightningAOE";
        private const string VisualClonePrefab = "EL_StrikeCausesLightningVisual";
        private const float VisualLifetime = 5f;   // seconds the spawned FX lives before TimedDestruction removes it
        private static GameObject _visualContainer;  // disabled parent that keeps the template from Awaking
        private static GameObject _visualTemplate;   // our registered clone (activeSelf == true)
        private static bool _sourceMissingLogged;
        private static bool _visualMissingLogged;

        public static void RegisterVisualPrefab() {
            var zns = ZNetScene.instance;
            if (zns == null || zns.GetPrefab(VisualClonePrefab) != null) {
                return;
            }

            var template = GetOrBuildTemplate(zns);
            if (template == null) {
                return;
            }

            if (!zns.m_prefabs.Contains(template)) {
                zns.m_prefabs.Add(template);
            }
            zns.m_namedPrefabs[VisualClonePrefab.GetStableHashCode()] = template;
            EpicLoot.Log($"StrikeCausesLightning: registered '{VisualClonePrefab}' with ZNetScene.");
        }

        private static GameObject GetOrBuildTemplate(ZNetScene zns) {
            if (_visualTemplate != null) {
                return _visualTemplate;
            }

            var source = zns.GetPrefab(VisualSourcePrefab);
            if (source == null) {
                if (!_sourceMissingLogged) {
                    EpicLoot.LogWarning($"StrikeCausesLightning: could not find '{VisualSourcePrefab}' prefab; strike visual will not display.");
                    _sourceMissingLogged = true;
                }
                return null;
            }

            if (_visualContainer == null) {
                _visualContainer = new GameObject("EL_StrikeCausesLightningContainer");
                _visualContainer.SetActive(false);
                Object.DontDestroyOnLoad(_visualContainer);
            }

            // Cloning under the disabled container keeps the template inactive-in-hierarchy (so nothing Awakes)
            // while preserving activeSelf == true for the eventual instances.
            var template = Object.Instantiate(source, _visualContainer.transform);
            template.name = VisualClonePrefab;

            // Strip the damage-dealing Aoe scripts (the AOE_ROD and AOE_AREA children); ChainLightning stays the
            // sole damage source. DestroyImmediate so they're gone before the template is ever instantiated.
            foreach (var aoe in template.GetComponentsInChildren<Aoe>(true)) {
                Object.DestroyImmediate(aoe);
            }

            // Those Aoe scripts were also what tore the object down after its ttl, so drive cleanup with a
            // TimedDestruction: on the owner it calls ZNetScene.Destroy VisualLifetime seconds after the strike
            // spawns and the ZDO removal propagates to the clients it synced to.
            var timed = template.GetComponent<TimedDestruction>() ?? template.AddComponent<TimedDestruction>();
            timed.m_timeout = VisualLifetime;
            timed.m_triggerOnAwake = true;

            _visualTemplate = template;
            return _visualTemplate;
        }

        // Postfix handler invoked by SharedCharacterDamagePatch (on-hit reaction).
        public static void OnDamageDealt(Character __instance, HitData hit, Character attacker) {
            var player = Player.m_localPlayer;
            if (hit == null || player == null || __instance == player || attacker != player
                || __instance.IsPlayer() || __instance.IsTamed() || __instance.IsDead()) {
                return;
            }

            var value = player.GetTotalActiveMagicEffectValue(MagicEffectType.StrikeCausesLightning, 1f);
            if (value <= 0f || Random.value > value * 0.01f) {
                return;
            }

            // No attacker: a strike credited to the player would count as a player attack and chain on to other
            // enemies in range; each strike must come from its own proc.
            DamageInRadius.DamageEnemiesInRadius(null, __instance.GetCenterPoint(),
                EffectConfig.Get(MagicEffectType.StrikeCausesLightning, RadiusKey, DefaultRadius),
                new HitData.DamageTypes {
                    m_lightning = value * EffectConfig.Get(MagicEffectType.StrikeCausesLightning,
                        DamagePerValueKey, DefaultDamagePerValue)
                });
            SpawnVisual(__instance.transform.position);
        }

        private static void SpawnVisual(Vector3 position) {
            var prefab = ZNetScene.instance?.GetPrefab(VisualClonePrefab);
            if (prefab == null) {
                if (!_visualMissingLogged) {
                    EpicLoot.LogWarning($"StrikeCausesLightning: '{VisualClonePrefab}' prefab not registered; strike visual will not display.");
                    _visualMissingLogged = true;
                }
                return;
            }

            Object.Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
