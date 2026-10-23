using BepInEx.Configuration;
using EpicLoot.Config;
using EpicLoot.General;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string OverwhelmingLaunch = nameof(OverwhelmingLaunch);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Overwhelming Launch (4-piece Thor set bonus). A hotkey hurls the held melee weapon at the point the player
    // is aiming, where it lands with the Sledge Demolisher's slam: its trigger effects and an area hit dealing the
    // weapon's own damage plus the effect value in percent. The weapon then flies back.
    //
    // Unlike Throwable + RecallWeapon, the item never leaves the hand slot or the inventory. The flying weapon is
    // a local-only model every client draws from an RPC; the real item is only locked out of attacking until that
    // model is back, which takes longer the further it was thrown.
    //
    // The windup is a real Attack (Flint Spear's throw, the same clone Throwable starts), so vanilla handles the
    // animation, facing, stamina check and in-attack lockout. Its trigger is intercepted: nothing is fired, the
    // landing point is picked and the slam is scheduled, and the slam itself runs on this client like vanilla's
    // own area attack (Character.Damage routes each hit to the target's owner).
    //
    // The throw cuts a swing short: pressed mid-attack, it stops that attack and goes straight into the throw
    // animation, which plays ThrowAnimationSpeed times as fast as vanilla's (on every client, since the animator's
    // speed is synced).
    public static class OverwhelmingLaunch
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultMaxDistance = 30f;       // metres; aiming further lands at this range
        public const float DefaultProjectileSpeed = 25f;   // metres/second, out and back
        public const float DefaultHangTime = 0.2f;         // seconds the weapon rests where it landed
        public const float DefaultRadius = 4f;             // the Demolisher slam's m_attackRayWidth
        public const float DefaultStaggerMultiplier = 2f;  // the Demolisher slam's m_staggerMultiplier
        public const float DefaultCostScale = 3f;          // times the weapon's primary attack cost
        public const float DefaultThrowAnimationSpeed = 1.3f;  // times vanilla's spear throw

        private const string MaxDistanceKey = "MaxDistance";
        private const string ProjectileSpeedKey = "ProjectileSpeed";
        private const string HangTimeKey = "HangTime";
        private const string RadiusKey = "Radius";
        private const string StaggerMultiplierKey = "StaggerMultiplier";
        private const string CostScaleKey = "CostScale";
        private const string ThrowAnimationSpeedKey = "ThrowAnimationSpeed";

        private const string ThrowAttackItem = "SpearFlint";
        private const string SlamEffectItem = "SledgeDemolisher";

        // The player animator's state for the spear throw (an Any State transition on the "spear_throw" trigger), and
        // how long the cut from an interrupted swing into it takes: the trigger's own transition time.
        private static readonly int ThrowStateHash = Animator.StringToHash("spear_throw");
        private const float InterruptCrossFade = 0.1f;

        // The Demolisher slam's trigger effects (SledgeDemolisher's m_shared.m_triggerEffect), by prefab name.
        private static readonly string[] SlamEffectPrefabs = { "fx_swing_camshake", "fx_sledge_demolisher_hit" };

        // Player-editable, so each gets a floor: a zero speed divides, a zero radius hits nothing.
        private static float MaxDistance => EffectConfig.GetClamped(MagicEffectType.OverwhelmingLaunch, MaxDistanceKey, DefaultMaxDistance, 1f, 200f);
        private static float ProjectileSpeed => Mathf.Max(1f, EffectConfig.Get(MagicEffectType.OverwhelmingLaunch, ProjectileSpeedKey, DefaultProjectileSpeed));
        private static float HangTime => Mathf.Max(0f, EffectConfig.Get(MagicEffectType.OverwhelmingLaunch, HangTimeKey, DefaultHangTime));
        private static float Radius => Mathf.Max(0.5f, EffectConfig.Get(MagicEffectType.OverwhelmingLaunch, RadiusKey, DefaultRadius));
        private static float StaggerMultiplier => Mathf.Max(0f, EffectConfig.Get(MagicEffectType.OverwhelmingLaunch, StaggerMultiplierKey, DefaultStaggerMultiplier));
        private static float CostScale => Mathf.Max(0f, EffectConfig.Get(MagicEffectType.OverwhelmingLaunch, CostScaleKey, DefaultCostScale));
        private static float ThrowAnimationSpeed => EffectConfig.GetClamped(MagicEffectType.OverwhelmingLaunch, ThrowAnimationSpeedKey, DefaultThrowAnimationSpeed, 0.25f, 4f);

        // Tooltip: "... weapon damage +{0}% ... Costs {1}x a normal attack" -- {1} follows a retune of CostScale.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.OverwhelmingLaunch, value => new object[] { value, CostScale });
        }

        private class PendingSlam
        {
            public float Time;
            public Vector3 Point;
            public ItemDrop.ItemData Weapon;
            public HitData.DamageTypes Damage;
        }

        // State for the local player only; SyncOwner drops it when the local player changes (respawn, logout).
        private static Player _owner;
        private static Attack _launchAttack;       // the windup, from Attack.Start until its trigger
        private static Attack _throwAttack;        // the same attack, kept for its whole animation (the speed-up)
        private static ItemDrop.ItemData _inFlightWeapon;
        private static float _inFlightUntil;
        private static PendingSlam _pendingSlam;
        private static bool _missingPrefabLogged;

        // "Launch [T]" in the combat key hints, shown while a launchable weapon is held.
        private static readonly SetAbilityKeyHint LaunchHint = new SetAbilityKeyHint("EL_OverwhelmingLaunch",
            "$mod_epicloot_overwhelminglaunch_hint", () => GetLaunchWeapon(Player.m_localPlayer) != null,
            () => ELConfig.OverwhelmingLaunchKey, () => ELConfig.OverwhelmingLaunchGamepadButton);

        private static int _landMask;
        private static readonly RaycastHit[] RayHits = new RaycastHit[32];
        private static readonly Collider[] SlamHits = new Collider[128];
        private static readonly HashSet<GameObject> SlamHitSet = new HashSet<GameObject>();

        // Same layers as vanilla Attack.m_attackMaskTerrain, which is only filled in once some attack has started.
        private static int LandMask => _landMask != 0 ? _landMask : _landMask = LayerMask.GetMask("Default", "static_solid",
            "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost",
            "hitbox", "character_noenv", "vehicle");

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _launchAttack = null;
            _throwAttack = null;
            _inFlightWeapon = null;
            _inFlightUntil = 0f;
            _pendingSlam = null;
        }

        internal static bool IsInFlight(ItemDrop.ItemData weapon)
        {
            return weapon != null && weapon == _inFlightWeapon && Time.time < _inFlightUntil;
        }

        private static bool IsLaunchable(Player player, ItemDrop.ItemData weapon)
        {
            if (weapon == null || weapon.m_dropPrefab == null || weapon.m_shared.m_tamedOnly ||
                (player.m_unarmedWeapon != null && weapon == player.m_unarmedWeapon.m_itemData))
            {
                return false;
            }

            switch (weapon.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                    break;
                default:
                    return false;
            }

            switch (weapon.m_shared.m_skillType)
            {
                case Skills.SkillType.Pickaxes:
                case Skills.SkillType.Bows:
                case Skills.SkillType.Crossbows:
                case Skills.SkillType.ElementalMagic:
                case Skills.SkillType.BloodMagic:
                case Skills.SkillType.Fishing:
                    return false;
            }

            if (!weapon.HavePrimaryAttack())
            {
                return false;
            }

            var attackType = weapon.m_shared.m_attack.m_attackType;
            return attackType == Attack.AttackType.Horizontal || attackType == Attack.AttackType.Vertical ||
                attackType == Attack.AttackType.Area;
        }

        // The weapon the hotkey acts on right now, or null. The memoized effect total is read first because the
        // key hint calls this every frame.
        internal static ItemDrop.ItemData GetLaunchWeapon(Player player)
        {
            if (player == null || !player.HasActiveMagicEffect(MagicEffectType.OverwhelmingLaunch, out float _))
            {
                return null;
            }

            var weapon = player.GetCurrentWeapon();
            return IsLaunchable(player, weapon) ? weapon : null;
        }

        private static KeyCode Bound(ConfigEntry<KeyCode> entry) => entry?.Value ?? KeyCode.None;

        private static bool KeyDown(KeyCode key) => key != KeyCode.None && ZInput.GetKeyDown(key, logWarning: false);

        private static bool KeyHeld(KeyCode key) => key != KeyCode.None && ZInput.GetKey(key, logWarning: false);

        // Called before the body of Player.Update, i.e. before vanilla reads this frame's buttons (T is OpenEmote,
        // read by HandleRadialInput).
        private static void OnLocalPlayerUpdate(Player player)
        {
            SyncOwner(player);
            if (player.IsDead())
            {
                _pendingSlam = null;
                return;
            }

            if (_pendingSlam != null && Time.time >= _pendingSlam.Time)
            {
                var slam = _pendingSlam;
                _pendingSlam = null;
                DoSlam(player, slam);
            }

            var key = Bound(ELConfig.OverwhelmingLaunchKey);
            var button = Bound(ELConfig.OverwhelmingLaunchGamepadButton);
            bool keyDown = KeyDown(key);
            bool buttonDown = SetAbilityKeys.GamepadButtonDown(player, button);
            if (!keyDown && !buttonDown || !player.TakeInput() || Hud.InRadial())
            {
                return;
            }

            var weapon = GetLaunchWeapon(player);
            if (weapon == null)
            {
                return;
            }

            // The press belongs to the launch whether or not one can start now.
            if (keyDown)
            {
                ConsumeKey(key, refresh: true);
            }
            if (buttonDown)
            {
                ConsumeKey(button, refresh: true);
            }

            TryStartLaunch(player, weapon);
        }

        // Keys whose vanilla action is read in the fixed step (attack, block, jump -- mostly a concern for the
        // gamepad button) have already been read by the time Player.Update runs, so they are held down here too.
        private static void OnLocalPlayerFixedUpdate(Player player)
        {
            var key = Bound(ELConfig.OverwhelmingLaunchKey);
            var button = Bound(ELConfig.OverwhelmingLaunchGamepadButton);
            bool keyHeld = KeyHeld(key);
            bool buttonHeld = SetAbilityKeys.GamepadButtonHeld(player, button);
            if (!keyHeld && !buttonHeld || !player.TakeInput() || GetLaunchWeapon(player) == null)
            {
                return;
            }

            if (keyHeld)
            {
                ConsumeKey(key, refresh: false);
            }
            if (buttonHeld)
            {
                ConsumeKey(button, refresh: false);
            }
        }

        // Vanilla buttons bound to a key, by action path -- the same matching EquipmentAndQuickSlots' quick-slot
        // hotkeys use. Refreshed on every key-down (rare), so a vanilla rebind is picked up without hooking it.
        private static readonly Dictionary<KeyCode, List<string>> CollidingButtons = new Dictionary<KeyCode, List<string>>();

        internal static void ConsumeKey(KeyCode key, bool refresh)
        {
            var zinput = ZInput.instance;
            if (zinput == null)
            {
                return;
            }

            if (refresh || !CollidingButtons.TryGetValue(key, out var names))
            {
                names = new List<string>();
                string path = ZInput.KeyCodeToPath(key);
                foreach (var pair in zinput.m_buttons)
                {
                    // GetActionPath reads bindings[0] without a count check; the rebind flow can leave a def with none.
                    var def = pair.Value;
                    if (def?.ButtonAction == null || def.ButtonAction.bindings.Count == 0)
                    {
                        continue;
                    }

                    if (def.GetActionPath(effective: true) == path || def.GetActionPath(effective: false) == path)
                    {
                        names.Add(pair.Key);
                    }
                }

                CollidingButtons[key] = names;
            }

            foreach (var name in names)
            {
                ZInput.ResetButtonStatus(name);
            }
        }

        private static void TryStartLaunch(Player player, ItemDrop.ItemData weapon)
        {
            // One launch at a time, even with another weapon swapped in: the state holds a single flight and slam.
            // Any other attack in progress is cut short.
            if (_pendingSlam != null || Time.time < _inFlightUntil || IsThrowing(player) ||
                player.InDodge() || !player.CanMove() ||
                player.IsKnockedBack() || player.IsStaggering() || player.InMinorAction() ||
                player.IsSwimming() || player.IsAttached() || player.InPlaceMode())
            {
                return;
            }

            bool interrupting = player.InAttack();

            var template = ObjectDB.instance?.GetItemPrefab(ThrowAttackItem)?.GetComponent<ItemDrop>();
            if (template == null)
            {
                if (!_missingPrefabLogged)
                {
                    _missingPrefabLogged = true;
                    EpicLoot.LogWarning($"[OverwhelmingLaunch] {ThrowAttackItem} not found; launch disabled.");
                }
                return;
            }

            // Costs go through the same GetAttackStamina/Health reductions as a normal swing (skill, equipment,
            // magic effects), so scaling the primary's base values makes the result exactly CostScale times one.
            // Eitr is the exception -- vanilla reads it off the weapon's primary attack -- see the eitr postfix.
            var primary = weapon.m_shared.m_attack;
            float costScale = CostScale;
            var attack = template.m_itemData.m_shared.m_secondaryAttack.Clone();
            attack.m_consumeItem = false;
            attack.m_attackStamina = primary.m_attackStamina * costScale;
            attack.m_attackHealth = primary.m_attackHealth * costScale;
            attack.m_attackHealthPercentage = primary.m_attackHealthPercentage * costScale;

            if (player.m_currentAttack != null)
            {
                player.m_currentAttack.Stop();
                player.m_previousAttack = player.m_currentAttack;
                player.m_currentAttack = null;
            }

            // Set before Start: Start already charges eitr through GetAttackEitr.
            _launchAttack = attack;
            if (!attack.Start(player, player.m_body, player.m_zanim, player.m_animEvent, player.m_visEquipment, weapon,
                player.m_previousAttack, player.m_timeSinceLastAttack, player.GetAttackDrawPercentage()))
            {
                _launchAttack = null;
                return;
            }

            _throwAttack = attack;
            player.m_currentAttack = attack;
            player.m_lastCombatTimer = 0f;
            if (interrupting)
            {
                CutToThrow(player, attack);
            }
        }

        // From Attack.Start until the throw animation ends (the release happens part way through).
        private static bool IsThrowing(Player player)
        {
            return _throwAttack != null && player.m_currentAttack == _throwAttack && !_throwAttack.IsDone() && player.InAttack();
        }

        // The trigger alone would let the swing finish its own exit transition first, and in that gap vanilla, seeing
        // no attack animation, would end the throw before it began. So the animator is sent straight into the throw,
        // and the trigger is taken back here so the throw does not start over once the cut is done; other clients play
        // the trigger, which only shows the throw a moment later there. The swing's animation speed (attack speed
        // effects, a hit's freeze frame) is dropped too, so the throw's own speed is worked out afresh. Shield Throw's
        // windup (the same spear throw) uses it too.
        internal static void CutToThrow(Player player, Attack throwAttack)
        {
            Animator animator = player.m_animator;
            if (animator == null || !animator.HasState(0, ThrowStateHash))
            {
                return;
            }

            animator.CrossFadeInFixedTime(ThrowStateHash, InterruptCrossFade, 0);
            animator.ResetTrigger(throwAttack.m_attackAnimation);
            if (player.m_animEvent != null)
            {
                player.m_animEvent.m_pauseTimer = 0f;
            }
            animator.speed = 1f;
        }

        // A swing cut short can still fire its own trigger while it fades out; that is the swing's, not the throw's.
        internal static bool IsFromInterruptedSwing(Player player)
        {
            Animator animator = player.m_animator;
            return animator != null && animator.IsInTransition(0) &&
                animator.GetCurrentAnimatorStateInfo(0).shortNameHash != ThrowStateHash &&
                animator.GetNextAnimatorStateInfo(0).shortNameHash == ThrowStateHash;
        }

        // The throw's speed-up, through the same manager as the attack speed effects (which multiply it further).
        private static double ModifyThrowSpeed(Character character, double speed)
        {
            return _owner != null && ReferenceEquals(character, _owner) && IsThrowing(_owner) ? speed * ThrowAnimationSpeed : speed;
        }

        // The windup's animation trigger: the weapon leaves the hand.
        private static void Release(Attack attack)
        {
            var player = attack.m_character as Player;
            var weapon = attack.m_weapon;
            if (player == null || player != Player.m_localPlayer || weapon == null || player.IsStaggering() ||
                !player.HasActiveMagicEffect(MagicEffectType.OverwhelmingLaunch, out float effectValue))
            {
                return;
            }

            bool leftHand = weapon == player.m_leftItem && weapon != player.m_rightItem;
            var vis = player.m_visEquipment;
            Transform hand = vis == null ? null : leftHand ? vis.m_leftHand : vis.m_rightHand;
            Vector3 handPosition = hand != null ? hand.position : player.GetCenterPoint();
            Vector3 landing = FindLandingPoint(player, MaxDistance);

            float speed = ProjectileSpeed;
            float hangTime = HangTime;
            float travelTime = Vector3.Distance(handPosition, landing) / speed;

            // Snapshot now, while the weapon is still in hand: that is when EpicLoot's GetDamage modifiers apply,
            // and the hands may hold something else by the time it lands.
            var damage = weapon.GetDamage();
            damage.Modify(1f + effectValue / 100f);

            float now = Time.time;
            _inFlightWeapon = weapon;
            _inFlightUntil = now + travelTime + hangTime + travelTime;
            _pendingSlam = new PendingSlam { Time = now + travelTime, Point = landing, Weapon = weapon, Damage = damage };
            EpicLoot.Log($"[OverwhelmingLaunch] Released {weapon.m_shared.m_name}: lands at {landing} in {travelTime:0.00}s, " +
                $"slam damage {damage.EpicLootGetTotalDamage():0.#}.");

            LaunchedWeaponVisual.Broadcast(player, weapon, leftHand, landing, travelTime, hangTime, travelTime);
        }

        // The first thing along the look direction within range, else the ground under the end of that range.
        private static Vector3 FindLandingPoint(Player player, float maxDistance)
        {
            Vector3 from = player.GetEyePoint();
            Vector3 dir = player.GetLookDir();
            int count = Physics.RaycastNonAlloc(from, dir, RayHits, maxDistance, LandMask, QueryTriggerInteraction.Ignore);
            float nearest = float.MaxValue;
            Vector3 landing = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                var hit = RayHits[i];
                if (hit.distance >= nearest || Projectile.FindHitObject(hit.collider) == player.gameObject)
                {
                    continue;
                }

                nearest = hit.distance;
                landing = hit.point;
            }

            if (nearest < float.MaxValue)
            {
                return landing;
            }

            Vector3 end = from + dir * maxDistance;
            if (Physics.Raycast(end + Vector3.up, Vector3.down, out var ground, 500f, LandMask, QueryTriggerInteraction.Ignore))
            {
                return ground.point;
            }

            if (ZoneSystem.instance != null)
            {
                end.y = ZoneSystem.instance.GetGroundHeight(end);
            }
            return end;
        }

        // The Demolisher slam's shockwave, camera shake and sound at the landing point. Spawned by prefab name: going
        // through the Demolisher item's m_triggerEffect (the call vanilla makes for its own swing) showed nothing in
        // game. That list stays as the fallback should an update rename the prefabs. fx_sledge_demolisher_hit has a
        // ZNetView, so the copy spawned here is what every other client sees.
        private static int PlaySlamEffects(Player player, Vector3 center, out int itemEffectCount)
        {
            var itemEffects = ObjectDB.instance?.GetItemPrefab(SlamEffectItem)?.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_triggerEffect;
            itemEffectCount = itemEffects?.m_effectPrefabs?.Length ?? 0;

            int spawned = 0;
            var scene = ZNetScene.instance;
            foreach (var name in SlamEffectPrefabs)
            {
                var prefab = scene != null ? scene.GetPrefab(name) : null;
                if (prefab != null)
                {
                    Object.Instantiate(prefab, center, player.transform.rotation);
                    spawned++;
                }
            }

            if (spawned == 0 && itemEffects != null)
            {
                spawned = itemEffects.Create(center, player.transform.rotation, null, 1f, -1, player.GetZDOID()).Length;
            }
            return spawned;
        }

        // Vanilla Attack.DoAreaAttack with the landing point as its origin, the Demolisher's effects and stagger,
        // and the launched weapon's snapshot damage.
        private static void DoSlam(Player player, PendingSlam slam)
        {
            var weapon = slam.Weapon;
            var center = slam.Point;
            var primary = weapon.m_shared.m_attack;
            var skill = weapon.m_shared.m_skillType;

            int effectsSpawned = PlaySlamEffects(player, center, out int itemEffectCount);

            float skillFactor = player.GetRandomSkillFactor(skill);
            float stagger = StaggerMultiplier;
            int hitCount = 0;
            int damagedCount = 0;
            bool raiseSkill = false;
            float maxAdrenalineMultiplier = 0f;

            SlamHitSet.Clear();
            int count = Physics.OverlapSphereNonAlloc(center, Radius, SlamHits, LandMask, QueryTriggerInteraction.UseGlobal);

            // Weapon strikes, not bonus hits: every on-hit enchant procs per target. The scope makes the launched
            // weapon the one those handlers read even if the hands changed mid-flight.
            var scope = HitSource.Enter(null, player, weapon);
            try
            {
                for (int i = 0; i < count; i++)
                {
                    var collider = SlamHits[i];
                    if (collider == null || collider.gameObject == player.gameObject)
                    {
                        continue;
                    }

                    var target = Projectile.FindHitObject(collider);
                    if (target == null || target == player.gameObject || !SlamHitSet.Add(target))
                    {
                        continue;
                    }

                    Vector3 point = collider is MeshCollider ? collider.ClosestPointOnBounds(center) : collider.ClosestPoint(center);
                    hitCount++;

                    var destructible = target.GetComponent<IDestructible>();
                    if (destructible == null)
                    {
                        continue;
                    }

                    // Knockback points out from the centre of the slam.
                    Vector3 dir = point - center;
                    dir.y = 0f;
                    if (dir.sqrMagnitude < 0.0001f)
                    {
                        dir = point - player.transform.position;
                        dir.y = 0f;
                    }
                    dir.Normalize();

                    var shared = weapon.m_shared;
                    var hit = new HitData
                    {
                        m_toolTier = (short)shared.m_toolTier,
                        m_statusEffectHash = shared.m_attackStatusEffect != null &&
                            (shared.m_attackStatusEffectChance == 1f || Random.Range(0f, 1f) < shared.m_attackStatusEffectChance)
                            ? shared.m_attackStatusEffect.NameHash() : 0,
                        m_skillLevel = player.GetSkillLevel(skill),
                        m_itemLevel = (short)weapon.m_quality,
                        m_itemWorldLevel = (byte)weapon.m_worldLevel,
                        m_pushForce = shared.m_attackForce * skillFactor,
                        m_backstabBonus = shared.m_backstabBonus,
                        m_staggerMultiplier = stagger,
                        m_dodgeable = shared.m_dodgeable,
                        m_blockable = shared.m_blockable,
                        m_skill = skill,
                        m_skillRaiseAmount = primary.m_raiseSkillAmount,
                        m_damage = slam.Damage,
                        m_point = point,
                        m_dir = dir,
                        m_hitCollider = collider,
                        m_hitType = HitData.HitType.PlayerHit,
                        m_healthReturn = primary.m_attackHealthReturnHit,
                        m_eitrAdd = primary.m_attackEitrAdd,
                        m_variant = shared.m_hitVariant,
                    };
                    hit.m_damage.Modify(skillFactor);
                    hit.SetAttacker(player);
                    player.GetSEMan().ModifyAttack(skill, ref hit);

                    bool isEnemy = false;
                    if (destructible is Character character)
                    {
                        isEnemy = BaseAI.IsEnemy(player, character) ||
                            (character.GetBaseAI() != null && character.GetBaseAI().IsAggravatable());
                        if (!player.IsPVPEnabled() && !isEnemy)
                        {
                            continue;
                        }

                        if (isEnemy && character.m_enemyAdrenalineMultiplier > maxAdrenalineMultiplier)
                        {
                            maxAdrenalineMultiplier = character.m_enemyAdrenalineMultiplier;
                        }

                        if (hit.m_dodgeable && character.IsDodgeInvincible())
                        {
                            (character as Player)?.HitWhileDodging();
                            continue;
                        }
                    }

                    if (primary.m_attackHealthReturnHit > 0f && isEnemy)
                    {
                        player.Heal(primary.m_attackHealthReturnHit);
                    }
                    if (primary.m_attackEitrAdd > 0f)
                    {
                        player.AddEitr(primary.m_attackEitrAdd);
                    }

                    destructible.Damage(hit);
                    damagedCount++;
                    if ((destructible.GetDestructibleType() & primary.m_skillHitType) != DestructibleType.None)
                    {
                        raiseSkill = true;
                    }
                }
            }
            finally
            {
                HitSource.Exit(scope);
            }

            EpicLoot.Log($"[OverwhelmingLaunch] Slam at {center}: {count} colliders, {damagedCount} damaged, " +
                $"{effectsSpawned} effects spawned (the Demolisher item lists {itemEffectCount}).");
            if (hitCount == 0)
            {
                return;
            }

            weapon.m_shared.m_hitEffect.Create(center, Quaternion.identity, null, 1f, -1, player.GetZDOID());
            if (weapon.m_shared.m_useDurability)
            {
                weapon.m_durability -= Game.m_durabilityRate;
            }
            player.AddNoise(primary.m_attackHitNoise);
            if (maxAdrenalineMultiplier > 0f)
            {
                player.AddAdrenaline(primary.m_attackAdrenaline * maxAdrenalineMultiplier);
            }
            if (raiseSkill)
            {
                player.RaiseSkill(skill, primary.m_raiseSkillAmount);
            }
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

        // The lockout. Every attack goes through Attack.Start -- vanilla Humanoid.StartAttack and Throwable's
        // replacement throw alike -- so refusing it here covers primary, secondary and thrown attacks at once.
        [HarmonyPatch(typeof(Attack), nameof(Attack.Start))]
        private static class Attack_Start_Patch
        {
            [HarmonyPrefix]
            private static bool Prefix(Attack __instance, Humanoid character, ItemDrop.ItemData weapon, ref bool __result)
            {
                if (character == null || character != Player.m_localPlayer || __instance == _launchAttack || !IsInFlight(weapon))
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
                if (__instance == null || __instance != _launchAttack)
                {
                    return true;
                }

                // Swallowed, and the throw's own trigger still to come.
                if (__instance.m_character is Player player && IsFromInterruptedSwing(player))
                {
                    return false;
                }

                _launchAttack = null;
                Release(__instance);
                return false;
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

        // Vanilla takes eitr from the weapon's primary attack rather than from the attack being started.
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
        private static class Attack_GetAttackEitr_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Attack __instance, ref float __result)
            {
                if (__instance != null && __instance == _launchAttack)
                {
                    __result *= CostScale;
                }
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
        private static class Player_Awake_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                __instance.m_nview?.Register<ZPackage>(LaunchedWeaponVisual.Rpc,
                    (sender, pkg) => LaunchedWeaponVisual.OnRpc(__instance, pkg));
            }
        }

        [HarmonyPatch(typeof(KeyHints), nameof(KeyHints.UpdateHints))]
        private static class KeyHints_UpdateHints_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(KeyHints __instance) => LaunchHint.Refresh(__instance);
        }
    }

    // The thrown weapon as every client sees it: a local-only copy of the item's model that arcs from the thrower's
    // hand to the landing point, rests there, then homes back onto the hand. The model in the hand is hidden for the
    // duration. Nothing here affects gameplay; the slam and the lockout run on the thrower's client.
    public class LaunchedWeaponVisual : MonoBehaviour
    {
        internal const string Rpc = "el-olv";

        private const float ArcPerMetre = 0.15f;
        private const float MaxArcHeight = 4f;
        private const float MinPhaseTime = 0.05f;

        private Player _thrower;
        private bool _leftHand;
        private GameObject _hiddenHandModel;
        private Vector3 _start;
        private Vector3 _landing;
        private float _outTime;
        private float _hangTime;
        private float _backTime;
        private float _arcHeight;
        private float _elapsed;

        internal static void Broadcast(Player player, ItemDrop.ItemData weapon, bool leftHand, Vector3 landing,
            float outTime, float hangTime, float backTime)
        {
            if (player.m_nview == null || !player.m_nview.IsValid() || weapon.m_dropPrefab == null)
            {
                return;
            }

            var pkg = new ZPackage();
            pkg.Write(weapon.m_dropPrefab.name.GetStableHashCode());
            pkg.Write(leftHand);
            pkg.Write(landing);
            pkg.Write(outTime);
            pkg.Write(hangTime);
            pkg.Write(backTime);
            player.m_nview.InvokeRPC(ZNetView.Everybody, Rpc, pkg);
        }

        internal static void OnRpc(Player thrower, ZPackage pkg)
        {
            if (thrower == null || (ZNet.instance != null && ZNet.instance.IsDedicated()))
            {
                return;
            }

            int itemHash = pkg.ReadInt();
            bool leftHand = pkg.ReadBool();
            Vector3 landing = pkg.ReadVector3();
            float outTime = pkg.ReadSingle();
            float hangTime = pkg.ReadSingle();
            float backTime = pkg.ReadSingle();

            var attach = ObjectDB.instance?.GetItemPrefab(itemHash)?.transform.Find("attach");
            if (attach == null)
            {
                return;
            }

            var root = new GameObject("EL_OverwhelmingLaunch");
            var model = Instantiate(attach.gameObject, root.transform, false);
            foreach (var collider in model.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
            model.AddComponent<Spinny>();

            var visual = root.AddComponent<LaunchedWeaponVisual>();
            visual.Begin(thrower, leftHand, landing, outTime, hangTime, backTime);
        }

        private void Begin(Player thrower, bool leftHand, Vector3 landing, float outTime, float hangTime, float backTime)
        {
            _thrower = thrower;
            _leftHand = leftHand;
            _start = HandPosition();
            _landing = landing;
            _outTime = Mathf.Max(MinPhaseTime, outTime);
            _hangTime = Mathf.Max(0f, hangTime);
            _backTime = Mathf.Max(MinPhaseTime, backTime);
            _arcHeight = Mathf.Min(MaxArcHeight, Vector3.Distance(_start, _landing) * ArcPerMetre);
            transform.position = _start;

            var vis = thrower.m_visEquipment;
            var handModel = vis == null ? null : leftHand ? vis.m_leftItemInstance : vis.m_rightItemInstance;
            if (handModel != null && handModel.activeSelf)
            {
                handModel.SetActive(false);
                _hiddenHandModel = handModel;
            }
        }

        private Vector3 HandPosition()
        {
            var vis = _thrower.m_visEquipment;
            var hand = vis == null ? null : _leftHand ? vis.m_leftHand : vis.m_rightHand;
            return hand != null ? hand.position : _thrower.GetCenterPoint();
        }

        private static Vector3 Arc(Vector3 from, Vector3 to, float t, float height)
        {
            return Vector3.Lerp(from, to, t) + Vector3.up * (height * 4f * t * (1f - t));
        }

        public void Update()
        {
            if (_thrower == null)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < _outTime)
            {
                transform.position = Arc(_start, _landing, _elapsed / _outTime, _arcHeight);
                return;
            }

            float returning = _elapsed - _outTime - _hangTime;
            if (returning < 0f)
            {
                transform.position = _landing;
                return;
            }

            if (returning >= _backTime)
            {
                Destroy(gameObject);
                return;
            }

            // Re-aimed every frame, since the thrower keeps moving.
            transform.position = Arc(_landing, HandPosition(), returning / _backTime, _arcHeight);
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
