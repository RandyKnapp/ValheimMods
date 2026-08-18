using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers {
    // Shared ice-nova visual, built from the vanilla fenring nova. Effects that want the nova ask for it by
    // template name and (optionally) a playback speed; each distinct name gets its own cached, trimmed clone.
    //
    // We clone the vanilla prefab and trim it rather than instantiating the shared prefab directly: the
    // original runs its emission bursts three cycles with staggered SFX, which is far too long for a shard
    // proc, and mutating the shared prefab would change the fenring's own nova. The clone is built while the
    // source is deactivated so none of its components (particle systems, ZSFX) Awake on the template, then
    // kept inactive across scene loads as a reusable template.
    public static class FrostNovaFx {
        private const string NovaFx = "fx_fenring_icenova";
        private const float SfxDelayReduction = 1.2f;   // trim from each SFX's trigger delay

        private static readonly Dictionary<string, GameObject> Templates = new Dictionary<string, GameObject>();
        private static bool _novaMissingLogged;

        // Spawns a fresh copy of the named nova template at the position. speedMultiplier > 1 plays the whole
        // effect faster (1.5 = 50% quicker). The template is built inactive, so the instance starts inactive
        // too and only begins playing once we activate it.
        public static void Spawn(string templateName, Vector3 position, float speedMultiplier = 1f) {
            var template = GetOrCreateTemplate(templateName, speedMultiplier);
            if (template == null) {
                return;
            }

            var instance = Object.Instantiate(template, position, Quaternion.identity);
            instance.SetActive(true);
        }

        // Lazily builds (and caches) a shortened, standalone copy of the fenring ice nova under the given
        // name. Runs on a proc, so ZNetScene is loaded and the source prefab is available; a null source is
        // logged once and leaves the template unbuilt.
        private static GameObject GetOrCreateTemplate(string templateName, float speedMultiplier) {
            if (Templates.TryGetValue(templateName, out var cached) && cached != null) {
                return cached;
            }

            var source = ZNetScene.instance?.GetPrefab(NovaFx);
            if (source == null) {
                if (!_novaMissingLogged) {
                    EpicLoot.LogWarning($"FrostNovaFx: could not find '{NovaFx}' prefab; frost nova visual will not display.");
                    _novaMissingLogged = true;
                }
                return null;
            }

            // Deactivate the source across the clone so no component (particle systems, ZSFX) wakes up on the
            // template; restore the source afterwards so the shared prefab is left exactly as it was.
            var wasActive = source.activeSelf;
            source.SetActive(false);
            var template = Object.Instantiate(source);
            source.SetActive(wasActive);

            template.name = templateName;
            Object.DontDestroyOnLoad(template);

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
        // shortened FX (clamped at 0 so nothing ends up with a negative delay).
        private static void TrimSfx(GameObject root) {
            bool found = false;
            foreach (var sfx in root.GetComponentsInChildren<ZSFX>(true)) {
                if (found) {
                    Object.Destroy(sfx.gameObject);
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
