using BepInEx.Configuration;
using EpicLoot.Config;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string ShieldThrow = nameof(ShieldThrow);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Shield Throw (4-piece Svalinn's Arc set bonus, NoRoll). A hotkey hurls the shield in the left hand where the
    // player aims. It flies in a straight line through every creature until it meets terrain or something solid (or
    // reaches MaxDistance), then flies back to the hand. Each enemy it passes takes a hit on the way out and another on
    // the way back, for the effect value in percent of the shield's block power as blunt (100 / 125 / 150 at Legendary /
    // Mythic / Ancient), with knockback and stagger. Block power already carries the Blocking skill and EpicLoot's block
    // power effects.
    //
    // The gauge: damage the player blocks with the shield (the exact amount vanilla takes off a held block, read where
    // BlockAttack hands it to HitData.BlockDamage, so guard breaks never count) is stored, up to GaugeCapacity times the
    // shield's block power. GaugeDecayDelay seconds after the last block it drains at GaugeDecayRate percent of full per
    // second. A throw spends all of it, for up to MaxGaugeBonusPercent more damage at full. It is not saved: it starts
    // empty with each new Player object (a respawn, a logout).
    //
    // As with Overwhelming Launch the item never leaves its slot. The flying shield is a local-only model every client
    // draws from an RPC, and the windup is a real Attack (Flint Spear's throw) whose trigger is intercepted. The Attack
    // runs on the weapon in the right hand, or bare hands, since a shield has no attack of its own; its costs are zeroed
    // and the flat StaminaCost is taken instead. While the shield is away the player cannot block, but can still attack.
    //
    // Multiplayer: everything gameplay runs on the thrower's client. The shield's path is a function of time that the
    // damage sweep and every client's visual share; the sweep deals bonus hits (see HitSource), which Character.Damage
    // routes to each target's owner.
    public static class ShieldThrow
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultMaxDistance = 15f;          // metres; the shield turns back here in open air
        public const float DefaultProjectileSpeed = 30f;      // metres/second, out and back
        public const float DefaultHangTime = 0.1f;            // seconds it rests where it turned
        public const float DefaultThrowAnimationSpeed = 1.3f; // times vanilla's spear throw
        public const float DefaultStaggerMultiplier = 1.5f;
        public const float DefaultHitRadius = 0.75f;          // metres either side of the flight line
        public const float DefaultStaminaCost = 20f;
        public const float DefaultMaxGaugeBonusPercent = 300f;
        public const float DefaultGaugeCapacity = 5f;         // times the shield's block power
        public const float DefaultGaugeDecayDelay = 10f;      // seconds after the last block
        public const float DefaultGaugeDecayRate = 20f;       // percent of a full gauge per second
        public const float DefaultPushForce = 40f;

        private const string MaxDistanceKey = "MaxDistance";
        private const string ProjectileSpeedKey = "ProjectileSpeed";
        private const string HangTimeKey = "HangTime";
        private const string ThrowAnimationSpeedKey = "ThrowAnimationSpeed";
        private const string StaggerMultiplierKey = "StaggerMultiplier";
        private const string HitRadiusKey = "HitRadius";
        private const string StaminaCostKey = "StaminaCost";
        private const string MaxGaugeBonusPercentKey = "MaxGaugeBonusPercent";
        private const string GaugeCapacityKey = "GaugeCapacity";
        private const string GaugeDecayDelayKey = "GaugeDecayDelay";
        private const string GaugeDecayRateKey = "GaugeDecayRate";
        private const string PushForceKey = "PushForce";

        private static float MaxDistance => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, MaxDistanceKey, DefaultMaxDistance, 5f, 60f);
        private static float ProjectileSpeed => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, ProjectileSpeedKey, DefaultProjectileSpeed, 5f, 100f);
        private static float HangTime => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, HangTimeKey, DefaultHangTime, 0f, 2f);
        private static float ThrowAnimationSpeed => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, ThrowAnimationSpeedKey, DefaultThrowAnimationSpeed, 0.25f, 4f);
        private static float StaggerMultiplier => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, StaggerMultiplierKey, DefaultStaggerMultiplier, 0f, 10f);
        private static float HitRadius => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, HitRadiusKey, DefaultHitRadius, 0.2f, 3f);
        private static float StaminaCost => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, StaminaCostKey, DefaultStaminaCost, 0f, 200f);
        private static float MaxGaugeBonusPercent => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, MaxGaugeBonusPercentKey, DefaultMaxGaugeBonusPercent, 0f, 1000f);
        private static float GaugeCapacity => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, GaugeCapacityKey, DefaultGaugeCapacity, 0.5f, 50f);
        private static float GaugeDecayDelay => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, GaugeDecayDelayKey, DefaultGaugeDecayDelay, 0f, 60f);
        private static float GaugeDecayRate => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, GaugeDecayRateKey, DefaultGaugeDecayRate, 1f, 100f);
        private static float PushForce => EffectConfig.GetClamped(MagicEffectType.ShieldThrow, PushForceKey, DefaultPushForce, 0f, 300f);

        // Tooltip: "... for {0}% of your block power ... for up to +{1}% damage ... Costs {2} stamina."
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.ShieldThrow, value => new object[] { value, MaxGaugeBonusPercent, StaminaCost });
        }

        private const string ThrowAttackItem = "SpearFlint";
        private const float WallStandOff = 0.3f;              // metres short of a wall, so the shield doesn't sink in
        private const float MinPhaseTime = 0.05f;
        private const float BlockingSkillRaise = 1f;          // once per throw that hits anything

        private class Flight
        {
            public Vector3 Start;
            public Vector3 End;
            public float OutTime;
            public float HangTime;
            public float BackTime;
            public float ReleaseTime;
            public float Damage;
            public Vector3 LastPosition;
            public int LastLeg;
            public bool AnyHit;
            public readonly HashSet<GameObject> OutHits = new HashSet<GameObject>();
            public readonly HashSet<GameObject> BackHits = new HashSet<GameObject>();
        }

        // The legs PathPosition reports.
        internal const int LegOut = 0;
        internal const int LegHang = 1;
        internal const int LegBack = 2;
        internal const int LegDone = 3;

        // State for the local player only; SyncOwner drops it when the local player changes (respawn, logout).
        private static Player _owner;
        private static Attack _windup;               // from Attack.Start until its trigger
        private static Attack _throwAttack;          // the same attack, kept for its whole animation (the speed-up)
        private static Flight _flight;
        private static bool _missingPrefabLogged;

        // The gauge, in blocked damage. _fullAt is the capacity when it was last filled, which is what decay drains
        // against, so a shield taken off mid-decay doesn't stop it.
        private static float _stored;
        private static float _fullAt;
        private static float _capacity;
        private static float _lastBlockTime = float.NegativeInfinity;

        // Across one BlockAttack call on the local player.
        private static bool _measuring;

        // "Throw Shield [T]" in the combat key hints, shown while the bonus is active and a shield is in hand.
        private static readonly SetAbilityKeyHint ThrowHint = new SetAbilityKeyHint("EL_ShieldThrow",
            "$mod_epicloot_shieldthrow_hint", () => GetThrowShield(Player.m_localPlayer) != null,
            () => ELConfig.ShieldThrowKey, () => ELConfig.ShieldThrowGamepadButton);

        private static readonly SetGaugeBar GaugeBar = new SetGaugeBar("EL_ShieldThrowGauge", 160f,
            new Color(0.85f, 0.66f, 0.22f), new Color(1f, 0.9f, 0.5f), new Color(0.5f, 0.38f, 0.1f));
        private static int _shownBonus = -1;
        private static string _shownBonusText = "";

        private static int _solidMask;
        private static int _characterMask;
        private static readonly RaycastHit[] RayHits = new RaycastHit[32];
        private static readonly Collider[] HitBuffer = new Collider[128];

        // What stops the shield: terrain, rock, trees and buildings. Bushes and non-solid pieces (banners) let it
        // through, as do creatures.
        private static int SolidMask => _solidMask != 0 ? _solidMask : _solidMask = LayerMask.GetMask("Default",
            "static_solid", "piece", "terrain", "blocker", "vehicle");

        // Characters only (vanilla Attack's character layers).
        private static int CharacterMask => _characterMask != 0 ? _characterMask : _characterMask = LayerMask.GetMask(
            "character", "character_net", "character_ghost", "hitbox", "character_noenv");

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _windup = null;
            _throwAttack = null;
            _flight = null;
            _stored = 0f;
            _fullAt = 0f;
            _capacity = 0f;
            _lastBlockTime = float.NegativeInfinity;
            _measuring = false;
        }

        // The shield the hotkey throws right now, or null. The memoized effect total is read first because the key
        // hint calls this every frame.
        internal static ItemDrop.ItemData GetThrowShield(Player player)
        {
            if (player == null || !player.HasActiveMagicEffect(MagicEffectType.ShieldThrow, out float _))
            {
                return null;
            }

            var shield = player.LeftItem;
            return shield != null && shield.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield &&
                shield.m_dropPrefab != null ? shield : null;
        }

        internal static bool IsInFlight => _flight != null;

        private static float BlockPower(Player player, ItemDrop.ItemData shield)
        {
            return shield.GetBlockPower(player.GetSkillFactor(Skills.SkillType.Blocking));
        }

        // The gauge as the HUD and the throw see it, 0-1, against the shield in hand now.
        private static float GaugeFraction => _capacity > 0f ? Mathf.Clamp01(_stored / _capacity) : 0f;

        // ---- The gauge ----

        // SharedHumanoidBlockAttackPatch prefix: measure this block if it is the local player's, with the bonus.
        public static void BeforeBlock(Humanoid blocker)
        {
            _measuring = false;
            if (blocker == null || blocker != Player.m_localPlayer)
            {
                return;
            }

            var player = (Player)blocker;
            SyncOwner(player);
            _measuring = GetThrowShield(player) != null;
        }

        // SharedHumanoidBlockAttackPatch postfix, ahead of its __result gate, so the flag always clears.
        public static void AfterBlock(Humanoid blocker)
        {
            _measuring = false;
        }

        // Vanilla calls BlockDamage only on a held block, with the exact amount the shield took off the hit.
        private static void OnBlocked(float blocked)
        {
            var player = _owner;
            var shield = GetThrowShield(player);
            if (shield == null || blocked <= 0f)
            {
                return;
            }

            _capacity = GaugeCapacity * BlockPower(player, shield);
            _fullAt = _capacity;
            _stored = Mathf.Min(_stored + blocked, _capacity);
            _lastBlockTime = Time.time;
        }

        private static void UpdateGauge(Player player, float dt)
        {
            var shield = GetThrowShield(player);
            _capacity = shield != null ? GaugeCapacity * BlockPower(player, shield) : 0f;

            if (_stored > 0f && Time.time - _lastBlockTime > GaugeDecayDelay)
            {
                float reference = Mathf.Max(_fullAt, 1f);
                _stored = Mathf.Max(0f, _stored - reference * GaugeDecayRate / 100f * dt);
            }
        }

        // ---- Input and the windup ----

        private static KeyCode Bound(ConfigEntry<KeyCode> entry) => entry?.Value ?? KeyCode.None;

        private static bool KeyDown(KeyCode key) => key != KeyCode.None && ZInput.GetKeyDown(key, logWarning: false);

        private static bool KeyHeld(KeyCode key) => key != KeyCode.None && ZInput.GetKey(key, logWarning: false);

        // Called before the body of Player.Update, i.e. before vanilla reads this frame's buttons (T is OpenEmote).
        private static void OnLocalPlayerUpdate(Player player)
        {
            SyncOwner(player);
            if (player.IsDead())
            {
                _flight = null;
                return;
            }

            UpdateGauge(player, Time.deltaTime);
            if (_flight != null)
            {
                AdvanceFlight(player, _flight);
            }

            var key = Bound(ELConfig.ShieldThrowKey);
            var button = Bound(ELConfig.ShieldThrowGamepadButton);
            bool keyDown = KeyDown(key);
            bool buttonDown = SetAbilityKeys.GamepadButtonDown(player, button);
            if (!keyDown && !buttonDown || !player.TakeInput() || Hud.InRadial())
            {
                return;
            }

            var shield = GetThrowShield(player);
            if (shield == null)
            {
                return;
            }

            // The press belongs to the throw whether or not one can start now.
            if (keyDown)
            {
                OverwhelmingLaunch.ConsumeKey(key, refresh: true);
            }
            if (buttonDown)
            {
                OverwhelmingLaunch.ConsumeKey(button, refresh: true);
            }

            TryStartThrow(player);
        }

        // Keys whose vanilla action is read in the fixed step have already been read by the time Player.Update runs,
        // so they are held down here too (Overwhelming Launch does the same).
        private static void OnLocalPlayerFixedUpdate(Player player)
        {
            var key = Bound(ELConfig.ShieldThrowKey);
            var button = Bound(ELConfig.ShieldThrowGamepadButton);
            bool keyHeld = KeyHeld(key);
            bool buttonHeld = SetAbilityKeys.GamepadButtonHeld(player, button);
            if (!keyHeld && !buttonHeld || !player.TakeInput() || GetThrowShield(player) == null)
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

        private static void TryStartThrow(Player player)
        {
            // One shield in the air at a time; a swing in progress is cut short.
            if (_flight != null || IsThrowing(player) ||
                player.InDodge() || !player.CanMove() ||
                player.IsKnockedBack() || player.IsStaggering() || player.InMinorAction() ||
                player.IsSwimming() || player.IsAttached() || player.InPlaceMode())
            {
                return;
            }

            float cost = StaminaCost;
            if (cost > 0f && !player.HaveStamina(cost))
            {
                Hud.instance?.StaminaBarEmptyFlash();
                return;
            }

            // A shield has no attack, so the windup runs on whatever attacks: the right-hand weapon, or bare hands.
            var weapon = player.GetCurrentWeapon();
            var template = ObjectDB.instance?.GetItemPrefab(ThrowAttackItem)?.GetComponent<ItemDrop>();
            if (weapon == null || template == null)
            {
                if (template == null && !_missingPrefabLogged)
                {
                    _missingPrefabLogged = true;
                    EpicLoot.LogWarning($"[ShieldThrow] {ThrowAttackItem} not found; shield throw disabled.");
                }
                return;
            }

            bool interrupting = player.InAttack();

            // The flat StaminaCost replaces the attack's own costs, which vanilla would scale by the weapon's skill.
            // Eitr comes off the weapon's primary attack instead -- see the eitr postfix.
            var attack = template.m_itemData.m_shared.m_secondaryAttack.Clone();
            attack.m_consumeItem = false;
            attack.m_attackStamina = 0f;
            attack.m_attackHealth = 0f;
            attack.m_attackHealthPercentage = 0f;

            if (player.m_currentAttack != null)
            {
                player.m_currentAttack.Stop();
                player.m_previousAttack = player.m_currentAttack;
                player.m_currentAttack = null;
            }

            // Set before Start: Start already charges eitr through GetAttackEitr.
            _windup = attack;
            if (!attack.Start(player, player.m_body, player.m_zanim, player.m_animEvent, player.m_visEquipment, weapon,
                player.m_previousAttack, player.m_timeSinceLastAttack, player.GetAttackDrawPercentage()))
            {
                _windup = null;
                return;
            }

            if (cost > 0f)
            {
                player.UseStamina(cost);
            }

            _throwAttack = attack;
            player.m_currentAttack = attack;
            player.m_lastCombatTimer = 0f;
            if (interrupting)
            {
                OverwhelmingLaunch.CutToThrow(player, attack);
            }
        }

        // From Attack.Start until the throw animation ends (the release happens part way through).
        private static bool IsThrowing(Player player)
        {
            return _throwAttack != null && player.m_currentAttack == _throwAttack && !_throwAttack.IsDone() && player.InAttack();
        }

        private static double ModifyThrowSpeed(Character character, double speed)
        {
            return _owner != null && ReferenceEquals(character, _owner) && IsThrowing(_owner) ? speed * ThrowAnimationSpeed : speed;
        }

        // ---- The throw ----

        internal static Vector3 LeftHandPosition(Player player)
        {
            var hand = player.m_visEquipment != null ? player.m_visEquipment.m_leftHand : null;
            return hand != null ? hand.position : player.GetCenterPoint();
        }

        // The windup's animation trigger: the shield leaves the hand.
        private static void Release(Attack attack)
        {
            var player = attack.m_character as Player;
            if (player == null || player != Player.m_localPlayer || player.IsStaggering() ||
                !player.HasActiveMagicEffect(MagicEffectType.ShieldThrow, out float effectValue))
            {
                return;
            }

            var shield = GetThrowShield(player);
            if (shield == null)
            {
                return;
            }

            Vector3 start = LeftHandPosition(player);
            Vector3 end = FindTurnPoint(player, start, MaxDistance, out bool hitSolid);

            float speed = ProjectileSpeed;
            float outTime = Mathf.Max(MinPhaseTime, Vector3.Distance(start, end) / speed);
            float hangTime = HangTime;

            // Snapshot now, while the shield is in hand: EpicLoot's block power modifiers only apply to the current
            // blocker. The gauge is spent whole.
            float fraction = GaugeFraction;
            float damage = BlockPower(player, shield) * effectValue / 100f * (1f + MaxGaugeBonusPercent / 100f * fraction);
            _stored = 0f;

            _flight = new Flight
            {
                Start = start,
                End = end,
                OutTime = outTime,
                HangTime = hangTime,
                BackTime = outTime,
                ReleaseTime = Time.time,
                Damage = damage,
                LastPosition = start,
                LastLeg = LegOut,
            };
            EpicLoot.Log($"[ShieldThrow] Released {shield.m_shared.m_name}: turns at {end} in {outTime:0.00}s " +
                $"(solid: {hitSolid}), gauge {fraction:P0}, damage {damage:0.#} per hit.");

            ThrownShieldVisual.Broadcast(player, shield, end, outTime, hangTime, outTime, hitSolid);
        }

        // Where the shield turns back: the first solid thing along the aim within range (creatures don't count), or the
        // end of that range in open air. The aim is taken from the eye, and the line from the hand to that point is
        // checked too, for a wall right beside the player.
        private static Vector3 FindTurnPoint(Player player, Vector3 start, float maxDistance, out bool hitSolid)
        {
            Vector3 eye = player.GetEyePoint();
            Vector3 dir = player.GetLookDir();
            hitSolid = RaycastSolid(player, eye, dir, maxDistance, out Vector3 aim);
            if (!hitSolid)
            {
                aim = eye + dir * maxDistance;
            }

            Vector3 toAim = aim - start;
            float distance = toAim.magnitude;
            if (distance < 0.01f)
            {
                return start;
            }

            Vector3 flightDir = toAim / distance;
            if (RaycastSolid(player, start, flightDir, distance, out Vector3 blocked))
            {
                aim = blocked;
                hitSolid = true;
                distance = Vector3.Distance(start, aim);
            }

            return hitSolid ? start + flightDir * Mathf.Max(0f, distance - WallStandOff) : aim;
        }

        private static bool RaycastSolid(Player player, Vector3 from, Vector3 dir, float distance, out Vector3 point)
        {
            int count = Physics.RaycastNonAlloc(from, dir, RayHits, distance, SolidMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            point = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                var hit = RayHits[i];
                if (hit.distance >= nearest || Projectile.FindHitObject(hit.collider) == player.gameObject)
                {
                    continue;
                }

                nearest = hit.distance;
                point = hit.point;
            }

            return nearest < float.MaxValue;
        }

        // The shield's position elapsed seconds after release, shared by the damage sweep and every client's visual: a
        // straight line out, a rest, then straight back onto the hand, re-aimed every frame since the thrower moves.
        internal static Vector3 PathPosition(Vector3 start, Vector3 end, Vector3 hand, float outTime, float hangTime,
            float backTime, float elapsed, out int leg)
        {
            if (elapsed < outTime)
            {
                leg = LegOut;
                return Vector3.Lerp(start, end, elapsed / outTime);
            }

            float returning = elapsed - outTime - hangTime;
            if (returning < 0f)
            {
                leg = LegHang;
                return end;
            }

            if (returning >= backTime)
            {
                leg = LegDone;
                return hand;
            }

            leg = LegBack;
            return Vector3.Lerp(end, hand, returning / backTime);
        }

        // Each frame on the thrower's client: sweep the stretch flown since last frame. A frame that crosses the turn
        // sweeps the rest of the way out first, so the two legs keep their own hit lists.
        private static void AdvanceFlight(Player player, Flight flight)
        {
            float elapsed = Time.time - flight.ReleaseTime;
            Vector3 position = PathPosition(flight.Start, flight.End, LeftHandPosition(player), flight.OutTime,
                flight.HangTime, flight.BackTime, elapsed, out int leg);

            Vector3 outDir = flight.End - flight.Start;
            if (flight.LastLeg == LegOut && leg != LegOut)
            {
                Sweep(player, flight, flight.LastPosition, flight.End, outDir, flight.OutHits);
                flight.LastPosition = flight.End;
            }

            if (leg == LegOut)
            {
                Sweep(player, flight, flight.LastPosition, position, outDir, flight.OutHits);
            }
            else if (leg != LegHang)
            {
                Vector3 backDir = position - flight.LastPosition;
                if (backDir.sqrMagnitude < 0.0001f)
                {
                    backDir = position - flight.End;
                }
                Sweep(player, flight, flight.LastPosition, position, backDir, flight.BackHits);
            }

            flight.LastPosition = position;
            flight.LastLeg = leg;
            if (leg == LegDone)
            {
                _flight = null;
            }
        }

        private static void Sweep(Player player, Flight flight, Vector3 from, Vector3 to, Vector3 dir, HashSet<GameObject> hitSet)
        {
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : player.transform.forward;

            int count = Physics.OverlapCapsuleNonAlloc(from, to, HitRadius, HitBuffer, CharacterMask, QueryTriggerInteraction.UseGlobal);
            int damaged = 0;
            for (int i = 0; i < count; i++)
            {
                if (HitCharacter(player, HitBuffer[i], to, dir, flight.Damage, hitSet))
                {
                    damaged++;
                }
            }

            if (damaged > 0 && !flight.AnyHit)
            {
                flight.AnyHit = true;
                player.RaiseSkill(Skills.SkillType.Blocking, BlockingSkillRaise);
            }
        }

        // Kinetic Quake's filters: each target once per leg, living enemies only (anything with PvP on), and a perfect
        // dodge counts as one.
        private static bool HitCharacter(Player player, Collider collider, Vector3 center, Vector3 dir, float damage,
            HashSet<GameObject> hitSet)
        {
            if (collider == null)
            {
                return false;
            }

            GameObject target = Projectile.FindHitObject(collider);
            if (target == null || target == player.gameObject || !hitSet.Add(target))
            {
                return false;
            }

            Character character = target.GetComponent<Character>();
            if (character == null || character.IsDead())
            {
                return false;
            }

            bool isEnemy = BaseAI.IsEnemy(player, character) ||
                (character.GetBaseAI() != null && character.GetBaseAI().IsAggravatable());
            if (!player.IsPVPEnabled() && !isEnemy)
            {
                return false;
            }

            if (character.IsDodgeInvincible())
            {
                (character as Player)?.HitWhileDodging();
                return false;
            }

            Vector3 point = collider is MeshCollider ? collider.ClosestPointOnBounds(center) : collider.ClosestPoint(center);
            var hit = new HitData
            {
                m_point = point,
                m_dir = dir,
                m_hitCollider = collider,
                m_pushForce = PushForce,
                m_staggerMultiplier = StaggerMultiplier,
                m_dodgeable = true,
                m_blockable = true,
                m_hitType = HitData.HitType.PlayerHit,
                m_skill = Skills.SkillType.None,
            };
            hit.m_damage.m_blunt = damage;
            hit.SetAttacker(player);
            HitSource.DealBonusDamage(character, hit);
            return true;
        }

        // ---- HUD ----

        private static void RefreshGauge(Hud hud)
        {
            var player = Player.m_localPlayer;
            bool show = player != null && player == _owner && GetThrowShield(player) != null;
            float fraction = show ? GaugeFraction : 0f;
            int bonus = Mathf.RoundToInt(MaxGaugeBonusPercent * fraction);
            if (bonus != _shownBonus)
            {
                _shownBonus = bonus;
                _shownBonusText = $"+{bonus}%";
            }

            GaugeBar.Refresh(hud, show, fraction, _shownBonusText);
        }

        // ---- Patches ----

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

        [HarmonyPatch(typeof(HitData), nameof(HitData.BlockDamage))]
        private static class HitData_BlockDamage_Patch
        {
            [HarmonyPrefix]
            private static void Prefix(float damage)
            {
                if (_measuring)
                {
                    OnBlocked(damage);
                }
            }
        }

        // No blocking while the shield is away. Only the owner's own answer is changed: other clients read the synced
        // flag, which UpdateBlock writes from this. m_blocking is left alone, so a held block resumes on the return.
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsBlocking))]
        private static class Humanoid_IsBlocking_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Humanoid __instance, ref bool __result)
            {
                if (_flight == null || !ReferenceEquals(__instance, Player.m_localPlayer))
                {
                    return true;
                }

                __result = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
        private static class Attack_OnAttackTrigger_Patch
        {
            [HarmonyPriority(Priority.First)]
            [HarmonyPrefix]
            private static bool Prefix(Attack __instance)
            {
                if (__instance == null || __instance != _windup)
                {
                    return true;
                }

                // Swallowed, and the throw's own trigger still to come.
                if (__instance.m_character is Player player && OverwhelmingLaunch.IsFromInterruptedSwing(player))
                {
                    return false;
                }

                _windup = null;
                Release(__instance);
                return false;
            }
        }

        // Vanilla takes eitr from the weapon's primary attack rather than from the attack being started; the throw's
        // only cost is StaminaCost.
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
        private static class Attack_GetAttackEitr_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Attack __instance, ref float __result)
            {
                if (__instance != null && __instance == _windup)
                {
                    __result = 0f;
                }
            }
        }

        // Added once, like the attack speed effects' handler.
        [HarmonyPatch(typeof(Game), nameof(Game.Awake))]
        private static class Game_Awake_Patch
        {
            private static bool _speedHandlerAdded;

            [HarmonyPostfix]
            private static void Postfix()
            {
                if (!_speedHandlerAdded)
                {
                    _speedHandlerAdded = true;
                    AnimationSpeedManager.Add(ModifyThrowSpeed);
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                __instance.m_nview?.Register<ZPackage>(ThrownShieldVisual.Rpc,
                    (sender, pkg) => ThrownShieldVisual.OnRpc(__instance, pkg));
            }
        }

        [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
        private static class KeyHints_UpdateHints_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(KeyHints __instance) => ThrowHint.Refresh(__instance);
        }

        [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
        private static class Hud_Update_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Hud __instance) => RefreshGauge(__instance);
        }
    }

    // The thrown shield as every client sees it: a local-only copy of the shield's model, laid flat and spinning like a
    // disc, flying ShieldThrow.PathPosition's line out and back onto the thrower's left hand. The model in the hand is
    // hidden meanwhile, and the shield's own block sound and sparks play where it turns if it struck something solid.
    // Nothing here affects gameplay; the hits run on the thrower's client.
    public class ThrownShieldVisual : MonoBehaviour
    {
        internal const string Rpc = "el-stv";

        private const float SpinDegreesPerSecond = 1080f;
        private const float ImpactFxLifetime = 5f;

        private Player _thrower;
        private EffectList _impactEffects;
        private bool _impactPlayed;
        private GameObject _hiddenHandModel;
        private Vector3 _start;
        private Vector3 _end;
        private float _outTime;
        private float _hangTime;
        private float _backTime;
        private float _elapsed;
        private float _spin;

        internal static void Broadcast(Player player, ItemDrop.ItemData shield, Vector3 end, float outTime, float hangTime,
            float backTime, bool hitSolid)
        {
            if (player.m_nview == null || !player.m_nview.IsValid() || shield.m_dropPrefab == null)
            {
                return;
            }

            var pkg = new ZPackage();
            pkg.Write(shield.m_dropPrefab.name.GetStableHashCode());
            pkg.Write(end);
            pkg.Write(outTime);
            pkg.Write(hangTime);
            pkg.Write(backTime);
            pkg.Write(hitSolid);
            player.m_nview.InvokeRPC(ZNetView.Everybody, Rpc, pkg);
        }

        internal static void OnRpc(Player thrower, ZPackage pkg)
        {
            if (thrower == null || (ZNet.instance != null && ZNet.instance.IsDedicated()))
            {
                return;
            }

            int itemHash = pkg.ReadInt();
            Vector3 end = pkg.ReadVector3();
            float outTime = pkg.ReadSingle();
            float hangTime = pkg.ReadSingle();
            float backTime = pkg.ReadSingle();
            bool hitSolid = pkg.ReadBool();

            var itemPrefab = ObjectDB.instance?.GetItemPrefab(itemHash);
            var attach = itemPrefab != null ? itemPrefab.transform.Find("attach") : null;
            if (attach == null)
            {
                return;
            }

            var root = new GameObject("EL_ShieldThrow");
            var model = Instantiate(attach.gameObject, root.transform, false);
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
            LayFlat(model.transform);

            var visual = root.AddComponent<ThrownShieldVisual>();
            visual._impactEffects = hitSolid ? itemPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_blockEffect : null;
            visual.Begin(thrower, end, outTime, hangTime, backTime);
        }

        // Turns the model so its thinnest dimension -- the shield's face normal -- points up, centred on the root, so
        // the root spins it like a disc about its middle rather than its grip. Works from the meshes' own bounds, so
        // no shield needs its own offset.
        private static void LayFlat(Transform model)
        {
            var filters = model.GetComponentsInChildren<MeshFilter>(true);
            bool any = false;
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            foreach (var filter in filters)
            {
                var mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Bounds bounds = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = model.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    min = Vector3.Min(min, local);
                    max = Vector3.Max(max, local);
                    any = true;
                }
            }

            if (!any)
            {
                return;
            }

            Vector3 size = max - min;
            Vector3 normal = size.x <= size.y && size.x <= size.z ? Vector3.right :
                size.y <= size.z ? Vector3.up : Vector3.forward;
            Quaternion tilt = Quaternion.FromToRotation(normal, Vector3.up);
            Vector3 center = Vector3.Scale(model.localScale, (min + max) * 0.5f);
            model.localRotation = tilt;
            model.localPosition = -(tilt * center);
        }

        private void Begin(Player thrower, Vector3 end, float outTime, float hangTime, float backTime)
        {
            _thrower = thrower;
            _start = ShieldThrow.LeftHandPosition(thrower);
            _end = end;
            _outTime = Mathf.Max(0.05f, outTime);
            _hangTime = Mathf.Max(0f, hangTime);
            _backTime = Mathf.Max(0.05f, backTime);
            transform.position = _start;

            var vis = thrower.m_visEquipment;
            var handModel = vis != null ? vis.m_leftItemInstance : null;
            if (handModel != null && handModel.activeSelf)
            {
                handModel.SetActive(false);
                _hiddenHandModel = handModel;
            }
        }

        public void Update()
        {
            if (_thrower == null)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            transform.position = ShieldThrow.PathPosition(_start, _end, ShieldThrow.LeftHandPosition(_thrower), _outTime,
                _hangTime, _backTime, _elapsed, out int leg);

            if (leg != ShieldThrow.LegOut && !_impactPlayed)
            {
                _impactPlayed = true;
                PlayImpact();
            }

            if (leg == ShieldThrow.LegDone)
            {
                Destroy(gameObject);
                return;
            }

            _spin = (_spin + SpinDegreesPerSecond * Time.deltaTime) % 360f;
            transform.rotation = Quaternion.AngleAxis(_spin, Vector3.up);
        }

        // The shield's own block effects, as local copies: a clang where it struck the wall.
        private void PlayImpact()
        {
            if (_impactEffects?.m_effectPrefabs == null)
            {
                return;
            }

            foreach (var effect in _impactEffects.m_effectPrefabs)
            {
                if (effect != null && effect.m_enabled && effect.m_prefab != null)
                {
                    LocalFx.Spawn(effect.m_prefab, _end, Quaternion.identity, lifetime: ImpactFxLifetime, noCameraShake: true);
                }
            }
        }

        public void OnDestroy()
        {
            // VisEquipment may have rebuilt the hand model meanwhile, destroying this one.
            if (_hiddenHandModel != null)
            {
                _hiddenHandModel.SetActive(true);
            }
        }
    }
}
