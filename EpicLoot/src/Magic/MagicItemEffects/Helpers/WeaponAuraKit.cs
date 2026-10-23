using Jotunn.Managers;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    // The parts every code-built weapon aura shares (Eitr Infusion's, the set weapon glows, Sniper's charge): finding
    // vanilla particle systems, copying one system out of a prefab on its own, and fitting the copies to the weapon in
    // hand.
    //
    // An aura template is a root holding one "Frame" child, under which sit the particle systems and a light. Every player
    // weapon carries vanilla's upgrade glow (ParticleIntensityScaler), whose particles emit from the weapon's own mesh,
    // placed by hand for each weapon; AttachFitted gives the aura's emitters that mesh and placement, so they cover the
    // weapon whatever its shape. A skinned weapon (dual axes, dual knives, claws: one mesh bound to the wearer's bones,
    // with no upgrade glow in hand) emits from that skinned mesh, so both blades are covered as they move. Any other
    // weapon without a glow emits from the bounds of its largest mesh, and one with no mesh at all from a small sphere at
    // the hand holding it.
    internal static class WeaponAuraKit
    {
        private const float FallbackRadius = 0.15f;    // metres, at the hand of a weapon with no mesh to emit from

        // Copied particles must be gone before an AuraFade ends (it waits this long plus a margin), or they pop.
        internal const float MaxAuraLifetime = 2.5f;

        // Copied from: the upgrade glow, and an item's dropped-item sparkle (the star glints).
        private static readonly string[] GlowSourceItems = { "SwordIron", "SwordBronze", "SwordSilver" };

        private static readonly Dictionary<int, ItemDrop.ItemData.ItemType> ItemTypes = new Dictionary<int, ItemDrop.ItemData.ItemType>();
        private static readonly HashSet<string> MissingLogged = new HashSet<string>();

        // ZNetScene first; then ObjectDB, which holds items ZNetScene may not (another mod's late registrations); then
        // Jotunn's cache of every loaded asset, for effect prefabs that only exist inside something else.
        internal static GameObject FindPrefab(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null && ObjectDB.instance != null)
            {
                prefab = ObjectDB.instance.GetItemPrefab(name);
            }
            if (prefab == null)
            {
                prefab = PrefabManager.Cache.GetPrefab<GameObject>(name);
            }
            return prefab;
        }

        // "Prefab" or "Prefab/child/path": the prefab's root, or the child at that path. Logs a missing one once, under
        // the caller's tag.
        internal static Transform FindSource(string path, string logTag)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            int slash = path.IndexOf('/');
            GameObject prefab = FindPrefab(slash > 0 ? path.Substring(0, slash) : path);
            Transform found = prefab == null ? null : slash > 0 ? prefab.transform.Find(path.Substring(slash + 1)) : prefab.transform;
            if (found == null && MissingLogged.Add(path))
            {
                EpicLoot.LogWarning($"[{logTag}] could not find '{path}'; showing the effect without it.");
            }
            return found;
        }

        internal static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }

                Transform found = FindDeep(child, name);
                if (found != null)
                {
                    return found;
                }
            }
            return null;
        }

        // The upgrade glow's particle system, from the first vanilla sword that has one.
        internal static ParticleSystem FindGlowSource()
        {
            foreach (string name in GlowSourceItems)
            {
                GameObject prefab = FindPrefab(name);
                ParticleIntensityScaler scaler = prefab != null ? prefab.GetComponentInChildren<ParticleIntensityScaler>(true) : null;
                ParticleSystem glow = scaler != null ? scaler.GetComponentInChildren<ParticleSystem>(true) : null;
                if (glow != null)
                {
                    return glow;
                }
            }
            return null;
        }

        // The star sparkle every dropped item shows, which is the system on an item prefab's own root.
        internal static ParticleSystem FindSparkleSource()
        {
            foreach (string name in GlowSourceItems)
            {
                GameObject prefab = FindPrefab(name);
                ParticleSystem sparkle = prefab != null ? prefab.GetComponent<ParticleSystem>() : null;
                if (sparkle != null)
                {
                    return sparkle;
                }
            }
            return null;
        }

        // One system on its own: its children are copied separately or not at all, and everything that isn't the
        // particles themselves (sounds, lights, net views, the upgrade glow's scaler) stays behind. Behaviours go first,
        // since some of them require the other components.
        internal static ParticleSystem CopySystem(ParticleSystem source, Transform parent, string name)
        {
            GameObject copy = Object.Instantiate(source.gameObject, parent, false);
            copy.name = name;
            copy.SetActive(true);
            for (int i = copy.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(copy.transform.GetChild(i).gameObject);
            }
            foreach (MonoBehaviour behaviour in copy.GetComponents<MonoBehaviour>())
            {
                Object.DestroyImmediate(behaviour);
            }
            foreach (Component component in copy.GetComponents<Component>())
            {
                if (!(component is Transform) && !(component is ParticleSystem) && !(component is ParticleSystemRenderer))
                {
                    Object.DestroyImmediate(component);
                }
            }

            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one;

            ParticleSystem system = copy.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = true;
            main.prewarm = false;
            main.stopAction = ParticleSystemStopAction.None;
            // The emitter takes the weapon's scale, the particles keep their own size.
            main.scalingMode = ParticleSystemScalingMode.Shape;
            if (main.simulationSpace == ParticleSystemSimulationSpace.Custom)
            {
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }

            ParticleSystem.ExternalForcesModule forces = system.externalForces;
            forces.enabled = false;
            ParticleSystem.SubEmittersModule subEmitters = system.subEmitters;
            subEmitters.enabled = false;
            ParticleSystem.CollisionModule collision = system.collision;
            collision.enabled = false;
            ParticleSystem.TriggerModule trigger = system.trigger;
            trigger.enabled = false;
            ParticleSystem.LightsModule lights = system.lights;
            lights.enabled = false;
            return system;
        }

        // Every system under branch, each copied on its own into parent and sized by scale. A prefab root's own system
        // is a dropped item's sparkle or an empty carrier rather than the effect, so it is left out unless includeRoot.
        // Lifetimes are capped, unless told otherwise, so an AuraFade never cuts particles off.
        internal static List<ParticleSystem> CopyBranch(Transform branch, Transform parent, float scale, bool includeRoot,
            bool clampLifetime = true)
        {
            var copies = new List<ParticleSystem>();
            if (branch == null)
            {
                return copies;
            }

            Transform prefabRoot = branch.root;
            foreach (ParticleSystem system in branch.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (system.transform == prefabRoot && !includeRoot || !system.gameObject.activeSelf)
                {
                    continue;
                }

                ParticleSystem copy = CopySystem(system, parent, system.name);
                ScaleSystem(copy, scale);
                if (clampLifetime)
                {
                    ClampLifetime(copy, MaxAuraLifetime);
                }
                copies.Add(copy);
            }
            return copies;
        }

        // Size and speed, as scaling the source's transform would.
        internal static void ScaleSystem(ParticleSystem system, float scale)
        {
            if (Mathf.Approximately(scale, 1f))
            {
                return;
            }

            ParticleSystem.MainModule main = system.main;
            if (main.startSize3D)
            {
                main.startSizeX = Scaled(main.startSizeX, scale);
                main.startSizeY = Scaled(main.startSizeY, scale);
                main.startSizeZ = Scaled(main.startSizeZ, scale);
            }
            else
            {
                main.startSize = Scaled(main.startSize, scale);
            }
            main.startSpeed = Scaled(main.startSpeed, scale);
        }

        internal static ParticleSystem.MinMaxCurve Scaled(ParticleSystem.MinMaxCurve curve, float scale)
        {
            switch (curve.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    curve.constant *= scale;
                    break;
                case ParticleSystemCurveMode.TwoConstants:
                    curve.constantMin *= scale;
                    curve.constantMax *= scale;
                    break;
                default:
                    curve.curveMultiplier *= scale;
                    break;
            }
            return curve;
        }

        internal static float MaxLifetime(ParticleSystem system)
        {
            ParticleSystem.MinMaxCurve lifetime = system.main.startLifetime;
            switch (lifetime.mode)
            {
                case ParticleSystemCurveMode.Constant:
                    return lifetime.constant;
                case ParticleSystemCurveMode.TwoConstants:
                    return lifetime.constantMax;
                default:
                    return lifetime.curveMultiplier;
            }
        }

        // A particle made to last as long as its effect (Dyrnwyn's flare) is left as it is: scaling an infinite lifetime
        // gives no number at all. AuraFade clears it instead.
        internal static void ClampLifetime(ParticleSystem system, float max)
        {
            float longest = MaxLifetime(system);
            if (longest > max && longest > 0f && !float.IsInfinity(longest))
            {
                ParticleSystem.MainModule main = system.main;
                main.startLifetime = Scaled(main.startLifetime, max / longest);
            }
        }

        // Each particle starts in a colour between color and pale; whatever colouring the source had over its life is
        // replaced with a plain fade in and out, so opacity comes from the start colour alone.
        internal static void Tint(ParticleSystem system, Color color, Color pale)
        {
            ParticleSystem.MainModule main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, pale);
            SetFadeInOut(system);
        }

        // The upgrade glow fades each particle in and out to an alpha of 0.03 at most, which only reads because vanilla
        // stacks around 150 metre-wide sprites; with that left in, a copy sized for an aura was all but invisible.
        internal static void SetFadeInOut(ParticleSystem system)
        {
            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f),
                        new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);
        }

        // A steady rate in place of the source's, with room for every particle it makes.
        internal static void SetRate(ParticleSystem system, float perSecond)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = perSecond;
            emission.rateOverDistance = 0f;
            emission.burstCount = 0;
            ParticleSystem.MainModule main = system.main;
            main.maxParticles = Mathf.Max(main.maxParticles, Mathf.CeilToInt(perSecond * Mathf.Max(0.1f, MaxLifetime(system)) * 1.5f) + 1);
        }

        // The upgrade glow's particles, recoloured and sized for an aura. A speed pushes them out from the weapon's
        // surface (the mesh shape emits along its normals); a negative one keeps the source's.
        internal static ParticleSystem AddGlow(ParticleSystem source, Transform frame, Color color, float alpha,
            float minSize, float maxSize, float rate, float speed = -1f)
        {
            ParticleSystem glow = CopySystem(source, frame, "Glow");
            ParticleSystem.MainModule main = glow.main;
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            if (speed >= 0f)
            {
                main.startSpeed = speed;
            }
            Color pale = Color.Lerp(color, Color.white, 0.35f);
            color.a = alpha;
            pale.a = alpha;
            Tint(glow, color, pale);
            SetRate(glow, rate);
            ClampLifetime(glow, MaxAuraLifetime);
            return glow;
        }

        // Star glints over the weapon, from an item's dropped-item sparkle.
        internal static ParticleSystem AddGlints(ParticleSystem source, Transform frame, Color color, float rate)
        {
            ParticleSystem glints = CopySystem(source, frame, "Glints");
            ParticleSystem.MainModule main = glints.main;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.2f);
            main.startSpeed = 0f;
            Tint(glints, color, Color.Lerp(color, Color.white, 0.5f));
            SetRate(glints, rate);
            return glints;
        }

        internal static Light AddLight(Transform frame, Color color, float intensity, float range)
        {
            var lightObject = new GameObject("Light");
            lightObject.transform.SetParent(frame, false);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
            return light;
        }

        // A template's root and frame, under an inactive holder so nothing in it wakes before it is fitted.
        internal static Transform NewTemplate(Transform holder, string name, out GameObject root)
        {
            root = new GameObject(name);
            root.transform.SetParent(holder, false);
            Transform frame = new GameObject("Frame").transform;
            frame.SetParent(root.transform, false);
            return frame;
        }

        // The model of the weapon in hand, and the hand holding it: the right hand's item, else the left's, counting
        // only weapons, so food being eaten (shown in the right hand while a bow stays in the left), a torch or a shield
        // never takes the aura. A hand whose model is mid-replacement (hash -1) counts as empty until the new one is in.
        internal static GameObject GetWeaponModel(VisEquipment vis, out Transform hand)
        {
            hand = null;
            if (vis == null)
            {
                return null;
            }

            if (vis.m_rightItemInstance != null && IsWeapon(vis.m_currentRightItemHash))
            {
                hand = vis.m_rightHand;
                return vis.m_rightItemInstance;
            }
            if (vis.m_leftItemInstance != null && IsWeapon(vis.m_currentLeftItemHash))
            {
                hand = vis.m_leftHand;
                return vis.m_leftItemInstance;
            }
            return null;
        }

        // The model of a weapon stowed on the back (Humanoid.HideHandItems), right before left as in the hands, and the
        // back joint it hangs from. Vanilla syncs the back items, so every client has the model.
        internal static GameObject GetStowedWeaponModel(VisEquipment vis, out Transform joint)
        {
            joint = null;
            if (vis == null)
            {
                return null;
            }

            GameObject weapon = null;
            if (vis.m_rightBackItemInstance != null && IsWeapon(vis.m_currentRightBackItemHash))
            {
                weapon = vis.m_rightBackItemInstance;
            }
            else if (vis.m_leftBackItemInstance != null && IsWeapon(vis.m_currentLeftBackItemHash))
            {
                weapon = vis.m_leftBackItemInstance;
            }

            joint = weapon != null ? weapon.transform.parent : null;
            return weapon;
        }

        private static bool IsWeapon(int itemHash)
        {
            if (itemHash == 0 || itemHash == -1)
            {
                return false;
            }

            if (!ItemTypes.TryGetValue(itemHash, out ItemDrop.ItemData.ItemType type))
            {
                GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(itemHash) : null;
                ItemDrop itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (itemDrop == null)
                {
                    return false;   // ObjectDB not ready yet; asked again next frame
                }

                type = itemDrop.m_itemData.m_shared.m_itemType;
                ItemTypes[itemHash] = type;
            }

            switch (type)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                    return true;
                default:
                    return false;
            }
        }

        // A copy of template fitted to weapon and switched on. The template is a root with a Frame child (NewTemplate);
        // the light, if any, goes to the middle of the weapon.
        internal static GameObject AttachFitted(GameObject template, GameObject weapon, Transform hand)
        {
            return AttachFitted(template, FindFit(weapon, hand));
        }

        // A copy of template around a bare hand (a set's hand glow): a small sphere at the hand's attach point.
        internal static GameObject AttachToHand(GameObject template, Transform hand)
        {
            return AttachFitted(template, new Fit { Parent = hand, Type = ParticleSystemShapeType.Sphere, Scale = Vector3.one });
        }

        internal static GameObject AttachFitted(GameObject template, Fit fit)
        {
            GameObject aura = Object.Instantiate(template, fit.Parent, false);
            aura.name = template.name;
            Transform root = aura.transform;
            Transform frame = root.GetChild(0);
            if (fit.RootPose != null)
            {
                root.localPosition = fit.RootPose.localPosition;
                root.localRotation = fit.RootPose.localRotation;
                root.localScale = fit.RootPose.localScale;
            }
            if (fit.FramePose != null)
            {
                if (fit.FramePose.parent == fit.RootPose)
                {
                    frame.localPosition = fit.FramePose.localPosition;
                    frame.localRotation = fit.FramePose.localRotation;
                    frame.localScale = fit.FramePose.localScale;
                }
                else
                {
                    frame.SetPositionAndRotation(fit.FramePose.position, fit.FramePose.rotation);
                    frame.localScale = Divide(fit.FramePose.lossyScale, root.lossyScale);
                }
            }

            foreach (ParticleSystem system in aura.GetComponentsInChildren<ParticleSystem>(true))
            {
                fit.Apply(system);
            }
            foreach (Light light in aura.GetComponentsInChildren<Light>(true))
            {
                light.transform.localPosition = fit.Center;
            }

            aura.SetActive(true);
            return aura;
        }

        // The middle of a fitted aura's weapon, in world space.
        internal static Vector3 GetCenter(GameObject aura)
        {
            Transform frame = aura.transform.childCount > 0 ? aura.transform.GetChild(0) : aura.transform;
            Light light = aura.GetComponentInChildren<Light>(true);
            return light != null ? light.transform.position : frame.position;
        }

        // Where an aura hangs and what its emitters emit from.
        internal struct Fit
        {
            public Transform Parent;
            public Transform RootPose;                 // the aura root copies this one's local pose, when set
            public Transform FramePose;                // the frame copies this one's pose, when set
            public ParticleSystemShapeType Type;
            public Mesh Mesh;
            public SkinnedMeshRenderer Skin;
            public ParticleSystemMeshShapeType MeshShapeType;
            public Vector3 Position;
            public Vector3 Rotation;
            public Vector3 Scale;
            public Vector3 Center;                     // the light's place in the frame
            public bool WorldSpace;                    // particles stay where they were made rather than follow the aura

            public void Apply(ParticleSystem system)
            {
                ParticleSystem.ShapeModule shape = system.shape;
                shape.enabled = true;
                shape.shapeType = Type;
                shape.position = Position;
                shape.rotation = Rotation;
                shape.scale = Scale;
                if (Type == ParticleSystemShapeType.Mesh)
                {
                    shape.mesh = Mesh;
                    shape.meshShapeType = MeshShapeType;
                    shape.useMeshColors = false;
                }
                else if (Type == ParticleSystemShapeType.SkinnedMeshRenderer)
                {
                    shape.skinnedMeshRenderer = Skin;
                    shape.meshShapeType = MeshShapeType;
                    shape.useMeshColors = false;
                }
                else if (Type == ParticleSystemShapeType.Sphere)
                {
                    shape.radius = FallbackRadius;
                }

                if (WorldSpace)
                {
                    ParticleSystem.MainModule main = system.main;
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    // A particle made to last as long as the aura would hang where it was made instead of on the weapon.
                    if (float.IsInfinity(MaxLifetime(system)))
                    {
                        ParticleSystem.EmissionModule emission = system.emission;
                        emission.enabled = false;
                    }
                }
            }
        }

        // The upgrade glow's own mesh and placement when the weapon has one, else the skinned mesh of a skinned weapon,
        // else the box of its longest mesh, else a small sphere at the hand.
        internal static Fit FindFit(GameObject weapon, Transform hand)
        {
            ParticleIntensityScaler scaler = weapon.GetComponentInChildren<ParticleIntensityScaler>(true);
            ParticleSystem glow = scaler != null ? scaler.GetComponentInChildren<ParticleSystem>(true) : null;
            if (glow != null)
            {
                ParticleSystem.ShapeModule shape = glow.shape;
                Mesh mesh = shape.mesh;
                if (shape.shapeType == ParticleSystemShapeType.Mesh && mesh != null)
                {
                    // Beside the glow's object rather than inside it: its scaler rescans its children each time it is
                    // enabled, and would turn the aura down to the weapon's own (usually zero) glow.
                    return new Fit
                    {
                        Parent = scaler.transform.parent != null ? scaler.transform.parent : weapon.transform,
                        RootPose = scaler.transform,
                        FramePose = glow.transform,
                        Type = ParticleSystemShapeType.Mesh,
                        Mesh = mesh,
                        MeshShapeType = shape.meshShapeType,
                        Position = shape.position,
                        Rotation = shape.rotation,
                        Scale = shape.scale,
                        Center = shape.position + Quaternion.Euler(shape.rotation) * Vector3.Scale(mesh.bounds.center, shape.scale),
                    };
                }
            }

            // Vanilla parents a held model to the hand, except a skinned one, which hangs from the body root (at the feet)
            // and is moved by the wearer's own bones. Its particles are emitted from the skinned mesh where it is now, and
            // live in the world, since nothing the aura could hang from follows both hands; the light goes to the hand.
            SkinnedMeshRenderer skin = hand != null && weapon.transform.parent != hand ? FindLargestSkin(weapon) : null;
            if (skin != null)
            {
                return new Fit
                {
                    Parent = hand,
                    Type = ParticleSystemShapeType.SkinnedMeshRenderer,
                    Skin = skin,
                    MeshShapeType = ParticleSystemMeshShapeType.Triangle,
                    Scale = Vector3.one,
                    WorldSpace = true,
                };
            }

            MeshFilter best = null;
            float bestLength = 0f;
            foreach (MeshFilter filter in weapon.GetComponentsInChildren<MeshFilter>())
            {
                Mesh mesh = filter.sharedMesh;
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                if (mesh == null || renderer == null || !renderer.enabled)
                {
                    continue;
                }

                float length = Vector3.Scale(mesh.bounds.size, filter.transform.lossyScale).magnitude;
                if (length > bestLength)
                {
                    best = filter;
                    bestLength = length;
                }
            }

            if (best != null)
            {
                Bounds bounds = best.sharedMesh.bounds;
                return new Fit
                {
                    Parent = best.transform,
                    Type = ParticleSystemShapeType.Box,
                    Position = bounds.center,
                    Scale = bounds.size,
                    Center = bounds.center,
                };
            }

            return new Fit
            {
                Parent = hand != null ? hand : weapon.transform,
                Type = ParticleSystemShapeType.Sphere,
                Scale = Vector3.one
            };
        }

        private static SkinnedMeshRenderer FindLargestSkin(GameObject weapon)
        {
            SkinnedMeshRenderer best = null;
            float bestSize = 0f;
            foreach (SkinnedMeshRenderer skin in weapon.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (skin.sharedMesh == null || !skin.enabled)
                {
                    continue;
                }

                float size = skin.sharedMesh.bounds.size.sqrMagnitude;
                if (size > bestSize)
                {
                    best = skin;
                    bestSize = size;
                }
            }
            return best;
        }

        internal static Vector3 Divide(Vector3 a, Vector3 b)
        {
            return new Vector3(Divide(a.x, b.x), Divide(a.y, b.y), Divide(a.z, b.z));
        }

        private static float Divide(float a, float b)
        {
            return Mathf.Abs(b) > 1e-5f ? a / b : a;
        }
    }

    // An aura that is going away (its weapon changed, its effect or set ended): it makes no new particles, its lights
    // dim, and it is removed once the last particles are gone.
    public class AuraFade : MonoBehaviour
    {
        private const float LightFadeSeconds = 0.5f;

        private Light[] _lights;
        private float[] _intensities;
        private float _started;

        public void Begin(float duration)
        {
            _started = Time.time;
            _lights = GetComponentsInChildren<Light>();
            _intensities = new float[_lights.Length];
            for (int i = 0; i < _lights.Length; i++)
            {
                _intensities[i] = _lights[i].intensity;
            }

            foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>())
            {
                // Particles that would outlive the fade (Dyrnwyn's flare never dies) go now rather than pop at its end.
                system.Stop(false, WeaponAuraKit.MaxLifetime(system) > duration
                    ? ParticleSystemStopBehavior.StopEmittingAndClear
                    : ParticleSystemStopBehavior.StopEmitting);
            }

            // Scheduled rather than counted in Update, which a hidden weapon model would pause.
            Destroy(gameObject, duration);
        }

        private void Update()
        {
            if (_lights == null)
            {
                return;
            }

            float left = Mathf.Clamp01(1f - (Time.time - _started) / LightFadeSeconds);
            for (int i = 0; i < _lights.Length; i++)
            {
                if (_lights[i] != null)
                {
                    _lights[i].intensity = _intensities[i] * left;
                }
            }
        }
    }
}
