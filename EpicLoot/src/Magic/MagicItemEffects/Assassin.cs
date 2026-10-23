using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string Assassin = nameof(Assassin);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // Assassin (4-piece Loki set bonus, NoRoll). Keeping the crosshair on an enemy within MaxDistance for HoverTime
    // seconds, with a melee weapon drawn, makes the player vanish and reappear behind it, already swinging: the
    // weapon's primary attack, free of cost, dealing value% of its damage. A cooldown follows, shown on the HUD with
    // the Fenris hood's icon; it shortens as the value grows, so higher rarities strike harder and more often.
    //
    // The strike is a real Attack (a clone of the weapon's primary, as Humanoid.StartAttack makes), so vanilla
    // handles the animation, the hit sweep and backstabs, and every on-hit enchantment procs. Holstering the weapon
    // is how a player keeps it from firing. While the crosshair charges, AssassinChargeHud fills a ring around it.
    public static class Assassin
    {
        // All tunable in this effect's Config block in magiceffects.json, under these key names.
        public const float DefaultMaxDistance = 20f;       // metres from the player to the target
        public const float DefaultHoverTime = 2f;          // seconds the crosshair has to stay on the target
        public const float DefaultCooldown = 340f;         // seconds at value 0; 180 at the Legendary 200
        public const float DefaultCooldownPerValue = 0.8f; // seconds removed per 1% of damage, 20 per rarity step
        public const float DefaultMinCooldown = 30f;

        private const string MaxDistanceKey = "MaxDistance";
        private const string HoverTimeKey = "HoverTime";
        private const string CooldownKey = "Cooldown";
        private const string CooldownPerValueKey = "CooldownPerValue";
        private const string MinCooldownKey = "MinCooldown";

        // Player-editable, so each gets a floor: a zero hover time would fire on a glance.
        private static float MaxDistance => EffectConfig.GetClamped(MagicEffectType.Assassin, MaxDistanceKey, DefaultMaxDistance, 2f, 50f);
        private static float HoverTime => Mathf.Max(0.1f, EffectConfig.Get(MagicEffectType.Assassin, HoverTimeKey, DefaultHoverTime));

        private const float HoverGrace = 0.3f;     // the crosshair may slip off a moving target this long
        private const float StandOff = 0.3f;       // gap between the player's capsule and the target's
        private const float MaxStepUp = 1.5f;      // the spot behind may sit this far above the target's feet...
        private const float MaxDrop = 4f;          // ...or this far below them (under a low flyer)
        private const float MaxWaterDepth = 1.2f;  // deeper than this and the player would surface swimming
        private const float MaxPitch = 45f;

        private const string HoodItem = "HelmetFenring";
        private static readonly string[] FxPrefabs = { "fx_JotunWitch_Dodge", "sfx_jotunwitch_dodge" };

        // Tooltip: "... within {2}m for {3}s ... {0}% damage. Cooldown: {1}s" -- {1} follows the value and a retune.
        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.Assassin,
                value => new object[] { value, GetCooldown(value), MaxDistance, HoverTime });
        }

        // Shrinks as the value grows, never past MinCooldown, and never to zero.
        private static float GetCooldown(float value)
        {
            float cooldown = EffectConfig.Get(MagicEffectType.Assassin, CooldownKey, DefaultCooldown) -
                value * EffectConfig.Get(MagicEffectType.Assassin, CooldownPerValueKey, DefaultCooldownPerValue);
            float floor = EffectConfig.Get(MagicEffectType.Assassin, MinCooldownKey, DefaultMinCooldown);
            return Mathf.Max(0.1f, Mathf.Max(floor, cooldown));
        }

        // Cooldown HUD indicator, built on first use so ObjectDB has the hood. Display only: the gate is _readyAt,
        // so a missing icon cannot leave the strike without a cooldown.
        private const string CooldownName = "EL_AssassinCooldown";
        private static StatusEffect _cooldownIndicator;
        private static bool _cooldownMissingLogged;
        private static bool _missingFxLogged;

        // State for the local player only; SyncOwner drops it when the local player changes (respawn, logout), as
        // death clears the indicator too.
        private static Player _owner;
        private static float _readyAt;
        private static Character _target;
        private static float _hoverStart;
        private static float _lastSeen;
        // The last strike started, for the eitr postfix: Attack.Start only checks eitr and Attack.Update spends it
        // later, so it stays set past Start. It matches only that one Attack instance.
        private static Attack _strike;

        private static int _groundMask;
        private static int _blockMask;
        private static readonly RaycastHit[] RayHits = new RaycastHit[32];
        private static readonly Collider[] OverlapHits = new Collider[16];

        // What the player can stand on and a line of sight stops at.
        private static int GroundMask => _groundMask != 0 ? _groundMask : _groundMask = LayerMask.GetMask("Default",
            "static_solid", "Default_small", "piece", "terrain", "vehicle");

        // What the player's capsule may not overlap. Not terrain: the spot came from a ray down onto it, and the
        // capsule would clip any slope.
        private static int BlockMask => _blockMask != 0 ? _blockMask : _blockMask = LayerMask.GetMask("Default",
            "static_solid", "Default_small", "piece", "vehicle", "character", "character_net", "character_ghost",
            "character_noenv");

        // How far the hover on the current target has charged, 0 to 1, or -1 when nothing is charging. A finished
        // charge reads 1 while it waits for the player to be free to strike.
        internal static float ChargeProgress
        {
            get
            {
                if (_target == null || _owner == null || _owner != Player.m_localPlayer)
                {
                    return -1f;
                }

                return Mathf.Clamp01((Time.time - _hoverStart) / HoverTime);
            }
        }

        private static void SyncOwner(Player player)
        {
            if (_owner == player)
            {
                return;
            }

            _owner = player;
            _readyAt = 0f;
            _target = null;
        }

        // The weapon the strike swings, or null: a drawn melee weapon, never fists.
        private static ItemDrop.ItemData GetStrikeWeapon(Player player)
        {
            var weapon = player.GetCurrentWeapon();
            if (weapon == null || weapon.m_shared.m_tamedOnly ||
                (player.m_unarmedWeapon != null && weapon == player.m_unarmedWeapon.m_itemData))
            {
                return null;
            }

            switch (weapon.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                    break;
                default:
                    return null;
            }

            switch (weapon.m_shared.m_skillType)
            {
                case Skills.SkillType.Pickaxes:
                case Skills.SkillType.Bows:
                case Skills.SkillType.Crossbows:
                case Skills.SkillType.ElementalMagic:
                case Skills.SkillType.BloodMagic:
                case Skills.SkillType.Fishing:
                    return null;
            }

            if (!weapon.HavePrimaryAttack())
            {
                return null;
            }

            var attackType = weapon.m_shared.m_attack.m_attackType;
            return attackType == Attack.AttackType.Horizontal || attackType == Attack.AttackType.Vertical ||
                attackType == Attack.AttackType.Area ? weapon : null;
        }

        private static bool CanHover(Player player)
        {
            return !player.IsDead() && player.TakeInput() && !player.InPlaceMode() && !player.IsAttached() &&
                !player.IsTeleporting() && !player.InIntro() && !player.InCutscene() && !player.IsSleeping() &&
                player.m_doodadController == null;
        }

        // The states Humanoid.StartAttack refuses to start an attack in, plus swimming.
        private static bool CanStrike(Player player)
        {
            return !player.InAttack() && !player.InDodge() && player.CanMove() && !player.IsKnockedBack() &&
                !player.IsStaggering() && !player.InMinorAction() && !player.IsSwimming();
        }

        private static void OnLocalPlayerUpdate(Player player)
        {
            SyncOwner(player);

            // Memoized total first: this runs every frame.
            float now = Time.time;
            if (!player.HasActiveMagicEffect(MagicEffectType.Assassin, out float value) || now < _readyAt ||
                !CanHover(player))
            {
                _target = null;
                return;
            }

            var weapon = GetStrikeWeapon(player);
            if (weapon == null)
            {
                _target = null;
                return;
            }

            var hovered = FindHoveredEnemy(player);
            if (hovered == null)
            {
                if (_target != null && now - _lastSeen > HoverGrace)
                {
                    _target = null;
                }
                return;
            }

            if (hovered != _target)
            {
                _target = hovered;
                _hoverStart = now;
            }
            _lastSeen = now;

            // Held long enough: strike as soon as the player is free to (mid-swing, it waits for the swing to end).
            if (now - _hoverStart < HoverTime || !CanStrike(player))
            {
                return;
            }

            // Whether or not it works, the hover starts over, so a target with no room behind it is not searched
            // around every frame.
            var target = _target;
            _target = null;
            TryAssassinate(player, target, weapon, value);
        }

        // Vanilla's hover ray (Player.FindHoverObject): the first thing under the crosshair, if it is a character.
        // Unlike vanilla's hover, a sleeping creature counts -- it is the assassin's best target.
        private static Character FindHoveredEnemy(Player player)
        {
            var camera = GameCamera.instance;
            if (camera == null)
            {
                return null;
            }

            int count = Physics.RaycastNonAlloc(camera.transform.position, camera.transform.forward, RayHits, 50f,
                player.m_interactMask);
            float nearest = float.MaxValue;
            Collider first = null;
            for (int i = 0; i < count; i++)
            {
                var hit = RayHits[i];
                if (hit.distance >= nearest ||
                    (hit.collider.attachedRigidbody != null && hit.collider.attachedRigidbody.gameObject == player.gameObject))
                {
                    continue;
                }

                nearest = hit.distance;
                first = hit.collider;
            }

            if (first == null)
            {
                return null;
            }

            var character = first.attachedRigidbody != null
                ? first.attachedRigidbody.GetComponent<Character>()
                : first.GetComponent<Character>();
            return IsValidTarget(player, character) ? character : null;
        }

        private static bool IsValidTarget(Player player, Character target)
        {
            if (target == null || target == player || target.IsDead() || target.m_nview == null || !target.m_nview.IsValid())
            {
                return false;
            }

            bool enemy = BaseAI.IsEnemy(player, target) ||
                (target is Player && player.IsPVPEnabled() && target.IsPVPEnabled());
            if (!enemy)
            {
                return false;
            }

            float distance = Vector3.Distance(player.GetCenterPoint(), target.GetCenterPoint()) - target.GetRadius();
            return distance <= MaxDistance && !ParticleMist.IsMistBlocked(player.GetCenterPoint(), target.GetCenterPoint());
        }

        private static void TryAssassinate(Player player, Character target, ItemDrop.ItemData weapon, float value)
        {
            if (!FindStrikePoint(player, target, out Vector3 point))
            {
                return;
            }

            Vector3 origin = player.transform.position;
            Vector3 facing = target.transform.position - point;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
            {
                facing = player.transform.forward;
            }
            Quaternion yaw = Quaternion.LookRotation(facing.normalized);

            PlayFx(origin + Vector3.up, yaw);
            MagicEffectsHelper.MovePlayer(player, point, yaw);
            AimAt(player, target, yaw);
            PlayFx(point + Vector3.up, yaw);

            StartStrike(player, weapon, value);
            StartCooldown(player, GetCooldown(value));
        }

        // A spot at the target's back the player fits into and the target can be seen from, else the first that works
        // going round it, ending in front.
        private static readonly float[] SearchAngles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };

        private static bool FindStrikePoint(Player player, Character target, out Vector3 point)
        {
            point = Vector3.zero;
            var capsule = player.m_collider;
            float radius = capsule != null ? capsule.radius : 0.4f;
            float height = capsule != null ? capsule.height : 1.8f;

            Vector3 feet = target.transform.position;
            Vector3 center = target.GetCenterPoint();
            Vector3 back = -target.transform.forward;
            back.y = 0f;
            if (back.sqrMagnitude < 0.0001f)
            {
                back = feet - player.transform.position;
                back.y = 0f;
            }
            back.Normalize();

            float distance = target.GetRadius() + radius + StandOff;
            foreach (float angle in SearchAngles)
            {
                Vector3 candidate = feet + Quaternion.Euler(0f, angle, 0f) * back * distance;
                if (!Physics.Raycast(candidate + Vector3.up * MaxStepUp, Vector3.down, out var ground, MaxStepUp + MaxDrop,
                    GroundMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                Vector3 spot = ground.point;
                if (Floating.GetLiquidLevel(spot) > spot.y + MaxWaterDepth ||
                    Physics.Linecast(center, spot + Vector3.up, GroundMask, QueryTriggerInteraction.Ignore) ||
                    !Fits(player, target, spot, radius, height))
                {
                    continue;
                }

                point = spot;
                return true;
            }

            return false;
        }

        // Whether the player's capsule, standing at the spot, overlaps anything but the player and the target.
        private static bool Fits(Player player, Character target, Vector3 spot, float radius, float height)
        {
            Vector3 bottom = spot + Vector3.up * (radius + 0.25f);
            Vector3 top = spot + Vector3.up * Mathf.Max(radius + 0.25f, height - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, OverlapHits, BlockMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var collider = OverlapHits[i];
                var owner = collider.attachedRigidbody != null ? collider.attachedRigidbody.transform : collider.transform;
                if (owner.IsChildOf(player.transform) || owner.IsChildOf(target.transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        // Turns the view, and with it the swing (Attack.Start faces the look yaw, the melee sweep pitches with the
        // eye), onto the target.
        private static void AimAt(Player player, Character target, Quaternion yaw)
        {
            Vector3 eye = player.m_eye != null ? player.m_eye.position : player.GetCenterPoint();
            Vector3 toTarget = (target.GetCenterPoint() - eye).normalized;
            player.m_lookYaw = yaw;
            player.m_lookPitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(toTarget.y, -1f, 1f)) * Mathf.Rad2Deg, -MaxPitch, MaxPitch);
            player.m_lookTransitionTime = 0f;
            if (player.m_eye != null)
            {
                player.UpdateEyeRotation();
                player.m_lookDir = player.m_eye.forward;
            }
        }

        // Humanoid.StartAttack's primary attack, minus its guards (checked already) and its costs, at value% damage.
        private static void StartStrike(Player player, ItemDrop.ItemData weapon, float value)
        {
            var attack = weapon.m_shared.m_attack.Clone();
            attack.m_attackStamina = 0f;
            attack.m_attackHealth = 0f;
            attack.m_attackHealthPercentage = 0f;
            attack.m_damageMultiplier *= value / 100f;

            if (player.m_currentAttack != null)
            {
                player.m_currentAttack.Stop();
                player.m_previousAttack = player.m_currentAttack;
                player.m_currentAttack = null;
            }

            // No previous attack: the strike opens a combo rather than continuing one.
            _strike = attack;
            bool started = false;
            try
            {
                started = attack.Start(player, player.m_body, player.m_zanim, player.m_animEvent, player.m_visEquipment,
                    weapon, null, float.MaxValue, player.GetAttackDrawPercentage());
            }
            finally
            {
                if (!started)
                {
                    _strike = null;
                }
            }

            if (!started)
            {
                return;
            }

            player.m_currentAttack = attack;
            player.m_currentAttackIsSecondary = false;
            player.m_lastCombatTimer = 0f;
        }

        // Networked prefabs, so one spawn here is seen by everyone.
        private static void PlayFx(Vector3 position, Quaternion rotation)
        {
            var scene = ZNetScene.instance;
            if (scene == null)
            {
                return;
            }

            foreach (var name in FxPrefabs)
            {
                var prefab = scene.GetPrefab(name);
                if (prefab == null)
                {
                    if (!_missingFxLogged)
                    {
                        _missingFxLogged = true;
                        EpicLoot.LogWarning($"[Assassin] could not find the '{name}' prefab; the teleport plays without it.");
                    }
                    continue;
                }

                Object.Instantiate(prefab, position, rotation);
            }
        }

        // m_ttl is set on the shared prototype before adding; AddStatusEffect clones it, so the added instance
        // carries this cooldown.
        private static void StartCooldown(Player player, float cooldown)
        {
            _readyAt = Time.time + cooldown;
            var indicator = GetOrCreateCooldownIndicator();
            if (indicator != null)
            {
                indicator.m_ttl = cooldown;
                player.GetSEMan().AddStatusEffect(indicator, true);
            }
        }

        // A null icon would render as an invisible HUD entry, so a missing hood logs once and shows nothing.
        private static StatusEffect GetOrCreateCooldownIndicator()
        {
            if (_cooldownIndicator != null)
            {
                return _cooldownIndicator;
            }

            var icon = ObjectDB.instance?.GetItemPrefab(HoodItem)?.GetComponent<ItemDrop>()?.m_itemData.GetIcon();
            if (icon == null)
            {
                if (!_cooldownMissingLogged)
                {
                    _cooldownMissingLogged = true;
                    EpicLoot.LogWarning($"[Assassin] could not find the '{HoodItem}' icon; the cooldown will not display.");
                }
                return null;
            }

            var se = ScriptableObject.CreateInstance<StatusEffect>();
            se.name = CooldownName;
            se.m_name = "$mod_epicloot_se_assassin";
            se.m_icon = icon;
            se.m_ttl = DefaultMinCooldown;   // overwritten per strike by StartCooldown with the value-scaled cooldown
            se.m_cooldownIcon = true;
            _cooldownIndicator = se;
            return _cooldownIndicator;
        }

        [HarmonyPatch(typeof(Player), nameof(Player.Update))]
        private static class Player_Update_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Player __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    OnLocalPlayerUpdate(__instance);
                }
            }
        }

        // Vanilla takes eitr from the weapon's primary attack rather than from the attack being started, so the free
        // strike's eitr is zeroed here.
        [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackEitr), typeof(Character), typeof(ItemDrop.ItemData))]
        private static class Attack_GetAttackEitr_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Attack __instance, ref float __result)
            {
                if (__instance != null && __instance == _strike)
                {
                    __result = 0f;
                }
            }
        }

        // Hud.Update rather than its UpdateCrosshair, which vanilla skips once there is no local player and would
        // leave a ring on screen through death.
        [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
        private static class Hud_Update_Patch
        {
            [HarmonyPostfix]
            private static void Postfix(Hud __instance) => AssassinChargeHud.Refresh(__instance);
        }
    }

    // The charge ring: vanilla's bow-draw ring (Hud.m_crosshairBow) filling clockwise around the crosshair over a faint
    // full track, with a label above it. Both are clones of vanilla's own crosshair widgets, so they follow the HUD's
    // scaling, font and hiding; a UI mod that removed those widgets gets no ring.
    internal static class AssassinChargeHud
    {
        private const string LabelToken = "$mod_epicloot_assassin_charge";
        private const float RingSize = 56f;       // pixels; vanilla's ring is 150 drawn down to 22
        private const float LabelGap = 4f;        // pixels between the ring and the label
        private const float LabelFontSize = 16f;
        private const float MinAlpha = 0.35f;     // a glance shows a faint ring; it brightens as it charges

        private static readonly Color TrackColor = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color ChargeColor = new Color(0.55f, 1f, 0.6f);

        private static Hud _hud;
        private static GameObject _root;
        private static Image _fill;
        private static TMP_Text _label;

        internal static void Refresh(Hud hud)
        {
            if (_hud != hud)
            {
                _hud = hud;
                Build(hud);
            }

            if (_root == null)
            {
                return;
            }

            float progress = Assassin.ChargeProgress;
            bool show = progress >= 0f && hud.m_crosshair != null && hud.m_crosshair.gameObject.activeSelf;
            if (_root.activeSelf != show)
            {
                _root.SetActive(show);
                if (show && _label != null)
                {
                    // Set on each appearance, so a language change mid-game is picked up.
                    _label.text = Localization.instance.Localize(LabelToken);
                }
            }

            if (!show)
            {
                return;
            }

            var color = ChargeColor;
            color.a = Mathf.Lerp(MinAlpha, 1f, progress);
            _fill.fillAmount = progress;
            _fill.color = color;
            if (_label != null)
            {
                _label.color = color;
            }
        }

        private static void Build(Hud hud)
        {
            _root = null;
            _fill = null;
            _label = null;

            var bow = hud.m_crosshairBow;
            if (bow == null)
            {
                return;
            }

            _root = new GameObject("EL_AssassinCharge", typeof(RectTransform));
            var rect = (RectTransform)_root.transform;
            rect.SetParent(bow.transform.parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = ((RectTransform)bow.transform).anchoredPosition;
            rect.sizeDelta = new Vector2(RingSize, RingSize);

            var track = CloneRing(bow, rect, "Track");
            track.color = TrackColor;

            _fill = CloneRing(bow, rect, "Fill");
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Radial360;
            _fill.fillOrigin = (int)Image.Origin360.Top;
            _fill.fillClockwise = true;
            _fill.fillAmount = 0f;

            var hoverName = hud.m_hoverName;
            if (hoverName != null)
            {
                var labelObject = Object.Instantiate(hoverName.gameObject, rect, false);
                labelObject.name = "Label";
                _label = labelObject.GetComponent<TMP_Text>();
                _label.text = "";
                _label.fontSize = LabelFontSize;
                _label.alignment = TextAlignmentOptions.Bottom;
                _label.textWrappingMode = TextWrappingModes.NoWrap;
                _label.raycastTarget = false;

                var labelRect = (RectTransform)labelObject.transform;
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 1f);
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.anchoredPosition = new Vector2(0f, LabelGap);
                labelRect.sizeDelta = new Vector2(300f, LabelFontSize * 2f);
                labelRect.localScale = Vector3.one;
            }

            _root.SetActive(false);
        }

        // Stretched over the root at scale 1: vanilla rescales its own ring every frame of a bow draw.
        private static Image CloneRing(Image bow, RectTransform parent, string name)
        {
            var ring = Object.Instantiate(bow.gameObject, parent, false);
            ring.name = name;
            ring.SetActive(true);

            var ringRect = (RectTransform)ring.transform;
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.pivot = new Vector2(0.5f, 0.5f);
            ringRect.anchoredPosition = Vector2.zero;
            ringRect.sizeDelta = Vector2.zero;
            ringRect.localScale = Vector3.one;

            var image = ring.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }
    }
}
