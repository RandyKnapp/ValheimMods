using HarmonyLib;
using JetBrains.Annotations;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects.Shards {
    // Lays a trail of burning behind the player while moving, damage scaled by the effect value
    public static class Trailblazer {
        // Tuning knobs (placeholders; balance later).
        private const float MinMoveSpeed = 1.5f;      // horizontal speed required to lay a trail
        private const float SpawnInterval = 0.35f;    // seconds between dropping trail patches
        private const float PatchRadius = 2.5f;       // burn radius of each patch
        private const float PatchLifetime = 3f;       // how long a patch lingers
        private const float PatchTickInterval = 0.5f; // seconds between burn ticks within a patch
        private const string VfxSourcePrefab = "vfx_FireAddFuel"; // vanilla prefab we clone
        private const string VfxClonePrefab = "EL_TrailblazerFire";  // our registered clone
        private static GameObject _vfxContainer; // disabled parent that keeps the template from Awaking
        private static GameObject _vfxTemplate;  // our registered clone (activeSelf == true)
        private static bool _sourceMissingLogged;
        private static bool _vfxMissingLogged;

        private static float _spawnTimer;

        public static void RegisterVfxPrefab() {
            var zns = ZNetScene.instance;
            if (zns == null || zns.GetPrefab(VfxClonePrefab) != null) {
                return;
            }

            var template = GetOrBuildTemplate(zns);
            if (template == null) {
                return;
            }

            if (!zns.m_nonNetViewPrefabs.Contains(template)) {
                zns.m_nonNetViewPrefabs.Add(template);
            }
            zns.m_namedPrefabs[VfxClonePrefab.GetStableHashCode()] = template;
            EpicLoot.Log($"Trailblazer: registered '{VfxClonePrefab}' with ZNetScene.");
        }

        private static GameObject GetOrBuildTemplate(ZNetScene zns) {
            if (_vfxTemplate != null) {
                return _vfxTemplate;
            }

            var source = zns.GetPrefab(VfxSourcePrefab);
            if (source == null) {
                if (!_sourceMissingLogged) {
                    EpicLoot.LogWarning($"Trailblazer: could not find '{VfxSourcePrefab}' prefab; fire trail will not display.");
                    _sourceMissingLogged = true;
                }
                return null;
            }

            if (_vfxContainer == null) {
                _vfxContainer = new GameObject("EL_TrailblazerFireContainer");
                _vfxContainer.SetActive(false);
                Object.DontDestroyOnLoad(_vfxContainer);
            }

            // Cloning under the disabled container keeps the template inactive-in-hierarchy (so nothing
            // Awakes) while preserving activeSelf == true for the eventual instances.
            var template = Object.Instantiate(source, _vfxContainer.transform);
            template.name = VfxClonePrefab;

            // A trail of smoke columns reads as smoldering, not burning -- drop the smoke entirely.
            var smoke = template.transform.Find("smoke");
            if (smoke != null) {
                Object.DestroyImmediate(smoke.gameObject);
            } else {
                EpicLoot.LogWarning($"Trailblazer: '{VfxSourcePrefab}' has no 'smoke' child; skipping removal.");
            }

            // The fire child only bursts on spawn; add a steady trickle so the patch visibly burns for its
            // whole lifetime, and stretch the (never-playing) template's emission window to cover it
            // without enabling looping (which would re-fire the burst).
            var fire = template.transform.Find("fire");
            var firePs = fire != null ? fire.GetComponent<ParticleSystem>() : null;
            if (firePs != null) {
                var emission = firePs.emission;
                emission.rateOverTime = 2f;

                var main = firePs.main;
                if (main.duration < PatchLifetime) {
                    main.duration = PatchLifetime;
                }
            } else {
                EpicLoot.LogWarning($"Trailblazer: '{VfxSourcePrefab}' has no 'fire' particle system; skipping emission tweak.");
            }

            // The patch's damage driver; inert (no owner, no damage) until Init is called on an instance.
            template.AddComponent<TrailblazerFire>();

            // The vanilla vfx already tears itself down with a TimedDestruction; align it with the patch
            // lifetime so the visual and the burn expire together.
            var timed = template.GetComponent<TimedDestruction>() ?? template.AddComponent<TimedDestruction>();
            timed.m_timeout = PatchLifetime;
            timed.m_triggerOnAwake = true;

            _vfxTemplate = template;
            return _vfxTemplate;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Update_Patch {
            [UsedImplicitly]
            private static void Postfix(Player __instance) {
                if (__instance != Player.m_localPlayer) {
                    return;
                }

                _spawnTimer -= Time.deltaTime;

                if (__instance.IsDead() ||
                    !__instance.HasActiveMagicEffect(MagicEffectType.Trailblazer, out var tickDamage) ||
                    tickDamage <= 0f) {
                    return;
                }

                // Only lay a trail while actually moving along the ground.
                var velocity = __instance.GetVelocity();
                velocity.y = 0f;
                if (velocity.magnitude < MinMoveSpeed || !__instance.IsOnGround()) {
                    return;
                }

                if (_spawnTimer > 0f) {
                    return;
                }
                _spawnTimer = SpawnInterval;

                var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(VfxClonePrefab) : null;
                if (prefab == null) {
                    if (!_vfxMissingLogged) {
                        EpicLoot.LogWarning($"Trailblazer: '{VfxClonePrefab}' prefab not registered; fire trail will not display.");
                        _vfxMissingLogged = true;
                    }
                    return;
                }

                var go = Object.Instantiate(prefab, __instance.transform.position, Quaternion.identity);
                go.GetComponent<TrailblazerFire>()
                    .Init(__instance, tickDamage, PatchRadius, PatchLifetime, PatchTickInterval);
            }
        }
    }

    // A single burning patch of the Trailblazer trail. It sits where it was dropped and burns enemies inside
    // its radius on an interval; the prefab's TimedDestruction removes it when its lifetime runs out.
    public class TrailblazerFire : MonoBehaviour {
        private Player _owner;
        private float _tickDamage;
        private float _radius;
        private float _lifetime;
        private float _lifeLeft;
        private float _tickInterval;
        private float _tickTimer;
        private Light _glow;
        private readonly List<Character> _inRange = new List<Character>();

        public void Init(Player owner, float tickDamage, float radius, float lifetime, float tickInterval) {
            _owner = owner;
            _tickDamage = tickDamage;
            _radius = radius;
            _lifetime = lifetime;
            _lifeLeft = lifetime;
            _tickInterval = tickInterval;

            _glow = gameObject.AddComponent<Light>();
            _glow.type = LightType.Point;
            _glow.color = new Color(1f, 0.5f, 0.15f);
            _glow.range = radius * 2f;
            _glow.shadows = LightShadows.None;
        }

        [UsedImplicitly]
        private void Update() {
            var dt = Time.deltaTime;
            _lifeLeft -= dt;

            if (_glow != null) {
                // Fade and flicker the glow as the patch burns down.
                _glow.intensity = Mathf.Clamp01(_lifeLeft / _lifetime) * (1.4f + Random.Range(-0.15f, 0.15f));
            }

            // No burn without an owner: covers the owner despawning mid-patch and un-Init'd instances
            // (e.g. a console-spawned EL_TrailblazerFire), which stay purely visual.
            if (_owner == null || _tickDamage <= 0f) {
                return;
            }

            _tickTimer -= dt;
            if (_tickTimer > 0f) {
                return;
            }
            _tickTimer = _tickInterval;

            _inRange.Clear();
            Character.GetCharactersInRange(transform.position, _radius, _inRange);
            foreach (var character in _inRange) {
                if (character == null || character.IsDead() || character.IsPlayer() || character.IsTamed()) {
                    continue;
                }

                var hit = new HitData();
                hit.m_point = character.transform.position;
                hit.m_dir = (character.transform.position - transform.position).normalized;
                hit.m_damage.m_fire = _tickDamage;
                hit.m_hitType = HitData.HitType.Burning;
                hit.SetAttacker(_owner);
                character.Damage(hit);
            }
        }
    }
}
