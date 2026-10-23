using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    // Local-only copies of vanilla fx prefabs, networked or not, seen by this client alone. Each source prefab is copied
    // once into a stripped template: built under an inactive root, so none of its Awakes run (no ZDO, no sound, no
    // particles), then its ZNetView and ZSyncTransform are removed, which makes its TimedDestruction fall back to a plain
    // Destroy. A spawn is then a single Instantiate of that template. Particle systems mostly simulate in Local scaling,
    // which ignores the root's scale, so the template a scaled spawn uses has them switched to Hierarchy.
    internal static class LocalFx
    {
        private static GameObject _root;
        private static readonly Dictionary<(GameObject, bool, bool), GameObject> Templates =
            new Dictionary<(GameObject, bool, bool), GameObject>();

        internal static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale = 1f,
            Transform parent = null, float lifetime = 10f, bool noCameraShake = false)
        {
            if (prefab == null)
            {
                return null;
            }

            bool scaled = !Mathf.Approximately(scale, 1f);
            GameObject template = GetTemplate(prefab, scaled, noCameraShake);
            GameObject fx = Object.Instantiate(template, position, rotation, parent);
            if (scaled)
            {
                fx.transform.localScale = template.transform.localScale * scale;
            }

            // A fallback, should the copy have no TimedDestruction of its own.
            if (lifetime > 0f)
            {
                Object.Destroy(fx, lifetime);
            }
            return fx;
        }

        // Builds the template ahead of the first spawn (see SetEffectWarmup).
        internal static void Prewarm(GameObject prefab, bool scaled, bool noCameraShake = false)
        {
            if (prefab != null)
            {
                GetTemplate(prefab, scaled, noCameraShake);
            }
        }

        private static GameObject GetTemplate(GameObject prefab, bool scaled, bool noCameraShake)
        {
            var key = (prefab, scaled, noCameraShake);
            if (Templates.TryGetValue(key, out GameObject template) && template != null)
            {
                return template;
            }

            if (_root == null)
            {
                _root = new GameObject("EL_LocalFxTemplates");
                _root.SetActive(false);
                Object.DontDestroyOnLoad(_root);
            }

            template = Object.Instantiate(prefab, _root.transform, false);
            template.name = prefab.name;

            foreach (var sync in template.GetComponentsInChildren<ZSyncTransform>(true))
            {
                Object.DestroyImmediate(sync);
            }
            foreach (var nview in template.GetComponentsInChildren<ZNetView>(true))
            {
                Object.DestroyImmediate(nview);
            }
            if (noCameraShake)
            {
                foreach (var shaker in template.GetComponentsInChildren<CamShaker>(true))
                {
                    Object.DestroyImmediate(shaker);
                }
            }
            if (scaled)
            {
                foreach (var particles in template.GetComponentsInChildren<ParticleSystem>(true))
                {
                    var main = particles.main;
                    main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                }
            }

            Templates[key] = template;
            return template;
        }
    }
}
