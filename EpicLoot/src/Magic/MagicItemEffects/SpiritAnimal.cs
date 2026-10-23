using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using EpicLoot.Adventure;
using EpicLoot.Config;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string SpiritAnimal = nameof(SpiritAnimal);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Spirit Animal (4-piece Fylgja's Bond set bonus, NoRoll). The player binds one of their tamed creatures as their
    // fylgja: the creature leaves the world for good and a snapshot of its ZDO is saved on the character. Whenever the
    // player is in combat, a spirit copy of it fights beside them for Lifetime seconds, and a fresh one rises when that
    // runs out if the fight goes on. A spirit that is killed returns ReturnDelay seconds later. The value is the spirit's
    // strength in percent of the original: its outgoing damage is scaled by value/100 and the damage it takes by
    // 100/value, so its effective health follows without fighting vanilla or StarLevelSystem over max health.
    //
    // The snapshot is every key on the creature's ZDO except a blocklist (health, AI and session state, saddle, timers,
    // ZDOID links, other effects' markers), so its level, StarLevelSystem modifiers, size, name and other mods' data all
    // carry over. A spirit is a non-persistent ZDO created by the summoner, filled with the snapshot before Instantiate
    // (the ZNetScene.CreateObject path), so every Awake sees the copied data. Non-persistent ZDOs are never saved and
    // never handed to another peer; the server deletes them when their owner leaves. So all owner logic runs here, on
    // the summoner's client.
    //
    // A spirit never dies: a Character.CheckDeath prefix removes it instead of letting OnDeath run, so no loot, no
    // ragdoll, and none of the tame-death hooks of other mods (StarLevelSystem's Splitter would spawn real tamed copies).
    // On every client it is drawn with the Spirit Caller's ghost shader using its own texture (SpiritAnimalVisual).
    public static class SpiritAnimal
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultLifetime = 120f;        // seconds a spirit lives
        public const float DefaultReturnDelay = 30f;      // seconds before a killed spirit can rise again
        public const float DefaultCombatTimeout = 10f;    // seconds after the last combat signal that the fight is over
        public const float DefaultLeashDistance = 60f;    // metres from the player beyond which the spirit fades

        private const string LifetimeKey = "Lifetime";
        private const string ReturnDelayKey = "ReturnDelay";
        private const string CombatTimeoutKey = "CombatTimeout";
        private const string LeashDistanceKey = "LeashDistance";

        private static float Lifetime => EffectConfig.GetClamped(MagicEffectType.SpiritAnimal, LifetimeKey, DefaultLifetime, 10f, 600f);
        private static float ReturnDelay => EffectConfig.GetClamped(MagicEffectType.SpiritAnimal, ReturnDelayKey, DefaultReturnDelay, 1f, 600f);
        private static float CombatTimeout => EffectConfig.GetClamped(MagicEffectType.SpiritAnimal, CombatTimeoutKey, DefaultCombatTimeout, 2f, 60f);
        private static float LeashDistance => EffectConfig.GetClamped(MagicEffectType.SpiritAnimal, LeashDistanceKey, DefaultLeashDistance, 20f, 200f);

        // Per character, in the profile: a spirit summons in any world.
        internal static readonly string RecordKey = EpicLoot.PluginId + "+SpiritAnimal";

        // Markers on a spirit's ZDO, read on every client.
        internal static readonly int OwnerHash = "el-spirit".GetStableHashCode();       // long: summoner's player ID
        internal static readonly int ExpireHash = "el-spirit-exp".GetStableHashCode();  // long: ZNet time ticks
        private static readonly int StrengthHash = "el-spirit-str".GetStableHashCode(); // float: value

        // StarLevelSystem's own keys (common/DataObjects.cs), written as bools before Instantiate: with SLS's setup
        // delay at 0 its setup runs during Instantiate, and spawn multiplication could otherwise clone the spirit.
        private static readonly int SlsSpawnMultHash = "SLS_MULT".GetStableHashCode();
        private static readonly int SlsSpawnManagedHash = "SLS_EXTMGD".GetStableHashCode();
        private static readonly int SlsNoSleepHash = "SLS_NOSLEEP".GetStableHashCode();
        internal static readonly int SlsSizeHash = "SLS_SIZE".GetStableHashCode();

        private const string IconItem = "StaffSpiritCaller";
        private static readonly string[] SummonFx = { "fx_summon_spirit_spawn" };
        private static readonly string[] FadeFx = { "vfx_ghost_death", "sfx_ghost_death" };

        private const float MaxWaterDepth = 1.2f;   // deeper than this, a creature that can't swim is not placed there
        private const float FlyerLift = 2f;
        private const float SpawnRayUp = 2f;
        private const float SpawnRayLength = 6f;
        private const float NoSpotRetryDelay = 2f;  // no place to rise: try again after this many seconds
        private const float VanishedRetryDelay = 3f; // removed without dying (despawn or limit mods): wait this long

        private enum State
        {
            Idle,
            Active,     // a spirit is out
            Returning   // killed; waits ReturnDelay before it may rise again
        }

        // State for the local player only; SyncOwner drops it when the local player changes (respawn, logout).
        private static Player _owner;
        private static State _state;
        private static Character _spirit;
        private static float _spiritValue = 100f;
        private static float _expireAt;
        private static float _returnAt;
        private static float _lastCombat = float.NegativeInfinity;

        private static bool _popupOpen;
        private static Character _bindTarget;

        // Work moved off the frame that summons or binds, done on the next local tick: the summon's fx and HUD
        // indicator, and the profile save after a bind.
        private static bool _summonFxPending;
        private static bool _profileSavePending;

        // The parsed record, re-parsed whenever the stored string changes (ValheimEnforcer replaces the dictionary).
        private static string _recordSource;
        private static SpiritRecord _record;
        private static bool _recordUnreadable;
        private static bool _problemMessaged;

        private static bool _slsUnusable;
        private static bool _missingFxLogged;
        private static bool _iconMissingLogged;

        private const string LifeIndicatorName = "EL_SpiritAnimal";
        private const string ReturnIndicatorName = "EL_SpiritAnimalReturning";
        private static StatusEffect _lifeIndicator;
        private static StatusEffect _returnIndicator;

        private static int _groundMask;
        private static int _blockMask;
        private static int _sightMask;
        private static readonly Collider[] OverlapHits = new Collider[16];

        private static int GroundMask => _groundMask != 0 ? _groundMask : _groundMask = LayerMask.GetMask("Default",
            "static_solid", "Default_small", "piece", "terrain", "vehicle");

        private static int BlockMask => _blockMask != 0 ? _blockMask : _blockMask = LayerMask.GetMask("Default",
            "static_solid", "Default_small", "piece", "vehicle", "character", "character_net", "character_ghost",
            "character_noenv");

        private static int SightMask => _sightMask != 0 ? _sightMask : _sightMask = LayerMask.GetMask("Default",
            "static_solid", "Default_small", "piece", "terrain");

        // Tooltip: "... at {0}% strength for {1} seconds ... returns {2} seconds after it falls."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.SpiritAnimal,
                value => new object[] { value, Lifetime, ReturnDelay });
        }

        internal static bool IsSpirit(Character character)
        {
            var nview = character != null ? character.m_nview : null;
            return nview != null && nview.IsValid() && nview.GetZDO().GetLong(OwnerHash, 0L) != 0L;
        }

        // The live spirit this client summoned and is tracking.
        internal static bool IsTracked(Character character) => character != null && character == _spirit;

        private static bool InCombat => Time.time - _lastCombat < CombatTimeout;

        private static void PingCombat() => _lastCombat = Time.time;

        private static string Localize(string token) => Localization.instance.Localize(token);

        private static string Format(string token, params object[] args)
        {
            string text = Localize(token);
            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            if (_spirit != null)
            {
                Fade(_spirit);
            }

            _owner = player;
            _state = State.Idle;
            _spirit = null;
            _lastCombat = float.NegativeInfinity;
            _bindTarget = null;
            _summonFxPending = false;
            _profileSavePending = false;
            ClosePopup();
            _recordSource = null;
            _record = null;
            _recordUnreadable = false;
            _problemMessaged = false;
        }

        // ---- warm-up (SetEffectWarmup) -----------------------------------------------------------------------------

        // World load, behind the loading screen.
        internal static void WarmupAtLoad(bool drawn)
        {
            SpiritRecord.Warmup();
            SpiritAnimalVisual.ResetCache();
            if (drawn)
            {
                SpiritAnimalVisual.GetBaseMaterial();
            }
        }

        // The local player's spawn: the saved spirit is read, and its materials made, before the first fight.
        internal static void WarmupForPlayer(Player player, bool drawn)
        {
            SyncOwner(player);
            GetLifeIndicator();
            GetReturnIndicator();
            var record = GetRecord(player, out bool _);
            if (drawn && record != null && ZNetScene.instance != null)
            {
                SpiritAnimalVisual.Prewarm(ZNetScene.instance.GetPrefab(record.PrefabHash));
            }
        }

        // ---- the record -------------------------------------------------------------------------------------------

        private static SpiritRecord GetRecord(Player player, out bool unreadable)
        {
            player.m_customData.TryGetValue(RecordKey, out string text);
            if (!ReferenceEquals(text, _recordSource))
            {
                _recordSource = text;
                _record = null;
                _recordUnreadable = false;
                _problemMessaged = false;
                if (!string.IsNullOrEmpty(text) && !SpiritRecord.TryParse(text, out _record))
                {
                    _recordUnreadable = true;
                    EpicLoot.LogWarning("[SpiritAnimal] the saved spirit animal could not be read (a newer or damaged " +
                        "record); it is kept as it is and cannot be summoned or replaced.");
                }
            }

            unreadable = _recordUnreadable;
            return _record;
        }

        private static void SaveRecord(Player player, SpiritRecord record)
        {
            string text = record.Serialize();
            player.m_customData[RecordKey] = text;
            _recordSource = text;
            _record = record;
            _recordUnreadable = false;
            _problemMessaged = false;
        }

        // ---- the per-frame tick -----------------------------------------------------------------------------------

        // Called before the body of Player.Update, i.e. before vanilla reads this frame's buttons.
        private static void OnLocalPlayerUpdate(Player player)
        {
            SyncOwner(player);
            RunDeferredWork(player);

            if (_popupOpen)
            {
                if (!UnifiedPopup.IsVisible())
                {
                    // Closed by something else.
                    _popupOpen = false;
                    _bindTarget = null;
                }
            }
            else
            {
                HandleBindKey(player);
            }

            Tick(player);
        }

        private static void Tick(Player player)
        {
            float now = Time.time;
            switch (_state)
            {
                case State.Active:
                    if (_spirit == null || _spirit.m_nview == null || !_spirit.m_nview.IsValid())
                    {
                        // Unloaded or removed by something else (the safety net, a despawn or creature-limit mod). A
                        // short wait rather than an immediate re-summon, or a mod that keeps removing it would have a
                        // creature spawned every frame.
                        _spirit = null;
                        _state = State.Returning;
                        _returnAt = now + VanishedRetryDelay;
                        RemoveIndicator(player, _lifeIndicator);
                    }
                    else if (player.IsDead() || now >= _expireAt ||
                        !player.HasActiveMagicEffect(MagicEffectType.SpiritAnimal, out float _) ||
                        Vector3.Distance(player.transform.position, _spirit.transform.position) > LeashDistance)
                    {
                        Fade(_spirit);
                        _spirit = null;
                        _state = State.Idle;
                        RemoveIndicator(player, _lifeIndicator);
                    }
                    break;

                case State.Returning:
                    if (now >= _returnAt)
                    {
                        _state = State.Idle;
                    }
                    break;
            }

            // An expired spirit is replaced in the same tick when the fight goes on.
            if (_state == State.Idle && CanSummon(player, out SpiritRecord record, out GameObject prefab, out float value))
            {
                Summon(player, record, prefab, value);
            }
        }

        private static bool CanSummon(Player player, out SpiritRecord record, out GameObject prefab, out float value)
        {
            record = null;
            prefab = null;
            value = 0f;
            if (!InCombat || _popupOpen || player.IsDead() || player.IsTeleporting() || player.InIntro() ||
                player.InCutscene() || player.IsSleeping() || ZNetScene.instance == null ||
                !player.HasActiveMagicEffect(MagicEffectType.SpiritAnimal, out value))
            {
                return false;
            }

            record = GetRecord(player, out bool unreadable);
            if (unreadable)
            {
                MessageProblemOnce(player, Localize("$mod_epicloot_spiritanimal_unreadable"));
                return false;
            }

            if (record == null)
            {
                return false;
            }

            prefab = ZNetScene.instance.GetPrefab(record.PrefabHash);
            if (prefab == null || prefab.GetComponent<ZNetView>() == null || prefab.GetComponent<Character>() == null)
            {
                MessageProblemOnce(player, Format("$mod_epicloot_spiritanimal_missing", record.DisplayName));
                return false;
            }

            return true;
        }

        private static void MessageProblemOnce(Player player, string text)
        {
            if (_problemMessaged)
            {
                return;
            }

            _problemMessaged = true;
            player.Message(MessageHud.MessageType.Center, text);
        }

        // ---- summoning ----------------------------------------------------------------------------------------------

        private static void Summon(Player player, SpiritRecord record, GameObject prefab, float value)
        {
            Character spirit = null;
            float lifetime = Lifetime;
            try
            {
                if (!TryFindSpawnPoint(player, prefab, record, out Vector3 position))
                {
                    // Nowhere to stand (deep water, Frostwalker ice, off a ledge): rising at the player's feet would drop
                    // a non-swimmer into the sea, so wait for the player to move.
                    _state = State.Returning;
                    _returnAt = Time.time + NoSpotRetryDelay;
                    return;
                }

                Vector3 facing = player.transform.forward;
                facing.y = 0f;
                Quaternion rotation = facing.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(facing.normalized) : Quaternion.identity;
                spirit = Spawn(record, prefab, position, rotation, player, value, lifetime);
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"[SpiritAnimal] summoning '{record.PrefabName}' failed: {e}");
            }

            if (spirit == null)
            {
                // Try again after a moment rather than every frame.
                _state = State.Returning;
                _returnAt = Time.time + 1f;
                return;
            }

            _spirit = spirit;
            _spiritValue = value;
            _expireAt = Time.time + lifetime;
            _state = State.Active;
            // The fx and HUD indicator follow on the next frame: the creature's own Instantiate is already a heavy one.
            _summonFxPending = true;
            EpicLoot.Log($"[SpiritAnimal] summoned '{record.PrefabName}' (level {record.Level}) at {value}% strength.");
        }

        private static void RunDeferredWork(Player player)
        {
            if (_summonFxPending)
            {
                _summonFxPending = false;
                if (_state == State.Active && _spirit != null && _spirit.m_nview != null && _spirit.m_nview.IsValid())
                {
                    RemoveIndicator(player, _returnIndicator);
                    ShowIndicator(player, GetLifeIndicator(), Mathf.Max(1f, _expireAt - Time.time));
                    PlayFx(SummonFx, _spirit.transform.position, _spirit.transform.rotation);
                }
            }
            else if (_profileSavePending)
            {
                _profileSavePending = false;
                SaveCharacter();
            }
        }

        // m_customData only reaches disk on a profile save (every 30 minutes, on sleep, on logout), and the world has
        // already lost the creature, so a crash before then would lose the bind. Vanilla's save also serializes the
        // whole explored map (millions of cells, then compressed), which is what makes it a long freeze; the map is
        // left to the next regular save, and the profile keeps the copy from the last one.
        private static void SaveCharacter()
        {
            if (Game.instance == null || Player.m_localPlayer == null)
            {
                return;
            }

            SkipMapSave = true;
            try
            {
                Game.instance.SavePlayerProfile(setLogoutPoint: false);
            }
            finally
            {
                SkipMapSave = false;
            }
        }

        private static bool SkipMapSave;

        // ZNetView.Awake's new-object branch, with the data written before Instantiate as ZNetScene.CreateObject does.
        private static Character Spawn(SpiritRecord record, GameObject prefab, Vector3 position, Quaternion rotation,
            Player player, float value, float lifetime)
        {
            var prefabView = prefab.GetComponent<ZNetView>();
            int hash = prefab.name.GetStableHashCode();

            ZDO zdo = ZDOMan.instance.CreateNewZDO(position, hash);   // owned by this peer
            zdo.Persistent = false;
            zdo.Type = prefabView.m_type;
            zdo.Distant = prefabView.m_distant;
            zdo.SetPrefab(hash);          // before Instantiate: an unset prefab reads as an invalid ZDO
            zdo.SetRotation(rotation);

            record.WriteTo(zdo);
            zdo.Set(ZDOVars.s_level, Mathf.Max(1, record.Level));    // a level-1 creature may have had no key
            zdo.Set(ZDOVars.s_tamed, true);
            zdo.Set(ZDOVars.s_sleeping, false);
            zdo.Set(ZDOVars.s_despawnInDay, false);
            zdo.Set(ZDOVars.s_eventCreature, false);
            zdo.Set(ZDOVars.s_follow, player.GetPlayerName());
            zdo.Set(SlsSpawnMultHash, true);
            zdo.Set(SlsSpawnManagedHash, true);
            zdo.Set(SlsNoSleepHash, true);
            zdo.Set(OwnerHash, player.GetPlayerID());
            zdo.Set(ExpireHash, ZNet.instance.GetTime().Ticks + (long)(lifetime * TimeSpan.TicksPerSecond));
            zdo.Set(StrengthHash, value);

            GameObject instance = null;
            ZNetView.m_useInitZDO = true;
            ZNetView.m_initZDO = zdo;
            try
            {
                instance = Object.Instantiate(prefab, position, rotation);
            }
            finally
            {
                // A throwing Awake must not leave our ZDO for the next networked object spawned anywhere to adopt.
                if (ZNetView.m_initZDO != null)
                {
                    ZNetView.m_initZDO = null;
                    ZDOMan.instance.DestroyZDO(zdo);
                    if (instance != null)
                    {
                        Object.Destroy(instance);
                        instance = null;
                    }
                }
                ZNetView.m_useInitZDO = false;
            }

            var character = instance != null ? instance.GetComponent<Character>() : null;
            if (character == null)
            {
                if (instance != null)
                {
                    ZNetScene.instance.Destroy(instance);
                }
                return null;
            }

            SetSpawnManaged(character);
            if (character.GetBaseAI() is MonsterAI ai)
            {
                ai.m_fallAsleepDistance = 0f;
                ai.SetFollowTarget(player.gameObject);
                ai.Alert();
            }

            return character;
        }

        // Already marked in the ZDO; the API also refreshes StarLevelSystem's own cache when it is there.
        private static void SetSpawnManaged(Character character)
        {
            if (_slsUnusable || !StarLevelSystem.API.IsAvailable || !StarLevelSystem.API.SupportsSpawnManaged)
            {
                return;
            }

            try
            {
                StarLevelSystem.API.SetCreatureSpawnManaged(character, true);
            }
            catch (Exception e)
            {
                _slsUnusable = true;
                EpicLoot.LogWarningForce($"StarLevelSystem's creature API could not be used for spirit animals this session. {e}");
            }
        }

        // A spot behind or beside the player the creature fits into and can be seen from.
        private static readonly float[] SpawnAngles = { 150f, -150f, 120f, -120f, 180f, 90f, -90f, 60f, -60f };

        private static bool TryFindSpawnPoint(Player player, GameObject prefab, SpiritRecord record, out Vector3 position)
        {
            var character = prefab.GetComponent<Character>();
            bool flyer = character != null && character.m_flying;
            bool swimmer = flyer || (character != null && character.m_canSwim);

            float scale = Mathf.Max(0.1f, prefab.transform.localScale.x);
            if (record.TryGetVec3(SlsSizeHash, out Vector3 size))
            {
                scale = Mathf.Max(scale, Mathf.Max(size.x, Mathf.Max(size.y, size.z)));
            }

            var capsule = prefab.GetComponent<CapsuleCollider>();
            float radius = (capsule != null ? capsule.radius : 0.5f) * scale;
            float height = Mathf.Max(radius * 2f, (capsule != null ? capsule.height : 1.5f) * scale);
            float distance = radius + 1.5f;

            Vector3 origin = player.transform.position;
            Vector3 forward = player.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            Vector3 eye = player.GetCenterPoint();

            foreach (float angle in SpawnAngles)
            {
                Vector3 candidate = origin + Quaternion.Euler(0f, angle, 0f) * forward * distance;
                if (!Physics.Raycast(candidate + Vector3.up * SpawnRayUp, Vector3.down, out var ground, SpawnRayLength,
                    GroundMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Vector3 spot = ground.point;
                if ((!swimmer && Floating.GetLiquidLevel(spot) > spot.y + MaxWaterDepth) ||
                    Physics.Linecast(eye, spot + Vector3.up * 0.5f, SightMask, QueryTriggerInteraction.Ignore) ||
                    !Fits(player, spot, radius, height))
                {
                    continue;
                }

                position = flyer ? spot + Vector3.up * FlyerLift : spot;
                return true;
            }

            // Nowhere around the player (a tight corridor, a crowd): their own spot, if that is ground the creature can
            // stand on. A flyer needs no ground and rises above them.
            if (flyer)
            {
                position = origin + Vector3.up * FlyerLift;
                return true;
            }

            position = origin;
            return Physics.Raycast(origin + Vector3.up * SpawnRayUp, Vector3.down, out var below, SpawnRayLength, GroundMask,
                    QueryTriggerInteraction.Ignore) &&
                (swimmer || Floating.GetLiquidLevel(below.point) <= below.point.y + MaxWaterDepth);
        }

        private static bool Fits(Player player, Vector3 spot, float radius, float height)
        {
            Vector3 bottom = spot + Vector3.up * (radius + 0.25f);
            Vector3 top = spot + Vector3.up * Mathf.Max(radius + 0.25f, height - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, OverlapHits, BlockMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var collider = OverlapHits[i];
                var owner = collider.attachedRigidbody != null ? collider.attachedRigidbody.transform : collider.transform;
                if (!owner.IsChildOf(player.transform))
                {
                    return false;
                }
            }

            return true;
        }

        // Removes a spirit this client owns, with the fade effect. Never kills it.
        internal static void Fade(Character spirit)
        {
            var nview = spirit != null ? spirit.m_nview : null;
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || ZNetScene.instance == null)
            {
                return;
            }

            PlayFx(FadeFx, spirit.GetCenterPoint(), spirit.transform.rotation);
            ZNetScene.instance.Destroy(spirit.gameObject);
        }

        private static void OnSpiritKilled(Character spirit)
        {
            bool tracked = IsTracked(spirit);
            Fade(spirit);
            if (!tracked)
            {
                return;
            }

            _spirit = null;
            _state = State.Returning;
            float delay = ReturnDelay;
            _returnAt = Time.time + delay;
            var player = Player.m_localPlayer;
            if (player != null)
            {
                RemoveIndicator(player, _lifeIndicator);
                ShowIndicator(player, GetReturnIndicator(), delay);
            }
        }

        // ---- binding ------------------------------------------------------------------------------------------------

        private static KeyCode Bound(ConfigEntry<KeyCode> entry) => entry?.Value ?? KeyCode.None;

        private static bool KeyDown(KeyCode key) => key != KeyCode.None && ZInput.GetKeyDown(key, logWarning: false);

        private static bool KeyHeld(KeyCode key) => key != KeyCode.None && ZInput.GetKey(key, logWarning: false);

        // The creature under the crosshair, if any.
        private static Character GetHoveredCreature(Player player)
        {
            var hovered = player.GetHoverObject();
            var character = hovered != null ? hovered.GetComponentInParent<Character>() : null;
            return character == null || character is Player ? null : character;
        }

        private static bool CanUseBindKey(Player player)
        {
            return player.TakeInput() && !Hud.InRadial() && !player.InPlaceMode() && !player.IsDead() &&
                player.HasActiveMagicEffect(MagicEffectType.SpiritAnimal, out float _);
        }

        private static void HandleBindKey(Player player)
        {
            var key = Bound(ELConfig.SpiritAnimalKey);
            var button = Bound(ELConfig.SpiritAnimalGamepadButton);
            bool keyDown = KeyDown(key);
            bool buttonDown = SetAbilityKeys.GamepadButtonDown(player, button);
            if (!keyDown && !buttonDown || !CanUseBindKey(player))
            {
                return;
            }

            var target = GetHoveredCreature(player);
            if (target == null)
            {
                return;
            }

            // The press belongs to the bind whether or not it goes ahead.
            if (keyDown)
            {
                OverwhelmingLaunch.ConsumeKey(key, refresh: true);
            }
            if (buttonDown)
            {
                OverwhelmingLaunch.ConsumeKey(button, refresh: true);
            }

            string refusal = GetRefusal(player, target);
            if (refusal != null)
            {
                player.Message(MessageHud.MessageType.Center, Format(refusal, target.GetHoverName()));
                return;
            }

            var current = GetRecord(player, out bool unreadable);
            if (unreadable)
            {
                player.Message(MessageHud.MessageType.Center, Localize("$mod_epicloot_spiritanimal_unreadable"));
                return;
            }

            if (!UnifiedPopup.IsAvailable())
            {
                EpicLoot.LogWarning("[SpiritAnimal] no popup is available to confirm the bind.");
                return;
            }

            // Claimed now, so that by the time the player answers, any newer data has arrived and we can destroy it.
            target.m_nview.ClaimOwnership();
            _bindTarget = target;
            _popupOpen = true;

            string body = Format("$mod_epicloot_spiritanimal_popup_body", target.GetHoverName());
            if (current != null)
            {
                body += "\n\n" + Format("$mod_epicloot_spiritanimal_popup_replace", current.DisplayName);
            }

            UnifiedPopup.Push(new YesNoPopup(Localize("$mod_epicloot_spiritanimal_popup_title"), body,
                OnPopupYes, OnPopupNo, localizeText: false));
            UnifiedPopup.SetFocus();
        }

        // Why the creature can't be bound, as a localization token taking its name, or null.
        private static string GetRefusal(Player player, Character target)
        {
            var nview = target.m_nview;
            if (target.IsDead() || nview == null || !nview.IsValid() || !target.IsTamed())
            {
                return "$mod_epicloot_spiritanimal_refuse_nottamed";
            }

            if (IsSpirit(target))
            {
                return "$mod_epicloot_spiritanimal_refuse_spirit";
            }

            if (!(target.GetBaseAI() is MonsterAI))
            {
                return "$mod_epicloot_spiritanimal_refuse_cannotfight";
            }

            var zdo = nview.GetZDO();
            var tameable = target.GetComponent<Tameable>();
            if (IsSummon(target, zdo, tameable))
            {
                return "$mod_epicloot_spiritanimal_refuse_summoned";
            }

            string follow = zdo.GetString(ZDOVars.s_follow);
            if (!string.IsNullOrEmpty(follow) && follow != player.GetPlayerName())
            {
                return "$mod_epicloot_spiritanimal_refuse_otherplayer";
            }

            // Someone else's tame told to stay follows nobody, so the check above passes it; their ward protects it.
            if (!PrivateArea.CheckAccess(target.transform.position, 0f, flash: false))
            {
                return "$mod_epicloot_spiritanimal_refuse_warded";
            }

            if (tameable != null && tameable.HaveRider())
            {
                return "$mod_epicloot_spiritanimal_refuse_ridden";
            }

            foreach (var container in target.GetComponentsInChildren<Container>(true))
            {
                if (container.GetInventory() != null && container.GetInventory().NrOfItems() > 0)
                {
                    return "$mod_epicloot_spiritanimal_refuse_items";
                }
            }

            if (!string.IsNullOrEmpty(zdo.GetString(BountyTargetComponent.BountyIDKey)) ||
                !string.IsNullOrEmpty(zdo.GetString(BountyTargetComponent.BountyTargetKey)))
            {
                return "$mod_epicloot_spiritanimal_refuse_bounty";
            }

            return null;
        }

        // Temporary summons that would expire anyway: the Staff of the Dead's skeletons, the Spirit Caller's spirits,
        // EpicLoot's bats. Vanilla marks its summons on Tameable, not with a timer. m_startsTamed alone (hens) is fine.
        private static bool IsSummon(Character target, ZDO zdo, Tameable tameable)
        {
            if (!zdo.Persistent || zdo.GetInt(ZDOVars.s_maxInstances) > 0)
            {
                return true;
            }

            if (tameable != null && (tameable.m_levelUpOwnerSkill != Skills.SkillType.None ||
                tameable.m_unsummonDistance > 0f || tameable.m_unsummonOnOwnerLogoutSeconds > 0f))
            {
                return true;
            }

            return target.GetComponent<TimedDestruction>() != null || target.GetComponent<CharacterTimedDestruction>() != null;
        }

        private static void ClosePopup()
        {
            if (!_popupOpen)
            {
                return;
            }

            _popupOpen = false;
            if (UnifiedPopup.IsVisible())
            {
                UnifiedPopup.Pop();
            }
        }

        private static void OnPopupNo()
        {
            ClosePopup();
            _bindTarget = null;
        }

        private static void OnPopupYes()
        {
            ClosePopup();
            var target = _bindTarget;
            _bindTarget = null;
            var player = Player.m_localPlayer;
            if (player == null || target == null || player != _owner)
            {
                return;
            }

            try
            {
                Bind(player, target);
            }
            catch (Exception e)
            {
                EpicLoot.LogError($"[SpiritAnimal] binding failed: {e}");
            }
        }

        private static void Bind(Player player, Character target)
        {
            if (!player.HasActiveMagicEffect(MagicEffectType.SpiritAnimal, out float _))
            {
                return;
            }

            string name = target.GetHoverName();
            string refusal = GetRefusal(player, target);
            if (refusal != null)
            {
                player.Message(MessageHud.MessageType.Center, Format(refusal, name));
                return;
            }

            GetRecord(player, out bool unreadable);
            if (unreadable)
            {
                player.Message(MessageHud.MessageType.Center, Localize("$mod_epicloot_spiritanimal_unreadable"));
                return;
            }

            // Another client may have won the ownership claim; destroying needs it.
            if (!target.m_nview.IsOwner())
            {
                player.Message(MessageHud.MessageType.Center, Format("$mod_epicloot_spiritanimal_refuse_tryagain", name));
                return;
            }

            // The old spirit goes with its record; the new one may rise at once.
            if (_spirit != null)
            {
                Fade(_spirit);
                _spirit = null;
            }
            _state = State.Idle;
            RemoveIndicator(player, _lifeIndicator);
            RemoveIndicator(player, _returnIndicator);

            var tameable = target.GetComponent<Tameable>();
            if (tameable != null)
            {
                tameable.DropSaddle(player.transform.position);
            }

            var record = SpiritRecord.Capture(target);
            PlayFx(FadeFx, target.GetCenterPoint(), target.transform.rotation);
            ZNetScene.instance.Destroy(target.gameObject);

            SaveRecord(player, record);
            // Saved on the next frame, off this one's capture and destroy (see SaveCharacter).
            _profileSavePending = true;

            player.Message(MessageHud.MessageType.Center, Format("$mod_epicloot_spiritanimal_bound", name));
            EpicLoot.Log($"[SpiritAnimal] bound '{record.PrefabName}' (level {record.Level}).");
        }

        // ---- hit handlers (called from the damage dispatchers) -------------------------------------------------------

        // Attacker side, Character.Damage: runs on the spirit's owner, which is this client for our spirit.
        public static void ModifyOutgoingHit(Character target, HitData hit, Character attacker)
        {
            if (attacker == null || target == null || attacker == target)
            {
                return;
            }

            if (IsTracked(attacker))
            {
                if (!Mathf.Approximately(_spiritValue, 100f))
                {
                    hit.m_damage.Modify(_spiritValue / 100f);
                }
                PingCombat();
                return;
            }

            if (attacker == Player.m_localPlayer && IsCombatant(target))
            {
                PingCombat();
            }
        }

        // Victim side, Character.RPC_Damage: runs on the victim's owner.
        public static void ModifyIncoming(Character target, HitData hit, Character attacker)
        {
            if (target == null)
            {
                return;
            }

            if (IsTracked(target))
            {
                if (!Mathf.Approximately(_spiritValue, 100f))
                {
                    hit.m_damage.Modify(100f / Mathf.Max(1f, _spiritValue));
                }
                if (attacker != null)
                {
                    PingCombat();
                }
                return;
            }

            if (target == Player.m_localPlayer && attacker != null && attacker != target && IsCombatant(attacker))
            {
                PingCombat();
            }
        }

        // A creature whose blows start a fight: not a player, a tame or a training dummy.
        private static bool IsCombatant(Character character)
        {
            return !(character is Player) && !character.IsTamed() && character.m_faction != Character.Faction.TrainingDummy;
        }

        // ---- effects and HUD ----------------------------------------------------------------------------------------

        // Networked prefabs, so one spawn here is seen by everyone.
        private static void PlayFx(string[] names, Vector3 position, Quaternion rotation)
        {
            var scene = ZNetScene.instance;
            if (scene == null)
            {
                return;
            }

            foreach (var name in names)
            {
                var prefab = scene.GetPrefab(name);
                if (prefab == null)
                {
                    if (!_missingFxLogged)
                    {
                        _missingFxLogged = true;
                        EpicLoot.LogWarning($"[SpiritAnimal] could not find the '{name}' prefab; it plays without it.");
                    }
                    continue;
                }

                Object.Instantiate(prefab, position, rotation);
            }
        }

        // m_ttl is set on the shared prototype before adding; AddStatusEffect clones it.
        private static void ShowIndicator(Player player, StatusEffect indicator, float seconds)
        {
            if (indicator == null)
            {
                return;
            }

            indicator.m_ttl = seconds;
            player.GetSEMan().AddStatusEffect(indicator, true);
        }

        private static void RemoveIndicator(Player player, StatusEffect indicator)
        {
            if (indicator != null)
            {
                player.GetSEMan().RemoveStatusEffect(indicator.NameHash(), true);
            }
        }

        private static StatusEffect GetLifeIndicator()
        {
            return _lifeIndicator != null ? _lifeIndicator : _lifeIndicator = CreateIndicator(LifeIndicatorName,
                "$mod_epicloot_se_spiritanimal", cooldown: false);
        }

        private static StatusEffect GetReturnIndicator()
        {
            return _returnIndicator != null ? _returnIndicator : _returnIndicator = CreateIndicator(ReturnIndicatorName,
                "$mod_epicloot_se_spiritanimal_returning", cooldown: true);
        }

        // Display only: the gates are _expireAt and _returnAt. A missing icon logs once and shows nothing.
        private static StatusEffect CreateIndicator(string name, string token, bool cooldown)
        {
            var icon = ObjectDB.instance?.GetItemPrefab(IconItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null)
            {
                if (!_iconMissingLogged)
                {
                    _iconMissingLogged = true;
                    EpicLoot.LogWarning($"[SpiritAnimal] could not find the '{IconItem}' icon; the HUD indicators will not display.");
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = name;
            se.m_name = token;
            se.m_icon = icon;
            se.m_ttl = DefaultLifetime;
            se.m_cooldownIcon = cooldown;
            return se;
        }

        private static string KeyLabel()
        {
            return SetAbilityKeys.ActiveLabel(Bound(ELConfig.SpiritAnimalKey), Bound(ELConfig.SpiritAnimalGamepadButton));
        }

        private static bool BindKeysUnbound() =>
            Bound(ELConfig.SpiritAnimalKey) == KeyCode.None && Bound(ELConfig.SpiritAnimalGamepadButton) == KeyCode.None;

        // ---- patches ------------------------------------------------------------------------------------------------

        // The bind's save leaves the explored map to the next regular save (see SaveCharacter).
        [HarmonyPatch(typeof(Minimap), nameof(Minimap.SaveMapData))]
        private static class Minimap_SaveMapData_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix() => !SkipMapSave;
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

        // Keys whose vanilla action is read in the fixed step (attack, block, jump -- mostly a concern for the
        // gamepad button) have already been read by the time Player.Update runs, so they are held down here too.
        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
        private static class PlayerController_FixedUpdate_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(PlayerController __instance)
            {
                var player = __instance.m_character;
                if (player == null || player != Player.m_localPlayer || _popupOpen)
                {
                    return;
                }

                var key = Bound(ELConfig.SpiritAnimalKey);
                var button = Bound(ELConfig.SpiritAnimalGamepadButton);
                bool keyHeld = KeyHeld(key);
                bool buttonHeld = SetAbilityKeys.GamepadButtonHeld(player, button);
                if (!keyHeld && !buttonHeld || !CanUseBindKey(player) || GetHoveredCreature(player) == null)
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
        }

        // The popup doesn't block game input: without these, moving the mouse would turn the player and clicking Yes
        // would also swing the weapon. Self-healing if something else closed the popup.
        [HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
        private static class Player_TakeInput_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(ref bool __result)
            {
                if (_popupOpen)
                {
                    if (UnifiedPopup.IsVisible())
                    {
                        __result = false;
                    }
                    else
                    {
                        _popupOpen = false;
                        _bindTarget = null;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
        private static class PlayerController_TakeInput_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(ref bool __result)
            {
                if (_popupOpen && UnifiedPopup.IsVisible())
                {
                    __result = false;
                }
            }
        }

        // Esc or the gamepad's B answers No. Read here, after every Update, because the pause menu opens on the same
        // press in its own Update unless a popup is still showing when it looks.
        [HarmonyPatch(typeof(UnifiedPopup), nameof(UnifiedPopup.LateUpdate))]
        private static class UnifiedPopup_LateUpdate_Patch
        {
            [HarmonyPostfix]
            private static void Postfix()
            {
                if (_popupOpen && UnifiedPopup.IsVisible() &&
                    (ZInput.GetKeyDown(KeyCode.Escape, logWarning: false) || ZInput.GetButtonDown("JoyButtonB")))
                {
                    OnPopupNo();
                }
            }
        }

        // Combat starts with an alerted monster targeting the player: vanilla's own combat-music trigger.
        [HarmonyPatch(typeof(Player), nameof(Player.RPC_OnTargeted))]
        private static class Player_RPC_OnTargeted_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance, bool alerted)
            {
                if (alerted && __instance == Player.m_localPlayer)
                {
                    PingCombat();
                }
            }
        }

        // Every client. LevelEffects' material cache is keyed by prefab and level, and has to be kept away from a
        // spirit before any Start runs, or a normal starred creature of the kind could be drawn with the spirit's
        // material (this only matters without StarLevelSystem, which replaces that method).
        [HarmonyPatch(typeof(Character), nameof(Character.Awake))]
        private static class Character_Awake_Patch
        {
            [HarmonyPriority(Priority.Low)]
            [HarmonyPostfix]
            private static void Postfix(Character __instance)
            {
                if (__instance is Player || !IsSpirit(__instance))
                {
                    return;
                }

                foreach (var levelEffects in __instance.GetComponentsInChildren<LevelEffects>(true))
                {
                    levelEffects.m_mainRender = null;
                }

                __instance.m_defeatSetGlobalKey = "";
                if (__instance.GetComponent<SpiritAnimalVisual>() == null)
                {
                    __instance.gameObject.AddComponent<SpiritAnimalVisual>();
                }
            }
        }

        // The one engine path into OnDeath. A spirit is removed instead, so OnDeath and every mod's hook on it never
        // run. CheckDeath only runs on the owner.
        [HarmonyPatch(typeof(Character), nameof(Character.CheckDeath))]
        private static class Character_CheckDeath_Patch
        {
            private static bool _logged;

            [HarmonyPrefix]
            private static bool Prefix(Character __instance)
            {
                // Runs for every owned creature every physics step: the cheap spirit lookup first.
                if (!SpiritAnimalVisual.IsSpiritInstance(__instance) || __instance.IsDead() || __instance.GetHealth() > 0f)
                {
                    return true;
                }

                if (!_logged)
                {
                    _logged = true;
                    EpicLoot.Log("[SpiritAnimal] a spirit fell and faded instead of dying.");
                }

                __instance.m_isDead = true;
                OnSpiritKilled(__instance);
                return false;
            }
        }

        // Appended: Tameable builds the text, and StarLevelSystem also postfixes it.
        [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
        private static class Character_GetHoverText_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Character __instance, ref string __result)
            {
                if (__instance is Player)
                {
                    return;
                }

                var nview = __instance.m_nview;
                if (nview == null || !nview.IsValid())
                {
                    return;
                }

                var zdo = nview.GetZDO();
                long expire = zdo.GetLong(ExpireHash, 0L);
                if (expire != 0L)
                {
                    double seconds = Math.Max(0.0, (expire - ZNet.instance.GetTime().Ticks) / (double)TimeSpan.TicksPerSecond);
                    __result += "\n" + Format("$mod_epicloot_spiritanimal_spirit_hover", Mathf.CeilToInt((float)seconds));
                    return;
                }

                var player = Player.m_localPlayer;
                if (player == null || BindKeysUnbound() || !__instance.IsTamed() ||
                    !player.HasActiveMagicEffect(MagicEffectType.SpiritAnimal, out float _) ||
                    !CanBindHovered(player, __instance))
                {
                    return;
                }

                __result += "\n[<color=yellow><b>" + KeyLabel() + "</b></color>] " + Localize("$mod_epicloot_spiritanimal_bind_hint");
            }

            // Hover text is rebuilt every frame; the refusal walks the creature's containers, so it is re-checked a few
            // times a second rather than each frame.
            private const float RecheckInterval = 0.25f;
            private static Character _checked;
            private static float _recheckAt;
            private static bool _canBind;

            private static bool CanBindHovered(Player player, Character target)
            {
                if (target != _checked || Time.time >= _recheckAt)
                {
                    _checked = target;
                    _recheckAt = Time.time + RecheckInterval;
                    _canBind = GetRefusal(player, target) == null;
                }
                return _canBind;
            }
        }

        // A spirit can't be renamed (alt), and only its summoner may pet or command it.
        [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
        private static class Tameable_Interact_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Tameable __instance, Humanoid user, bool alt, ref bool __result)
            {
                var character = __instance.m_character;
                if (!IsSpirit(character))
                {
                    return true;
                }

                long owner = character.m_nview.GetZDO().GetLong(OwnerHash, 0L);
                if (alt || !(user is Player player) || player.GetPlayerID() != owner)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Tameable), nameof(Tameable.UseItem))]
        private static class Tameable_UseItem_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Tameable __instance, ref bool __result)
            {
                if (!IsSpirit(__instance.m_character))
                {
                    return true;
                }

                __result = false;
                return false;
            }
        }

        // ---- the saved creature -------------------------------------------------------------------------------------

        internal sealed class SpiritRecord
        {
            private const int Version = 1;

            public int PrefabHash;
            public string PrefabName = "";
            public int Level = 1;
            public string NameToken = "";
            public string TamedName = "";

            private readonly List<KeyValuePair<int, float>> _floats = new List<KeyValuePair<int, float>>();
            private readonly List<KeyValuePair<int, Vector3>> _vec3s = new List<KeyValuePair<int, Vector3>>();
            private readonly List<KeyValuePair<int, Quaternion>> _quats = new List<KeyValuePair<int, Quaternion>>();
            private readonly List<KeyValuePair<int, int>> _ints = new List<KeyValuePair<int, int>>();
            private readonly List<KeyValuePair<int, long>> _longs = new List<KeyValuePair<int, long>>();
            private readonly List<KeyValuePair<int, string>> _strings = new List<KeyValuePair<int, string>>();
            private readonly List<KeyValuePair<int, byte[]>> _bytes = new List<KeyValuePair<int, byte[]>>();

            public string DisplayName => !string.IsNullOrEmpty(TamedName) ? TamedName : Localize(NameToken);

            public bool TryGetVec3(int hash, out Vector3 value)
            {
                foreach (var pair in _vec3s)
                {
                    if (pair.Key == hash)
                    {
                        value = pair.Value;
                        return true;
                    }
                }

                value = Vector3.zero;
                return false;
            }

            // Session-only state, ZDOID links, and what a fresh spirit sets itself. Everything else is kept, so other
            // mods' keys carry over.
            private static HashSet<int> _blocked;

            private static readonly string[] BlockedNames =
            {
                // health and combat state
                "health", "max_health", "Attackers", "Modifiers", "cheated", "cheatedQueued", "bosscount", "dead",
                "dodgeinv", "IsBlocking", "HitDir", "HitPoint", "InitVel", "lastAttack", "seAttrib", "stamina", "eitr",
                "adrenaline",
                // AI state
                "alert", "aggravated", "huntplayer", "haveTarget", "patrol", "patrolPoint", "patrolSpawnPoint",
                "spawnpoint", "SpawnPoint", "ShownAlertMessage", "targetHear", "targetSee", "LookTarget", "LookDir",
                "lastWorldTime", "inWater", "landed", "DebugFly", "emote", "emoteID", "emote_oneshot", "inBed",
                // taming and breeding
                "TameTimeLeft", "TameLastFeeding", "lovePoints", "pregnant", "MaxInstances", "RandomSkillFactor",
                // riding and attachment
                "HaveSaddle", "user", "RideSpeed", "attachJoint", "relPos", "relRot",
                // time and items
                "alive_time", "timeOfDeath", "SpawnTime", "spawn_time", "lastTime", "autoDespawn", "items",
                "addedDefaultItems", "drops", "item", "0_item", "item0",
                // written by the spawn itself
                "level", "tamed", "sleeping", "DespawnInDay", "EventCreature", "follow",
                // EpicLoot's own markers
                "el-msd", "el-sd", "el-hh", "el-prs", "el-lky", "el-lky-rolls", "epic loot executioner multiplier",
                "epic loot modify stagger damage", "el-spirit", "el-spirit-exp", "el-spirit-str",
                BountyTargetComponent.BountyIDKey, BountyTargetComponent.BountyDataKey,
                BountyTargetComponent.BountyTargetKey, BountyTargetComponent.MonsterIDKey,
                BountyTargetComponent.IsAddKey, BountyTargetComponent.BountyTargetNameKey,
                // StarLevelSystem links to spawners, locations and minions, and its spawn flags
                "SLS_LOC_OWNER", "SLS_SPAWNER", "SLS_SPAWNER_POS", "SLS_SUMMONED", "SLS_NEM_PIN", "SLS_NEM_SCORE",
                "SLS_NEM_SCOREDATA", "SLS_NEM_BOSS", "SLS_RAIDS_ACTIVE", "SLS_PHASE_LVL", "SLS_CUSTOM_LOOT",
                "SLS_MULT", "SLS_EXTMGD", "SLS_NOSLEEP",
            };

            private static HashSet<int> Blocked
            {
                get
                {
                    if (_blocked != null)
                    {
                        return _blocked;
                    }

                    var set = new HashSet<int>(ZDOVars.s_sessionHashes);
                    foreach (var name in BlockedNames)
                    {
                        set.Add(name.GetStableHashCode());
                    }
                    // BaseAI's spawn time ("spawntime"; the "SpawnTime"/"spawn_time" names above are vanilla's retired
                    // ones): the original's tick, possibly from another world's clock.
                    set.Add(ZDOVars.s_spawnTime);
                    foreach (var pair in new[] { ZDOVars.s_zdoidUser, ZDOVars.s_zdoidRodOwner, ZDOVars.s_sessionCatchID,
                        ZDOVars.s_toRemoveTarget, ZDOVars.s_toRemoveSpawnID, ZDOVars.s_toRemoveParentID })
                    {
                        set.Add(pair.Key);
                        set.Add(pair.Value);
                    }
                    return _blocked = set;
                }
            }

            public static SpiritRecord Capture(Character target)
            {
                var zdo = target.m_nview.GetZDO();
                var record = new SpiritRecord
                {
                    PrefabHash = zdo.GetPrefab(),
                    PrefabName = Utils.GetPrefabName(target.gameObject),
                    Level = Mathf.Max(1, target.GetLevel()),
                    NameToken = target.m_name ?? "",
                    TamedName = zdo.GetString(ZDOVars.s_tamedName)
                };

                // Animator parameters ZSyncAnimation mirrors to the ZDO are session state too; the session set only
                // holds the ones this client has written itself.
                var skip = new HashSet<int>(Blocked);
                skip.UnionWith(ZDOExtraData.s_sessionOnly);
                var zanim = target.GetComponent<ZSyncAnimation>();
                if (zanim != null)
                {
                    foreach (var hashes in new[] { zanim.m_boolHashes, zanim.m_floatHashes, zanim.m_intHashes })
                    {
                        if (hashes == null)
                        {
                            continue;
                        }
                        foreach (int hash in hashes)
                        {
                            skip.Add(438569 + hash);
                        }
                    }
                }

                ZDOExtraData.GetData(zdo.m_uid, out var floats, out var vec3s, out var quats, out var ints, out var longs,
                    out var strings, out var bytes, out _);
                Copy(floats, record._floats, skip);
                Copy(vec3s, record._vec3s, skip);
                Copy(quats, record._quats, skip);
                Copy(ints, record._ints, skip);
                Copy(longs, record._longs, skip);
                Copy(strings, record._strings, skip);
                Copy(bytes, record._bytes, skip);
                return record;
            }

            // A dry run of every generic path on empty data, so the first bind or summon of a session does not compile
            // the seven list types' copies, writers and readers.
            internal static void Warmup()
            {
                var record = new SpiritRecord();
                var skip = Blocked;
                Copy(new List<KeyValuePair<int, float>>(), record._floats, skip);
                Copy(new List<KeyValuePair<int, Vector3>>(), record._vec3s, skip);
                Copy(new List<KeyValuePair<int, Quaternion>>(), record._quats, skip);
                Copy(new List<KeyValuePair<int, int>>(), record._ints, skip);
                Copy(new List<KeyValuePair<int, long>>(), record._longs, skip);
                Copy(new List<KeyValuePair<int, string>>(), record._strings, skip);
                Copy(new List<KeyValuePair<int, byte[]>>(), record._bytes, skip);
                TryParse(record.Serialize(), out _);
            }

            private static void Copy<T>(List<KeyValuePair<int, T>> source, List<KeyValuePair<int, T>> target, HashSet<int> skip)
            {
                if (source == null)
                {
                    return;
                }

                foreach (var pair in source)
                {
                    if (!skip.Contains(pair.Key))
                    {
                        target.Add(pair);
                    }
                }
            }

            public void WriteTo(ZDO zdo)
            {
                foreach (var pair in _floats) zdo.Set(pair.Key, pair.Value);
                foreach (var pair in _vec3s) zdo.Set(pair.Key, pair.Value);
                foreach (var pair in _quats) zdo.Set(pair.Key, pair.Value);
                foreach (var pair in _ints) zdo.Set(pair.Key, pair.Value);
                foreach (var pair in _longs) zdo.Set(pair.Key, pair.Value);
                foreach (var pair in _strings) zdo.Set(pair.Key, pair.Value ?? "");
                foreach (var pair in _bytes) zdo.Set(pair.Key, pair.Value ?? Array.Empty<byte>());
            }

            public string Serialize()
            {
                var pkg = new ZPackage();
                pkg.Write(Version);
                pkg.Write(PrefabHash);
                pkg.Write(PrefabName ?? "");
                pkg.Write(Level);
                pkg.Write(NameToken ?? "");
                pkg.Write(TamedName ?? "");
                WriteList(pkg, _floats, (p, v) => p.Write(v));
                WriteList(pkg, _vec3s, (p, v) => p.Write(v));
                WriteList(pkg, _quats, (p, v) => p.Write(v));
                WriteList(pkg, _ints, (p, v) => p.Write(v));
                WriteList(pkg, _longs, (p, v) => p.Write(v));
                WriteList(pkg, _strings, (p, v) => p.Write(v ?? ""));
                WriteList(pkg, _bytes, (p, v) => p.Write(v ?? Array.Empty<byte>()));
                return pkg.GetBase64();
            }

            // False for a record from a newer version or a damaged one; the caller leaves the stored string untouched.
            public static bool TryParse(string text, out SpiritRecord record)
            {
                record = null;
                try
                {
                    var pkg = new ZPackage(text);
                    if (pkg.ReadInt() != Version)
                    {
                        return false;
                    }

                    var result = new SpiritRecord
                    {
                        PrefabHash = pkg.ReadInt(),
                        PrefabName = pkg.ReadString(),
                        Level = pkg.ReadInt(),
                        NameToken = pkg.ReadString(),
                        TamedName = pkg.ReadString()
                    };
                    ReadList(pkg, result._floats, p => p.ReadSingle());
                    ReadList(pkg, result._vec3s, p => p.ReadVector3());
                    ReadList(pkg, result._quats, p => p.ReadQuaternion());
                    ReadList(pkg, result._ints, p => p.ReadInt());
                    ReadList(pkg, result._longs, p => p.ReadLong());
                    ReadList(pkg, result._strings, p => p.ReadString());
                    ReadList(pkg, result._bytes, p => p.ReadByteArray());
                    record = result;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            private static void WriteList<T>(ZPackage pkg, List<KeyValuePair<int, T>> list, Action<ZPackage, T> write)
            {
                pkg.Write(list.Count);
                foreach (var pair in list)
                {
                    pkg.Write(pair.Key);
                    write(pkg, pair.Value);
                }
            }

            private static void ReadList<T>(ZPackage pkg, List<KeyValuePair<int, T>> list, Func<ZPackage, T> read)
            {
                int count = pkg.ReadInt();
                if (count < 0 || count > 100000)
                {
                    throw new FormatException("bad count");
                }

                for (int i = 0; i < count; i++)
                {
                    int key = pkg.ReadInt();
                    list.Add(new KeyValuePair<int, T>(key, read(pkg)));
                }
            }
        }
    }

    // A spirit as every client sees it: each opaque material swapped for the Spirit Caller's ghost material carrying
    // the creature's own texture, re-checked for a few seconds to catch MaterialVariation, StarLevelSystem's colouring
    // and late gear. On the owner it also neutralises what would misbehave on a temporary copy and removes the spirit
    // if the tracker no longer knows it (an orphan) or it has outlived its time.
    public class SpiritAnimalVisual : MonoBehaviour
    {
        private const string SourcePrefab = "Wolf_spiritcaller";
        private const string MarkerSuffix = " (ELSpirit)";
        private const float WatchSeconds = 3f;
        private const float WatchInterval = 0.25f;
        private const float ExpireGraceSeconds = 2f;

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int SkinBumpMap = Shader.PropertyToID("_SkinBumpMap");
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");

        // The shader's only colour input (an HDR tint; the material's _Color and _EmissiveColor are leftovers it never
        // reads). The Spirit Caller's spirits use (3, 0.64, 1.18); the ranger's fylgja glows green at that brightness.
        private static readonly Color SpiritTint = new Color(0.64f, 3f, 0.8f, 1f);

        private static Material _baseMaterial;
        private static bool _baseMissingLogged;
        // Keyed by the source's texture pair and its tiling. Cleared on each world load (ResetCache), so textures a mod
        // makes per creature do not pile up materials across sessions.
        private static readonly Dictionary<(Texture, Texture, Vector4), Material> Materials =
            new Dictionary<(Texture, Texture, Vector4), Material>();
        private static readonly HashSet<Material> SpiritMaterials = new HashSet<Material>();

        // Every live spirit on this client, for the hot-path check in Character.CheckDeath.
        private static readonly HashSet<Character> Instances = new HashSet<Character>();

        private Character _character;
        private bool _drawn;
        private float _watchUntil;
        private float _nextWatch;

        internal static bool IsSpiritInstance(Character character) => character != null && Instances.Contains(character);

        private void Awake()
        {
            _character = GetComponent<Character>();
            _drawn = SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
            if (_character != null)
            {
                Instances.Add(_character);
            }
        }

        private void OnDestroy()
        {
            Instances.Remove(_character);
        }

        internal static void ResetCache()
        {
            Materials.Clear();
            SpiritMaterials.Clear();
            _baseMaterial = null;
            _baseMissingLogged = false;
        }

        // The materials a spirit of this prefab will need, made ahead of its first summon.
        internal static void Prewarm(GameObject prefab)
        {
            var baseMaterial = GetBaseMaterial();
            if (prefab == null || baseMaterial == null)
            {
                return;
            }

            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer))
                {
                    continue;
                }

                foreach (var source in renderer.sharedMaterials)
                {
                    if (source != null && source.renderQueue < 3000 && !SpiritMaterials.Contains(source))
                    {
                        GetSpiritMaterial(baseMaterial, source);
                    }
                }
            }
        }

        // Every component has woken by now, so the ones that would misbehave can be found and removed.
        private void Start()
        {
            // Would spawn a real, persistent adult; would breed; would hold items; would drop loot.
            foreach (var growup in GetComponentsInChildren<Growup>(true)) Destroy(growup);
            foreach (var procreation in GetComponentsInChildren<Procreation>(true)) Destroy(procreation);
            foreach (var container in GetComponentsInChildren<Container>(true)) Destroy(container);
            var drop = GetComponent<CharacterDrop>();
            if (drop != null)
            {
                drop.SetDropsEnabled(false);
                drop.m_drops = new List<CharacterDrop.Drop>();
            }

            if (_character != null && _character.GetBaseAI() is MonsterAI ai)
            {
                ai.m_consumeItems = new List<ItemDrop>();   // no eating food off the ground
                ai.m_fallAsleepDistance = 0f;
            }

            if (_drawn)
            {
                Swap();
            }
            _watchUntil = Time.time + WatchSeconds;
            InvokeRepeating(nameof(OwnerCheck), 1f, 1f);
        }

        private void Update()
        {
            if (!_drawn || Time.time > _watchUntil || Time.time < _nextWatch)
            {
                return;
            }

            _nextWatch = Time.time + WatchInterval;
            Swap();
        }

        private void OwnerCheck()
        {
            var nview = _character != null ? _character.m_nview : null;
            if (nview == null || !nview.IsValid() || !nview.IsOwner() || ZNet.instance == null)
            {
                return;
            }

            long expire = nview.GetZDO().GetLong(SpiritAnimal.ExpireHash, 0L);
            bool expired = expire != 0L &&
                ZNet.instance.GetTime().Ticks > expire + (long)(ExpireGraceSeconds * TimeSpan.TicksPerSecond);
            if (expired || !SpiritAnimal.IsTracked(_character))
            {
                SpiritAnimal.Fade(_character);
            }
        }

        private void Swap()
        {
            var baseMaterial = GetBaseMaterial();
            if (baseMaterial == null)
            {
                return;
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer))
                {
                    continue;
                }

                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null || source.renderQueue >= 3000 || SpiritMaterials.Contains(source))
                    {
                        continue;
                    }

                    materials[i] = GetSpiritMaterial(baseMaterial, source);
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        // One per texture pair and tiling, so a summon every two minutes does not leak a material each time.
        private static Material GetSpiritMaterial(Material baseMaterial, Material source)
        {
            bool hasMain = source.HasProperty(MainTex);
            Texture main = hasMain ? source.GetTexture(MainTex) : null;
            Texture bump = source.HasProperty(BumpMap) ? source.GetTexture(BumpMap) : null;
            Vector2 scale = hasMain ? source.GetTextureScale(MainTex) : Vector2.one;
            Vector2 offset = hasMain ? source.GetTextureOffset(MainTex) : Vector2.zero;
            var key = (main, bump, new Vector4(scale.x, scale.y, offset.x, offset.y));
            if (Materials.TryGetValue(key, out var material) && material != null)
            {
                return material;
            }

            material = new Material(baseMaterial) { name = baseMaterial.name + MarkerSuffix };
            SpiritMaterials.Add(material);
            if (main != null && material.HasProperty(MainTex))
            {
                material.SetTexture(MainTex, main);
                material.SetTextureScale(MainTex, scale);
                material.SetTextureOffset(MainTex, offset);
            }
            if (material.HasProperty(SkinBumpMap))
            {
                // The wolf's normal map would not fit another creature's UVs.
                material.SetTexture(SkinBumpMap, bump);
            }
            if (material.HasProperty(TintColor))
            {
                material.SetColor(TintColor, SpiritTint);
            }

            Materials[key] = material;
            return material;
        }

        internal static Material GetBaseMaterial()
        {
            if (_baseMaterial != null)
            {
                return _baseMaterial;
            }

            var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(SourcePrefab) : null;
            var renderer = prefab != null ? prefab.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            _baseMaterial = renderer != null ? renderer.sharedMaterial : null;
            if (_baseMaterial == null && !_baseMissingLogged)
            {
                _baseMissingLogged = true;
                EpicLoot.LogWarning($"[SpiritAnimal] could not find the '{SourcePrefab}' material; spirits look like the creature.");
            }

            return _baseMaterial;
        }
    }
}
