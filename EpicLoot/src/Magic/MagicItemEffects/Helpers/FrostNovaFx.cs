using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Shared ice-nova visual, built from the vanilla fenring nova. Each effect that wants the nova registers
    // its own named variant (with an optional playback speed) at ZNetScene setup, then spawns it by name.
    //
    // We clone the vanilla prefab and trim it rather than instantiating the shared prefab directly: the
    // original runs its emission bursts three cycles with staggered SFX, which is far too long for a shard
    // proc, and mutating the shared prefab would change the fenring's own nova.
    //
    // The clone keeps the vanilla ZNetView, ZSyncTransform and TimedDestruction, so a spawn is a networked
    // object: its ZDO carries the variant's name as the prefab hash, nearby clients build their own copy from
    // the registered template, and the owner's TimedDestruction removes it for everyone. That only works if
    // every client has the variant registered before the ZDO arrives, which is why the templates are built
    // and injected into ZNetScene each world load (RegisterPrefab) instead of lazily on the first proc.
    public static class FrostNovaFx {
        private const string NovaFx = "fx_fenring_icenova";
        private const float SfxDelayReduction = 1.2f;   // trim from each SFX's trigger delay

        private static readonly Dictionary<string, GameObject> Templates = new Dictionary<string, GameObject>();
        private static GameObject _container;   // disabled parent that keeps the templates from Awaking
        private static bool _novaMissingLogged;
        private static readonly HashSet<string> UnregisteredLogged = new HashSet<string>();

        // Builds (once) and injects into ZNetScene the named nova variant. speedMultiplier > 1 plays the whole
        // effect faster (1.5 = 50% quicker). Hook from PrefabManager.OnPrefabsRegistered, which fires as a
        // ZNetScene.Awake postfix on every client and the server each world load. Idempotent; a name that is
        // registered twice keeps the speed it was first built with.
        public static void RegisterPrefab(string templateName, float speedMultiplier = 1f) {
            var zns = ZNetScene.instance;
            if (zns == null || zns.GetPrefab(templateName) != null) {
                return;
            }

            var template = GetOrBuildTemplate(zns, templateName, speedMultiplier);
            if (template == null) {
                return;
            }

            if (!zns.m_prefabs.Contains(template)) {
                zns.m_prefabs.Add(template);
            }
            zns.m_namedPrefabs[templateName.GetStableHashCode()] = template;
        }

        // Spawns a fresh networked copy of the registered nova variant at the position. Call it on one client
        // only (the player whose effect procced); the ZDO shows it to everyone else.
        public static void Spawn(string templateName, Vector3 position) {
            var prefab = ZNetScene.instance?.GetPrefab(templateName);
            if (prefab == null) {
                if (UnregisteredLogged.Add(templateName)) {
                    EpicLoot.LogWarning($"FrostNovaFx: '{templateName}' prefab not registered; frost nova visual will not display.");
                }
                return;
            }

            Object.Instantiate(prefab, position, Quaternion.identity);
        }

        // Builds and caches a shortened copy of the fenring ice nova under the given name. The cache outlives
        // the world, so later world loads only re-inject it. A missing source is logged once and leaves the
        // template unbuilt.
        private static GameObject GetOrBuildTemplate(ZNetScene zns, string templateName, float speedMultiplier) {
            if (Templates.TryGetValue(templateName, out var cached) && cached != null) {
                return cached;
            }

            var source = zns.GetPrefab(NovaFx);
            if (source == null) {
                if (!_novaMissingLogged) {
                    EpicLoot.LogWarning($"FrostNovaFx: could not find '{NovaFx}' prefab; frost nova visual will not display.");
                    _novaMissingLogged = true;
                }
                return null;
            }

            if (_container == null) {
                _container = new GameObject("EL_FrostNovaContainer");
                _container.SetActive(false);
                Object.DontDestroyOnLoad(_container);
            }

            // Cloning under the disabled container keeps the template inactive-in-hierarchy, so no component
            // (ZNetView, particle systems, ZSFX) wakes up on it, while activeSelf stays true. That matters for
            // remote clients: ZNetScene.CreateObject instantiates the registered prefab and never calls
            // SetActive, so an inactive template would show them nothing.
            var template = Object.Instantiate(source, _container.transform);
            template.name = templateName;

            TrimParticleSystems(template);
            TrimSfx(template);

            if (!Mathf.Approximately(speedMultiplier, 1f) && speedMultiplier > 0f) {
                ApplySpeed(template, speedMultiplier);
            }

            Templates[templateName] = template;
            return template;
        }

        // Every particle system on the nova runs its emission bursts three cycles; collapse each to a single
        // cycle and clear the start delay so our copy fires once, immediately.
        private static void TrimParticleSystems(GameObject root) {
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true)) {
                var main = ps.main;
                main.startDelay = 0f;

                var emission = ps.emission;
                var burstCount = emission.burstCount;
                if (burstCount <= 0) {
                    continue;
                }

                var bursts = new ParticleSystem.Burst[burstCount];
                emission.GetBursts(bursts);
                for (var i = 0; i < bursts.Length; i++) {
                    bursts[i].cycleCount = 1;
                }
                emission.SetBursts(bursts);
            }
        }

        // The nova carries three ZSFX sources, each staggered to line up with the original three-cycle visual.
        // Keep only the first and pull SfxDelayReduction seconds off its trigger delay so the audio tracks the
        // shortened FX (clamped at 0 so nothing ends up with a negative delay). DestroyImmediate so the extra
        // sources are gone before the template is ever instantiated.
        private static void TrimSfx(GameObject root) {
            bool found = false;
            foreach (var sfx in root.GetComponentsInChildren<ZSFX>(true)) {
                if (found) {
                    Object.DestroyImmediate(sfx.gameObject);
                    continue;
                }
                sfx.m_minDelay = Mathf.Max(0f, sfx.m_minDelay - SfxDelayReduction);
                sfx.m_maxDelay = Mathf.Max(0f, sfx.m_maxDelay - SfxDelayReduction);
                found = true;
            }
        }

        // Plays the whole effect faster: the particles simulate at the multiplied rate and the surviving SFX
        // delay shrinks by the same factor so the audio still lands with the visual.
        private static void ApplySpeed(GameObject root, float speedMultiplier) {
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true)) {
                var main = ps.main;
                main.simulationSpeed *= speedMultiplier;
            }

            foreach (var sfx in root.GetComponentsInChildren<ZSFX>(true)) {
                sfx.m_minDelay /= speedMultiplier;
                sfx.m_maxDelay /= speedMultiplier;
            }
        }
    }
}
