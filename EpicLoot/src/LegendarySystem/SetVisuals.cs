using EpicLoot.MagicItemEffects;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpicLoot.LegendarySystem
{
    // The code-built visuals of the full set a player wears, on every client: a glow fitted to the weapon in hand
    // (WeaponGlow), the same kind of glow on both hands (HandGlow), an arrow on the string while a bow is drawn
    // (DrawArrow, SetDrawArrow) and an aura around the wearer (BodyAura), all from the set's entry in legendaries.json.
    // The wearer's client publishes the set's ID to the player ZDO (SetLooks.Publish, on every equip change); each client
    // reads it a few times a second, so a remote wearer's change shows within a moment rather than at SetAura's 10 s
    // refresh, which this also calls on a change.
    //
    // A weapon stowed on the back (R) is unequipped, so it breaks the set's bonuses, but it still counts toward the set's
    // looks (SetLooks), and the glow moves with it to the back. Templates are built at world load (SetEffectWarmup), one
    // per distinct config, so a server's copy of the config pushed on connect reuses them, and a live edit builds new
    // ones the next time the set is seen.
    //
    // The drawn arrow needs no data of its own on the network: vanilla syncs the "bow_aim" animator bool, which is on
    // exactly while a bow is drawn. How far it is drawn ("drawpercent") is not synced, so on other clients the arrow
    // grows in over RemoteDrawSeconds instead of with the draw.
    public class SetVisuals : MonoBehaviour
    {
        private const float PollSeconds = 0.25f;
        private const float GlowFadeSeconds = 3f;      // longer than any glow particle lives (WeaponAuraKit.MaxAuraLifetime)
        private const string LogTag = "SetVisuals";

        private static readonly int BowAimHash = Animator.StringToHash("bow_aim");
        private const float RemoteDrawSeconds = 1f;    // about a bow's draw at middling skill
        private const float ArrowMinScale = 0.25f;     // the drawn arrow's size as the draw starts; full by half draw

        private static GameObject _templateRoot;
        private static readonly Dictionary<string, GameObject> GlowTemplates = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, GameObject> AuraTemplates = new Dictionary<string, GameObject>();
        private static readonly HashSet<string> UnknownPresetsLogged = new HashSet<string>();

        private Player _player;
        private float _nextPoll;
        private string _setID = "";
        private LegendarySetInfo _set;
        private GameObject _glowTemplate;
        private GameObject _weapon;
        private GameObject _glow;
        private GameObject _auraTemplate;
        private GameObject _aura;
        private GameObject _handTemplate;
        private GameObject _leftHandGlow;
        private GameObject _rightHandGlow;
        private GameObject _arrowTemplate;
        private GameObject _arrow;
        private float _remoteDraw;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void LateUpdate()
        {
            if (Time.time >= _nextPoll)
            {
                _nextPoll = Time.time + PollSeconds;
                Poll();
            }

            UpdateGlow();
            UpdateHandGlows();
            UpdateArrow();
        }

        private void OnDestroy()
        {
            StopAura();
        }

        // The main-menu preview player has no ZDO, so it shows nothing; nor does a dead one.
        private void Poll()
        {
            ZDO zdo = _player != null && _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            string setID = zdo != null && !_player.IsDead() ? zdo.GetString(SetAura.ZdoHash) : "";
            LegendarySetInfo set = null;
            if (!string.IsNullOrEmpty(setID))
            {
                UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out set);
            }

            // A config reload replaces the set objects, so a new reference is a possibly new look.
            if (setID == _setID && ReferenceEquals(set, _set))
            {
                return;
            }

            if (setID != _setID && zdo != null)
            {
                SetAura.Refresh(_player);
            }
            _setID = setID;
            _set = set;

            GameObject glowTemplate = set?.WeaponGlow != null ? GetGlowTemplate(set.WeaponGlow) : null;
            if (glowTemplate != _glowTemplate)
            {
                _glowTemplate = glowTemplate;
                RemoveGlow();
            }

            GameObject handTemplate = set?.HandGlow != null ? GetGlowTemplate(set.HandGlow) : null;
            if (handTemplate != _handTemplate)
            {
                _handTemplate = handTemplate;
                RemoveHandGlows();
            }

            GameObject arrowTemplate = set?.DrawArrow != null ? SetDrawArrow.GetTemplate(set.DrawArrow, TemplateRoot()) : null;
            if (arrowTemplate != _arrowTemplate)
            {
                _arrowTemplate = arrowTemplate;
                RemoveArrow();
            }

            GameObject auraTemplate = set?.BodyAura != null ? GetAuraTemplate(set.BodyAura) : null;
            if (auraTemplate != _auraTemplate)
            {
                StopAura();
                _auraTemplate = auraTemplate;
                if (auraTemplate != null)
                {
                    _aura = Instantiate(auraTemplate, transform, false);
                    _aura.name = auraTemplate.name;
                }
            }
        }

        // A re-equip makes a new hand model, and stowing or drawing the weapon swaps its hand model for a back one or
        // back, so the glow is fitted again to the new one. A weapon in hand comes before one on the back.
        private void UpdateGlow()
        {
            Transform hand = null;
            GameObject weapon = null;
            if (_glowTemplate != null)
            {
                weapon = WeaponAuraKit.GetWeaponModel(_player.m_visEquipment, out hand);
                if (weapon == null)
                {
                    weapon = WeaponAuraKit.GetStowedWeaponModel(_player.m_visEquipment, out hand);
                }
            }
            if (weapon == _weapon)
            {
                return;
            }

            RemoveGlow();
            _weapon = weapon;
            if (weapon != null)
            {
                _glow = WeaponAuraKit.AttachFitted(_glowTemplate, weapon, hand);
            }
        }

        private void RemoveGlow()
        {
            if (_glow != null)
            {
                _glow.AddComponent<AuraFade>().Begin(GlowFadeSeconds);
            }
            _glow = null;
            _weapon = null;
        }

        // The hand joints belong to the body and are never rebuilt, so each hand's glow is made once and stays on it.
        private void UpdateHandGlows()
        {
            VisEquipment vis = _player.m_visEquipment;
            if (_handTemplate == null || vis == null)
            {
                return;
            }

            if (_leftHandGlow == null && vis.m_leftHand != null)
            {
                _leftHandGlow = WeaponAuraKit.AttachToHand(_handTemplate, vis.m_leftHand);
            }
            if (_rightHandGlow == null && vis.m_rightHand != null)
            {
                _rightHandGlow = WeaponAuraKit.AttachToHand(_handTemplate, vis.m_rightHand);
            }
        }

        private void RemoveHandGlows()
        {
            if (_leftHandGlow != null)
            {
                _leftHandGlow.AddComponent<AuraFade>().Begin(GlowFadeSeconds);
            }
            if (_rightHandGlow != null)
            {
                _rightHandGlow.AddComponent<AuraFade>().Begin(GlowFadeSeconds);
            }
            _leftHandGlow = null;
            _rightHandGlow = null;
        }

        // On the drawing (right) hand, pointing at the bow hand, as an arrow on the string would; gone the moment the draw
        // ends, since that is when the arrow flies. It grows in over the first half of the draw.
        private void UpdateArrow()
        {
            float draw = _arrowTemplate != null ? ReadDraw() : 0f;
            VisEquipment vis = _player.m_visEquipment;
            Transform drawHand = vis != null ? vis.m_rightHand : null;
            Transform bowHand = vis != null ? vis.m_leftHand : null;
            if (draw <= 0f || drawHand == null || bowHand == null)
            {
                if (_arrow != null && _arrow.activeSelf)
                {
                    _arrow.SetActive(false);
                }
                return;
            }

            if (_arrow == null)
            {
                _arrow = Instantiate(_arrowTemplate, transform, false);
                _arrow.name = _arrowTemplate.name;
            }

            Vector3 along = bowHand.position - drawHand.position;
            if (along.sqrMagnitude < 0.0001f)
            {
                along = _player.transform.forward;
            }
            _arrow.transform.SetPositionAndRotation(drawHand.position, Quaternion.LookRotation(along, _player.transform.up));
            _arrow.transform.localScale = Vector3.one * Mathf.Lerp(ArrowMinScale, 1f, Mathf.Clamp01(draw * 2f));
            if (!_arrow.activeSelf)
            {
                _arrow.SetActive(true);
            }
        }

        // 0 while no bow is drawn. The wearer's own client knows the draw; others only see the synced bow_aim.
        private float ReadDraw()
        {
            Animator animator = _player.m_animator;
            if (animator == null || _player.IsDead() || !animator.GetBool(BowAimHash))
            {
                _remoteDraw = 0f;
                return 0f;
            }

            if (ReferenceEquals(_player, Player.m_localPlayer))
            {
                return Mathf.Max(0.01f, _player.GetAttackDrawPercentage());
            }

            _remoteDraw = Mathf.MoveTowards(_remoteDraw, 1f, Time.deltaTime / RemoteDrawSeconds);
            return Mathf.Max(0.01f, _remoteDraw);
        }

        private void RemoveArrow()
        {
            if (_arrow != null)
            {
                Destroy(_arrow);
            }
            _arrow = null;
        }

        // Particles already in the air finish their fall.
        private void StopAura()
        {
            if (_aura != null)
            {
                float longest = 0f;
                foreach (ParticleSystem system in _aura.GetComponentsInChildren<ParticleSystem>())
                {
                    system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    longest = Mathf.Max(longest, WeaponAuraKit.MaxLifetime(system));
                }
                Destroy(_aura, longest + 0.5f);
            }
            _aura = null;
            _auraTemplate = null;
        }

        // World load (SetEffectWarmup): every set's templates, so the first wearer seen costs one Instantiate.
        internal static void WarmupAtLoad(bool drawn)
        {
            if (!drawn)
            {
                return;
            }

            foreach (LegendarySetInfo set in UniqueLegendaryHelper.AllSets.Values)
            {
                if (set?.WeaponGlow != null)
                {
                    GetGlowTemplate(set.WeaponGlow);
                }
                if (set?.HandGlow != null)
                {
                    GetGlowTemplate(set.HandGlow);
                }
                if (set?.DrawArrow != null)
                {
                    SetDrawArrow.GetTemplate(set.DrawArrow, TemplateRoot());
                }
                if (set?.BodyAura != null)
                {
                    GetAuraTemplate(set.BodyAura);
                }
            }
        }

        private static Transform TemplateRoot()
        {
            if (_templateRoot == null)
            {
                _templateRoot = new GameObject("EL_SetVisualTemplates");
                _templateRoot.SetActive(false);
                DontDestroyOnLoad(_templateRoot);
            }
            return _templateRoot.transform;
        }

        // Config values are player-editable, so each is kept in range here.
        private static GameObject GetGlowTemplate(WeaponGlowInfo info)
        {
            Color color = ParseColor(info.Color, Color.white);
            Color glintColor = ParseColor(info.GlintColor, Color.Lerp(color, Color.white, 0.5f));
            Color accentColor = ParseColor(info.AccentColor, Color.clear);
            float alpha = Mathf.Clamp(info.Alpha, 0.02f, 1f);
            float rate = Mathf.Clamp(info.Rate, 0f, 200f);
            float size = Mathf.Clamp(info.Size, 0.2f, 4f);
            float speed = Mathf.Clamp(info.Speed, 0f, 2f);
            float glints = Mathf.Clamp(info.Glints, 0f, 30f);
            float accentScale = Mathf.Clamp(info.AccentScale, 0.05f, 5f);
            float lightIntensity = Mathf.Clamp(info.LightIntensity, 0f, 4f);
            float lightRange = Mathf.Clamp(info.LightRange, 0f, 10f);

            string key = string.Join("|", ColorUtility.ToHtmlStringRGBA(color), ColorUtility.ToHtmlStringRGBA(glintColor),
                ColorUtility.ToHtmlStringRGBA(accentColor), alpha, rate, size, speed, glints, info.Accent, accentScale,
                lightIntensity, lightRange);
            if (GlowTemplates.TryGetValue(key, out GameObject template) && template != null)
            {
                return template;
            }

            if (ZNetScene.instance == null)
            {
                return null;
            }

            Transform frame = WeaponAuraKit.NewTemplate(TemplateRoot(), "EL_SetWeaponGlow", out GameObject root);

            ParticleSystem glowSource = rate > 0f ? WeaponAuraKit.FindGlowSource() : null;
            if (glowSource != null)
            {
                WeaponAuraKit.AddGlow(glowSource, frame, color, alpha, 0.1f * size, 0.28f * size, rate, speed);
            }

            ParticleSystem sparkleSource = glints > 0f ? WeaponAuraKit.FindSparkleSource() : null;
            if (sparkleSource != null)
            {
                WeaponAuraKit.AddGlints(sparkleSource, frame, glintColor, glints);
            }

            if (!string.IsNullOrEmpty(info.Accent))
            {
                // An item's root system is its dropped-item sparkle; an effect prefab's root is the effect.
                Transform branch = WeaponAuraKit.FindSource(info.Accent, LogTag);
                bool includeRoot = branch != null && branch.root.GetComponent<ItemDrop>() == null;
                foreach (ParticleSystem accent in WeaponAuraKit.CopyBranch(branch, frame, accentScale, includeRoot))
                {
                    if (accentColor.a > 0f)
                    {
                        WeaponAuraKit.Tint(accent, accentColor, Color.Lerp(accentColor, Color.white, 0.3f));
                    }
                }
            }

            if (lightIntensity > 0f && lightRange > 0f)
            {
                WeaponAuraKit.AddLight(frame, color, lightIntensity, lightRange);
            }

            root.SetActive(false);
            GlowTemplates[key] = root;
            return root;
        }

        private static GameObject GetAuraTemplate(BodyAuraInfo info)
        {
            Color color = ParseColor(info.Color, Color.white);
            Color color2 = ParseColor(info.Color2, color);
            float density = Mathf.Clamp(info.Density, 0.1f, 5f);
            string preset = info.Preset ?? "";

            string key = string.Join("|", preset, ColorUtility.ToHtmlStringRGBA(color), ColorUtility.ToHtmlStringRGBA(color2), density);
            if (AuraTemplates.TryGetValue(key, out GameObject template) && template != null)
            {
                return template;
            }

            if (ZNetScene.instance == null)
            {
                return null;
            }

            switch (preset)
            {
                case FallingLeaves.Name:
                    template = FallingLeaves.Build(TemplateRoot(), color, color2, density);
                    break;
                default:
                    if (UnknownPresetsLogged.Add(preset))
                    {
                        EpicLoot.LogWarning($"[{LogTag}] unknown BodyAura preset '{preset}'; the set shows no aura.");
                    }
                    return null;
            }

            if (template != null)
            {
                AuraTemplates[key] = template;
            }
            return template;
        }

        internal static Color ParseColor(string html, Color fallback)
        {
            return !string.IsNullOrEmpty(html) && ColorUtility.TryParseHtmlString(html, out Color color) ? color : fallback;
        }

        // Every player on every client carries the component; a headless server draws nothing.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    __instance.gameObject.AddComponent<SetVisuals>();
                }
            }
        }
    }

    // Leaves drifting down around the wearer. A copy of the leaves a beech tree sheds (its material, sprite rotation and
    // shrink-in/out), brought down from canopy size to a ring at head height. They simulate in world space, so they stay
    // where they fell as the wearer moves on, and a few more per metre travelled leave a trail. The material is cut-out,
    // so the leaves fade by shrinking. Vanilla's own wind script pushes them around as it does the tree's.
    internal static class FallingLeaves
    {
        internal const string Name = "FallingLeaves";

        private const string Source = "Beech1/leaf_particles";
        private const float Height = 2.05f;             // the middle of the ring, above the feet
        private const float Radius = 0.9f;
        private const float Thickness = 0.4f;           // the ring's height, as a fraction of its radius
        private const float RatePerSecond = 5f;
        private const float RatePerMetre = 0.5f;
        private const int MaxLeaves = 40;
        private const float Gravity = 0.05f;            // of Valheim's -20 m/s2
        private const float FallSpeed = 0.7f;           // terminal speed, so they drift rather than drop
        private const float WindMultiplier = 0.4f;

        internal static GameObject Build(Transform holder, Color color, Color color2, float density)
        {
            Transform source = WeaponAuraKit.FindSource(Source, "SetVisuals");
            ParticleSystem sourceSystem = source != null ? source.GetComponent<ParticleSystem>() : null;
            if (sourceSystem == null)
            {
                return null;
            }

            ParticleSystem leaves = WeaponAuraKit.CopySystem(sourceSystem, holder, "EL_SetAura_" + Name);
            GameObject root = leaves.gameObject;
            root.layer = LayerMask.NameToLayer("effect");

            ParticleSystem.MainModule main = leaves.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.startColor = new ParticleSystem.MinMaxGradient(color, color2);
            main.gravityModifier = Gravity;
            main.maxParticles = Mathf.CeilToInt(MaxLeaves * density);

            ParticleSystem.EmissionModule emission = leaves.emission;
            emission.enabled = true;
            emission.rateOverTime = RatePerSecond * density;
            emission.rateOverDistance = RatePerMetre * density;
            emission.burstCount = 0;

            ParticleSystem.ShapeModule shape = leaves.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Radius;
            shape.radiusThickness = 1f;
            shape.position = new Vector3(0f, Height, 0f);
            shape.rotation = Vector3.zero;
            shape.scale = new Vector3(1f, Thickness, 1f);

            // A tumble as they fall; the source turns its leaves only by speed, and these barely move.
            ParticleSystem.RotationOverLifetimeModule spin = leaves.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);

            ParticleSystem.LimitVelocityOverLifetimeModule limit = leaves.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = FallSpeed;
            limit.dampen = 0.3f;

            ParticleSystem.ForceOverLifetimeModule force = leaves.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            GlobalWind wind = root.AddComponent<GlobalWind>();
            wind.m_particleVelocity = false;
            wind.m_particleForce = true;
            wind.m_multiplier = WindMultiplier;

            return root;
        }
    }
}
