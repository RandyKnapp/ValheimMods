using EpicLoot.MagicItemEffects;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EpicLoot.LegendarySystem
{
    // The arrow a set lays on the string while its wearer draws a bow (DrawArrow in legendaries.json), built once per
    // distinct config: a copy of a vanilla arrow's mesh with its own materials (the conjured frost arrow already glows),
    // the upgrade glow's particles and an optional accent along its length, and a light at its middle. SetVisuals places
    // the root on the drawing hand facing the bow hand; the mesh sits on the root's forward axis with its back end at the
    // root, scaled to Length.
    internal static class SetDrawArrow
    {
        private const string LogTag = "SetVisuals";

        private static readonly Dictionary<string, GameObject> Templates = new Dictionary<string, GameObject>();
        private static readonly HashSet<string> NoMeshLogged = new HashSet<string>();

        // Config values are player-editable, so each is kept in range here.
        internal static GameObject GetTemplate(DrawArrowInfo info, Transform holder)
        {
            Color color = SetVisuals.ParseColor(info.Color, Color.white);
            Color accentColor = SetVisuals.ParseColor(info.AccentColor, Color.clear);
            float alpha = Mathf.Clamp(info.Alpha, 0.02f, 1f);
            float rate = Mathf.Clamp(info.Rate, 0f, 200f);
            float size = Mathf.Clamp(info.Size, 0.1f, 4f);
            float length = info.Length > 0f ? Mathf.Clamp(info.Length, 0.2f, 3f) : 0f;
            float accentScale = Mathf.Clamp(info.AccentScale, 0.05f, 5f);
            float lightIntensity = Mathf.Clamp(info.LightIntensity, 0f, 4f);
            float lightRange = Mathf.Clamp(info.LightRange, 0f, 10f);

            string key = string.Join("|", info.Model, ColorUtility.ToHtmlStringRGBA(color), ColorUtility.ToHtmlStringRGBA(accentColor),
                alpha, rate, size, length, info.Accent, accentScale, lightIntensity, lightRange);
            if (Templates.TryGetValue(key, out GameObject template) && template != null)
            {
                return template;
            }

            if (ZNetScene.instance == null)
            {
                return null;
            }

            template = Build(info, holder, color, accentColor, alpha, rate, size, length, accentScale, lightIntensity, lightRange);
            if (template != null)
            {
                Templates[key] = template;
            }
            return template;
        }

        private static GameObject Build(DrawArrowInfo info, Transform holder, Color color, Color accentColor, float alpha,
            float rate, float size, float length, float accentScale, float lightIntensity, float lightRange)
        {
            Transform source = WeaponAuraKit.FindSource(info.Model, LogTag);
            MeshFilter sourceFilter = source != null ? source.GetComponent<MeshFilter>() : null;
            MeshRenderer sourceRenderer = source != null ? source.GetComponent<MeshRenderer>() : null;
            Mesh mesh = sourceFilter != null ? sourceFilter.sharedMesh : null;
            if (source != null && (mesh == null || sourceRenderer == null))
            {
                if (NoMeshLogged.Add(info.Model))
                {
                    EpicLoot.LogWarning($"[{LogTag}] '{info.Model}' has no mesh to show as a drawn arrow; the set shows none.");
                }
            }
            if (mesh == null || sourceRenderer == null)
            {
                return null;
            }

            // Inactive, so a copy only plays once it has been placed on the hand.
            var root = new GameObject("EL_SetDrawArrow");
            root.SetActive(false);
            root.transform.SetParent(holder, false);

            var model = new GameObject("Model");
            model.layer = source.gameObject.layer;
            model.transform.SetParent(root.transform, false);
            model.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = model.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = sourceRenderer.sharedMaterials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            // The source's pose under its own root keeps its point on the root's forward (a projectile flies along its
            // root's forward). Then: scaled to length, centred on the axis, back end on the root.
            model.transform.localRotation = source.localRotation;
            model.transform.localScale = source.localScale;
            Bounds placed = PlacedBounds(mesh.bounds, model.transform);
            if (length > 0f && placed.size.z > 0.001f)
            {
                model.transform.localScale = source.localScale * (length / placed.size.z);
                placed = PlacedBounds(mesh.bounds, model.transform);
            }
            model.transform.localPosition = new Vector3(-placed.center.x, -placed.center.y, -placed.min.z);

            // The particles emit from the mesh's box, so they lie along the shaft and follow it.
            var fit = new WeaponAuraKit.Fit
            {
                Parent = model.transform,
                Type = ParticleSystemShapeType.Box,
                Position = mesh.bounds.center,
                Scale = mesh.bounds.size,
                Center = mesh.bounds.center,
            };

            ParticleSystem glowSource = rate > 0f ? WeaponAuraKit.FindGlowSource() : null;
            if (glowSource != null)
            {
                fit.Apply(WeaponAuraKit.AddGlow(glowSource, model.transform, color, alpha, 0.1f * size, 0.28f * size, rate, 0.02f));
            }

            if (!string.IsNullOrEmpty(info.Accent))
            {
                Transform branch = WeaponAuraKit.FindSource(info.Accent, LogTag);
                bool includeRoot = branch != null && branch.root.GetComponent<ItemDrop>() == null;
                foreach (ParticleSystem accent in WeaponAuraKit.CopyBranch(branch, model.transform, accentScale, includeRoot))
                {
                    if (accentColor.a > 0f)
                    {
                        WeaponAuraKit.Tint(accent, accentColor, Color.Lerp(accentColor, Color.white, 0.3f));
                    }
                    fit.Apply(accent);
                }
            }

            if (lightIntensity > 0f && lightRange > 0f)
            {
                Light light = WeaponAuraKit.AddLight(model.transform, color, lightIntensity, lightRange);
                light.transform.localPosition = fit.Center;
            }

            return root;
        }

        // The mesh's box as it sits under its parent, given the model's rotation and scale (its position still zero).
        private static Bounds PlacedBounds(Bounds meshBounds, Transform model)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(Vector3.zero, model.localRotation, model.localScale);
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = meshBounds.center + Vector3.Scale(meshBounds.extents,
                    new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 point = matrix.MultiplyPoint3x4(corner);
                min = Vector3.Min(min, point);
                max = Vector3.Max(max, point);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }
    }
}
