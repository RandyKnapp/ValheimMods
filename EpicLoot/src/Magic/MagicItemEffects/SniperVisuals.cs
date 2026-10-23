using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpicLoot.MagicItemEffects
{
    // Sniper's look (Egil's Mark), on every client: gold energy gathering at the bow as a Sniper shot is drawn, a flash
    // when the draw is full, a golden halo, trail and light around the arrow in flight, and a golden burst where it hits.
    // The bow's own glow while the set is worn is the set's WeaponGlow (SetVisuals).
    //
    // The draw is mirrored by the shooter to the player ZDO (el-snpd: 0 to 1 in steps of 0.05, 0 unless a Sniper bow is
    // being drawn with the effect active), so every client draws the same charge and flash. Vanilla's own synced
    // "drawpercent" can't stand in for it: it is never reset after a release. The arrow carries a ZDO flag (el-snipe)
    // set in Projectile.Setup, before its first send, which other clients find when they create it (ZNetScene.CreateObject,
    // the only path that builds a remote object, after every Awake and with the ZDO filled in). The impact goes out as an
    // RPC on the shooter's player rather than on the arrow, since an arrow destroyed before its first send (a point-blank
    // shot) never reaches anyone else.
    //
    // Everything is copied at world load (SetEffectWarmup) from vanilla prefabs: the sparkler's sparks and flare, the
    // fire arrow's flare, the critical hit's burst and a dropped item's star glint.
    public static class SniperVisuals
    {
        internal static readonly Color Gold = new Color(1f, 0.76f, 0.23f);         // #FFC23A
        internal static readonly Color PaleGold = new Color(1f, 0.91f, 0.66f);     // #FFE9A8
        private static readonly Color LightGold = new Color(1f, 0.78f, 0.35f);     // #FFC85A

        internal static readonly int DrawZdoHash = "el-snpd".GetStableHashCode();
        internal static readonly int ArrowZdoHash = "el-snipe".GetStableHashCode();
        private const string ImpactRpc = "el-snhit";

        private const string SparksSource = "Sparkler/attach/equiped/vfx_Firework_Sparkler";
        private const string FlareSource = "Sparkler/attach/equiped/vfx_Firework_Sparkler/Flare";
        private const string HaloSource = "bow_projectile_fire/visual/flare";
        private const string BurstSource = "fx_crit/particles";
        private const string LogTag = "Sniper";

        private const float FlashSeconds = 1f;
        private const float ImpactSeconds = 1.5f;

        private static GameObject _templateRoot;
        private static bool _built;
        internal static GameObject ChargeTemplate { get; private set; }
        internal static GameObject FlashTemplate { get; private set; }
        internal static GameObject ArrowTemplate { get; private set; }
        internal static GameObject ImpactTemplate { get; private set; }

        internal static bool Drawn { get; } = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        // World load (SetEffectWarmup). Vanilla prefabs don't change, so the templates are kept for the session.
        internal static void WarmupAtLoad(bool drawn)
        {
            if (drawn)
            {
                BuildTemplates();
            }
        }

        internal static void BuildTemplates()
        {
            if (_built || ZNetScene.instance == null)
            {
                return;
            }

            _built = true;
            _templateRoot = new GameObject("EL_SniperTemplates");
            _templateRoot.SetActive(false);
            Object.DontDestroyOnLoad(_templateRoot);

            ChargeTemplate = BuildCharge();
            FlashTemplate = BuildFlash();
            ArrowTemplate = BuildArrow();
            ImpactTemplate = BuildImpact();
        }

        // Called from Sniper's Projectile.Setup postfix, on the shooter's client, for an arrow Sniper sped up.
        internal static void MarkArrow(Projectile projectile)
        {
            ZNetView nview = projectile.GetComponent<ZNetView>();
            if (nview != null && nview.IsValid() && nview.IsOwner())
            {
                nview.GetZDO().Set(ArrowZdoHash, true);
            }

            SniperArrowFx.Attach(projectile, local: true);
        }

        // On the shooter's client, the moment the arrow stops on something; every client spawns the burst.
        private static void SendImpact(Vector3 point, Vector3 normal)
        {
            Player player = Player.m_localPlayer;
            if (player != null && player.m_nview != null && player.m_nview.IsValid())
            {
                player.m_nview.InvokeRPC(ZNetView.Everybody, ImpactRpc, point, normal);
            }
        }

        private static void OnImpact(Vector3 point, Vector3 normal)
        {
            if (!Drawn)
            {
                return;
            }

            BuildTemplates();
            if (ImpactTemplate != null)
            {
                Quaternion rotation = normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
                Object.Destroy(Object.Instantiate(ImpactTemplate, point, rotation), ImpactSeconds);
            }
        }

        internal static void SpawnFlash(Vector3 position)
        {
            if (FlashTemplate != null)
            {
                Object.Destroy(Object.Instantiate(FlashTemplate, position, Quaternion.identity), FlashSeconds);
            }
        }

        // Converging sparks and a flare at the middle, both run at zero until SniperCharge turns them up with the draw.
        private static GameObject BuildCharge()
        {
            GameObject root = NewRoot("EL_SniperCharge");

            ParticleSystem sparks = Copy(SparksSource, root.transform, "Sparks");
            if (sparks != null)
            {
                ParticleSystem.MainModule main = sparks.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Local;
                main.startLifetime = 0.42f;
                main.startSpeed = -1.4f;                // inwards, from the sphere's surface to its middle
                main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
                main.maxParticles = 64;
                ParticleSystem.ShapeModule shape = sparks.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.6f;
                shape.radiusThickness = 0f;
                WeaponAuraKit.Tint(sparks, Gold, PaleGold);
                WeaponAuraKit.SetRate(sparks, 0f);
            }

            ParticleSystem core = Copy(FlareSource, root.transform, "Core");
            if (core != null)
            {
                ParticleSystem.MainModule main = core.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Local;
                main.startLifetime = 0.2f;
                main.startSpeed = 0f;
                main.startSize = 0.2f;
                main.startDelay = 0f;
                ParticleSystem.ShapeModule shape = core.shape;
                shape.enabled = false;
                WeaponAuraKit.Tint(core, WithAlpha(Gold, 0.6f), WithAlpha(PaleGold, 0.6f));
                WeaponAuraKit.SetRate(core, 0f);
                main.maxParticles = 16;
            }

            Light light = WeaponAuraKit.AddLight(root.transform, LightGold, 0f, 1.5f);
            light.enabled = false;
            return root;
        }

        // A star glint and a puff of the flare, with a light that dies away.
        private static GameObject BuildFlash()
        {
            GameObject root = NewRoot("EL_SniperFlash");

            ParticleSystem sparkleSource = WeaponAuraKit.FindSparkleSource();
            if (sparkleSource != null)
            {
                ParticleSystem star = WeaponAuraKit.CopySystem(sparkleSource, root.transform, "Star");
                OneShot(star, 1, 0.4f, 0.9f);
                WeaponAuraKit.Tint(star, PaleGold, Color.white);
            }

            ParticleSystem puff = Copy(FlareSource, root.transform, "Puff");
            if (puff != null)
            {
                OneShot(puff, 1, 0.25f, 1.4f);
                WeaponAuraKit.Tint(puff, WithAlpha(Gold, 0.8f), WithAlpha(PaleGold, 0.8f));
            }

            AddPulse(root.transform, 3f, 4f, 0.3f);
            return root;
        }

        // A halo, a trail of sparks shed along the flight, and a light. SniperArrowFx moves it with the arrow.
        private static GameObject BuildArrow()
        {
            GameObject root = NewRoot("EL_SniperArrow");

            ParticleSystem halo = Copy(HaloSource, root.transform, "Halo");
            if (halo != null)
            {
                ParticleSystem.MainModule main = halo.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.scalingMode = ParticleSystemScalingMode.Local;
                main.startLifetime = 0.15f;
                main.startSpeed = 0f;
                main.startSize = 1.1f;
                main.startDelay = 0.05f;                // clear of the bow before it shows
                ParticleSystem.ShapeModule shape = halo.shape;
                shape.enabled = false;
                WeaponAuraKit.Tint(halo, WithAlpha(Gold, 0.4f), WithAlpha(PaleGold, 0.4f));
                WeaponAuraKit.SetRate(halo, 30f);
            }

            ParticleSystem trail = Copy(SparksSource, root.transform, "Trail");
            if (trail != null)
            {
                ParticleSystem.MainModule main = trail.main;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Local;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
                main.maxParticles = 160;
                ParticleSystem.ShapeModule shape = trail.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.05f;
                WeaponAuraKit.Tint(trail, Gold, PaleGold);
                ParticleSystem.EmissionModule emission = trail.emission;
                emission.enabled = true;
                emission.rateOverTime = 0f;
                emission.rateOverDistance = 1.5f;
                emission.burstCount = 0;
            }

            WeaponAuraKit.AddLight(root.transform, LightGold, 1.6f, 5f);
            root.AddComponent<SniperArrowFx>();
            return root;
        }

        // The critical hit's burst in gold, a flash of the flare and a light that dies away.
        private static GameObject BuildImpact()
        {
            GameObject root = NewRoot("EL_SniperImpact");

            ParticleSystem burst = Copy(BurstSource, root.transform, "Burst");
            if (burst != null)
            {
                OneShot(burst, 24, -1f, -1f);
                WeaponAuraKit.ClampLifetime(burst, 1.2f);
                WeaponAuraKit.Tint(burst, Gold, Color.Lerp(PaleGold, Color.white, 0.5f));
            }

            ParticleSystem flash = Copy(FlareSource, root.transform, "Flash");
            if (flash != null)
            {
                OneShot(flash, 1, 0.3f, 1.6f);
                WeaponAuraKit.Tint(flash, WithAlpha(Gold, 0.8f), WithAlpha(PaleGold, 0.8f));
            }

            AddPulse(root.transform, 3f, 6f, 0.35f);
            return root;
        }

        // Active, under the inactive holder: a copy wakes and plays as soon as it is made.
        private static GameObject NewRoot(string name)
        {
            var root = new GameObject(name);
            root.layer = LayerMask.NameToLayer("effect");
            root.transform.SetParent(_templateRoot.transform, false);
            return root;
        }

        private static ParticleSystem Copy(string path, Transform parent, string name)
        {
            Transform source = WeaponAuraKit.FindSource(path, LogTag);
            ParticleSystem system = source != null ? source.GetComponent<ParticleSystem>() : null;
            return system != null ? WeaponAuraKit.CopySystem(system, parent, name) : null;
        }

        // A single burst of count particles; a negative lifetime or size keeps the source's.
        private static void OneShot(ParticleSystem system, int count, float lifetime, float size)
        {
            ParticleSystem.MainModule main = system.main;
            main.loop = false;
            main.playOnAwake = true;
            main.startDelay = 0f;
            if (lifetime > 0f)
            {
                main.startLifetime = lifetime;
            }
            if (size > 0f)
            {
                main.startSize = size;
            }
            main.duration = Mathf.Max(0.05f, WeaponAuraKit.MaxLifetime(system));
            main.maxParticles = Mathf.Max(main.maxParticles, count);

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.rateOverDistance = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        }

        private static void AddPulse(Transform parent, float intensity, float range, float seconds)
        {
            Light light = WeaponAuraKit.AddLight(parent, LightGold, intensity, range);
            light.gameObject.AddComponent<LightPulse>().Seconds = seconds;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        // On the shooter's client only (only the owner simulates a projectile): the first hit that stops a Sniper arrow.
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.OnHit))]
        private static class Projectile_OnHit_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Projectile __instance, out bool __state)
            {
                __state = __instance.m_didHit;
            }

            [HarmonyPostfix]
            private static void Postfix(Projectile __instance, bool __state, Vector3 hitPoint, Vector3 normal)
            {
                if (!__state && __instance.m_didHit && __instance.TryGetComponent(out SniperArrowMarker marker) && marker.Local)
                {
                    SendImpact(hitPoint, normal);
                }
            }
        }

        // Every client: a stopped arrow stops glowing (it may stay stuck in the ground for a while).
        [HarmonyPatch(typeof(Projectile), nameof(Projectile.RPC_OnHit))]
        private static class Projectile_RPC_OnHit_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Projectile __instance)
            {
                if (__instance.TryGetComponent(out SniperArrowMarker marker) && marker.Fx != null)
                {
                    marker.Fx.Fade();
                }
            }
        }

        // Other clients' copy of a Sniper arrow.
        [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.CreateObject))]
        private static class ZNetScene_CreateObject_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(GameObject __result, ZDO zdo)
            {
                if (__result != null && zdo != null && Drawn && zdo.GetBool(ArrowZdoHash) &&
                    __result.TryGetComponent(out Projectile projectile))
                {
                    SniperArrowFx.Attach(projectile, local: false);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                __instance.m_nview?.Register<Vector3, Vector3>(ImpactRpc, (sender, point, normal) => OnImpact(point, normal));
                if (Drawn)
                {
                    __instance.gameObject.AddComponent<SniperCharge>();
                }
            }
        }
    }

    // The gathering charge on one player's bow, on every client. The shooter's own client works the draw out and mirrors
    // it to the ZDO; everyone else reads it from there. Shown at the middle of the bow's largest mesh, growing with the
    // draw, and easing off over a quarter second after a release.
    public class SniperCharge : MonoBehaviour
    {
        private const float EaseRate = 4f;              // per second
        private const float SparkRate = 90f;            // a second at full draw; scales with the draw squared
        private const float CoreRate = 25f;

        private Player _player;
        private float _published = -1f;
        private float _target;
        private float _shown;
        private GameObject _fx;
        private ParticleSystem _sparks;
        private ParticleSystem _core;
        private Light _light;
        private GameObject _weapon;
        private Renderer _anchor;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void LateUpdate()
        {
            float target = ReadDraw();
            if (target >= 1f && _target < 1f)
            {
                SniperVisuals.SpawnFlash(GetPosition());
            }
            _target = target;

            float shown = Mathf.MoveTowards(_shown, target, EaseRate * Time.deltaTime);
            if (shown <= 0f && _shown <= 0f)
            {
                return;
            }

            _shown = shown;
            if (!EnsureFx())
            {
                return;
            }

            _fx.transform.position = GetPosition();
            Apply(shown);
        }

        // The main-menu preview player has no ZDO, so it never charges.
        private float ReadDraw()
        {
            ZDO zdo = _player != null && _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            if (zdo == null)
            {
                return 0f;
            }

            if (!ReferenceEquals(_player, Player.m_localPlayer))
            {
                return Mathf.Clamp01(zdo.GetFloat(SniperVisuals.DrawZdoHash));
            }

            float draw = 0f;
            if (!_player.IsDead() && Sniper.IsSniperBow(_player.GetCurrentWeapon()) &&
                _player.HasActiveMagicEffect(MagicEffectType.Sniper, out float value) && value > 0f)
            {
                draw = _player.GetAttackDrawPercentage();
            }

            // Written only on change, in steps, so the ZDO moves about twenty times in a whole draw.
            float stepped = draw >= 1f ? 1f : Mathf.Floor(draw * 20f) / 20f;
            if (!Mathf.Approximately(stepped, _published))
            {
                _published = stepped;
                zdo.Set(SniperVisuals.DrawZdoHash, stepped);
            }
            return draw;
        }

        private bool EnsureFx()
        {
            if (_fx != null)
            {
                return true;
            }

            SniperVisuals.BuildTemplates();
            GameObject template = SniperVisuals.ChargeTemplate;
            if (template == null)
            {
                return false;
            }

            _fx = Instantiate(template, transform, false);
            _fx.name = template.name;
            _sparks = _fx.transform.Find("Sparks")?.GetComponent<ParticleSystem>();
            _core = _fx.transform.Find("Core")?.GetComponent<ParticleSystem>();
            _light = _fx.GetComponentInChildren<Light>(true);
            return true;
        }

        private void Apply(float draw)
        {
            if (_sparks != null)
            {
                ParticleSystem.EmissionModule emission = _sparks.emission;
                emission.rateOverTime = SparkRate * draw * draw;
            }
            if (_core != null)
            {
                ParticleSystem.EmissionModule emission = _core.emission;
                emission.rateOverTime = draw > 0.01f ? CoreRate : 0f;
                ParticleSystem.MainModule main = _core.main;
                main.startSize = Mathf.Lerp(0.2f, 1f, draw);
            }
            if (_light != null)
            {
                _light.enabled = draw > 0.01f;
                _light.intensity = 2.2f * draw;
                _light.range = Mathf.Lerp(1.5f, 4f, draw);
            }
        }

        // The middle of the bow in hand, else the player's middle.
        private Vector3 GetPosition()
        {
            GameObject weapon = WeaponAuraKit.GetWeaponModel(_player.m_visEquipment, out Transform _);
            if (weapon != _weapon)
            {
                _weapon = weapon;
                _anchor = weapon != null ? LargestRenderer(weapon) : null;
            }

            if (_anchor != null)
            {
                return _anchor.bounds.center;
            }
            return weapon != null ? weapon.transform.position : _player.GetCenterPoint();
        }

        private static Renderer LargestRenderer(GameObject weapon)
        {
            Renderer best = null;
            float bestSize = 0f;
            foreach (MeshRenderer renderer in weapon.GetComponentsInChildren<MeshRenderer>())
            {
                float size = renderer.bounds.size.sqrMagnitude;
                if (renderer.enabled && size > bestSize)
                {
                    best = renderer;
                    bestSize = size;
                }
            }
            return best;
        }
    }

    // Follows one Sniper arrow, as a separate object rather than its child, so the arrow can be destroyed or stopped at
    // any moment without taking the trail with it: when it stops (RPC_OnHit) or is gone, the light dims and the last
    // sparks finish.
    public class SniperArrowFx : MonoBehaviour
    {
        private const float FadeSeconds = 0.6f;

        private Transform _target;
        private bool _fading;

        internal static void Attach(Projectile projectile, bool local)
        {
            if (!SniperVisuals.Drawn || projectile == null || projectile.GetComponent<SniperArrowMarker>() != null)
            {
                return;
            }

            SniperVisuals.BuildTemplates();
            GameObject template = SniperVisuals.ArrowTemplate;
            var marker = projectile.gameObject.AddComponent<SniperArrowMarker>();
            marker.Local = local;
            if (template == null)
            {
                return;
            }

            Transform target = projectile.transform;
            GameObject fx = Instantiate(template, target.position, target.rotation);
            fx.name = template.name;
            SniperArrowFx follower = fx.GetComponent<SniperArrowFx>();
            follower._target = target;
            marker.Fx = follower;
        }

        private void LateUpdate()
        {
            if (_fading)
            {
                return;
            }

            if (_target == null)
            {
                Fade();
                return;
            }

            transform.SetPositionAndRotation(_target.position, _target.rotation);
        }

        internal void Fade()
        {
            if (_fading)
            {
                return;
            }

            _fading = true;
            gameObject.AddComponent<AuraFade>().Begin(FadeSeconds);
        }
    }

    // On a Sniper arrow: its follower, and whether this client fired it.
    public class SniperArrowMarker : MonoBehaviour
    {
        internal SniperArrowFx Fx;
        internal bool Local;
    }

    // A light that dies away over Seconds from the moment its copy wakes.
    public class LightPulse : MonoBehaviour
    {
        public float Seconds = 0.3f;

        private Light _light;
        private float _intensity;
        private float _started;

        private void Awake()
        {
            _light = GetComponent<Light>();
            _intensity = _light != null ? _light.intensity : 0f;
            _started = Time.time;
        }

        private void Update()
        {
            if (_light == null)
            {
                return;
            }

            float left = Mathf.Clamp01(1f - (Time.time - _started) / Mathf.Max(0.01f, Seconds));
            _light.intensity = _intensity * left;
            _light.enabled = left > 0f;
        }
    }
}
