using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Transformation;

public class GiantsBloodForm : MonoBehaviour
{
    public const string ZdoKey = "el-giant";

    private const float ReconcileInterval = 1f;

    private static int _blockMask;
    private static int BlockMask => _blockMask != 0 ? _blockMask : _blockMask =
        LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "vehicle");

    private static readonly Dictionary<Character, GiantsBloodForm> ActiveForms = new();

    public bool Active { get; private set; }
    public float Scale { get; private set; }

    private Player _player;
    private ZNetView _nview;
    private float _reconcileTimer;
    
    private Vector3 _savedRootScale;
    private float _savedHeight;
    private float _savedRadius;
    private Vector3 _savedCenter;

    private float _collisionScale = 1f;
    private float _extraMass;

    public static bool TryGet(Character character, out GiantsBloodForm form)
    {
        form = null;
        return character != null && ActiveForms.TryGetValue(character, out form);
    }

    public static float Of(Character character) =>
        TryGet(character, out var form) && form.Active ? form.Scale : 1f;

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

        if (Active) UpdateCollision();

        _reconcileTimer += Time.deltaTime;
        if (_reconcileTimer < ReconcileInterval) return;
        Refresh();
    }

    public void Refresh()
    {
        _reconcileTimer = 0f;
        if (_nview == null || !_nview.IsValid()) return;

        var zdo = _nview.GetZDO();
        if (_nview.IsOwner())
        {
            var giantScale = GiantScale();
            if (!Mathf.Approximately(giantScale, zdo.GetFloat(ZdoKey, 0f))) zdo.Set(ZdoKey, giantScale);
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
    private float GiantScale()
    {
        var value = GiantsBlood.GiantsBloodValue(_player);
        if (value <= 0f) return 0f;

        return 1f + value / 100f;
    }

    private void Apply(float scale)
    {
        if (!Active)
        {
            _savedRootScale = _player.transform.localScale;

            if (_player.m_collider != null)
            {
                _savedHeight = _player.m_collider.height;
                _savedRadius = _player.m_collider.radius;
                _savedCenter = _player.m_collider.center;
            }

            _collisionScale = 1f;
            Active = true;
            ActiveForms[_player] = this;
        }

        Scale = scale;

        // Held and worn items hang from bones under the root, so they scale with it and need nothing of their own.
        _player.transform.localScale = _savedRootScale * scale;

        // The collider keeps its current size (normal on the first application) and UpdateCollision grows it
        // once CanBeBig says there is room; growing it here would push the player into a low ceiling. It is
        // still set, since its local size is measured against the new root scale.
        SetCollisionScale(Small ? 1f : Mathf.Min(_collisionScale, scale));

        UpdateExtraMass();
    }

    private void Revert()
    {
        if (!Active) return;

        _player.transform.localScale = _savedRootScale;

        if (_player.m_collider != null)
        {
            _player.m_collider.height = _savedHeight;
            _player.m_collider.radius = _savedRadius;
            _player.m_collider.center = _savedCenter;
        }

        Active = false;
        Scale = 0f;
        _collisionScale = 1f;
        ActiveForms.Remove(_player);

        UpdateExtraMass();
    }

    // Mass grows with volume. Only the owner simulates its body, and the change is applied as a difference
    // rather than through SetExtraMass(0), which would drop the pull mass a cart adds while attached.
    private void UpdateExtraMass()
    {
        if (!_nview.IsOwner() || _player.m_body == null) return;

        var extraMass = Active ? _player.m_originalMass * (Mathf.Pow(Scale, 3) - 1f) : 0f;
        _player.m_body.mass += extraMass - _extraMass;
        _extraMass = extraMass;
    }

    private bool Small => _player.IsCrouching() || _player.InDodge() || !_player.IsOnGround();

    private void UpdateCollision()
    {
        float needSmall = Small ? 1f : Scale;
        if (Mathf.Approximately(needSmall, _collisionScale)) return;

        if (needSmall > _collisionScale && !CanBeBig(needSmall)) return;

        SetCollisionScale(needSmall);
    }

    private void SetCollisionScale(float s)
    {
        if (_player.m_collider == null || Scale <= 0f) return;

        var local = s / Scale;
        _player.m_collider.height = _savedHeight * local;
        _player.m_collider.radius = _savedRadius * local;
        _player.m_collider.center = _savedCenter * local;

        _collisionScale = s;
    }

    private bool CanBeBig(float s)
    {
        if (_player.m_collider == null) return false;

        var radius = _savedRadius * s;
        var halfSpan = Mathf.Max(0f, _savedHeight * s * 0.5f - radius);
        var center = _player.transform.position + _player.transform.rotation * (_savedCenter * s);
        var up = _player.transform.up * halfSpan;

        return !Physics.CheckCapsule(center - up, center + up, radius * 0.95f, BlockMask,
            QueryTriggerInteraction.Ignore);
    }

    [HarmonyPatch(typeof(Player), nameof(Player.Awake))]
    private static class Player_Awake_Patch
    {
        private static void Postfix(Player __instance) => __instance.gameObject.AddComponent<GiantsBloodForm>();
    }

    // Vanilla's SetExtraMass (a cart attaching or detaching) rewrites the mass from the original, so put the
    // giant's share back on top.
    [HarmonyPatch(typeof(Character), nameof(Character.SetExtraMass))]
    private static class Character_SetExtraMass_Patch
    {
        private static void Postfix(Character __instance)
        {
            if (TryGet(__instance, out var form) && __instance.m_body != null)
            {
                __instance.m_body.mass += form._extraMass;
            }
        }
    }
}
