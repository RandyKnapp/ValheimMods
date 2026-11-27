using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string HomingMissile = nameof(HomingMissile);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Only the thrower's client simulates a projectile (Projectile.FixedUpdate is owner-only), so bending its velocity
    // there is the whole effect; every other client just sees the synced position.
    public static class HomingMissile
    {
        public const float DefaultAcquireAngle = 25f;
        public const float DefaultAcquireRange = 20f;
        public const float DefaultLoseRangeMultiplier = 1.6f;
        public const float DefaultStartTurnRate = 45f;
        public const float DefaultFullTurnRate = 1440f;
        public const float DefaultRampSeconds = 3f;

        private const string AcquireAngleKey = "AcquireAngle";
        private const string LoseRangeMultiplierKey = "LoseRangeMultiplier";
        private const string StartTurnRateKey = "StartTurnRate";
        private const string FullTurnRateKey = "FullTurnRate";
        private const string RampSecondsKey = "RampSeconds";

        private static int _losMask;

        internal static float AcquireAngle => EffectConfig.GetClamped(MagicEffectType.HomingMissile, AcquireAngleKey, DefaultAcquireAngle, 1f, 180f);
        internal static float LoseRangeMultiplier => EffectConfig.GetClamped(MagicEffectType.HomingMissile, LoseRangeMultiplierKey, DefaultLoseRangeMultiplier, 1f, 10f);

        // The rolled value: how far the wisp and the thrown weapon look for a target.
        internal static float RangeOf(ItemDrop.ItemData item)
        {
            float range = item != null && item.IsMagic() ? item.GetMagicItem().GetTotalEffectValue(MagicEffectType.HomingMissile) : 0f;
            return range > 0f ? range : DefaultAcquireRange;
        }
        internal static float StartTurnRate => EffectConfig.GetClamped(MagicEffectType.HomingMissile, StartTurnRateKey, DefaultStartTurnRate, 0f, 3600f);
        internal static float FullTurnRate => EffectConfig.GetClamped(MagicEffectType.HomingMissile, FullTurnRateKey, DefaultFullTurnRate, 0f, 36000f);
        internal static float RampSeconds => EffectConfig.GetClamped(MagicEffectType.HomingMissile, RampSecondsKey, DefaultRampSeconds, 0.01f, 60f);

        // Rises with the square of the flight time, so the first second is lazy and the end is a dime.
        internal static float TurnRateAt(float flightTime)
        {
            float ramp = Mathf.Clamp01(flightTime / RampSeconds);
            return Mathf.Lerp(StartTurnRate, FullTurnRate, ramp * ramp);
        }

        internal static readonly Dictionary<Projectile, HomingProjectile> Flights = new Dictionary<Projectile, HomingProjectile>();

        internal static bool IsHomingWeapon(ItemDrop.ItemData item)
        {
            return item != null && item.IsMagic() &&
                item.HasMagicEffect(MagicEffectType.HomingMissile, includeSocketed: true) &&
                (item.m_shared.m_skillType == Skills.SkillType.Spears ||
                 item.HasMagicEffect(MagicEffectType.Throwable, includeSocketed: true));
        }

        internal static bool IsValidTarget(Player owner, Character character)
        {
            if (owner == null || character == null || character == owner || character.IsDead())
            {
                return false;
            }

            return BaseAI.IsEnemy(owner, character) ||
                (character.GetBaseAI() != null && character.GetBaseAI().IsAggravatable());
        }

        // The living enemy inside the cone around dir that sits closest to its centre, distance breaking near ties.
        internal static Character FindTarget(Player owner, Vector3 origin, Vector3 dir, float maxAngle, float range)
        {
            Character best = null;
            float bestScore = float.MaxValue;
            foreach (Character character in Character.GetAllCharacters())
            {
                if (!IsValidTarget(owner, character))
                {
                    continue;
                }

                Vector3 to = character.GetCenterPoint() - origin;
                float distance = to.magnitude;
                if (distance > range || distance < 0.5f)
                {
                    continue;
                }

                float angle = Vector3.Angle(dir, to);
                if (angle > maxAngle)
                {
                    continue;
                }

                float score = angle / maxAngle + 0.25f * distance / range;
                if (score < bestScore && HasLineOfSight(origin, character))
                {
                    best = character;
                    bestScore = score;
                }
            }

            return best;
        }

        // The head bone where there is one; necks, serpents and the like have none, so the top of the collider instead.
        internal static Vector3 TopPoint(Character character)
        {
            if (character.m_head != null)
            {
                return character.m_head.position;
            }

            Bounds bounds = character.m_collider != null ? character.m_collider.bounds : new Bounds(character.transform.position, Vector3.one);
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        internal static Character LockedTarget()
        {
            foreach (HomingProjectile flight in Flights.Values)
            {
                if (flight != null && flight.InFlight && flight.Target != null)
                {
                    return flight.Target;
                }
            }

            return null;
        }

        private static bool HasLineOfSight(Vector3 origin, Character character)
        {
            if (_losMask == 0)
            {
                _losMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");
            }

            return !Physics.Linecast(origin, character.GetCenterPoint(), _losMask);
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
        private static class Projectile_Setup_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Projectile __instance, Character owner, ItemDrop.ItemData item)
            {
                if (!(owner is Player player) || player != Player.m_localPlayer || !IsHomingWeapon(item) ||
                    __instance.m_nview == null || !__instance.m_nview.IsValid())
                {
                    return;
                }

                __instance.gameObject.AddComponent<HomingProjectile>().Begin(__instance, player);
            }
        }

        [HarmonyPatch(typeof(Projectile), nameof(Projectile.FixedUpdate))]
        private static class Projectile_FixedUpdate_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Projectile __instance)
            {
                if (Flights.TryGetValue(__instance, out HomingProjectile homing) &&
                    __instance.m_nview != null && __instance.m_nview.IsValid() && __instance.m_nview.IsOwner())
                {
                    homing.Steer(Time.fixedDeltaTime);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    HomingWisp.Update(__instance);
                }
            }
        }
    }

    // On the thrower's copy of one homing projectile: the lock, and the steering applied before vanilla moves it.
    public class HomingProjectile : MonoBehaviour
    {
        private const float ReacquireInterval = 0.2f;

        private Projectile _projectile;
        private Player _owner;
        private float _gravity;
        private float _reacquireTimer;
        private float _flightTime;
        private float _range;

        internal Character Target { get; private set; }
        internal bool InFlight => _projectile != null && !_projectile.m_didHit;

        internal void Begin(Projectile projectile, Player owner)
        {
            _projectile = projectile;
            _owner = owner;
            _gravity = projectile.m_gravity;
            _range = HomingMissile.RangeOf(projectile.m_weapon);
            HomingMissile.Flights[projectile] = this;

            Target = HomingWisp.MarkedTarget;
            if (Target == null)
            {
                Vector3 heading = projectile.m_vel.sqrMagnitude > 0.01f ? projectile.m_vel.normalized : owner.transform.forward;
                Target = HomingMissile.FindTarget(owner, transform.position, heading, HomingMissile.AcquireAngle, _range);
            }
            ApplyGravity();
        }

        private void OnDestroy()
        {
            if (_projectile != null)
            {
                HomingMissile.Flights.Remove(_projectile);
            }
        }

        internal void Steer(float dt)
        {
            if (_projectile.m_didHit)
            {
                return;
            }

            _flightTime += dt;
            Vector3 velocity = _projectile.m_vel;
            float speed = velocity.magnitude;
            if (speed < 0.01f)
            {
                return;
            }

            Vector3 heading = velocity / speed;
            if (!HomingMissile.IsValidTarget(_owner, Target) ||
                Vector3.Distance(transform.position, Target.GetCenterPoint()) > _range * HomingMissile.LoseRangeMultiplier)
            {
                Target = null;
                _reacquireTimer -= dt;
                if (_reacquireTimer <= 0f)
                {
                    _reacquireTimer = ReacquireInterval;
                    Target = HomingMissile.FindTarget(_owner, transform.position, heading, HomingMissile.AcquireAngle, _range);
                }

                ApplyGravity();
                if (Target == null)
                {
                    return;
                }
            }

            Vector3 wanted = (Target.GetCenterPoint() - transform.position).normalized;
            Vector3 steered = Vector3.RotateTowards(heading, wanted, HomingMissile.TurnRateAt(_flightTime) * Mathf.Deg2Rad * dt, 0f);
            _projectile.m_vel = steered * speed;
        }

        private void ApplyGravity()
        {
            _projectile.m_gravity = Target != null ? 0f : _gravity;
        }
    }

    // The thrower's lock wisp: a tinted, local-only copy of the Wisplight's ball. With a homing weapon in hand it leaves
    // the chest and flies to the enemy a throw would lock onto, hovers over it (and stays put through the throw
    // animation, since that is the enemy the throw takes), burns red while a throw is chasing it, and comes home when
    // there is nothing to mark. The copy is built under an inactive holder and stripped of its ZNetView and behaviours
    // before it ever wakes, so it never registers a ZDO.
    internal static class HomingWisp
    {
        private static readonly Color PreviewColor = new Color(1f, 0.55f, 0.15f);
        private static readonly Color LockColor = new Color(1f, 0.18f, 0.08f);

        private const string WisplightItem = "Demister";
        private const string BallPrefab = "demister_ball";
        private const float SearchInterval = 0.1f;

        private static GameObject _holder;
        private static GameObject _template;
        private static HomingWispBall _ball;
        private static Character _marked;
        private static float _searchTimer;

        internal static Character MarkedTarget => HomingMissile.IsValidTarget(Player.m_localPlayer, _marked) ? _marked : null;

        internal static void Update(Player player)
        {
            Character locked = HomingMissile.LockedTarget();

            Character preview = null;
            if (!player.IsDead() && HomingMissile.IsHomingWeapon(player.GetCurrentWeapon()))
            {
                if (player.InAttack() && HomingMissile.IsValidTarget(player, _marked))
                {
                    preview = _marked;
                }
                else
                {
                    _searchTimer -= Time.deltaTime;
                    if (_searchTimer > 0f && HomingMissile.IsValidTarget(player, _marked))
                    {
                        preview = _marked;
                    }
                    else
                    {
                        _searchTimer = SearchInterval;
                        Vector3 origin = player.GetEyePoint();
                        preview = HomingMissile.FindTarget(player, origin, player.GetAimDir(origin),
                            HomingMissile.AcquireAngle, HomingMissile.RangeOf(player.GetCurrentWeapon()));
                    }
                }
            }
            _marked = preview;

            Character wanted = locked ?? preview;
            if (wanted != null)
            {
                Show(player.GetCenterPoint());
                if (_ball != null)
                {
                    _ball.SetGoal(wanted, locked != null ? LockColor : PreviewColor);
                }
            }
            else if (_ball != null && _ball.gameObject.activeSelf)
            {
                _ball.ReturnHome(player);
            }
        }

        internal static void WarmupAtLoad(bool drawn)
        {
            if (drawn)
            {
                GetTemplate();
            }
        }

        // One ball per session, hidden when it gets home and shown again for the next mark, so a lock copies no
        // prefab and no materials.
        private static void Show(Vector3 position)
        {
            if (_ball != null)
            {
                if (!_ball.gameObject.activeSelf)
                {
                    _ball.transform.position = position;
                    _ball.gameObject.SetActive(true);
                }
                return;
            }

            GameObject template = GetTemplate();
            if (template == null)
            {
                return;
            }

            GameObject ball = Object.Instantiate(template, position, Quaternion.identity);
            ball.name = template.name;
            _ball = ball.GetComponent<HomingWispBall>();
        }

        private static GameObject GetTemplate()
        {
            if (_template != null)
            {
                return _template;
            }

            GameObject source = FindSourcePrefab();
            if (source == null)
            {
                return null;
            }

            if (_holder == null)
            {
                _holder = new GameObject("EL_HomingWispTemplate");
                _holder.SetActive(false);
                Object.DontDestroyOnLoad(_holder);
            }

            _template = Object.Instantiate(source, _holder.transform, false);
            _template.name = "EL_HomingWisp";
            foreach (ParticleSystemRenderer glint in _template.GetComponents<ParticleSystemRenderer>())
            {
                Object.DestroyImmediate(glint);
            }
            foreach (ParticleSystem glint in _template.GetComponents<ParticleSystem>())
            {
                Object.DestroyImmediate(glint);
            }
            foreach (LODGroup lod in _template.GetComponentsInChildren<LODGroup>(true))
            {
                Object.DestroyImmediate(lod);
            }
            foreach (MonoBehaviour behaviour in _template.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Object.DestroyImmediate(behaviour);
            }
            foreach (ParticleSystemForceField field in _template.GetComponentsInChildren<ParticleSystemForceField>(true))
            {
                Object.DestroyImmediate(field);
            }
            foreach (AudioSource audio in _template.GetComponentsInChildren<AudioSource>(true))
            {
                Object.DestroyImmediate(audio);
            }
            foreach (Collider collider in _template.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
            foreach (Rigidbody body in _template.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.DestroyImmediate(body);
            }
            _template.AddComponent<HomingWispBall>();
            return _template;
        }

        private static GameObject FindSourcePrefab()
        {
            GameObject item = WeaponAuraKit.FindPrefab(WisplightItem);
            return item != null ? item : WeaponAuraKit.FindPrefab(BallPrefab);
        }
    }

    public class HomingWispBall : MonoBehaviour
    {
        private const float FlySpeed = 30f;
        private const float FlySnap = 6f;
        private const float HoverHeight = 0.8f;
        private const float BobAmplitude = 0.15f;
        private const float BobRate = 2.5f;
        private const float HomeRadius = 0.4f;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColor = Shader.PropertyToID("_Color");
        // The floating ball's extras; the Wisplight item itself shows neither.
        private static readonly string[] HiddenParts = { "effects/flame/energy (1)", "effects/flame/embers" };

        private Character _target;
        private Player _home;
        private bool _returning;
        private Color _color;
        private ParticleSystem[] _systems;
        private Light[] _lights;
        private float _bobPhase;
        private readonly Dictionary<ParticleSystem, float> _alphas = new Dictionary<ParticleSystem, float>();
        private readonly Dictionary<Material, float> _emissions = new Dictionary<Material, float>();
        private readonly HashSet<Material> _tintable = new HashSet<Material>();
        // Renderer.materials hands out copies that destroying the GameObject does not free.
        private readonly List<Material> _materialCopies = new List<Material>();

        private void Awake()
        {
            foreach (string path in HiddenParts)
            {
                Transform child = transform.Find(path);
                if (child != null)
                {
                    child.gameObject.SetActive(false);
                }
            }

            _systems = GetComponentsInChildren<ParticleSystem>(true);
            _lights = GetComponentsInChildren<Light>(true);
            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
            foreach (ParticleSystem system in _systems)
            {
                ParticleSystem.MinMaxGradient startColor = system.main.startColor;
                _alphas[system] = startColor.mode == ParticleSystemGradientMode.TwoColors ? startColor.colorMax.a : startColor.color.a;
            }
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                Material[] copies = renderer.materials;
                _materialCopies.AddRange(copies);
                foreach (Material material in copies)
                {
                    if (material.HasProperty(EmissionColor))
                    {
                        Color emission = material.GetColor(EmissionColor);
                        _emissions[material] = Mathf.Max(emission.r, emission.g, emission.b);
                    }
                    if (!(renderer is ParticleSystemRenderer) && material.HasProperty(BaseColor))
                    {
                        _tintable.Add(material);
                    }
                }
            }
        }

        internal void SetGoal(Character target, Color color)
        {
            _target = target;
            _returning = false;
            if (color != _color)
            {
                _color = color;
                Tint(color);
            }
        }

        internal void ReturnHome(Player home)
        {
            _target = null;
            _home = home;
            _returning = true;
        }

        private void Update()
        {
            Vector3 goal;
            if (_target != null && !_target.IsDead())
            {
                float bob = BobAmplitude * Mathf.Sin(Time.time * BobRate + _bobPhase);
                goal = HomingMissile.TopPoint(_target) + Vector3.up * (HoverHeight + bob);
            }
            else if (_returning && _home != null)
            {
                goal = _home.GetCenterPoint();
            }
            else
            {
                Hide();
                return;
            }

            float distance = Vector3.Distance(transform.position, goal);
            float step = (FlySpeed + distance * FlySnap) * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, goal, step);
            if (_returning && distance < HomeRadius)
            {
                Hide();
            }
        }

        private void Hide()
        {
            _target = null;
            _home = null;
            _returning = false;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (Material material in _materialCopies)
            {
                Destroy(material);
            }
            _materialCopies.Clear();
        }

        private void Tint(Color color)
        {
            Color pale = Color.Lerp(color, Color.white, 0.35f);
            foreach (ParticleSystem system in _systems)
            {
                float alpha = _alphas.TryGetValue(system, out float a) ? a : 1f;
                Color tint = color;
                Color paleTint = pale;
                tint.a = alpha;
                paleTint.a = alpha;
                ParticleSystem.MainModule main = system.main;
                main.startColor = new ParticleSystem.MinMaxGradient(tint, paleTint);
                RecolorLiveParticles(system, tint);
            }
            foreach (Light light in _lights)
            {
                light.color = color;
            }
            foreach (KeyValuePair<Material, float> emission in _emissions)
            {
                emission.Key.SetColor(EmissionColor, color * emission.Value);
            }
            foreach (Material material in _tintable)
            {
                material.SetColor(BaseColor, color);
            }
        }

        // A start colour only reaches particles born after it is set; the flare is one particle that never dies.
        private static void RecolorLiveParticles(ParticleSystem system, Color tint)
        {
            int count = system.particleCount;
            if (count == 0)
            {
                return;
            }

            var particles = new ParticleSystem.Particle[count];
            int live = system.GetParticles(particles);
            for (int i = 0; i < live; i++)
            {
                particles[i].startColor = tint;
            }
            system.SetParticles(particles, live);
        }
    }
}
