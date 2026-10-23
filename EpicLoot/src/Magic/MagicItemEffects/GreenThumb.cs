using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

// Green Thumb: seeds and saplings the player plants take less time to grow.
//
// The planter's value is stamped on the plant's ZDO the moment it is placed. Piece.SetCreator runs on the
// planter's client, which owns the freshly instantiated object, so that is the one place the planter's gear
// is both known and writable. From then on the bonus belongs to the plant: it survives the planter
// unequipping, logging off or walking away, and it applies on whichever machine later owns the plant and
// calls Grow() -- another player or a dedicated server, neither of which could see the planter's gear.
// Every client scales Plant.GetGrowTime from the same stamp, so the half-grown model swap agrees too.
public static class GreenThumb
{
    // Plant-ZDO key: the planter's GreenThumb value (a percentage) when the plant went in.
    public const string ZdoKey = "el-gt";
    private static readonly int ZdoKeyHash = ZdoKey.GetStableHashCode();

    // Cap on the grow-time reduction, in percent. Read live rather than applied when stamping, so a retune
    // reaches plants already in the ground.
    private const string MaxKey = "Max";
    public const float DefaultMax = 75f;

    // Whatever the config says, a plant never grows instantly.
    private const float HardMax = 95f;

    private const string VfxName = "vfx_el_green_thumb";
    private const string VfxSourcePrefab = "Pickable_Mushroom_Magecap";
    private const string VfxSourcePath = "visual/Particle System";
    private static readonly Color VfxTint = new Color(0.6f, 1f, 0.3f, 1f);

    // The Magecap emitter is a 0.67 m sphere; at the plant's root half of it would be underground.
    private static readonly Vector3 VfxOffset = new Vector3(0f, 0.5f, 0f);

    private static GameObject _vfxSource;
    private static Material _vfxMaterial;
    private static bool _vfxSourceMissingLogged;

    // Fraction of the grow time removed, 0 for a plant sown without Green Thumb.
    public static float GetGrowTimeReduction(Plant plant)
    {
        var percent = GetStampedValue(plant);
        if (percent <= 0f)
        {
            return 0f;
        }

        var max = EffectConfig.GetClamped(MagicEffectType.GreenThumb, MaxKey, DefaultMax, 0f, HardMax);
        return Mathf.Min(percent, max) * 0.01f;
    }

    private static float GetStampedValue(Plant plant)
    {
        if (plant == null || plant.m_nview == null || !plant.m_nview.IsValid())
        {
            return 0f;
        }

        return plant.m_nview.GetZDO().GetFloat(ZdoKeyHash);
    }

    // Runs every slow-update pass for every loaded plant on every machine, so it stays a single ZDO read
    // for plants without the stamp.
    [HarmonyPatch(typeof(Plant), nameof(Plant.GetGrowTime))]
    private static class Plant_GetGrowTime_Patch
    {
        private static void Postfix(Plant __instance, ref float __result)
        {
            var reduction = GetGrowTimeReduction(__instance);
            if (reduction > 0f)
            {
                __result *= 1f - reduction;
            }
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    private static class Piece_SetCreator_Patch
    {
        // Vanilla keeps only the first creator. Mirror that, so a later SetCreator call from another mod
        // never re-stamps an existing plant with whatever the caller happens to be wearing.
        private static void Prefix(Piece __instance, out bool __state)
        {
            __state = __instance.GetCreator() == 0L;
        }

        private static void Postfix(Piece __instance, long uid, bool __state)
        {
            var player = Player.m_localPlayer;
            if (!__state || player == null || uid != player.GetPlayerID())
            {
                return;
            }

            var nview = __instance.m_nview;
            if (nview == null || !nview.IsValid() || !nview.IsOwner())
            {
                return;
            }

            var plant = __instance.GetComponent<Plant>();
            if (plant == null)
            {
                return;
            }

            if (!player.HasActiveMagicEffect(MagicEffectType.GreenThumb, out float value) || value <= 0f)
            {
                return;
            }

            nview.GetZDO().Set(ZdoKeyHash, value);
            AttachVfx(plant);
        }
    }

    // Plant.Awake runs inside Instantiate, before SetCreator, so on the planter's client the stamp is not
    // there yet and the SetCreator postfix attaches the effect instead. This covers everyone else: other
    // clients receiving the plant, and every machine loading it again later.
    [HarmonyPatch(typeof(Plant), nameof(Plant.Awake))]
    private static class Plant_Awake_Patch
    {
        private static void Postfix(Plant __instance)
        {
            if (GetStampedValue(__instance) > 0f)
            {
                AttachVfx(__instance);
            }
        }
    }

    private static void AttachVfx(Plant plant)
    {
        if (ZNet.instance == null || ZNet.instance.IsDedicated())
        {
            return;
        }

        var parent = plant.transform;
        if (parent.Find(VfxName) != null)
        {
            return;
        }

        var source = GetVfxSource();
        if (source == null)
        {
            return;
        }

        var vfx = Object.Instantiate(source, parent, false);
        vfx.name = VfxName;
        vfx.transform.localPosition = VfxOffset;
        vfx.transform.localRotation = Quaternion.identity;

        // One shared tinted material for every plant. Assigning renderer.material would instead leak a
        // material instance per plant, since nothing destroys it when the plant grows.
        if (_vfxMaterial != null && vfx.TryGetComponent(out ParticleSystemRenderer renderer))
        {
            renderer.sharedMaterial = _vfxMaterial;
        }

        // The Magecap emitter always simulates, even off screen. A field of crops has far more of these than
        // a Mistlands clearing has mushrooms, so only simulate the ones in view.
        if (vfx.TryGetComponent(out ParticleSystem particles))
        {
            var main = particles.main;
            main.cullingMode = ParticleSystemCullingMode.Pause;
        }
    }

    private static GameObject GetVfxSource()
    {
        if (_vfxSource != null)
        {
            return _vfxSource;
        }

        var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(VfxSourcePrefab) : null;
        var source = prefab != null ? prefab.transform.Find(VfxSourcePath) : null;
        if (source == null)
        {
            if (!_vfxSourceMissingLogged)
            {
                _vfxSourceMissingLogged = true;
                EpicLoot.LogWarning($"[GreenThumb] {VfxSourcePrefab}/{VfxSourcePath} not found; plants will grow faster without the visual effect.");
            }

            return null;
        }

        _vfxSource = source.gameObject;

        var sourceRenderer = source.GetComponent<ParticleSystemRenderer>();
        if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
        {
            _vfxMaterial = new Material(sourceRenderer.sharedMaterial) { name = "el_green_thumb_particle" };
            if (_vfxMaterial.HasProperty("_Color"))
            {
                _vfxMaterial.color = VfxTint;
            }

            if (_vfxMaterial.HasProperty("_EmissionColor"))
            {
                _vfxMaterial.SetColor("_EmissionColor", VfxTint);
            }
        }

        return _vfxSource;
    }
}
