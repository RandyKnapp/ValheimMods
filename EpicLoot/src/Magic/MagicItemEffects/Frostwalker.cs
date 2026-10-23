using System.Collections.Generic;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Frostwalker = nameof(Frostwalker);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Frostwalker (4-piece Rime of Elivagar set bonus, NoRoll). The water freezes under the wearer so they can walk on
    // it, draining value Eitr per second (AshlandsDrainMultiplier times that over the boiling sea). Natural Eitr regen is
    // paused on the ice and in the water alike, so breaking through to swim is no way to refill. Wading out to knee depth
    // or swimming freezes it. Crouching on the ice, or running dry, breaks it, and it stays broken until the wearer is
    // back on solid ground or presses crouch again while swimming.
    //
    // The ice is a real collider rather than patched physics: one invisible box under the local player, on the Water
    // layer, excluded from everything but the "character" layer. Vanilla counts any contact below the capsule as ground,
    // so walking, jumping, dodging and footsteps just work, and every water reaction (swimming, Wet, splash steps, the
    // Ashlands burn) keys off the liquid depth at the feet, which the ice keeps at 0. It has no Rigidbody, so it never
    // becomes a ground body whose velocity vanilla adds to the player. The Water layer is in no vanilla ray mask but the
    // water-piece placement one, so projectiles, melee, the camera, AI and ordinary building all ignore it.
    //
    // Everything runs in a CalculateLiquidDepth postfix: it is the first step of Character.CustomFixedUpdate, so a lift
    // or a depth override is seen by UpdateWater, UpdateGroundContact, UpdateMotion and UpdateAshlandsWater that tick.
    // The Eitr is taken from m_eitr directly rather than through UseEitr, whose EpicLoot patches (adrenaline on Eitr
    // use, cost reduction, cost conversion) would all react to a 50 Hz drain. The owner mirrors "the ice is holding me"
    // and "I wear the full set" to its ZDO: FrostwalkerIceField on every client lays ice chunks around and ahead of any
    // player with the first, and FrostwalkerSnow sheds snowflakes around any player with the second.
    public static class Frostwalker
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultAshlandsDrainMultiplier = 3f;   // drain multiplier over the boiling Ashlands sea
        public const float DefaultMinEitr = 10f;                  // Eitr needed before the water will freeze
        public const float DefaultRadius = 3f;                    // metres around (and ahead of) the player of whole ice
        public const float DefaultChunkSize = 4f;                 // metres across one ice chunk, a resized vanilla floe

        private const string AshlandsDrainMultiplierKey = "AshlandsDrainMultiplier";
        private const string MinEitrKey = "MinEitr";
        private const string RadiusKey = "Radius";
        private const string ChunkSizeKey = "ChunkSize";

        public static readonly int ZdoHash = "el-fwk".GetStableHashCode();
        public static readonly int SnowZdoHash = "el-fws".GetStableHashCode();

        public const string FloePrefab = "ice1";                 // Deep North floating floe: the look and the shatter
        private const string FreezeFxPrefab = "fx_iceshard_hit";
        private const string IconItem = "IceShoes";

        public const float IceOffset = 0.05f;          // ice top above the surface, so the feet stay dry
        private const float PlatformSize = 3f;
        private const float PlatformThickness = 1f;    // the player's rigidbody uses discrete collision; keep it thick
        private const float EnterDepth = 0.4f;         // vanilla's knee depth: wading this deep freezes the water
        private const float ExitDepth = 0.3f;          // shallower than this over terrain, the ice lets go (ashore)
        private const float AboveSurface = 0.6f;       // a dock or floe this low still counts as the waterline
        private const float SwimFreezeDelay = 0.3f;
        private const float RefreezeCooldown = 1f;     // outlasts vanilla's 0.2 s IsOnGround grace after a break
        private const float OffIceDelay = 0.3f;        // standing on both the ice and a rock must not flicker the state
        private const float MaxRise = 3f;              // vanilla caps grounded upward speed at 3 m/s
        private const float MaxFall = 6f;
        private const float Discontinuity = 1f;        // a surface jump this big in one tick is a teleport, not a wave
        private const float FellThrough = 0.6f;
        private const float ClimbedOff = 1.5f;
        private const float BreakFallHeight = 4f;      // vanilla fall damage starts at 4 m; the water takes the landing
        private const float BreakFallReach = 1.5f;
        private const float DryFeetTolerance = 0.15f;
        private const float EdgeOfWorld = 10420f;      // beyond it vanilla pushes swimmers back out to sea
        private const float NoWater = -1000f;

        private enum State
        {
            Off,
            Frozen,     // the platform follows the player; it holds them whenever _onIce
            Sunk        // broken by crouch or an empty pool; stays off until solid ground or a crouch while swimming
        }

        private static Player _player;
        private static State _state;
        private static bool _onIce;
        private static float _offIceTime;
        private static float _top;
        private static float _lastSurface;
        private static float _swimTime;
        private static float _cooldown;
        private static float _drainPerSecond;
        private static bool _published;
        private static bool _freezeFxPending;
        private static bool _hasSet;
        private static bool _snowPublished;

        private static GameObject _platform;
        private static Collider _platformCollider;
        private static int _liftBlockMask;

        private const string IndicatorName = "EL_Frostwalker";
        private static readonly int IndicatorHash = IndicatorName.GetStableHashCode();
        private static StatusEffect _indicator;
        private static bool _iconMissingLogged;

        // Player-editable, so each is kept in a sane range.
        private static float AshlandsDrainMultiplier => EffectConfig.GetClamped(MagicEffectType.Frostwalker,
            AshlandsDrainMultiplierKey, DefaultAshlandsDrainMultiplier, 1f, 20f);
        private static float MinEitr => EffectConfig.GetClamped(MagicEffectType.Frostwalker,
            MinEitrKey, DefaultMinEitr, 1f, 1000f);
        public static float Radius => EffectConfig.GetClamped(MagicEffectType.Frostwalker,
            RadiusKey, DefaultRadius, 1f, 10f);
        public static float ChunkSize => EffectConfig.GetClamped(MagicEffectType.Frostwalker,
            ChunkSizeKey, DefaultChunkSize, 1.5f, 10f);

        // The ice is under the local player right now, as opposed to armed beneath a dock they are standing on.
        public static bool IsHoldingLocalPlayer =>
            _state == State.Frozen && _onIce && _player != null && _player == Player.m_localPlayer;

        // The local player has Frostwalker active, i.e. wears the full set (it is a set-only effect).
        public static bool LocalPlayerHasSet => _hasSet && _player != null && _player == Player.m_localPlayer;

        // Tooltip: "... draining {0} Eitr per second ({1} over the boiling sea) ..."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Frostwalker,
                value => new object[] { value, value * AshlandsDrainMultiplier });
        }

        // Sampled at sea level, inside the zone's WaterVolume trigger (y -20 to 40), because a character's own
        // m_waterLevel drops to -10000 the moment its body leaves that trigger. Hot springs and dungeon pools sit well
        // above sea level, so the sea depth measured under them is negative and they never freeze.
        public static float SampleSurface(Vector3 position)
        {
            return Floating.GetLiquidLevel(new Vector3(position.x, ZoneSystem.instance.m_waterLevel, position.z),
                1f, LiquidType.Water);
        }

        private static readonly Collider[] VolumeHits = new Collider[8];
        private static int _waterVolumeMask;

        // The sea's volume at a spot, as SampleSurface finds it, or null.
        internal static WaterVolume FindWaterVolume(Vector3 position)
        {
            if (_waterVolumeMask == 0)
            {
                _waterVolumeMask = LayerMask.GetMask("WaterVolume");
            }

            var point = new Vector3(position.x, ZoneSystem.instance.m_waterLevel, position.z);
            int count = Physics.OverlapSphereNonAlloc(point, 0f, VolumeHits, _waterVolumeMask);
            for (int i = 0; i < count; i++)
            {
                if (VolumeHits[i].TryGetComponent(out WaterVolume volume) && volume.GetLiquidType() == LiquidType.Water)
                {
                    return volume;
                }
            }
            return null;
        }

        // World load (SetEffectWarmup): the floe copy and the snow emitter every client draws from.
        internal static void WarmupAtLoad(bool drawn)
        {
            if (drawn)
            {
                FloeTemplate.Get();
                FrostwalkerSnow.Prewarm();
            }
        }

        public static string GetSecondsLeftText()
        {
            Player player = Player.m_localPlayer;
            return player != null && _drainPerSecond > 0f ? StatusEffect.GetTimeString(player.m_eitr / _drainPerSecond) : "";
        }

        private static void Tick(Player player)
        {
            if (!ReferenceEquals(player, _player))
            {
                Reset(player);
            }

            float dt = Time.fixedDeltaTime;
            _cooldown -= dt;

            bool hasEffect = player.HasActiveMagicEffect(MagicEffectType.Frostwalker, out float drain);
            _hasSet = hasEffect;
            PublishSnow(player);
            if (!hasEffect && _state == State.Off)
            {
                return;
            }

            Vector3 pos = player.transform.position;
            bool allowed = hasEffect && IsAllowed(player, pos);
            float surface = SampleSurface(pos);
            bool water = surface > NoWater;
            float seaDepth = !water ? 0f :
                ZoneSystem.instance.GetGroundHeight(pos, out float ground) ? surface - ground : float.PositiveInfinity;

            // Vanilla's depth for this tick, computed just before this postfix.
            float depth = player.m_cashedInLiquidDepth;
            float swimThreshold = player.m_swimDepth - 0.4f;
            _swimTime = depth > swimThreshold ? _swimTime + dt : 0f;

            // No natural regen in the water either, or crouching through the ice to swim would refill the pool for free
            // before climbing back on. This is vanilla's own test, so wading counts too; on the ice the depth is held at
            // 0 and HoldUp pauses regen itself.
            if (hasEffect && player.InWater())
            {
                PauseEitrRegen(player);
            }

            // Fresh contacts from the last physics step, not IsOnGround or m_lastGroundCollider: both keep reporting the
            // ice for 0.2 s after it is gone, which would end Sunk and refreeze the player the instant they broke it.
            Collider contact = player.m_groundContact ? player.m_lowestContactCollider : null;
            bool onPlatform = contact != null && contact == _platformCollider;
            bool onOther = contact != null && !onPlatform;
            if (onPlatform)
            {
                _onIce = true;
                _offIceTime = 0f;
            }
            else if (onOther)
            {
                _offIceTime += dt;
                if (_offIceTime >= OffIceDelay)
                {
                    _onIce = false;
                }
            }

            switch (_state)
            {
                case State.Off:
                    // Wading out (or standing on a low dock or floe over deep water), or swimming. Never in mid-air, so a
                    // fall from a height still splashes into the water rather than landing hard on ice.
                    bool atWaterline = onOther && pos.y >= surface - swimThreshold && pos.y <= surface + AboveSurface;
                    if (allowed && water && _cooldown <= 0f && player.m_eitr >= MinEitr && seaDepth >= EnterDepth &&
                        (atWaterline || _swimTime >= SwimFreezeDelay))
                    {
                        Freeze(player, pos, surface);
                    }
                    break;

                case State.Frozen:
                    if (!hasEffect)
                    {
                        Unfreeze(player, shatter: true);
                    }
                    else if (!allowed || !water || Mathf.Abs(surface - _lastSurface) > Discontinuity ||
                             seaDepth < ExitDepth || (onOther && pos.y > _top + ClimbedOff))
                    {
                        Unfreeze(player, shatter: false);
                    }
                    else if (pos.y < _top - FellThrough || IsBreakingFall(player, pos))
                    {
                        Unfreeze(player, shatter: true);
                    }
                    else if (_onIce && player.m_eitr <= 0f)
                    {
                        Sink(player);
                    }
                    else
                    {
                        HoldUp(player, pos, surface, Mathf.Max(0f, drain), dt);
                    }
                    break;

                case State.Sunk:
                    if (!allowed || (onOther && depth < EnterDepth))
                    {
                        _state = State.Off;
                    }
                    break;
            }

            _lastSurface = surface;
            Publish(player);
        }

        private static bool IsAllowed(Player player, Vector3 pos)
        {
            return !player.IsDead() && !player.InIntro() && !player.IsTeleporting() && !player.IsAttached() &&
                   !player.IsDebugFlying() && !player.InInterior() && player.GetStandingOnShip() == null &&
                   new Vector2(pos.x, pos.z).magnitude < EdgeOfWorld;
        }

        // Falling onto the ice from higher than vanilla's fall damage threshold breaks through, so the water takes the
        // landing the way it would without the set.
        private static bool IsBreakingFall(Player player, Vector3 pos)
        {
            return !player.IsOnGround() && player.m_body.linearVelocity.y < 0f &&
                   player.m_maxAirAltitude - _top > BreakFallHeight && pos.y - _top < BreakFallReach;
        }

        private static void Freeze(Player player, Vector3 pos, float surface)
        {
            float top = surface + IceOffset;
            if (pos.y < top)
            {
                Vector3 lifted = new Vector3(pos.x, top, pos.z);
                if (!IsLiftClear(player, lifted))
                {
                    return;
                }

                // Moved as vanilla's UnderWorldCheck moves a body: transform and rigidbody together, falling speed dropped.
                player.transform.position = lifted;
                player.m_body.position = lifted;
                Vector3 velocity = player.m_body.linearVelocity;
                velocity.y = 0f;
                player.m_body.linearVelocity = velocity;
                player.m_swimTimer = 999f;          // or UpdateSwimming keeps pulling toward swim depth for 0.5 s
                player.m_lastGroundTouch = 0f;
                player.m_maxAirAltitude = top;      // the lift is never a fall
                player.m_cashedInLiquidDepth = 0f;
                _onIce = true;
                _offIceTime = 0f;
            }

            // While crouch is toggled, jump turns into a dodge.
            player.m_crouchToggled = false;
            player.m_body.WakeUp();
            _state = State.Frozen;
            _top = top;
            _freezeFxPending = true;
            PlacePlatform(new Vector3(pos.x, top, pos.z));
        }

        // A swimmer under a dock or a hull is not lifted into it.
        private static bool IsLiftClear(Player player, Vector3 feet)
        {
            if (_liftBlockMask == 0)
            {
                _liftBlockMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");
            }

            CapsuleCollider capsule = player.m_collider;
            float radius = capsule.radius;
            Vector3 bottom = feet + Vector3.up * (radius + 0.05f);
            Vector3 top = feet + Vector3.up * Mathf.Max(radius + 0.05f, capsule.height - radius);
            return !Physics.CheckCapsule(bottom, top, radius, _liftBlockMask, QueryTriggerInteraction.Ignore);
        }

        private static void HoldUp(Player player, Vector3 pos, float surface, float drain, float dt)
        {
            // The top chases the swell no faster than vanilla lets a grounded body rise, so a steep crest can't punch
            // the player upward through depenetration.
            float target = surface + IceOffset;
            _top = target > _top ? Mathf.Min(target, _top + MaxRise * dt) : Mathf.Max(target, _top - MaxFall * dt);

            // Led by this tick's travel so the player never runs off the edge of the box between physics steps.
            Vector3 velocity = player.m_body.linearVelocity;
            PlacePlatform(new Vector3(pos.x + velocity.x * dt, _top, pos.z + velocity.z * dt));

            if (!_onIce)
            {
                return;
            }

            // Carry the body up and down with the ice: a static collider moving under a body neither wakes it nor
            // lifts it smoothly. Skipped just after a jump, whose upward velocity this would cancel.
            if (player.IsOnGround() && player.m_jumpTimer > 0.1f)
            {
                velocity.y = Mathf.Clamp((_top - pos.y) / dt, -MaxFall, MaxRise);
                player.m_body.linearVelocity = velocity;
            }

            // Same test as vanilla's UpdateAshlandsWater, which is what would otherwise be burning the player.
            bool boiling = !(WorldGenerator.GetAshlandsOceanGradient(pos) < 0f);
            _drainPerSecond = drain * Game.m_eitrRate * (boiling ? AshlandsDrainMultiplier : 1f);
            player.m_eitr = Mathf.Max(0f, player.m_eitr - _drainPerSecond * dt);
            PauseEitrRegen(player);

            // The liquid level vanilla compares against is refreshed once per rendered frame and can trail the ice.
            if (pos.y >= _top - DryFeetTolerance)
            {
                player.m_cashedInLiquidDepth = 0f;
            }

            ShowIndicator(player);
        }

        // Holds natural regen off the way a spell cast does (RPC_UseEitr restarts the same timer), without spending any.
        private static void PauseEitrRegen(Player player)
        {
            player.m_eitrRegenTimer = Mathf.Max(player.m_eitrRegenTimer, player.m_eitrRegenDelay);
        }

        private static void Unfreeze(Player player, bool shatter)
        {
            bool wasHolding = IsHoldingLocalPlayer;
            _state = State.Off;
            _onIce = false;
            _offIceTime = 0f;
            _cooldown = RefreezeCooldown;
            _freezeFxPending = false;
            if (_platform != null)
            {
                _platform.SetActive(false);
            }

            // A sleeping body would never notice its support is gone.
            player.m_body.WakeUp();
            if (shatter && wasHolding)
            {
                PlayShatterFx(player.transform.position);
            }
        }

        private static void Sink(Player player)
        {
            Unfreeze(player, shatter: true);
            _state = State.Sunk;
        }

        private static void Reset(Player player)
        {
            _player = player;
            _state = State.Off;
            _onIce = false;
            _offIceTime = 0f;
            _swimTime = 0f;
            _cooldown = 0f;
            _published = false;
            _freezeFxPending = false;
            _hasSet = false;
            _snowPublished = false;
            if (_platform != null)
            {
                _platform.SetActive(false);
            }
        }

        // Written only on change, so the ZDO revision moves once per freeze or break rather than every tick.
        private static void Publish(Player player)
        {
            bool holding = IsHoldingLocalPlayer;
            if (holding == _published)
            {
                return;
            }

            _published = holding;
            if (holding && _freezeFxPending)
            {
                _freezeFxPending = false;
                PlayFreezeFx(player.transform.position);
            }

            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            zdo?.Set(ZdoHash, holding);
        }

        // Also only on change: once when the fourth piece goes on, once when one comes off.
        private static void PublishSnow(Player player)
        {
            if (_hasSet == _snowPublished)
            {
                return;
            }

            _snowPublished = _hasSet;
            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            zdo?.Set(SnowZdoHash, _hasSet);
        }

        // The box lives at the scene root, never under the player: a collider beneath the player's rigidbody would
        // become part of it. A world change destroys it with the scene, and the next freeze builds a new one.
        private static void PlacePlatform(Vector3 top)
        {
            if (_platform == null)
            {
                _platform = new GameObject("EL_FrostwalkerPlatform");
                _platform.layer = LayerMask.NameToLayer("Water");
                BoxCollider box = _platform.AddComponent<BoxCollider>();
                box.size = new Vector3(PlatformSize, PlatformThickness, PlatformSize);
                // Only the local player (and any creature this client owns that wanders onto it) touches it: never
                // ships, dropped items or other players' synced bodies.
                box.excludeLayers = ~LayerMask.GetMask("character");
                _platformCollider = box;
                _platform.AddComponent<FootStepCollider>().m_material = FootStep.GroundMaterial.Ice;
            }

            _platform.transform.position = top + Vector3.down * (PlatformThickness * 0.5f);
            if (!_platform.activeSelf)
            {
                _platform.SetActive(true);
            }
        }

        // Vanilla effect prefabs carry a ZNetView, so instantiating one on the owner replicates it to every client,
        // exactly as a Destructible's own destroyed effect does.
        private static void PlayFreezeFx(Vector3 position)
        {
            GameObject prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(FreezeFxPrefab) : null;
            if (prefab != null)
            {
                Object.Instantiate(prefab, position, Quaternion.identity);
            }
        }

        private static void PlayShatterFx(Vector3 position)
        {
            GameObject floe = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(FloePrefab) : null;
            Destructible destructible = floe != null ? floe.GetComponentInChildren<Destructible>(true) : null;
            destructible?.m_destroyedEffect.Create(position, Quaternion.identity);
        }

        // AddStatusEffect clones the prototype and no-ops on a present NameHash; the guard skips even that.
        private static void ShowIndicator(Player player)
        {
            SEMan seMan = player.GetSEMan();
            if (seMan.HaveStatusEffect(IndicatorHash))
            {
                return;
            }

            StatusEffect indicator = GetOrCreateIndicator();
            if (indicator != null)
            {
                seMan.AddStatusEffect(indicator);
            }
        }

        // A null icon would render as an invisible HUD entry, so a missing icon logs once and shows nothing.
        private static StatusEffect GetOrCreateIndicator()
        {
            if (_indicator != null)
            {
                return _indicator;
            }

            Sprite icon = ObjectDB.instance?.GetItemPrefab(IconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    _iconMissingLogged = true;
                    EpicLoot.LogWarning($"[Frostwalker] could not find the '{IconItem}' icon; the HUD indicator will not display.");
                }
                return null;
            }

            SE_Frostwalker se = ScriptableObject.CreateInstance<SE_Frostwalker>();
            se.name = IndicatorName;
            se.m_name = "$mod_epicloot_se_frostwalker";
            se.m_icon = icon;
            se.m_ttl = 0f;      // never expires on its own; IsDone removes it
            _indicator = se;
            return _indicator;
        }

        // The first step of Character.CustomFixedUpdate and called from nowhere else. It runs for every character, so
        // the local-player test comes first.
        [HarmonyPatch(typeof(Character), nameof(Character.CalculateLiquidDepth))]
        private static class Character_CalculateLiquidDepth_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Character __instance)
            {
                if (ReferenceEquals(__instance, Player.m_localPlayer) && __instance is Player player)
                {
                    Tick(player);
                }
            }
        }

        // Crouch is a toggle flipped on the press, so the press is consumed here before vanilla can toggle it.
        [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
        private static class Player_SetControls_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(Player __instance, ref bool crouch)
            {
                if (!crouch || !ReferenceEquals(__instance, _player) || __instance != Player.m_localPlayer)
                {
                    return;
                }

                if (IsHoldingLocalPlayer)
                {
                    Sink(__instance);
                    Publish(__instance);
                    crouch = false;
                }
                else if (_state == State.Sunk && __instance.IsSwimming())
                {
                    // Re-armed: the swim trigger freezes the water again as soon as there is Eitr for it.
                    _state = State.Off;
                    _cooldown = 0f;
                    _swimTime = SwimFreezeDelay;
                    crouch = false;
                }
            }
        }

        // Every player, local or remote, carries the visuals; a headless server draws nothing.
        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
                {
                    __instance.gameObject.AddComponent<FrostwalkerSnow>();
                    if (ZNetScene.instance != null)
                    {
                        FrostwalkerIceField.Ensure();
                    }
                }
            }
        }
    }

    // HUD entry while the ice holds the local player: the Ice Shoes icon, with the seconds of Eitr left at the current
    // drain as its icon text.
    public class SE_Frostwalker : StatusEffect
    {
        public override string GetIconText()
        {
            return Frostwalker.GetSecondsLeftText();
        }

        public override bool IsDone()
        {
            return !Frostwalker.IsHoldingLocalPlayer;
        }
    }

    // Draws the ice on this client, the way RuneMagic's path of ice does: the sea is cut into a fixed world grid of
    // ChunkSize cells, and each cell can hold one chunk, a copy of the Deep North floe resized to cover its cell. Every
    // player the ice is holding (the local player from Frostwalker's own state, anyone else from the flag their client
    // mirrors to the ZDO) asks for ice around two points: their feet, and a point led ahead of them by their speed. A
    // chunk wants full size within Radius of either point, fading to nothing one chunk width further out. So a walker's
    // ice forms ahead of them before they reach it, the chunk under their feet is always whole, and what they leave
    // behind melts. Chunks rise out of the water as they grow and sink back as they melt. A cell several players ask
    // for is drawn once, at the largest size any of them wants. Each cell's turn and height are hashed from its
    // coordinates, so every client lays the same ice. Visual only: the collider is Frostwalker's platform.
    public class FrostwalkerIceField : MonoBehaviour
    {
        private const float LeadTime = 0.6f;            // seconds of travel the ice reaches ahead of a walker
        private const float MaxLeadPerRadius = 0.8f;    // ...never more than this share of Radius ahead
        private const float VelocitySmoothing = 8f;
        private const float TeleportDistance = 10f;     // a jump this big in one frame resets a walker's speed
        private const float GrowRate = 2.5f;            // of full size per second
        private const float MeltRate = 0.6f;
        private const float Overlap = 1.12f;            // a chunk is this much wider than its cell, so no seams show
        private const float EmergeDepth = 0.35f;        // a new chunk rises from this far under the ice line
        private const float MinCellDepth = 0.1f;        // no chunk where the sea at the cell centre is shallower
        private const float YawWobble = 4f;             // degrees either side of the cell's quarter turn
        private const float MaxDrop = 0.035f;           // per-cell height offset below the ice line, against z-fighting
        private const int MaxCachedCells = 4096;
        private const int MaxSpawnsPerFrame = 4;        // new chunks start at size 0, so spreading them never shows
        private const int MaxPooled = 48;               // melted chunks kept for reuse rather than destroyed

        private sealed class Chunk
        {
            public GameObject Root;
            public WaterVolume Volume;  // the sea under the cell, so its surface is read without a physics query
            public Vector3 Center;
            public Quaternion Rotation;
            public float Drop;
            public float Size;      // 0..1 of full size
            public float Target;
        }

        private sealed class Walker
        {
            public Vector3 LastPosition;
            public Vector3 Velocity;
            public bool Seen;
        }

        private static FrostwalkerIceField _instance;

        private readonly Dictionary<Vector2Int, Chunk> _chunks = new Dictionary<Vector2Int, Chunk>();
        private readonly Dictionary<Vector2Int, bool> _wetCells = new Dictionary<Vector2Int, bool>();
        private readonly Dictionary<Player, Walker> _walkers = new Dictionary<Player, Walker>();
        private readonly List<Vector2Int> _melted = new List<Vector2Int>();
        private readonly List<Player> _gone = new List<Player>();
        private readonly Stack<GameObject> _pool = new Stack<GameObject>();
        private float _chunkSize;
        private int _spawnBudget;

        // One per game scene: it is a scene object, so a logout takes it and every chunk with it.
        public static void Ensure()
        {
            if (_instance == null)
            {
                _instance = new GameObject("EL_FrostwalkerIceField").AddComponent<FrostwalkerIceField>();
            }
        }

        private void OnDestroy()
        {
            ClearChunks();
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f || ZoneSystem.instance == null)
            {
                return;
            }

            // A live retune of the chunk size re-cuts the grid.
            float chunkSize = Frostwalker.ChunkSize;
            if (!Mathf.Approximately(chunkSize, _chunkSize))
            {
                _chunkSize = chunkSize;
                ClearChunks();
                _wetCells.Clear();
            }

            foreach (Chunk chunk in _chunks.Values)
            {
                chunk.Target = 0f;
            }

            _spawnBudget = MaxSpawnsPerFrame;
            AskForIce(chunkSize, dt);
            GrowAndMelt(dt);
        }

        private void AskForIce(float chunkSize, float dt)
        {
            foreach (Walker walker in _walkers.Values)
            {
                walker.Seen = false;
            }

            float radius = Frostwalker.Radius;
            foreach (Player player in Player.GetAllPlayers())
            {
                if (!IsHeld(player))
                {
                    continue;
                }

                Walker walker = Track(player, dt);
                Vector3 feet = player.transform.position;
                Vector3 travel = new Vector3(walker.Velocity.x, 0f, walker.Velocity.z) * LeadTime;
                Vector3 lead = feet + Vector3.ClampMagnitude(travel, radius * MaxLeadPerRadius);
                AskAround(feet, radius, chunkSize);
                AskAround(lead, radius, chunkSize);
            }

            _gone.Clear();
            foreach (KeyValuePair<Player, Walker> entry in _walkers)
            {
                if (!entry.Value.Seen)
                {
                    _gone.Add(entry.Key);
                }
            }
            foreach (Player player in _gone)
            {
                _walkers.Remove(player);
            }
        }

        // The main-menu preview player has no ZDO, so it never asks for ice.
        private static bool IsHeld(Player player)
        {
            if (player == null)
            {
                return false;
            }

            if (ReferenceEquals(player, Player.m_localPlayer))
            {
                return Frostwalker.IsHoldingLocalPlayer;
            }

            ZDO zdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
            return zdo != null && zdo.GetBool(Frostwalker.ZdoHash);
        }

        // Speed from position, not the rigidbody: a remote player's body is moved by its sync, not its velocity.
        private Walker Track(Player player, float dt)
        {
            Vector3 position = player.transform.position;
            if (!_walkers.TryGetValue(player, out Walker walker))
            {
                walker = new Walker { LastPosition = position };
                _walkers[player] = walker;
            }

            Vector3 step = position - walker.LastPosition;
            walker.LastPosition = position;
            walker.Velocity = step.sqrMagnitude > TeleportDistance * TeleportDistance
                ? Vector3.zero
                : Vector3.Lerp(walker.Velocity, step / dt, Mathf.Clamp01(VelocitySmoothing * dt));
            walker.Seen = true;
            return walker;
        }

        private void AskAround(Vector3 point, float radius, float chunkSize)
        {
            float reach = radius + chunkSize;
            int minX = Mathf.FloorToInt((point.x - reach) / chunkSize);
            int maxX = Mathf.FloorToInt((point.x + reach) / chunkSize);
            int minZ = Mathf.FloorToInt((point.z - reach) / chunkSize);
            int maxZ = Mathf.FloorToInt((point.z + reach) / chunkSize);
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    Vector3 center = new Vector3((x + 0.5f) * chunkSize, 0f, (z + 0.5f) * chunkSize);
                    float dx = center.x - point.x;
                    float dz = center.z - point.z;
                    float distance = Mathf.Sqrt(dx * dx + dz * dz);
                    float target = 1f - Mathf.Clamp01((distance - radius) / chunkSize);
                    if (target <= 0f)
                    {
                        continue;
                    }

                    Vector2Int cell = new Vector2Int(x, z);
                    if (!_chunks.TryGetValue(cell, out Chunk chunk))
                    {
                        // A few new chunks a frame: the rest are asked for again on the next one.
                        if (_spawnBudget <= 0 || !IsWet(cell, center))
                        {
                            continue;
                        }

                        chunk = Spawn(cell, center);
                        if (chunk == null)
                        {
                            continue;
                        }
                        _spawnBudget--;
                    }

                    chunk.Target = Mathf.Max(chunk.Target, target);
                }
            }
        }

        // Cached per cell: the sea floor does not move. Cleared whenever the cache grows past a few thousand cells. A
        // cell in a zone this client has not loaded (a remote walker at the edge of its area) has no water volume and
        // no terrain yet, so it is left out without being cached, and read once the zone loads.
        private bool IsWet(Vector2Int cell, Vector3 center)
        {
            if (_wetCells.TryGetValue(cell, out bool wet))
            {
                return wet;
            }

            if (!ZoneSystem.instance.IsZoneLoaded(center))
            {
                return false;
            }

            if (_wetCells.Count >= MaxCachedCells)
            {
                _wetCells.Clear();
            }

            float surface = Frostwalker.SampleSurface(center);
            wet = surface > -1000f &&
                  (!ZoneSystem.instance.GetGroundHeight(center, out float ground) || surface - ground >= MinCellDepth);
            _wetCells[cell] = wet;
            return wet;
        }

        private Chunk Spawn(Vector2Int cell, Vector3 center)
        {
            GameObject template = FloeTemplate.Get();
            if (template == null)
            {
                return null;
            }

            // Hashed from the cell so every client turns and sets it the same way. Quarter turns keep the floe's
            // footprint on the grid; the wobble keeps the grid from looking ruled.
            int hash = cell.x * 73856093 ^ cell.y * 19349663;
            float yaw = 90f * (hash & 3) + ((hash >> 2) & 255) / 255f * 2f * YawWobble - YawWobble;
            Chunk chunk = new Chunk
            {
                Center = center,
                Rotation = Quaternion.Euler(0f, yaw, 0f),
                Drop = ((hash >> 10) & 255) / 255f * MaxDrop,
            };

            if (_pool.Count > 0)
            {
                chunk.Root = _pool.Pop();
                chunk.Root.transform.rotation = chunk.Rotation;
            }
            else
            {
                chunk.Root = Instantiate(template, center, chunk.Rotation);
                chunk.Root.name = "EL_FrostwalkerChunk";
            }

            chunk.Volume = Frostwalker.FindWaterVolume(center);
            Place(chunk);
            chunk.Root.SetActive(true);
            _chunks[cell] = chunk;
            return chunk;
        }

        private void Recycle(GameObject root)
        {
            if (_pool.Count < MaxPooled)
            {
                root.SetActive(false);
                _pool.Push(root);
            }
            else
            {
                Destroy(root);
            }
        }

        private void GrowAndMelt(float dt)
        {
            _melted.Clear();
            foreach (KeyValuePair<Vector2Int, Chunk> entry in _chunks)
            {
                Chunk chunk = entry.Value;
                if (chunk.Root == null)
                {
                    _melted.Add(entry.Key);
                    continue;
                }

                chunk.Size = chunk.Target > chunk.Size
                    ? Mathf.Min(chunk.Target, chunk.Size + GrowRate * dt)
                    : Mathf.Max(chunk.Target, chunk.Size - MeltRate * dt);
                if (chunk.Size <= 0f)
                {
                    Recycle(chunk.Root);
                    _melted.Add(entry.Key);
                    continue;
                }

                Place(chunk);
            }

            foreach (Vector2Int cell in _melted)
            {
                _chunks.Remove(cell);
            }
        }

        // Scaled about the floe's own centre to cover its cell, top on the ice line at that spot's wave height, and
        // lowered by however much it has still to grow.
        private void Place(Chunk chunk)
        {
            float scale = _chunkSize * Overlap / (2f * FloeTemplate.HalfWidth) * chunk.Size;
            float surface = chunk.Volume != null
                ? chunk.Volume.GetWaterSurface(new Vector3(chunk.Center.x, ZoneSystem.instance.m_waterLevel, chunk.Center.z))
                : Frostwalker.SampleSurface(chunk.Center);
            if (surface < -1000f)
            {
                surface = ZoneSystem.instance.m_waterLevel;
            }

            Transform root = chunk.Root.transform;
            root.localScale = Vector3.one * Mathf.Max(0.001f, scale);
            Vector3 centerOffset = chunk.Rotation * (FloeTemplate.Center * scale);
            float top = surface + Frostwalker.IceOffset - chunk.Drop - (1f - chunk.Size) * EmergeDepth;
            root.position = new Vector3(chunk.Center.x - centerOffset.x, top - FloeTemplate.Top * scale,
                chunk.Center.z - centerOffset.z);
        }

        private void ClearChunks()
        {
            foreach (Chunk chunk in _chunks.Values)
            {
                if (chunk.Root != null)
                {
                    Destroy(chunk.Root);
                }
            }
            _chunks.Clear();

            foreach (GameObject pooled in _pool)
            {
                if (pooled != null)
                {
                    Destroy(pooled);
                }
            }
            _pool.Clear();
        }
    }

    // Snow drifting around a player who wears the full set, on every client: the local player from Frostwalker's own
    // state, anyone else from the flag their client mirrors to the ZDO. Flakes spawn at random heights in a box around
    // the player and simulate in world space, so they stay where they were shed as the player moves on. They drift down,
    // settle on whatever is below (ground, pieces, decks, the sea, the ice) and fade. A few more spawn per metre
    // travelled, which leaves a sparse trail. The material is vanilla's own snowflake, from the Freezing status effect.
    public class FrostwalkerSnow : MonoBehaviour
    {
        private const string SourcePrefab = "vfx_Freezing";
        private const string SourceMaterial = "snow_flake";

        private const float SpawnHalfWidth = 1.6f;   // metres either side of the player
        private const float SpawnLow = 0.4f;         // spawn heights above the feet
        private const float SpawnHigh = 2.8f;
        private const float RatePerSecond = 6f;
        private const float RatePerMetre = 0.6f;     // the trail
        private const float Lifetime = 8f;           // one from the top lands after about 5 s and rests for the rest
        private const float FallSpeed = 0.75f;       // terminal speed, so flakes drift rather than drop
        private const float Gravity = 0.03f;         // of Valheim's -20 m/s2
        private const int MaxFlakes = 96;

        private static Material _material;
        private static bool _materialMissing;

        // The configured emitter every player's snow is copied from, kept inactive. Setting up a ParticleSystem's
        // modules one by one costs far more than copying a finished one, and every client did it per wearer.
        private static GameObject _templateRoot;
        private static GameObject _template;

        private Player _player;
        private ParticleSystem _emitter;
        private bool _active;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void Update()
        {
            bool active = HasSet();
            if (active == _active)
            {
                return;
            }

            if (active)
            {
                if (_emitter == null)
                {
                    _emitter = BuildEmitter();
                }
                if (_emitter == null)
                {
                    return;     // retried next frame
                }
                _emitter.Play();
            }
            else if (_emitter != null)
            {
                // Flakes already in the air finish falling and fade.
                _emitter.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            _active = active;
        }

        // The main-menu preview player has no ZDO, so it never snows.
        private bool HasSet()
        {
            if (_player == null)
            {
                return false;
            }

            if (ReferenceEquals(_player, Player.m_localPlayer))
            {
                return Frostwalker.LocalPlayerHasSet;
            }

            ZDO zdo = _player.m_nview != null ? _player.m_nview.GetZDO() : null;
            return zdo != null && zdo.GetBool(Frostwalker.SnowZdoHash);
        }

        private ParticleSystem BuildEmitter()
        {
            GameObject template = GetTemplate();
            if (template == null)
            {
                return null;
            }

            GameObject copy = Instantiate(template, transform, false);
            copy.name = template.name;
            return copy.GetComponent<ParticleSystem>();
        }

        // World load (SetEffectWarmup).
        internal static void Prewarm()
        {
            GetTemplate();
        }

        // Built under an inactive root and configured before it ever wakes, so no copy plays a frame with Unity's
        // defaults.
        private static GameObject GetTemplate()
        {
            if (_template != null)
            {
                return _template;
            }

            Material material = GetMaterial();
            if (material == null)
            {
                return null;
            }

            if (_templateRoot == null)
            {
                _templateRoot = new GameObject("EL_FrostwalkerSnowTemplate");
                _templateRoot.SetActive(false);
                DontDestroyOnLoad(_templateRoot);
            }

            GameObject root = new GameObject("EL_FrostwalkerSnow");
            root.layer = LayerMask.NameToLayer("effect");
            root.transform.SetParent(_templateRoot.transform, false);

            ParticleSystem emitter = root.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = emitter.main;
            main.duration = 5f;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startLifetime = Lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.15f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.startColor = Color.white;
            main.gravityModifier = Gravity;
            main.maxParticles = MaxFlakes;

            ParticleSystem.EmissionModule emission = emitter.emission;
            emission.enabled = true;
            emission.rateOverTime = RatePerSecond;
            emission.rateOverDistance = RatePerMetre;

            ParticleSystem.ShapeModule shape = emitter.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, (SpawnLow + SpawnHigh) * 0.5f, 0f);
            shape.scale = new Vector3(SpawnHalfWidth * 2f, SpawnHigh - SpawnLow, SpawnHalfWidth * 2f);
            shape.randomDirectionAmount = 1f;   // a little sideways drift, in any direction

            ParticleSystem.LimitVelocityOverLifetimeModule limit = emitter.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = FallSpeed;
            limit.dampen = 0.5f;

            Gradient fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f),
                        new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            ParticleSystem.ColorOverLifetimeModule color = emitter.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(fade);

            // Landing kills a flake's speed but not its life, so it rests where it fell until it fades. Characters are
            // left out, so flakes fall through players rather than piling on their heads. Medium quality caches what
            // it finds instead of casting for every flake every frame, resting ones included.
            ParticleSystem.CollisionModule collision = emitter.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.collidesWith = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain",
                "vehicle", "Water");
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.dampen = 1f;
            collision.bounce = 0f;
            collision.lifetimeLoss = 0f;
            collision.radiusScale = 0.5f;

            ParticleSystemRenderer flakeRenderer = root.GetComponent<ParticleSystemRenderer>();
            flakeRenderer.sharedMaterial = material;
            flakeRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            flakeRenderer.shadowCastingMode = ShadowCastingMode.Off;
            flakeRenderer.receiveShadows = false;

            _template = root;
            return _template;
        }

        // Shared with the vanilla effect, so it is only ever read, never modified.
        private static Material GetMaterial()
        {
            if (_material != null)
            {
                return _material;
            }

            // ZNetScene registers every prefab before any player exists, so a failed search is final.
            if (_materialMissing || ZNetScene.instance == null)
            {
                return null;
            }

            GameObject source = ZNetScene.instance.GetPrefab(SourcePrefab);
            if (source != null)
            {
                foreach (ParticleSystemRenderer sourceRenderer in source.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    Material candidate = sourceRenderer.sharedMaterial;
                    if (candidate != null && candidate.name.StartsWith(SourceMaterial))
                    {
                        _material = candidate;
                        return _material;
                    }
                }
            }

            _materialMissing = true;
            EpicLoot.LogWarning($"[Frostwalker] could not find the '{SourceMaterial}' material on '{SourcePrefab}'; the snow will not display.");
            return null;
        }
    }

    // A visual-only copy of the Deep North floe, built once inactive (so nothing on it wakes up) with every behaviour,
    // collider and rigidbody stripped: no ZNetView, so a floe is never networked, and nothing for anyone to bump into.
    // Its footprint is measured from the mesh bounds, since renderer bounds are empty on an inactive object.
    internal static class FloeTemplate
    {
        public static float HalfWidth { get; private set; } = 4f;   // the narrower half-extent, so a resize covers a cell
        public static float Top { get; private set; } = 0.35f;
        public static Vector3 Center { get; private set; }

        private static GameObject _template;
        private static bool _missingLogged;

        public static GameObject Get()
        {
            if (_template != null)
            {
                return _template;
            }

            GameObject source = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(Frostwalker.FloePrefab) : null;
            if (source == null)
            {
                if (!_missingLogged)
                {
                    _missingLogged = true;
                    EpicLoot.LogWarning($"[Frostwalker] could not find the '{Frostwalker.FloePrefab}' prefab; the ice will not be drawn.");
                }
                return null;
            }

            bool wasActive = source.activeSelf;
            source.SetActive(false);
            GameObject template = Object.Instantiate(source);
            source.SetActive(wasActive);

            template.name = "EL_FrostwalkerFloeTemplate";
            Object.DontDestroyOnLoad(template);
            StripToVisual(template);
            Measure(template);
            _template = template;
            return _template;
        }

        // Behaviours first, newest first, and the ZNetView last, so nothing that depends on another is left orphaned.
        private static void StripToVisual(GameObject template)
        {
            MonoBehaviour[] behaviours = template.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = behaviours.Length - 1; i >= 0; i--)
            {
                if (behaviours[i] is not ZNetView)
                {
                    Object.DestroyImmediate(behaviours[i]);
                }
            }

            foreach (ZNetView nview in template.GetComponentsInChildren<ZNetView>(true))
            {
                Object.DestroyImmediate(nview);
            }

            foreach (Joint joint in template.GetComponentsInChildren<Joint>(true))
            {
                Object.DestroyImmediate(joint);
            }

            foreach (Collider collider in template.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }

            foreach (Rigidbody body in template.GetComponentsInChildren<Rigidbody>(true))
            {
                Object.DestroyImmediate(body);
            }
        }

        private static void Measure(GameObject template)
        {
            Transform root = template.transform;
            bool any = false;
            Bounds bounds = default;
            foreach (MeshFilter filter in template.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds mesh = filter.sharedMesh.bounds;
                Matrix4x4 toRoot = root.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 sign = new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f);
                    Vector3 point = toRoot.MultiplyPoint3x4(mesh.center + Vector3.Scale(mesh.extents, sign));
                    if (any)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                    }
                }
            }

            if (!any)
            {
                EpicLoot.LogWarning($"[Frostwalker] '{Frostwalker.FloePrefab}' has no mesh to measure; using default floe dimensions.");
                return;
            }

            HalfWidth = Mathf.Max(0.1f, Mathf.Min(bounds.extents.x, bounds.extents.z));
            Top = bounds.max.y;
            Center = new Vector3(bounds.center.x, 0f, bounds.center.z);
        }
    }
}
