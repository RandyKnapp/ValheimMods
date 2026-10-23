using System;
using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Transformation;

public class HexenForm : MonoBehaviour
{
    public const string PrefabName = "JotunWitch";
    public const string ZdoKey = "EpicLoot.Hexen";
    public const float DefaultScale = 0.6f;

    private const float PollInterval = 0.5f;
    // The JotunWitch prefab's EyePos, which its Visual clone does not carry.
    private const float WitchEyeHeight = 2.45f;
    private const float WitchEyeForward = 0.163f;

    private static readonly Dictionary<Character, HexenForm> ActiveForms = new();

    // The hexen console command: a scale forces the form on, 0 forces it off, null lets the set decide.
    public static float? DebugScale;

    public bool Active { get; private set; }
    public float Scale { get; private set; }
    public bool Flying => Active && _player.m_flying;

    private Player _player;
    private ZNetView _nview;
    private float _pollTimer;
    private float _drainPerSecond;

    private GameObject _visual;
    private ItemDrop.ItemData _blast;
    private ItemDrop.ItemData _bolt;
    private ItemDrop.ItemData _dodgeLeft;
    private ItemDrop.ItemData _dodgeRight;
    private ItemDrop.ItemData _dodgeUp;
    private ItemDrop.ItemData _dodgeDown;
    private ItemDrop.ItemData _forcedWeapon;
    private ItemDrop.ItemData _queuedDodge;

    private GameObject _savedVisual;
    private Animator _savedAnimator;
    private CharacterAnimEvent _savedAnimEvent;
    private LODGroup _savedLodGroup;
    private Vector3 _savedLodRef;
    private Transform _savedHead;
    private Transform[] _savedFeet;
    private List<FootStep.StepEffect> _savedStepEffects;
    private Animator _savedFootStepAnimator;
    private Vector3 _savedEyePos;
    private float _savedFlyTurnSpeed;
    private EffectList _savedFlyingEffect;
    private EffectList _savedHitEffects;
    private EffectList _witchFlyingEffect;

    public static bool TryGet(Character character, out HexenForm form)
    {
        form = null;
        return character != null && ActiveForms.TryGetValue(character, out form);
    }

    private void Awake()
    {
        _player = GetComponent<Player>();
        _nview = _player.m_nview;
    }

    private void OnDestroy()
    {
        ActiveForms.Remove(_player);
    }

    private void Update()
    {
        if (_nview == null || !_nview.IsValid()) return;

        if (Flying && _nview.IsOwner()) DrainEitr(Time.deltaTime);

        _pollTimer += Time.deltaTime;
        if (_pollTimer < PollInterval) return;
        _pollTimer = 0f;
        Refresh();
    }

    // The owner decides the form from the set (or the console override) and mirrors it to the ZDO; every
    // client, the owner included, then follows the ZDO.
    public void Refresh()
    {
        if (_nview == null || !_nview.IsValid()) return;

        var zdo = _nview.GetZDO();
        if (_nview.IsOwner())
        {
            var wanted = WantedScale();
            if (!Mathf.Approximately(wanted, zdo.GetFloat(ZdoKey, 0f))) zdo.Set(ZdoKey, wanted);
        }

        var scale = zdo.GetFloat(ZdoKey, 0f);
        if (scale <= 0f)
        {
            if (Active) Revert();
        }
        else if (!Active || !Mathf.Approximately(scale, Scale))
        {
            Apply(scale);
        }
    }

    private float WantedScale()
    {
        var hasSet = _player.HasActiveMagicEffect(MagicEffectType.Hexen, out var drain);
        _drainPerSecond = hasSet ? drain : Hexen.FallbackDrain;
        if (DebugScale.HasValue) return DebugScale.Value;
        return hasSet ? Hexen.Scale : 0f;
    }

    // Taken from m_eitr directly, like Frostwalker, so the Eitr-use patches do not react to a per-frame
    // drain. Regen is held off the way a cast does, so the drain is the whole cost of staying up.
    private void DrainEitr(float dt)
    {
        _player.m_eitr = Mathf.Max(0f, _player.m_eitr - _drainPerSecond * Game.m_eitrRate * dt);
        _player.m_eitrRegenTimer = Mathf.Max(_player.m_eitrRegenTimer, _player.m_eitrRegenDelay);
    }

    private bool CanTakeOff => _player.m_eitr >= Hexen.MinEitr;
    private bool OutOfEitr => _player.m_eitr <= 0f;

    private bool Apply(float scale)
    {
        Scale = scale;
        var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(PrefabName) : null;
        var prefabVisual = prefab != null ? prefab.transform.Find("Visual") : null;
        var witch = prefab != null ? prefab.GetComponent<Humanoid>() : null;
        if (prefabVisual == null || witch == null)
        {
            EpicLoot.LogWarning($"HexenForm: prefab '{PrefabName}' is missing or has no Visual/Humanoid");
            return false;
        }

        _blast = WeaponFrom(witch, "JotunWitch_attack_magicblast");
        _bolt = WeaponFrom(witch, "JotunWitch_attack_lightningbolt");
        _dodgeLeft = WeaponFrom(witch, "JotunWitch_attack_dodge");
        _dodgeRight = WeaponFrom(witch, "JotunWitch_attack_dodge2");
        _dodgeUp = WeaponFrom(witch, "JotunWitch_attack_dodge_up");
        _dodgeDown = WeaponFrom(witch, "JotunWitch_attack_dodge_down");
        if (_blast == null || _bolt == null || _dodgeLeft == null || _dodgeRight == null || _dodgeUp == null || _dodgeDown == null)
        {
            EpicLoot.LogWarning($"HexenForm: '{PrefabName}' no longer carries the expected default attack items");
            return false;
        }

        if (Active) Revert();

        _visual = Instantiate(prefabVisual.gameObject, _player.transform);
        _visual.name = "HexenVisual";
        _visual.transform.localPosition = prefabVisual.localPosition;
        _visual.transform.localRotation = Quaternion.identity;
        _visual.transform.localScale = prefabVisual.localScale * scale;
        foreach (var levelEffects in _visual.GetComponentsInChildren<LevelEffects>(true))
        {
            Destroy(levelEffects);
        }

        var animator = _visual.GetComponentInChildren<Animator>();
        animator.logWarnings = false;
        var lodGroup = _visual.GetComponent<LODGroup>();

        _savedVisual = _player.m_visual;
        _savedAnimator = _player.m_animator;
        _savedAnimEvent = _player.m_animEvent;
        _savedLodGroup = _player.m_lodGroup;
        _savedLodRef = _player.m_originalLocalRef;
        _savedHead = _player.m_head;
        _savedEyePos = _player.m_eye.localPosition;
        _savedFlyTurnSpeed = _player.m_flyTurnSpeed;
        _savedFlyingEffect = _player.m_flyingContinuousEffect;
        _savedHitEffects = _player.m_hitEffects;

        _savedVisual.SetActive(false);
        _player.m_visual = _visual;
        _player.m_animator = animator;
        _player.m_animEvent = animator.GetComponent<CharacterAnimEvent>();
        _player.m_lodGroup = lodGroup;
        _player.m_originalLocalRef = lodGroup != null ? lodGroup.localReferencePoint : Vector3.zero;
        _player.m_head = Utils.GetBoneTransform(animator, HumanBodyBones.Head) ?? _savedHead;
        _player.m_zanim.m_animator = animator;
        _player.m_eye.localPosition = new Vector3(_savedEyePos.x, Mathf.Max(_savedEyePos.y, WitchEyeHeight * scale), WitchEyeForward * scale);
        _player.m_flyTurnSpeed = witch.m_flyTurnSpeed;
        _witchFlyingEffect = witch.m_flyingContinuousEffect;
        _player.m_flyingContinuousEffect = _witchFlyingEffect;
        _player.m_hitEffects = witch.m_hitEffects;

        var footStep = _player.GetComponent<FootStep>();
        var witchFootStep = witch.GetComponent<FootStep>();
        if (footStep != null)
        {
            _savedFeet = footStep.m_feet;
            _savedStepEffects = footStep.m_effects;
            _savedFootStepAnimator = footStep.m_animator;
            footStep.m_feet = FindFeet(_visual.transform);
            if (witchFootStep != null) footStep.m_effects = witchFootStep.m_effects;
            footStep.m_animator = animator;
        }

        Active = true;
        ActiveForms[_player] = this;
        return true;
    }

    private void Revert()
    {
        if (!Active) return;

        if (_player.m_flying) _player.Land();
        if (_player.m_currentAttack != null)
        {
            _player.m_currentAttack.Stop();
            _player.m_currentAttack = null;
        }

        Character.SetupContinuousEffect(_player.transform, _player.transform.position, false, _witchFlyingEffect, ref _player.m_flyingEffects_instances);

        _player.m_visual = _savedVisual;
        _player.m_animator = _savedAnimator;
        _player.m_animEvent = _savedAnimEvent;
        _player.m_lodGroup = _savedLodGroup;
        _player.m_originalLocalRef = _savedLodRef;
        _player.m_head = _savedHead;
        _player.m_zanim.m_animator = _savedAnimator;
        _player.m_eye.localPosition = _savedEyePos;
        _player.m_flyTurnSpeed = _savedFlyTurnSpeed;
        _player.m_flyingContinuousEffect = _savedFlyingEffect;
        _player.m_hitEffects = _savedHitEffects;

        var footStep = _player.GetComponent<FootStep>();
        if (footStep != null && _savedFeet != null)
        {
            footStep.m_feet = _savedFeet;
            footStep.m_effects = _savedStepEffects;
            footStep.m_animator = _savedFootStepAnimator;
        }

        _savedVisual.SetActive(true);
        Destroy(_visual);
        _visual = null;
        _forcedWeapon = null;
        _queuedDodge = null;

        Active = false;
        ActiveForms.Remove(_player);
    }

    private static ItemDrop.ItemData WeaponFrom(Humanoid witch, string prefabName)
    {
        foreach (var go in witch.m_defaultItems)
        {
            if (go == null || go.name != prefabName) continue;
            var drop = go.GetComponent<ItemDrop>();
            if (drop == null) return null;
            var item = drop.m_itemData.Clone();
            item.m_dropPrefab = go;
            return item;
        }

        return null;
    }

    // Her FootStep leaves m_feet unassigned, so the feet come from the armature's bone names instead.
    private static Transform[] FindFeet(Transform root)
    {
        var feet = new List<Transform>(2);
        var left = Utils.FindChild(root, "LeftFoot");
        var right = Utils.FindChild(root, "RightFoot");
        if (left != null) feet.Add(left);
        if (right != null) feet.Add(right);
        return feet.ToArray();
    }

    private void Dodge(Vector3 direction)
    {
        var local = _player.transform.InverseTransformDirection(direction);
        _queuedDodge = Mathf.Abs(local.x) > Mathf.Abs(local.z)
            ? (local.x < 0f ? _dodgeLeft : _dodgeRight)
            : (local.z >= 0f ? _dodgeUp : _dodgeDown);
        _player.StartAttack(null, false);
        _queuedDodge = null;
    }

    private void UpdateFlightInput(bool descendHeld, bool ascendHeld)
    {
        var moveDir = _player.m_moveDir;
        moveDir.y = OutOfEitr ? -1f : (ascendHeld ? 1f : 0f) - (descendHeld ? 1f : 0f);
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
        _player.m_moveDir = moveDir;

        if ((descendHeld || OutOfEitr) && _player.IsOnGround()) _player.Land();
    }

    private static bool JumpHeld => ZInput.GetButton("Jump") || ZInput.GetButton("JoyJump");
    private static bool CrouchHeld => ZInput.GetButton("Crouch") || ZInput.GetButton("JoyCrouch");

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static class Player_Awake_Patch
    {
        private static void Postfix(Player __instance)
        {
            __instance.gameObject.AddComponent<HexenForm>();
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetCurrentWeapon))]
    private static class Humanoid_GetCurrentWeapon_Patch
    {
        private static bool Prefix(Humanoid __instance, ref ItemDrop.ItemData __result)
        {
            if (!TryGet(__instance, out var form)) return true;
            __result = form._forcedWeapon ?? form._blast;
            return false;
        }
    }

    // Every witch item carries its attack in the primary slot, so the secondary flag is cleared and the
    // secondary input is answered by swapping in the lightning bolt item instead.
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    private static class Humanoid_StartAttack_Patch
    {
        private static void Prefix(Humanoid __instance, ref bool secondaryAttack)
        {
            if (!TryGet(__instance, out var form)) return;
            form._forcedWeapon = form._queuedDodge ?? (secondaryAttack ? form._bolt : form._blast);
            secondaryAttack = false;
        }

        private static void Postfix(Humanoid __instance)
        {
            if (TryGet(__instance, out var form)) form._forcedWeapon = null;
        }
    }

    // Both vanilla methods read animator layer 1, which her single-layer rig lacks; Unity warns on every
    // call and her states carry none of the tags they look for anyway.
    [HarmonyPatch(typeof(Player), nameof(Player.InMinorAction))]
    private static class Player_InMinorAction_Patch
    {
        private static bool Prefix(Player __instance, ref bool __result)
        {
            if (!TryGet(__instance, out _)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.InMinorActionSlowdown))]
    private static class Player_InMinorActionSlowdown_Patch
    {
        private static bool Prefix(Player __instance, ref bool __result)
        {
            if (!TryGet(__instance, out _)) return true;
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Dodge))]
    private static class Player_Dodge_Patch
    {
        private static bool Prefix(Player __instance, Vector3 dodgeDir)
        {
            if (!TryGet(__instance, out var form)) return true;
            form.Dodge(dodgeDir);
            return false;
        }
    }

    // UpdateRotation turns the whole root toward m_moveDir, which flight input gives a vertical
    // component, so climbing pitched her face-up as if walking a wall. Only the horizontal part
    // steers; pure ascent or descent keeps her current heading.
    [HarmonyPatch(typeof(Character), nameof(Character.UpdateRotation))]
    private static class Character_UpdateRotation_Patch
    {
        private static bool Prefix(Character __instance, ref float __result, ref Vector3 __state)
        {
            if (!TryGet(__instance, out var form) || !form.Flying) return true;

            __state = __instance.m_moveDir;
            var flat = new Vector3(__state.x, 0f, __state.z);
            if (flat.sqrMagnitude < 0.0001f)
            {
                __result = 0f;
                return false;
            }

            __instance.m_moveDir = flat;
            return true;
        }

        private static void Postfix(Character __instance, Vector3 __state)
        {
            if (TryGet(__instance, out var form) && form.Flying) __instance.m_moveDir = __state;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
    private static class Player_SetControls_Patch
    {
        private static void Prefix(Player __instance, ref bool jump, ref bool crouch, bool blockHold, bool dodge)
        {
            if (!TryGet(__instance, out var form)) return;

            if (form.Flying)
            {
                crouch = false;
            }

            var dodging = blockHold || __instance.m_blocking || __instance.IsCrouching() || __instance.m_crouchToggled || dodge;
            if (jump && !dodging)
            {
                if (!__instance.m_flying && !__instance.IsSwimming() && form.CanTakeOff) __instance.TakeOff();
                jump = false;
            }
        }

        private static void Postfix(Player __instance)
        {
            if (TryGet(__instance, out var form) && form.Flying)
            {
                form.UpdateFlightInput(CrouchHeld, JumpHeld);
            }
        }
    }
}
