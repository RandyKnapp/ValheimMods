using EpicLoot.Config;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Earthshaker = nameof(Earthshaker);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Earthshaker (4-piece Freyja's Falcon set bonus). A hotkey pressed in mid-air dives the player toward where they
    // aim, at least MinDiveAngle below the horizon, steering after the aim at a limited turn rate, and on touching
    // down slams an area: the effect value as damage, half blunt and half pierce, raised by the inventory's weight
    // over a threshold and by how far the player fell. The area, and the impact's look, grow with the fall too. A dive
    // can start at any height, but one begun with less than MinHeight of clear air below deals LowDivePenalty percent
    // less damage.
    //
    // The dive runs on the diver's client. Every physics step the velocity is forced along the dive direction (an
    // UpdateMotion postfix, the last motion code in Character.CustomFixedUpdate) and a capsule cast looks one step
    // ahead. The player's body uses discrete collision with a 2 m/s depenetration limit, so left to vanilla a 30 m/s
    // impact sinks into the ground or drops through a thin floor, and ground contact is only reported a step or two
    // late. The cast lands the dive itself, on the exact surface, and vanilla's fall damage never sees the drop.
    //
    // Other clients see the dive through the player's own synced transform, the lean through vanilla's synced visual
    // tilt, the wings through two ZDO bools, and the slam through one RPC that every client plays locally at the
    // slam's size (a networked fx prefab spawns at its own size only).
    public static class Earthshaker
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultRadius = 5f;
        public const float DefaultMinHeight = 3f;
        public const float DefaultLowDivePenalty = 50f;     // percent less damage from a dive begun under MinHeight
        public const float DefaultStaminaCost = 25f;
        public const float DefaultDiveSpeed = 30f;
        public const float DefaultMinDiveAngle = 45f;
        public const float DefaultStaggerMultiplier = 2f;
        public const float DefaultPushForce = 80f;
        public const float DefaultWeightThreshold = 300f;
        public const float DefaultWeightPerPercent = 10f;
        public const float DefaultMaxWeightBonus = 300f;
        public const float DefaultFallThreshold = 5f;
        public const float DefaultFallBonusPerMetre = 2f;
        public const float DefaultMaxFallBonus = 0f;        // 0 = no cap
        public const float DefaultFallPerRadiusMetre = 15f; // metres fallen for each extra metre of slam radius
        public const float DefaultMaxRadius = 0f;           // 0 = no cap
        public const float DefaultSteerRate = 60f;          // degrees per second the dive turns after the aim

        private const string RadiusKey = "Radius";
        private const string FallPerRadiusMetreKey = "FallPerRadiusMetre";
        private const string MaxRadiusKey = "MaxRadius";
        private const string MinHeightKey = "MinHeight";
        private const string LowDivePenaltyKey = "LowDivePenalty";
        private const string StaminaCostKey = "StaminaCost";
        private const string DiveSpeedKey = "DiveSpeed";
        private const string MinDiveAngleKey = "MinDiveAngle";
        private const string SteerRateKey = "SteerRate";
        private const string StaggerMultiplierKey = "StaggerMultiplier";
        private const string PushForceKey = "PushForce";
        private const string WeightThresholdKey = "WeightThreshold";
        private const string WeightPerPercentKey = "WeightPerPercent";
        private const string MaxWeightBonusKey = "MaxWeightBonus";
        private const string FallThresholdKey = "FallThreshold";
        private const string FallBonusPerMetreKey = "FallBonusPerMetre";
        private const string MaxFallBonusKey = "MaxFallBonus";

        // Player-editable, so each is clamped: WeightPerPercent divides, a zero radius hits nothing.
        private static float Radius => Get(RadiusKey, DefaultRadius, 1f, 20f);
        private static float FallPerRadiusMetre => Get(FallPerRadiusMetreKey, DefaultFallPerRadiusMetre, 1f, 1000f);
        private static float MaxRadius => Get(MaxRadiusKey, DefaultMaxRadius, 0f, 200f);
        private static float MinHeight => Get(MinHeightKey, DefaultMinHeight, 0f, 50f);
        private static float LowDivePenalty => Get(LowDivePenaltyKey, DefaultLowDivePenalty, 0f, 100f);
        private static float StaminaCost => Get(StaminaCostKey, DefaultStaminaCost, 0f, 200f);
        private static float DiveSpeed => Get(DiveSpeedKey, DefaultDiveSpeed, 10f, 80f);
        private static float MinDiveAngle => Get(MinDiveAngleKey, DefaultMinDiveAngle, 0f, 90f);
        private static float SteerRate => Get(SteerRateKey, DefaultSteerRate, 0f, 360f);
        private static float StaggerMultiplier => Get(StaggerMultiplierKey, DefaultStaggerMultiplier, 0f, 10f);
        private static float PushForce => Get(PushForceKey, DefaultPushForce, 0f, 500f);
        private static float WeightThreshold => Get(WeightThresholdKey, DefaultWeightThreshold, 0f, 10000f);
        private static float WeightPerPercent => Get(WeightPerPercentKey, DefaultWeightPerPercent, 0.1f, 1000f);
        private static float MaxWeightBonus => Get(MaxWeightBonusKey, DefaultMaxWeightBonus, 0f, 10000f);
        private static float FallThreshold => Get(FallThresholdKey, DefaultFallThreshold, 0f, 1000f);
        private static float FallBonusPerMetre => Get(FallBonusPerMetreKey, DefaultFallBonusPerMetre, 0f, 100f);
        private static float MaxFallBonus => Get(MaxFallBonusKey, DefaultMaxFallBonus, 0f, 100000f);

        private static float Get(string key, float fallback, float min, float max) =>
            EffectConfig.GetClamped(MagicEffectType.Earthshaker, key, fallback, min, max);

        // Fixed rules of the dive, not worth a config key.
        private const float LandingNormalY = 0.35f;           // flatter than about 70 degrees lands; steeper slides
        private const float StuckTime = 0.5f;                 // seconds moving less than StuckDistance: the dive gives up
        private const float StuckDistance = 1f;
        private const float MaxDiveTime = 20f;
        private const float PositionJumpLimit = 2f;           // metres off the expected path: something else moved us
        private const float WaterExitSpeed = 5f;              // the most downward speed kept when the dive meets water
        private const float LandingAltitudeMargin = 0.9f;     // over vanilla's 0.8 m landing thud, far under its 4 m fall damage
        private const float ContactAltitudeMargin = 0.5f;     // a wall grazed mid-dive: no fall damage, no landing dust
        private const float FallDamageGuardTime = 1f;         // how long after a landing vanilla's own contact is still ours
        private const float SlamCenterHeight = 0.6f;
        private const float MaxStoopAngle = 60f;              // how far the body leans into the dive
        private const float StoopTurnSpeed = 360f;            // degrees per second

        // Player ZDO flags, written by the owner only on change: wearing the full set (wings), and diving.
        internal static readonly int WingsZdoHash = "el-ffw".GetStableHashCode();
        internal static readonly int DivingZdoHash = "el-ffd".GetStableHashCode();
        private const string FxRpc = "el-esk";

        // Networked vanilla sounds, spawned once by the diver: the Fallen Valkyrie's screech as the dive starts and
        // her wing flap for the wings.
        private const string DiveFxPrefab = "sfx_fallenvalkyrie_screech";
        internal const string WingFlapFxPrefab = "sfx_fallenvalkyrie_wingflap";

        // The slam's look, played by every client from the RPC as a local copy scaled to the slam's radius: the first
        // ground slam of the list that exists, then a burst of black feathers. At the base radius they are their
        // normal size.
        private static readonly string[] SlamFxPrefabs = { "fx_goblinbrute_groundslam", "vfx_troll_groundslam" };
        private const string FeatherFxPrefab = "fx_raven_despawn";
        private const float LocalFxLifetime = 10f;           // a fallback, should a copy have no TimedDestruction

        // Tooltip: every number in the text follows a retune of its config key.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Earthshaker, value => new object[]
            {
                value, Radius, MinHeight, StaminaCost, WeightPerPercent, WeightThreshold, MaxWeightBonus,
                FallBonusPerMetre, FallThreshold, FallPerRadiusMetre, LowDivePenalty
            });
        }

        // "Earthshaker [T]" in the combat key hints, while a dive could start.
        private static readonly SetAbilityKeyHint DiveHint = new SetAbilityKeyHint("EL_Earthshaker",
            "$mod_epicloot_earthshaker_hint", () => CanShowHint(Player.m_localPlayer),
            () => ELConfig.EarthshakerKey, () => ELConfig.EarthshakerGamepadButton);

        // State for the local player only; SyncOwner drops it when the local player changes (respawn, logout).
        private static Player _owner;
        private static bool _hasSet;
        private static bool _wingsPublished;
        private static bool _divePublished;

        private static bool _diving;
        private static bool _landPending;          // velocity set to arrive at the surface this step; finish next step
        private static Vector3 _diveDir;
        private static float _diveSpeed;
        private static float _diveValue;
        private static bool _lowDive;              // begun with less than MinHeight of clear air below
        private static float _peakY;               // highest point of this flight, jump climb included
        private static float _diveStartTime;
        private static Vector3 _progressPosition;
        private static float _lastProgressTime;
        private static Vector3 _expectedPosition;
        private static bool _hasExpected;
        private static CollisionDetectionMode _savedCollisionMode;
        private static float _fallDamageGuardUntil;

        private static readonly HashSet<string> MissingFxLogged = new HashSet<string>();
        // Unordered results, so room for clutter (a crowd, a building's pieces) without dropping the nearest surface.
        private static readonly RaycastHit[] CastHits = new RaycastHit[64];
        private static readonly Collider[] SlamHits = new Collider[256];   // a long fall makes a wide slam
        private static readonly HashSet<GameObject> SlamHitSet = new HashSet<GameObject>();

        private static int _groundMask;
        private static int _castMask;
        private static int _slamMask;

        // Vanilla's Character.s_groundRayMask, which is only filled in once some character has woken.
        private static int GroundMask => _groundMask != 0 ? _groundMask : _groundMask = LayerMask.GetMask("Default",
            "static_solid", "Default_small", "piece", "terrain", "blocker", "vehicle");

        // The ground plus creatures and other players: landing on someone is a landing.
        private static int CastMask => _castMask != 0 ? _castMask : _castMask = GroundMask |
            LayerMask.GetMask("character", "character_net", "character_noenv");

        // Characters only (vanilla Attack's character layers): a base's walls neither take the slam nor fill the
        // hit buffer ahead of the enemies inside it.
        private static int SlamMask => _slamMask != 0 ? _slamMask : _slamMask = LayerMask.GetMask("character",
            "character_net", "character_ghost", "hitbox", "character_noenv");

        internal static bool LocalPlayerHasSet => _hasSet && _owner != null && _owner == Player.m_localPlayer;

        public static bool IsDiving(Character character)
        {
            return _diving && character != null && ReferenceEquals(character, _owner);
        }

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _hasSet = false;
            _wingsPublished = false;
            _divePublished = false;
            _diving = false;
            _landPending = false;
            _hasExpected = false;
            _fallDamageGuardUntil = 0f;
        }

        private static bool IsAirborne(Player player)
        {
            return !player.IsDead() && !player.IsOnGround() && !player.IsSwimming() && !player.IsAttached();
        }

        private static bool CanShowHint(Player player)
        {
            return player != null && player == _owner && _hasSet && !_diving && IsAirborne(player);
        }

        private static KeyCode Bound(BepInEx.Configuration.ConfigEntry<KeyCode> entry) => entry?.Value ?? KeyCode.None;

        private static bool KeyDown(KeyCode key) => key != KeyCode.None && ZInput.GetKeyDown(key, logWarning: false);

        private static bool KeyHeld(KeyCode key) => key != KeyCode.None && ZInput.GetKey(key, logWarning: false);

        // Called before the body of Player.Update, i.e. before vanilla reads this frame's buttons (T is OpenEmote,
        // read by HandleRadialInput).
        private static void OnLocalPlayerUpdate(Player player)
        {
            SyncOwner(player);
            _hasSet = player.HasActiveMagicEffect(MagicEffectType.Earthshaker, out float _);
            PublishWings(player);

            var key = Bound(ELConfig.EarthshakerKey);
            var button = Bound(ELConfig.EarthshakerGamepadButton);
            bool keyDown = KeyDown(key);
            bool buttonDown = SetAbilityKeys.GamepadButtonDown(player, button);
            if (!keyDown && !buttonDown || !_hasSet || !player.TakeInput() || Hud.InRadial() || !IsAirborne(player))
            {
                return;
            }

            // On the ground the key keeps its vanilla meaning; in the air with the set it belongs to Earthshaker,
            // whether or not a dive can start now.
            if (keyDown)
            {
                OverwhelmingLaunch.ConsumeKey(key, refresh: true);
            }
            if (buttonDown)
            {
                OverwhelmingLaunch.ConsumeKey(button, refresh: true);
            }

            TryStartDive(player);
        }

        // Keys whose vanilla action is read in the fixed step (attack, block, jump -- mostly a concern for the gamepad
        // button) have already been read by the time Player.Update runs, so they are held down here too.
        private static void OnLocalPlayerFixedUpdate(Player player)
        {
            var key = Bound(ELConfig.EarthshakerKey);
            var button = Bound(ELConfig.EarthshakerGamepadButton);
            bool keyHeld = KeyHeld(key);
            bool buttonHeld = SetAbilityKeys.GamepadButtonHeld(player, button);
            if (!keyHeld && !buttonHeld || !player.TakeInput() || !_diving && (!_hasSet || !IsAirborne(player)))
            {
                return;
            }

            if (keyHeld)
            {
                OverwhelmingLaunch.ConsumeKey(key, refresh: false);
            }
            if (buttonHeld)
            {
                OverwhelmingLaunch.ConsumeKey(button, refresh: false);
            }
        }

        private static void TryStartDive(Player player)
        {
            if (_diving || player.IsDebugFlying() || player.IsTeleporting() || player.InCutscene() ||
                player.InLiquidSwimDepth() || player.IsStaggering() || player.IsKnockedBack() || player.InAttack() ||
                player.GetAttackDrawPercentage() > 0f || player.InPlaceMode() || player.m_grappling > 0f ||
                player.m_body == null)
            {
                return;
            }

            if (!player.HasActiveMagicEffect(MagicEffectType.Earthshaker, out float value))
            {
                return;
            }

            float cost = StaminaCost;
            if (cost > 0f)
            {
                if (!player.HaveStamina(cost))
                {
                    Hud.instance?.StaminaBarEmptyFlash();
                    return;
                }
                player.UseStamina(cost);
            }

            StartDive(player, value, !HasClearance(player, MinHeight));
        }

        // Clear air under the feet: nothing solid within the height (a sphere the width of the body, cast down), and
        // no water surface either.
        private static bool HasClearance(Player player, float height)
        {
            if (height <= 0f)
            {
                return true;
            }

            Vector3 feet = player.transform.position;
            float radius = player.m_collider != null ? player.m_collider.radius * 0.9f : 0.3f;
            Vector3 origin = feet + Vector3.up * (radius + 0.05f);
            if (Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit _, height + 0.05f, GroundMask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            return feet.y - Floating.GetLiquidLevel(feet) >= height;
        }

        // The look direction's heading, pitched at least minAngle below the horizon. Looking straight up or down
        // leaves no heading in the look, so the body's facing gives it.
        private static Vector3 DiveDirection(Vector3 look, Vector3 fallbackForward, float minAngle)
        {
            Vector3 flat = new Vector3(look.x, 0f, look.z);
            if (flat.sqrMagnitude < 0.0001f)
            {
                flat = new Vector3(fallbackForward.x, 0f, fallbackForward.z);
            }
            if (flat.sqrMagnitude < 0.0001f)
            {
                flat = Vector3.forward;
            }
            flat.Normalize();

            float below = Mathf.Asin(Mathf.Clamp(-look.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
            float angle = Mathf.Clamp(Mathf.Max(below, minAngle), 0f, 90f) * Mathf.Deg2Rad;
            return (flat * Mathf.Cos(angle) + Vector3.down * Mathf.Sin(angle)).normalized;
        }

        // A wall in the way: carry on along its face, still at least MinDiveAngle down.
        private static Vector3 SlideAlong(Vector3 dir, Vector3 normal, float minAngle)
        {
            Vector3 slid = Vector3.ProjectOnPlane(dir, normal);
            if (slid.sqrMagnitude < 0.01f)
            {
                return Vector3.down;
            }

            slid.Normalize();
            return DiveDirection(slid, slid, minAngle);
        }

        private static void StartDive(Player player, float value, bool low)
        {
            var body = player.m_body;
            _diveDir = DiveDirection(player.GetLookDir(), player.transform.forward, MinDiveAngle);
            _diveSpeed = DiveSpeed;
            _diveValue = value;
            _lowDive = low;
            _peakY = Mathf.Max(player.m_maxAirAltitude, body.position.y);
            _diving = true;
            _landPending = false;
            _diveStartTime = Time.time;
            _progressPosition = body.position;
            _lastProgressTime = Time.time;
            _hasExpected = false;

            _savedCollisionMode = body.collisionDetectionMode;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            player.m_autoRun = false;
            FaceHeading(player);

            SpawnNetworkedFx(DiveFxPrefab, body.position, player.transform.rotation);
            PublishDive(player, true);
            EpicLoot.Log($"[Earthshaker] Dive from {body.position.y - _peakY:+0.#;-0.#;0} m below the peak, " +
                $"heading {_diveDir}, base damage {value:0.#}{(low ? ", too low for full damage" : "")}.");
        }

        private static void FaceHeading(Player player)
        {
            Vector3 flat = new Vector3(_diveDir.x, 0f, _diveDir.z);
            if (flat.sqrMagnitude > 0.001f)
            {
                player.m_body.rotation = Quaternion.LookRotation(flat.normalized);
            }
        }

        // One physics step of the dive, after vanilla's own motion code has run.
        private static void OnDiveStep(Player player, float dt)
        {
            var body = player.m_body;
            if (body == null || player.IsDead() || player.IsDebugFlying() || player.InIntro() || player.IsTeleporting() ||
                player.IsAttached() || player.InCutscene())
            {
                EndDive(player, landed: false);
                return;
            }

            // Into the sea: no slam, and no 30 m/s plunge to the bottom either.
            if (player.InLiquidSwimDepth())
            {
                var velocity = body.linearVelocity;
                velocity.y = Mathf.Max(velocity.y, -WaterExitSpeed);
                body.linearVelocity = velocity;
                EndDive(player, landed: false);
                return;
            }

            Vector3 position = body.position;
            if (_hasExpected && (position - _expectedPosition).sqrMagnitude > PositionJumpLimit * PositionJumpLimit)
            {
                EpicLoot.Log($"[Earthshaker] Dive ended: moved {Vector3.Distance(position, _expectedPosition):0.#} m off its path.");
                EndDive(player, landed: false);
                return;
            }

            if (_landPending)
            {
                body.linearVelocity = Vector3.zero;
                EndDive(player, landed: true);
                return;
            }

            float now = Time.time;
            if ((position - _progressPosition).sqrMagnitude > StuckDistance * StuckDistance)
            {
                _progressPosition = position;
                _lastProgressTime = now;
            }
            if (now - _lastProgressTime > StuckTime || now - _diveStartTime > MaxDiveTime)
            {
                EpicLoot.Log("[Earthshaker] Dive ended: stuck.");
                EndDive(player, landed: false);
                return;
            }
            _peakY = Mathf.Max(_peakY, position.y);

            // Steering: the dive turns after wherever the player now aims, at most SteerRate degrees a second, and
            // never shallower than MinDiveAngle. Looking straight down keeps the current heading.
            float steerRate = SteerRate;
            if (steerRate > 0f)
            {
                Vector3 wanted = DiveDirection(player.GetLookDir(), _diveDir, MinDiveAngle);
                _diveDir = Vector3.RotateTowards(_diveDir, wanted, steerRate * Mathf.Deg2Rad * dt, 0f).normalized;
            }

            if (CastAhead(player, _diveDir, _diveSpeed * dt + 0.1f, out RaycastHit hit, out bool landing))
            {
                if (landing)
                {
                    // Arrive exactly at the surface this step, slam now, stop on the next step.
                    float travel = Mathf.Max(0f, hit.distance - 0.02f);
                    Vector3 arrival = position + _diveDir * travel;
                    body.linearVelocity = dt > 0f ? _diveDir * (travel / dt) : Vector3.zero;
                    _expectedPosition = arrival;
                    _hasExpected = true;
                    _landPending = true;
                    Land(player, arrival, hit.point);
                    return;
                }

                _diveDir = SlideAlong(_diveDir, hit.normal, MinDiveAngle);
            }

            body.linearVelocity = _diveDir * _diveSpeed;
            FaceHeading(player);
            _expectedPosition = position + body.linearVelocity * dt;
            _hasExpected = true;
        }

        // The body's own capsule swept one step along the dive. A hit flatter than LandingNormalY, or on a
        // character, is a landing; anything steeper is a wall to slide along.
        private static bool CastAhead(Player player, Vector3 dir, float distance, out RaycastHit nearest, out bool landing)
        {
            nearest = default;
            landing = false;
            var capsule = player.m_collider;
            if (capsule == null)
            {
                return false;
            }

            Transform t = capsule.transform;
            Vector3 center = t.TransformPoint(capsule.center);
            Vector3 up = t.up;
            float radius = capsule.radius * 0.95f;
            float half = Mathf.Max(0f, capsule.height * 0.5f - capsule.radius);
            int count = Physics.CapsuleCastNonAlloc(center + up * half, center - up * half, radius, dir, CastHits,
                distance, CastMask, QueryTriggerInteraction.Ignore);

            bool found = false;
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = CastHits[i];
                // Colliders the capsule already overlaps report distance 0 at the origin; those are vanilla's to
                // resolve (and the fallback in UpdateGroundContact catches a real landing among them).
                if (candidate.collider == null || candidate.collider.attachedRigidbody == player.m_body ||
                    candidate.distance <= 0f && candidate.point == Vector3.zero || candidate.distance >= best)
                {
                    continue;
                }

                best = candidate.distance;
                nearest = candidate;
                found = true;
            }

            if (!found)
            {
                return false;
            }

            landing = nearest.normal.y >= LandingNormalY || nearest.collider.GetComponentInParent<Character>() != null;
            return true;
        }

        private static float WeightBonus(float weight)
        {
            return Mathf.Clamp((weight - WeightThreshold) / WeightPerPercent, 0f, MaxWeightBonus);
        }

        private static float FallBonus(float fall)
        {
            float bonus = Mathf.Max(0f, fall - FallThreshold) * FallBonusPerMetre;
            float cap = MaxFallBonus;
            return cap > 0f ? Mathf.Min(bonus, cap) : bonus;
        }

        // The base radius plus a metre for every FallPerRadiusMetre fallen, counted continuously (45 m adds 3 m).
        private static float SlamRadius(float fall)
        {
            float baseRadius = Radius;
            float radius = baseRadius + fall / FallPerRadiusMetre;
            float cap = MaxRadius;
            return cap > 0f ? Mathf.Min(radius, Mathf.Max(cap, baseRadius)) : radius;
        }

        private static void Land(Player player, Vector3 arrival, Vector3 groundPoint)
        {
            float fall = Mathf.Max(0f, _peakY - arrival.y);
            float weight = player.GetInventory()?.GetTotalWeight() ?? 0f;
            float weightBonus = WeightBonus(weight);
            float fallBonus = FallBonus(fall);
            float multiplier = 1f + (weightBonus + fallBonus) / 100f;
            float damage = _diveValue * multiplier;
            float radius = SlamRadius(fall);
            // Damage only: the impact still looks and reaches as far as the fall says.
            if (_lowDive)
            {
                damage *= 1f - LowDivePenalty / 100f;
            }

            // This landing is never a fall: vanilla still meets the ground a step or two later and measures from
            // m_maxAirAltitude.
            player.m_maxAirAltitude = arrival.y + LandingAltitudeMargin;
            _fallDamageGuardUntil = Time.time + FallDamageGuardTime;

            EpicLoot.Log($"[Earthshaker] Landed: fell {fall:0.#} m (+{fallBonus:0.#}%), carrying {weight:0.#} " +
                $"(+{weightBonus:0.#}%), x{multiplier:0.##}{(_lowDive ? $", -{LowDivePenalty:0.#}% for a low dive" : "")} -> " +
                $"{damage:0.#} damage in {radius:0.#} m.");
            DoSlam(player, arrival + Vector3.up * SlamCenterHeight, groundPoint, damage, multiplier, radius);
        }

        private static void EndDive(Player player, bool landed)
        {
            if (!_diving)
            {
                return;
            }

            _diving = false;
            _landPending = false;
            _hasExpected = false;
            var body = player.m_body;
            if (body != null)
            {
                body.collisionDetectionMode = _savedCollisionMode;
                if (landed)
                {
                    body.linearVelocity = Vector3.zero;
                }
            }

            if (landed)
            {
                // A dodge pressed late in the dive is queued until the player is on the ground.
                player.m_queuedDodgeTimer = 0f;
            }
            else
            {
                // The rest of the fall counts from here, as if the player had only just left the ground.
                player.m_maxAirAltitude = player.transform.position.y;
            }

            PublishDive(player, false);
        }

        // Runs before vanilla's UpdateGroundContact, which turns any ground contact into a landing and deals fall
        // damage from m_maxAirAltitude.
        private static void OnGroundContact(Player player)
        {
            if (!player.m_groundContact)
            {
                return;
            }

            float y = player.transform.position.y;
            if (_diving && !_landPending)
            {
                if (player.m_groundContactNormal.y < LandingNormalY)
                {
                    // A steep face grazed mid-dive; the dive slides on.
                    player.m_maxAirAltitude = Mathf.Min(player.m_maxAirAltitude, y + ContactAltitudeMargin);
                    return;
                }

                // The cast missed this surface (it moved into the path, or the dive started touching it).
                Land(player, player.m_body.position, player.m_groundContactPoint);
                EndDive(player, landed: true);
            }

            if (_diving || Time.time < _fallDamageGuardUntil)
            {
                player.m_maxAirAltitude = Mathf.Min(player.m_maxAirAltitude, y + LandingAltitudeMargin);
                if (!_diving)
                {
                    _fallDamageGuardUntil = 0f;
                }
            }
        }

        // The slam: vanilla Attack.DoAreaAttack's shape, but characters only, and an EpicLoot bonus hit -- the damage
        // is the set's, not a weapon's, so weapon on-hit enchants stay out of it.
        private static void DoSlam(Player player, Vector3 center, Vector3 groundPoint, float damage, float multiplier,
            float radius)
        {
            PlaySlamFx(player, groundPoint, multiplier, radius);

            float stagger = StaggerMultiplier;
            float push = PushForce;
            float half = damage * 0.5f;
            int damaged = 0;

            SlamHitSet.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, radius, SlamHits, SlamMask, QueryTriggerInteraction.UseGlobal);
            for (int i = 0; i < count; i++)
            {
                var collider = SlamHits[i];
                if (collider == null)
                {
                    continue;
                }

                var target = Projectile.FindHitObject(collider);
                if (target == null || target == player.gameObject || !SlamHitSet.Add(target))
                {
                    continue;
                }

                var character = target.GetComponent<Character>();
                if (character == null || character.IsDead())
                {
                    continue;
                }

                bool isEnemy = BaseAI.IsEnemy(player, character) ||
                    (character.GetBaseAI() != null && character.GetBaseAI().IsAggravatable());
                if (!player.IsPVPEnabled() && !isEnemy)
                {
                    continue;
                }

                if (character.IsDodgeInvincible())
                {
                    (character as Player)?.HitWhileDodging();
                    continue;
                }

                Vector3 point = collider is MeshCollider ? collider.ClosestPointOnBounds(center) : collider.ClosestPoint(center);

                // Knockback points out from the centre of the slam.
                Vector3 dir = point - center;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                {
                    dir = character.transform.position - player.transform.position;
                    dir.y = 0f;
                }
                if (dir.sqrMagnitude < 0.0001f)
                {
                    dir = player.transform.forward;
                }
                dir.Normalize();

                var hit = new HitData
                {
                    m_point = point,
                    m_dir = dir,
                    m_hitCollider = collider,
                    m_pushForce = push,
                    m_staggerMultiplier = stagger,
                    m_dodgeable = true,
                    m_blockable = true,
                    m_hitType = HitData.HitType.PlayerHit,
                    m_skill = Skills.SkillType.None,
                };
                hit.m_damage.m_blunt = half;
                hit.m_damage.m_pierce = half;
                hit.SetAttacker(player);
                HitSource.DealBonusDamage(character, hit);
                damaged++;
            }

            SlamHitSet.Clear();
            EpicLoot.Log($"[Earthshaker] Slam at {center}: {count} colliders, {damaged} damaged for {damage:0.#} " +
                $"in {radius:0.#} m.");
        }

        private static void PlaySlamFx(Player player, Vector3 point, float multiplier, float radius)
        {
            var nview = player.m_nview;
            if (nview != null && nview.IsValid())
            {
                nview.InvokeRPC(ZNetView.Everybody, FxRpc, point, multiplier, radius);
            }
        }

        // A networked vanilla fx prefab, spawned once by the owner so everyone sees it.
        internal static void SpawnNetworkedFx(string name, Vector3 position, Quaternion rotation)
        {
            var prefab = FindFxPrefab(name, logMissing: true);
            if (prefab != null)
            {
                Object.Instantiate(prefab, position, rotation);
            }
        }

        private static GameObject FindFxPrefab(string name, bool logMissing)
        {
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(name) : null;
            if (prefab == null && logMissing && MissingFxLogged.Add(name))
            {
                EpicLoot.LogWarning($"[Earthshaker] could not find the '{name}' prefab; playing without it.");
            }
            return prefab;
        }

        private static void SpawnLocalFx(GameObject prefab, Vector3 position, float scale)
        {
            LocalFx.Spawn(prefab, position, Quaternion.identity, scale, lifetime: LocalFxLifetime);
        }

        // World load (SetEffectWarmup): the slam's local copies, at the base radius and scaled, and the wings' assets.
        internal static void WarmupAtLoad(bool drawn)
        {
            if (!drawn)
            {
                return;
            }

            foreach (var name in SlamFxPrefabs)
            {
                var prefab = FindFxPrefab(name, logMissing: false);
                if (prefab != null)
                {
                    LocalFx.Prewarm(prefab, scaled: false);
                    LocalFx.Prewarm(prefab, scaled: true);
                    break;
                }
            }

            var feathers = FindFxPrefab(FeatherFxPrefab, logMissing: false);
            LocalFx.Prewarm(feathers, scaled: false);
            LocalFx.Prewarm(feathers, scaled: true);

            FreyjaWings.Prewarm();
        }

        // Every client, the diver included, at the slam's size: the ground slam, the feathers, and a camera shake
        // that grows with the hit and reaches further for a wider slam.
        private static void OnFxRpc(Vector3 point, float multiplier, float radius)
        {
            if (ZNet.instance != null && ZNet.instance.IsDedicated())
            {
                return;
            }

            float scale = Mathf.Max(0.2f, radius / DefaultRadius);
            foreach (var name in SlamFxPrefabs)
            {
                var prefab = FindFxPrefab(name, logMissing: false);
                if (prefab != null)
                {
                    SpawnLocalFx(prefab, point, scale);
                    break;
                }
            }

            var feathers = FindFxPrefab(FeatherFxPrefab, logMissing: true);
            if (feathers != null)
            {
                SpawnLocalFx(feathers, point, scale);
            }

            if (GameCamera.instance != null)
            {
                GameCamera.instance.AddShake(point, 20f + radius * 2f, Mathf.Clamp(0.75f + 0.25f * multiplier, 1f, 3f), false);
            }
        }

        // Both written only on change, so the ZDO revision moves once per change rather than every frame.
        private static void PublishWings(Player player)
        {
            if (_hasSet == _wingsPublished)
            {
                return;
            }

            _wingsPublished = _hasSet;
            player.m_nview?.GetZDO()?.Set(WingsZdoHash, _hasSet);
        }

        private static void PublishDive(Player player, bool diving)
        {
            if (diving == _divePublished)
            {
                return;
            }

            _divePublished = diving;
            player.m_nview?.GetZDO()?.Set(DivingZdoHash, diving);
        }

        // The lean into the dive, pitched about the feet like vanilla's wall-run tilt.
        private static Quaternion StoopRotation()
        {
            float below = Mathf.Asin(Mathf.Clamp(-_diveDir.y, -1f, 1f)) * Mathf.Rad2Deg;
            return Quaternion.Euler(Mathf.Clamp(below, 0f, MaxStoopAngle), 0f, 0f);
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    OnLocalPlayerUpdate(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
        private static class PlayerController_FixedUpdate_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(PlayerController __instance)
            {
                var player = __instance.m_character;
                if (player != null && player == Player.m_localPlayer)
                {
                    OnLocalPlayerFixedUpdate(player);
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateMotion))]
        private static class Character_UpdateMotion_Patch
        {
            // Without this, vanilla's air control queues a velocity change (AddForce) on top of the dive whenever a
            // move key is held -- and auto-run rewrites m_moveDir from the look direction.
            [HarmonyPrefix]
            private static void Prefix(Character __instance)
            {
                if (IsDiving(__instance))
                {
                    __instance.m_moveDir = Vector3.zero;
                }
            }

            [HarmonyPostfix]
            private static void Postfix(Character __instance, float dt)
            {
                if (IsDiving(__instance))
                {
                    OnDiveStep((Player)__instance, dt);
                }
            }
        }

        [HarmonyPatch(typeof(Character), nameof(Character.UpdateGroundContact))]
        private static class Character_UpdateGroundContact_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Character __instance)
            {
                if ((_diving || Time.time < _fallDamageGuardUntil) && ReferenceEquals(__instance, _owner) &&
                    __instance == Player.m_localPlayer)
                {
                    OnGroundContact((Player)__instance);
                }
            }
        }

        // Players tilt their visual for wall runs (CanWallRun is true for every player), and vanilla syncs that tilt
        // to other clients through the ZDO. The lean rides the same path: written here after vanilla's own easing,
        // which then brings it back upright once the dive ends.
        [HarmonyPatch(typeof(Character), nameof(Character.UpdateGroundTilt))]
        private static class Character_UpdateGroundTilt_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Character __instance, float dt)
            {
                if (!IsDiving(__instance) || __instance.m_visual == null)
                {
                    return;
                }

                var visual = __instance.m_visual.transform;
                visual.localRotation = Quaternion.RotateTowards(visual.localRotation, StoopRotation(), StoopTurnSpeed * dt);
                var rotation = visual.localRotation;
                if (!rotation.Equals(__instance.m_tiltRotCached))
                {
                    __instance.m_nview.GetZDO()?.Set(ZDOVars.s_tiltrot, rotation);
                    __instance.m_tiltRotCached = rotation;
                }
            }
        }

        // A committed dive: no movement keys, jump, dodge, attacks, block or crouch.
        [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
        private static class Player_SetControls_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, ref Vector3 movedir, ref bool attack, ref bool attackHold,
                ref bool secondaryAttack, ref bool secondaryAttackHold, ref bool block, ref bool blockHold, ref bool jump,
                ref bool crouch, ref bool autoRun, ref bool dodge)
            {
                if (!IsDiving(__instance))
                {
                    return;
                }

                movedir = Vector3.zero;
                attack = attackHold = secondaryAttack = secondaryAttackHold = false;
                block = blockHold = jump = crouch = autoRun = dodge = false;
            }
        }

        // Every attack goes through Attack.Start, Overwhelming Launch's throw included.
        [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        private static class Attack_Start_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Humanoid character, ref bool __result)
            {
                if (!IsDiving(character))
                {
                    return true;
                }

                __result = false;
                return false;
            }
        }

        // Every player, local or remote, carries the wings; a headless server draws nothing.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                __instance.m_nview?.Register<Vector3, float, float>(FxRpc,
                    (sender, point, multiplier, radius) => OnFxRpc(point, multiplier, radius));
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    __instance.gameObject.AddComponent<FreyjaWings>();
                }
            }
        }

        [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
        private static class KeyHints_UpdateHints_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(KeyHints __instance) => DiveHint.Refresh(__instance);
        }
    }

    // Dark wings on anyone wearing the full Freyja's Falcon set, on every client: the local player from Earthshaker's
    // own state, anyone else from the flag their client mirrors to the ZDO. Folded down the back on the ground,
    // unfurled in the air (a slow glide flap, and beats while rising), swept half way back and held still in the dive,
    // folded again on landing.
    //
    // Built entirely in code: per wing a three-bone chain (arm, forearm, hand) with one bone per feather, skinned to a
    // single quad-per-feather mesh shared by every player; a generated feather texture; and a clone of the Fallen
    // Valkyrie's material, for its alpha-tested, two-sided creature shader. The rig hangs under the player's Visual,
    // so VisEquipment puts it in the LOD group and vanilla hides it with the body (first person, distance).
    public class FreyjaWings : MonoBehaviour
    {
        // Rig size, in metres before WingScale.
        private const float WingScale = 0.8f;
        private const float ArmLength = 0.45f;
        private const float ForearmLength = 0.55f;
        private const float HandLength = 0.5f;
        private const float RootSpacing = 0.07f;                                  // each root this far from the spine
        private static readonly Vector3 AnchorOffset = new Vector3(0f, 0.04f, -0.17f); // from Spine2, in the back's frame

        // Pose blending, seconds.
        private const float UnfurlTime = 0.3f;
        private const float FoldTime = 0.4f;
        private const float TuckTime = 0.12f;          // from 0 to the full tuck; the dive goes DiveSweep of it
        private const float UntuckTime = 0.3f;
        private const float DiveSweep = 0.5f;          // how far from spread toward the full tuck the dive sweeps
        private const float AirborneHysteresis = 0.12f;

        // Flapping: a slow glide while spread, beats while rising, a strong beat on each jump.
        private const float GlideRate = 0.6f;          // cycles per second
        private const float GlideAngle = 6f;           // degrees either side
        private const float BeatRate = 2.4f;
        private const float BeatAngle = 26f;
        private const float BeatDecay = 0.5f;          // seconds a jump's beat takes to settle back to the glide
        private const float RisingSpeed = 1.5f;        // m/s upward that counts as rising
        private const float JumpImpulse = 4f;          // a jump in one frame's change of upward speed
        private const float FlapSoundInterval = 0.35f;
        private const float RemoteStoopCatchUp = 360f; // degrees per second

        private const int Arm = 0;
        private const int Forearm = 1;
        private const int Hand = 2;
        private const int SegmentCount = 3;

        private static readonly int FallingZdoKey = 438569 + ZSyncAnimation.GetHash("falling");

        private struct FeatherDef
        {
            public int Segment;
            public float Along;       // fraction of the segment's length
            public float Length;
            public float Width;
            public float Lift;        // offset along the wing's surface normal, so overlapping feathers never z-fight
            public float Lead;        // offset toward the leading edge (coverts hide the bone line)
            public float Spread;      // angle in the wing plane: 0 trails straight back, 90 points along the bone
            public float Fold;
            public float Tuck;
        }

        // One orientation per segment and pose, in the wing root's frame (x out along the spread wing, y up,
        // z forward), given as the direction the bone points and the direction its feathers trail.
        private struct SegmentPose
        {
            public Quaternion Fold;
            public Quaternion Spread;
            public Quaternion Tuck;
        }

        private class Wing
        {
            public Transform Root;
            public Transform[] Segments;
            public Transform[] Feathers;
            public SkinnedMeshRenderer Renderer;
        }

        private static readonly Quaternion[] PoseFrames = new Quaternion[SegmentCount];   // scratch for ApplyPose
        private static FeatherDef[] _feathers;
        private static SegmentPose[] _segmentPoses;
        private static float[] _segmentLengths;
        private static Mesh _mesh;
        private static Material _material;
        private static bool _assetsFailed;
        private static bool _boneMissingLogged;

        // The rig every player's wings are copied from, kept inactive: one Instantiate per player instead of building
        // ~110 objects. Instantiate remaps each renderer's bones to the copy's own transforms.
        private static GameObject _templateRoot;
        private static Transform _templateAnchor;

        private Player _player;
        private bool _built;
        private bool _failed;
        private bool _visible;
        private Transform _anchor;
        private Transform _spine;
        private Transform _spine2;
        private Transform _neck;
        private Wing _right;
        private Wing _left;

        private bool _airborne;
        private float _airborneFlipTime;
        private float _spread;
        private float _tuck;
        private float _flapPhase;
        private float _flapAngle;
        private float _beat;
        private float _prevVelocityY;
        private float _lastFlapSound;

        // The pose last written to the rig; an unchanged pose (standing, a held glide) is not written again.
        private float _appliedSpread = float.NaN;
        private float _appliedTuck = float.NaN;
        private float _appliedFlap = float.NaN;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private bool IsLocal => _player != null && ReferenceEquals(_player, Player.m_localPlayer);

        // The main-menu preview player has no ZDO, so it never grows wings.
        private bool ShouldShow()
        {
            if (_player == null)
            {
                return false;
            }

            if (IsLocal)
            {
                return Earthshaker.LocalPlayerHasSet && (ELConfig.ShowOwnFalconWings?.Value ?? true);
            }

            ZDO zdo = _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            return zdo != null && zdo.GetBool(Earthshaker.WingsZdoHash);
        }

        private void Update()
        {
            bool show = ShouldShow();
            if (show && !_built && !_failed)
            {
                Build();
            }

            if (!_built || show == _visible)
            {
                return;
            }

            _visible = show;
            _right.Renderer.enabled = show;
            _left.Renderer.enabled = show;
            if (show)
            {
                // Start from the pose the player is in rather than unfolding on the spot.
                _airborne = IsAirborne();
                _spread = _airborne ? 1f : 0f;
                _tuck = 0f;
                _airborneFlipTime = 0f;
            }
        }

        private void LateUpdate()
        {
            if (_player == null)
            {
                return;
            }

            if (!IsLocal)
            {
                CatchUpRemoteStoop();
            }

            if (!_built || !_visible)
            {
                // Your own wings hidden (Show Own Falcon Wings): the flaps the owner plays for everyone still sound.
                if (IsLocal && Earthshaker.LocalPlayerHasSet)
                {
                    UpdatePose(Time.deltaTime);
                }
                return;
            }

            // The spine the wings hang from is gone (a mod rebuilt the model): rebuilt on the next frame.
            if (_spine2 == null)
            {
                Teardown();
                return;
            }

            UpdatePose(Time.deltaTime);
            PlaceAnchor();
            if (_right.Renderer.isVisible || _left.Renderer.isVisible)
            {
                ApplyPose();
            }
        }

        private void Teardown()
        {
            if (_anchor != null)
            {
                Destroy(_anchor.gameObject);
            }

            _anchor = null;
            _right = null;
            _left = null;
            _built = false;
            _visible = false;
        }

        // Vanilla eases a remote player's visual toward the synced tilt at 50 degrees per second, too slow to show a
        // dive that lasts about a second; while one is on, close the gap faster.
        private void CatchUpRemoteStoop()
        {
            ZDO zdo = _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            if (zdo == null || _player.m_visual == null || !zdo.GetBool(Earthshaker.DivingZdoHash))
            {
                return;
            }

            var visual = _player.m_visual.transform;
            var target = zdo.GetQuaternion(ZDOVars.s_tiltrot, Quaternion.identity);
            visual.localRotation = Quaternion.RotateTowards(visual.localRotation, target, RemoteStoopCatchUp * Time.deltaTime);
        }

        // The owner writes every animator bool it sets into the ZDO (ZSyncAnimation.SetBool); remote animators only
        // receive the ones the prefab lists, but the ZDO has them all. "falling" is set after 0.1 s in the air and
        // cleared on the ground, in swim depth and when attached.
        private bool IsAirborne()
        {
            ZDO zdo = _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            return zdo != null && zdo.GetInt(FallingZdoKey) != 0;
        }

        private bool IsDiving()
        {
            if (IsLocal)
            {
                return Earthshaker.IsDiving(_player);
            }

            ZDO zdo = _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            return zdo != null && zdo.GetBool(Earthshaker.DivingZdoHash);
        }

        private void UpdatePose(float dt)
        {
            bool raw = IsAirborne();
            if (raw != _airborne)
            {
                _airborneFlipTime += dt;
                if (_airborneFlipTime >= AirborneHysteresis)
                {
                    _airborne = raw;
                    _airborneFlipTime = 0f;
                    if (raw)
                    {
                        PlayFlapSound();
                    }
                }
            }
            else
            {
                _airborneFlipTime = 0f;
            }

            // In the dive the wings stay open and sweep only DiveSweep of the way back toward the full tuck.
            bool diving = IsDiving();
            float spreadTarget = _airborne || diving ? 1f : 0f;
            float tuckTarget = diving ? DiveSweep : 0f;
            _spread = Mathf.MoveTowards(_spread, spreadTarget, dt / (spreadTarget > _spread ? UnfurlTime : FoldTime));
            _tuck = Mathf.MoveTowards(_tuck, tuckTarget, dt / (tuckTarget > _tuck ? TuckTime : UntuckTime));

            float velocityY = _player.GetVelocity().y;
            if (_spread > 0.5f && !diving && velocityY - _prevVelocityY > JumpImpulse)
            {
                // A jump (or double jump): start a downstroke from the top of the beat.
                _beat = 1f;
                _flapPhase = 0.25f;
                PlayFlapSound();
            }
            _prevVelocityY = velocityY;
            _beat = Mathf.MoveTowards(_beat, 0f, dt / BeatDecay);

            float rising = Mathf.Max(_beat, velocityY > RisingSpeed ? 1f : 0f);
            float rate = Mathf.Lerp(GlideRate, BeatRate, rising);
            float amplitude = Mathf.Lerp(GlideAngle, BeatAngle, rising);
            _flapPhase = Mathf.Repeat(_flapPhase + dt * rate, 1f);
            float held = Mathf.Clamp01(_tuck / DiveSweep);      // the wings are held still through the dive
            _flapAngle = amplitude * Mathf.Sin(_flapPhase * Mathf.PI * 2f) * _spread * (1f - held);
        }

        // The owner plays the networked flap, so every client hears it once.
        private void PlayFlapSound()
        {
            if (!IsLocal || Time.time - _lastFlapSound < FlapSoundInterval)
            {
                return;
            }

            _lastFlapSound = Time.time;
            Earthshaker.SpawnNetworkedFx(Earthshaker.WingFlapFxPrefab, _anchor != null ? _anchor.position : transform.position,
                transform.rotation);
        }

        // The back's frame from the spine bones, so the wings follow a lean or a crouch whatever the bones' own axes.
        private void PlaceAnchor()
        {
            Transform visual = _player.m_visual.transform;
            Vector3 up = _neck != null && _spine != null ? _neck.position - _spine.position : visual.up;
            if (up.sqrMagnitude < 0.000001f)
            {
                up = visual.up;
            }
            up.Normalize();

            Vector3 forward = Vector3.ProjectOnPlane(visual.forward, up);
            if (forward.sqrMagnitude < 0.000001f)
            {
                forward = visual.forward;
            }

            var rotation = Quaternion.LookRotation(forward.normalized, up);
            _anchor.SetPositionAndRotation(_spine2.position + rotation * AnchorOffset, rotation);
        }

        // The same local rotations for both wings: the left one is mirrored by its root's negative scale.
        private void ApplyPose()
        {
            float s = _spread;
            float k = _tuck;
            if (s == _appliedSpread && k == _appliedTuck && _flapAngle == _appliedFlap)
            {
                return;
            }

            _appliedSpread = s;
            _appliedTuck = k;
            _appliedFlap = _flapAngle;

            var frames = PoseFrames;
            for (int i = 0; i < SegmentCount; i++)
            {
                var pose = _segmentPoses[i];
                frames[i] = Quaternion.Slerp(Quaternion.Slerp(pose.Fold, pose.Spread, s), pose.Tuck, k);
            }

            // The flap raises and lowers the whole wing at the shoulder, with a little more bend at the elbow.
            var flap = Quaternion.AngleAxis(_flapAngle, Vector3.forward);
            var arm = flap * frames[Arm];
            var forearm = Quaternion.Inverse(frames[Arm]) * Quaternion.AngleAxis(_flapAngle * 0.4f, Vector3.forward) *
                frames[Forearm];
            var hand = Quaternion.Inverse(frames[Forearm]) * frames[Hand];
            _right.Segments[Arm].localRotation = arm;
            _left.Segments[Arm].localRotation = arm;
            _right.Segments[Forearm].localRotation = forearm;
            _left.Segments[Forearm].localRotation = forearm;
            _right.Segments[Hand].localRotation = hand;
            _left.Segments[Hand].localRotation = hand;

            for (int i = 0; i < _feathers.Length; i++)
            {
                var def = _feathers[i];
                float angle = Mathf.Lerp(Mathf.Lerp(def.Fold, def.Spread, s), def.Tuck, k);
                var rotation = Quaternion.Euler(0f, -angle, 0f);
                _right.Feathers[i].localRotation = rotation;
                _left.Feathers[i].localRotation = rotation;
            }
        }

        private void Build()
        {
            var visual = _player.m_visual;
            if (visual == null || !EnsureTemplate())
            {
                _failed = true;
                return;
            }

            var animator = _player.m_animator;
            _spine = FindBone(visual.transform, "Spine", animator, HumanBodyBones.Spine);
            _spine2 = FindBone(visual.transform, "Spine2", animator, HumanBodyBones.UpperChest) ??
                FindBone(visual.transform, null, animator, HumanBodyBones.Chest);
            _neck = FindBone(visual.transform, "Neck", animator, HumanBodyBones.Neck);
            if (_spine2 == null)
            {
                if (!_boneMissingLogged)
                {
                    _boneMissingLogged = true;
                    EpicLoot.LogWarning("[Earthshaker] no Spine2/UpperChest bone on the player model; the wings are not shown.");
                }
                _failed = true;
                return;
            }

            _anchor = Instantiate(_templateAnchor.gameObject, visual.transform, false).transform;
            _anchor.name = _templateAnchor.name;
            _right = WingFrom(_anchor.Find("RightWing"));
            _left = WingFrom(_anchor.Find("LeftWing"));
            if (_right == null || _left == null)
            {
                Destroy(_anchor.gameObject);
                _anchor = null;
                _failed = true;
                return;
            }

            SetLayer(_anchor, visual.layer);
            _right.Renderer.enabled = false;
            _left.Renderer.enabled = false;
            _appliedSpread = float.NaN;
            _built = true;

            // VisEquipment collects the LOD renderers only when the equipment changes; add ours now.
            if (_player.m_visEquipment != null)
            {
                _player.m_visEquipment.UpdateLodgroup();
            }
        }

        // A copy's wing, read back from its renderer's bones: the segments first, then the feathers, as BuildWing
        // ordered them.
        private static Wing WingFrom(Transform root)
        {
            var renderer = root != null ? root.GetComponent<SkinnedMeshRenderer>() : null;
            var bones = renderer != null ? renderer.bones : null;
            if (bones == null || bones.Length != SegmentCount + _feathers.Length)
            {
                return null;
            }

            var wing = new Wing
            {
                Root = root,
                Segments = new Transform[SegmentCount],
                Feathers = new Transform[_feathers.Length],
                Renderer = renderer,
            };
            System.Array.Copy(bones, 0, wing.Segments, 0, SegmentCount);
            System.Array.Copy(bones, SegmentCount, wing.Feathers, 0, _feathers.Length);
            return wing;
        }

        // World load (SetEffectWarmup), so neither the first player to show wings nor any later one builds them.
        internal static void Prewarm()
        {
            EnsureTemplate();
        }

        private static bool EnsureTemplate()
        {
            if (_templateAnchor != null)
            {
                return true;
            }

            if (!EnsureAssets())
            {
                return false;
            }

            if (_templateRoot == null)
            {
                _templateRoot = new GameObject("EL_FalconWingsTemplate");
                _templateRoot.SetActive(false);
                DontDestroyOnLoad(_templateRoot);
            }

            _templateAnchor = new GameObject("EL_FalconWings").transform;
            _templateAnchor.SetParent(_templateRoot.transform, false);
            BuildWing(_templateAnchor, 1f);
            BuildWing(_templateAnchor, -1f);
            return true;
        }

        private static Transform FindBone(Transform visual, string name, Animator animator, HumanBodyBones fallback)
        {
            Transform bone = name != null ? Utils.FindChild(visual, name) : null;
            if (bone == null && animator != null && animator.isHuman)
            {
                bone = animator.GetBoneTransform(fallback);
            }
            return bone;
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            for (int i = 0; i < t.childCount; i++)
            {
                SetLayer(t.GetChild(i), layer);
            }
        }

        // The left wing is the right one mirrored through a negative scale on its root.
        private static Wing BuildWing(Transform anchor, float side)
        {
            var wing = new Wing
            {
                Root = new GameObject(side > 0f ? "RightWing" : "LeftWing").transform,
                Segments = new Transform[SegmentCount],
                Feathers = new Transform[_feathers.Length],
            };
            wing.Root.SetParent(anchor, false);
            wing.Root.localPosition = new Vector3(RootSpacing * side, 0f, 0f);
            wing.Root.localScale = new Vector3(WingScale * side, WingScale, WingScale);

            Transform parent = wing.Root;
            float offset = 0f;
            for (int i = 0; i < SegmentCount; i++)
            {
                var segment = new GameObject(i == Arm ? "Arm" : i == Forearm ? "Forearm" : "Hand").transform;
                segment.SetParent(parent, false);
                segment.localPosition = new Vector3(offset, 0f, 0f);
                wing.Segments[i] = segment;
                parent = segment;
                offset = _segmentLengths[i];
            }

            for (int i = 0; i < _feathers.Length; i++)
            {
                var def = _feathers[i];
                var feather = new GameObject("Feather" + i).transform;
                feather.SetParent(wing.Segments[def.Segment], false);
                feather.localPosition = new Vector3(def.Along * _segmentLengths[def.Segment], def.Lift, def.Lead);
                wing.Feathers[i] = feather;
            }

            var bones = new Transform[SegmentCount + _feathers.Length];
            wing.Segments.CopyTo(bones, 0);
            wing.Feathers.CopyTo(bones, SegmentCount);

            var renderer = wing.Root.gameObject.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = _mesh;
            renderer.sharedMaterial = _material;
            renderer.bones = bones;
            renderer.rootBone = wing.Root;
            renderer.quality = SkinQuality.Bone1;
            renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 7f);
            renderer.updateWhenOffscreen = false;
            wing.Renderer = renderer;
            return wing;
        }

        private static bool EnsureAssets()
        {
            if (_mesh != null && _material != null)
            {
                return true;
            }
            if (_assetsFailed)
            {
                return false;
            }

            _feathers = BuildFeatherTable();
            _segmentLengths = new[] { ArmLength, ForearmLength, HandLength };
            _segmentPoses = BuildSegmentPoses();
            _material = BuildMaterial();
            if (_material == null)
            {
                _assetsFailed = true;
                EpicLoot.LogWarning("[Earthshaker] no creature shader found for the wings; they are not shown.");
                return false;
            }

            // Only once there is a material to carry it.
            SetTexture(_material, "_MainTex", BuildFeatherTexture());
            _mesh = BuildMesh(_feathers);
            return true;
        }

        // An orientation whose x axis is the bone and whose -z axis is the feathers' trailing direction.
        private static Quaternion Frame(Vector3 bone, Vector3 trailing)
        {
            Vector3 x = bone.normalized;
            Vector3 z = -Vector3.ProjectOnPlane(trailing, x).normalized;
            Vector3 y = Vector3.Cross(z, x);
            return Quaternion.LookRotation(z, y);
        }

        // Right wing, root frame. Folded: a Z down the back (arm down, forearm back up so the wrist arches at the
        // shoulder, hand down again), the surface flat against the back. Spread: raised in a V and swept back at the
        // wrist, the surface tilted toward the camera behind the player so it reads as wings rather than an edge.
        // Tucked: swept back along the body toward the feet. The dive goes DiveSweep of the way from spread to tucked,
        // which with the body leaning in reads as a wide V over the back. Checked offline against a body outline
        // (back, side, top and gameplay-camera views) before tuning in game.
        private static SegmentPose[] BuildSegmentPoses()
        {
            return new[]
            {
                new SegmentPose
                {
                    Fold = Frame(new Vector3(0.22f, -0.97f, 0f), new Vector3(-1f, 0f, 0f)),
                    Spread = Frame(new Vector3(0.82f, 0.52f, -0.22f), new Vector3(0f, -0.55f, -0.85f)),
                    Tuck = Frame(new Vector3(0.5f, -0.6f, -0.62f), new Vector3(-1f, 0f, 0f)),
                },
                new SegmentPose
                {
                    Fold = Frame(new Vector3(-0.12f, 0.99f, -0.02f), new Vector3(-1f, 0f, 0f)),
                    Spread = Frame(new Vector3(0.92f, 0.33f, -0.2f), new Vector3(0.05f, -0.5f, -0.85f)),
                    Tuck = Frame(new Vector3(0.25f, -0.9f, -0.35f), new Vector3(-1f, 0f, 0f)),
                },
                new SegmentPose
                {
                    Fold = Frame(new Vector3(0.06f, -1f, 0f), new Vector3(-1f, 0f, 0f)),
                    Spread = Frame(new Vector3(0.9f, 0.1f, -0.42f), new Vector3(0.3f, -0.45f, -0.85f)),
                    Tuck = Frame(new Vector3(0.1f, -0.9f, -0.42f), new Vector3(-1f, 0f, 0f)),
                },
            };
        }

        // Flight feathers (primaries on the hand, secondaries on the forearm, tertials at the elbow) and three rows
        // of coverts over their bases. In the folded pose the forearm points up, so its feathers fold to about -70
        // (back down the bone); everything else folds along its bone, fanned a little toward the spine so the
        // folded wings cover the back.
        private static FeatherDef[] BuildFeatherTable()
        {
            var list = new List<FeatherDef>();

            void Row(int segment, int count, float alongFrom, float alongTo, float lengthFrom, float lengthTo,
                float width, int layer, float spreadFrom, float spreadTo, float foldFrom, float foldTo, float tuckFrom,
                float tuckTo, float lead = 0f)
            {
                for (int i = 0; i < count; i++)
                {
                    float f = count > 1 ? i / (float)(count - 1) : 0f;
                    list.Add(new FeatherDef
                    {
                        Segment = segment,
                        Along = Mathf.Lerp(alongFrom, alongTo, f),
                        Length = Mathf.Lerp(lengthFrom, lengthTo, f),
                        Width = width,
                        Lift = layer * 0.012f + (i % 3) * 0.002f,
                        Lead = lead,
                        Spread = Mathf.Lerp(spreadFrom, spreadTo, f),
                        Fold = Mathf.Lerp(foldFrom, foldTo, f),
                        Tuck = Mathf.Lerp(tuckFrom, tuckTo, f),
                    });
                }
            }

            // Primaries, inner to outer: longer and fanning further toward the tip.
            Row(Hand, 9, 0.08f, 0.92f, 0.58f, 0.85f, 0.11f, 0, 12f, 80f, 70f, 84f, 76f, 86f);
            // Secondaries along the forearm.
            Row(Forearm, 10, 0.04f, 0.96f, 0.52f, 0.48f, 0.13f, 0, -6f, 8f, -72f, -62f, 84f, 78f);
            // Tertials at the elbow end of the arm, longest nearest the elbow.
            Row(Arm, 4, 0.5f, 0.95f, 0.36f, 0.46f, 0.14f, 0, -18f, -6f, 60f, 70f, 55f, 65f);
            // Greater coverts over the secondaries, primary coverts over the primaries, scapulars over the arm.
            Row(Forearm, 8, 0.06f, 0.94f, 0.27f, 0.27f, 0.12f, 1, -4f, 6f, -72f, -64f, 82f, 76f, 0.02f);
            Row(Hand, 5, 0.06f, 0.6f, 0.3f, 0.3f, 0.1f, 1, 8f, 45f, 76f, 80f, 80f, 80f, 0.02f);
            Row(Arm, 6, 0.1f, 0.95f, 0.26f, 0.26f, 0.13f, 1, -10f, -10f, 66f, 66f, 58f, 58f, 0.02f);
            // Lesser coverts along the leading edge.
            Row(Forearm, 7, 0.05f, 0.9f, 0.15f, 0.15f, 0.1f, 2, 0f, 0f, -70f, -70f, 80f, 80f, 0.03f);

            return list.ToArray();
        }

        // One quad per feather in its own bone's space (base at the bone, tip along -z, drooping slightly), so every
        // bindpose is identity. Three rows of vertices give the droop a curve.
        private static Mesh BuildMesh(FeatherDef[] feathers)
        {
            int boneCount = SegmentCount + feathers.Length;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var triangles = new List<int>();

            for (int f = 0; f < feathers.Length; f++)
            {
                var def = feathers[f];
                int bone = SegmentCount + f;
                int first = vertices.Count;
                for (int row = 0; row < 3; row++)
                {
                    float v = row * 0.5f;
                    float z = -v * def.Length;
                    float y = -0.05f * def.Length * v * v;
                    vertices.Add(new Vector3(-def.Width * 0.5f, y, z));
                    vertices.Add(new Vector3(def.Width * 0.5f, y, z));
                    uvs.Add(new Vector2(0f, v));
                    uvs.Add(new Vector2(1f, v));
                    normals.Add(Vector3.up);
                    normals.Add(Vector3.up);
                    weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1f });
                    weights.Add(new BoneWeight { boneIndex0 = bone, weight0 = 1f });
                }

                for (int row = 0; row < 2; row++)
                {
                    int a = first + row * 2;
                    triangles.Add(a);
                    triangles.Add(a + 2);
                    triangles.Add(a + 1);
                    triangles.Add(a + 1);
                    triangles.Add(a + 2);
                    triangles.Add(a + 3);
                }
            }

            var bindposes = new Matrix4x4[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                bindposes[i] = Matrix4x4.identity;
            }

            var mesh = new Mesh { name = "EL_FalconWing" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.boneWeights = weights.ToArray();
            mesh.bindposes = bindposes;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 7f);
            return mesh;
        }

        // The Fallen Valkyrie's material carries the creature shader with alpha test and two-sided rendering
        // already switched on; only its textures change. Textures go on the material itself, since MaterialMan
        // replaces every property block on the player's renderers.
        private static Material BuildMaterial()
        {
            Material source = null;
            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("FallenValkyrie") : null;
            if (prefab != null)
            {
                foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var candidate = renderer.sharedMaterial;
                    if (candidate != null && candidate.shader != null && candidate.shader.name == "Custom/Creature")
                    {
                        source = candidate;
                        break;
                    }
                }
            }

            Material material;
            if (source != null)
            {
                material = new Material(source);
            }
            else
            {
                var shader = Shader.Find("Custom/Creature");
                if (shader == null)
                {
                    return null;
                }
                material = new Material(shader);
                material.EnableKeyword("_ALPHATEST_ON");
            }

            material.name = "EL_FalconWings";
            SetTexture(material, "_BumpMap", null);
            SetTexture(material, "_EmissionMap", null);
            SetTexture(material, "_MetallicGlossMap", null);
            material.DisableKeyword("_NORMALMAP");
            material.DisableKeyword("_EMISSION");
            material.DisableKeyword("_METALLICGLOSSMAP");
            SetColor(material, "_Color", Color.white);
            SetColor(material, "_EmissionColor", Color.black);
            SetFloat(material, "_Metallic", 0f);
            SetFloat(material, "_Glossiness", 0.3f);
            SetFloat(material, "_Cutoff", 0.5f);
            SetFloat(material, "_Cull", 0f);
            SetFloat(material, "_TwoSidedNormals", 1f);
            material.EnableKeyword("_TWOSIDEDNORMALS_ON");
            return material;
        }

        private static void SetTexture(Material material, string name, Texture texture)
        {
            if (material.HasProperty(name))
            {
                material.SetTexture(name, texture);
            }
        }

        private static void SetColor(Material material, string name, Color color)
        {
            if (material.HasProperty(name))
            {
                material.SetColor(name, color);
            }
        }

        private static void SetFloat(Material material, string name, float value)
        {
            if (material.HasProperty(name))
            {
                material.SetFloat(name, value);
            }
        }

        // A dark flight feather, base at v = 0 and tip at v = 1: a bare quill, then a downy start to the vane, barbs
        // angled toward the tip, a lighter rachis, a near-black base shading to a faint violet sheen at the tip, and
        // a few splits between the barbs for the ragged look of the Fallen Valkyries.
        private static Texture2D BuildFeatherTexture()
        {
            const int width = 128;
            const int height = 512;
            const float barbsPerUnitV = 150f;
            const float barbSlant = 55f;
            var tears = new[] { new Vector3(0.42f, -1f, 0.55f), new Vector3(0.63f, 1f, 0.7f), new Vector3(0.78f, -1f, 0.4f) };
            var dark = new Color(0.03f, 0.03f, 0.04f);
            var sheen = new Color(0.13f, 0.11f, 0.2f);
            var quillBase = new Color(0.24f, 0.23f, 0.26f);
            var quillTip = new Color(0.12f, 0.11f, 0.14f);

            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float t = Mathf.Clamp01((v - 0.08f) / 0.92f);
                float vane = 0.46f * Mathf.Sqrt(Mathf.Min(1f, t * 4f)) * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t * t * t));
                float rachis = Mathf.Lerp(0.022f, 0.006f, v);

                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width - 0.5f;
                    float a = Mathf.Abs(u);
                    float side = u < 0f ? -1f : 1f;
                    float half = side > 0f ? vane * 0.72f : vane;    // the outer vane is the narrow one

                    float barb = v * barbsPerUnitV - a * barbSlant;
                    float stripe = 0.5f + 0.5f * Mathf.Sin(barb * Mathf.PI * 2f);
                    float edge = half * (0.94f + 0.06f * Mathf.Sin(barb * Mathf.PI));
                    float alpha = v < 0.08f ? 0f : Mathf.Clamp01((edge - a) * width / 1.5f);

                    foreach (var tear in tears)
                    {
                        if (side == tear.y && a > edge * (1f - tear.z) && Mathf.Abs(barb - tear.x * barbsPerUnitV) < 0.6f)
                        {
                            alpha = 0f;
                        }
                    }

                    // Down at the base of the vane: thins out toward the quill.
                    if (v < 0.2f && Hash(x, y) < (0.2f - v) / 0.12f * 0.8f)
                    {
                        alpha = 0f;
                    }

                    float edgeFraction = half > 0.001f ? Mathf.Clamp01(a / half) : 0f;
                    Color color = Color.Lerp(dark, sheen, Mathf.Pow(v, 1.6f) * (0.55f + 0.45f * edgeFraction));
                    color *= (0.8f + 0.25f * stripe) * (0.95f + 0.1f * Hash(x + 17, y + 31));

                    if (a < rachis)
                    {
                        color = Color.Lerp(quillBase, quillTip, v);
                        alpha = v < 0.995f ? 1f : alpha;
                    }

                    color.a = alpha;
                    pixels[y * width + x] = color;
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, true)
            {
                name = "EL_FalconFeather",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 4,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true, true);
            return texture;
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return (h & 0xFFFF) / 65535f;
            }
        }
    }
}
