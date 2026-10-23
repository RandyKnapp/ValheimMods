using HarmonyLib;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace EpicLoot.LegendarySystem
{
    public static class SetArmorTint
    {
        private const string ZdoKey = "el-tint";

        private static readonly int MainTexID = Shader.PropertyToID("_MainTex");
        private static readonly int ChestTexID = Shader.PropertyToID("_ChestTex");
        private static readonly int LegsTexID = Shader.PropertyToID("_LegsTex");
        private static readonly int[] TextureSlots = { MainTexID, ChestTexID, LegsTexID };

        // Tinted copy -> the texture it was made from, and the copies made so far by source and tint.
        private static readonly Dictionary<Texture, Texture> TintedSources = new Dictionary<Texture, Texture>();
        private static readonly Dictionary<string, Texture2D> TintedCopies = new Dictionary<string, Texture2D>();

        private class TintState
        {
            public string SetID = "";
            public int OverrideVersion;
            public bool Dirty;
            public bool Tinted;
        }

        private static readonly ConditionalWeakTable<VisEquipment, TintState> States = new ConditionalWeakTable<VisEquipment, TintState>();

        // Local-only preview for the settint console command; applied to the local player in place of any set tint.
        public static ArmorTintInfo Override { get; private set; }
        private static int _overrideVersion;

        public static void SetOverride(ArmorTintInfo tint)
        {
            Override = tint;
            _overrideVersion++;
        }

        public static void Publish(Player player)
        {
            if (player == null || player != Player.m_localPlayer || player.m_nview == null ||
                !(player.m_nview.GetZDO() is ZDO zdo))
            {
                return;
            }

            string setID = "";
            foreach (LegendarySetProgress progress in SetBonusEvaluator.GetVisibleSetProgress(player))
            {
                if (progress.IsFull && progress.Set.ArmorTint != null)
                {
                    setID = progress.Set.ID;
                    break;
                }
            }

            if (zdo.GetString(ZdoKey) != setID)
            {
                zdo.Set(ZdoKey, setID);
            }
        }

        private static void Refresh(VisEquipment vis)
        {
            if (vis == null || !vis.m_isPlayer || vis.m_isArmorStand || vis.m_nview == null ||
                !(vis.m_nview.GetZDO() is ZDO zdo))
            {
                return;
            }

            string setID = zdo.GetString(ZdoKey);
            bool local = Player.m_localPlayer != null && vis.gameObject == Player.m_localPlayer.gameObject;
            int overrideVersion = local ? _overrideVersion : 0;
            TintState state = States.GetOrCreateValue(vis);
            if (state.SetID == setID && state.OverrideVersion == overrideVersion && !state.Dirty)
            {
                return;
            }

            state.Dirty = false;
            state.SetID = setID;
            state.OverrideVersion = overrideVersion;

            ArmorTintInfo tint = local ? Override : null;
            if (tint == null && !string.IsNullOrEmpty(setID) && UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out LegendarySetInfo set))
            {
                tint = set.ArmorTint;
            }

            // Nothing to apply and nothing of ours to undo, so leave the materials alone. This is also what keeps
            // non-player users of the player rig out (the Deep North Shadow): they never carry a set ID.
            if (tint == null && !state.Tinted)
            {
                return;
            }

            state.Tinted = tint != null;
            string key = tint == null ? null : TintKey(tint);

            // Chest and leg items paint their cloth into the body material's _ChestTex/_LegsTex, which the
            // Custom/Player shader cannot recolour, so every slot is swapped for a tinted copy instead.
            if (vis.m_bodyModel != null)
            {
                Material body = vis.m_bodyModel.material;
                TintSlot(body, ChestTexID, vis.m_emptyBodyTexture, tint, key);
                TintSlot(body, LegsTexID, vis.m_emptyLegsTexture, tint, key);
            }

            foreach (Renderer renderer in ArmorRenderers(vis))
            {
                // Only tinting needs the renderer's own material copies; a tinted texture to undo can only sit on a
                // copy made here, which sharedMaterials already returns.
                foreach (Material material in tint != null ? renderer.materials : renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    foreach (int slot in TextureSlots)
                    {
                        TintSlot(material, slot, null, tint, key);
                    }
                }
            }
        }

        private static string TintKey(ArmorTintInfo tint)
        {
            return $"{tint.Color}|{tint.Strength:0.###}|{tint.Hue:0.###}|{tint.Saturation:0.###}|{tint.Value:0.###}";
        }

        private static void TintSlot(Material material, int texID, Texture empty, ArmorTintInfo tint, string key)
        {
            // GetTexture logs a Unity error for a slot the shader lacks.
            if (!material.HasProperty(texID))
            {
                return;
            }

            Texture current = material.GetTexture(texID);
            if (current == null)
            {
                return;
            }

            Texture source = TintedSources.TryGetValue(current, out Texture original) ? original : current;
            if (tint == null || source == empty)
            {
                if (current != source)
                {
                    material.SetTexture(texID, source);
                }

                return;
            }

            string copyKey = $"{source.GetInstanceID()}|{key}";
            if (!TintedCopies.TryGetValue(copyKey, out Texture2D tinted) || tinted == null)
            {
                tinted = MakeTintedCopy(source, tint);
                TintedCopies[copyKey] = tinted;
                TintedSources[tinted] = source;
            }

            if (current != tinted)
            {
                material.SetTexture(texID, tinted);
            }
        }

        private static Texture2D MakeTintedCopy(Texture source, ArmorTintInfo tint)
        {
            RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, true, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);

            Color target = Color.white;
            bool colorize = !string.IsNullOrEmpty(tint.Color) && ColorUtility.TryParseHtmlString(tint.Color, out target);
            Color.RGBToHSV(target, out float targetHue, out float targetSaturation, out _);

            float strength = Mathf.Clamp01(tint.Strength);
            float hueShift = Mathf.Clamp(tint.Hue, -0.5f, 0.5f);
            float saturationShift = Mathf.Clamp(tint.Saturation, -1f, 1f);
            float valueShift = Mathf.Clamp(tint.Value, -1f, 1f);

            Color32[] pixels = copy.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                Color.RGBToHSV(pixel, out float h, out float s, out float v);
                if (colorize)
                {
                    h = Mathf.Repeat(h + Mathf.DeltaAngle(h * 360f, targetHue * 360f) / 360f * strength, 1f);
                    s = Mathf.Lerp(s, targetSaturation, strength);
                }

                Color adjusted = Color.HSVToRGB(Mathf.Repeat(h + hueShift, 1f), Mathf.Clamp01(s + saturationShift), Mathf.Clamp01(v + valueShift));
                pixels[i] = new Color32((byte)Mathf.RoundToInt(adjusted.r * 255f), (byte)Mathf.RoundToInt(adjusted.g * 255f),
                    (byte)Mathf.RoundToInt(adjusted.b * 255f), pixel.a);
            }

            copy.SetPixels32(pixels);
            copy.Apply(true, false);
            if (copy.width % 4 == 0 && copy.height % 4 == 0)
            {
                copy.Compress(true);
            }

            copy.Apply(false, true);
            copy.name = source.name + "_tinted";
            copy.wrapMode = source.wrapMode;
            copy.filterMode = source.filterMode;
            copy.anisoLevel = source.anisoLevel;
            return copy;
        }

        private static IEnumerable<Renderer> ArmorRenderers(VisEquipment vis)
        {
            foreach (GameObject instance in Instances(vis))
            {
                if (instance == null)
                {
                    continue;
                }

                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(renderer is ParticleSystemRenderer))
                    {
                        yield return renderer;
                    }
                }
            }
        }

        private static IEnumerable<GameObject> Instances(VisEquipment vis)
        {
            if (vis.m_helmetItemInstance != null)
            {
                yield return vis.m_helmetItemInstance;
            }

            foreach (List<GameObject> list in new[] { vis.m_chestItemInstances, vis.m_legItemInstances, vis.m_shoulderItemInstances, vis.m_utilityItemInstances })
            {
                if (list == null)
                {
                    continue;
                }

                foreach (GameObject instance in list)
                {
                    yield return instance;
                }
            }
        }

        private static void MarkDirty(VisEquipment vis, bool changed)
        {
            if (changed && vis != null)
            {
                States.GetOrCreateValue(vis).Dirty = true;
            }
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.UpdateEquipmentVisuals))]
        private static class VisEquipment_UpdateEquipmentVisuals_Patch
        {
            private static void Postfix(VisEquipment __instance) => Refresh(__instance);
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetChestEquipped))]
        private static class VisEquipment_SetChestEquipped_Patch
        {
            private static void Postfix(VisEquipment __instance, bool __result) => MarkDirty(__instance, __result);
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetLegEquipped))]
        private static class VisEquipment_SetLegEquipped_Patch
        {
            private static void Postfix(VisEquipment __instance, bool __result) => MarkDirty(__instance, __result);
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetHelmetEquipped))]
        private static class VisEquipment_SetHelmetEquipped_Patch
        {
            private static void Postfix(VisEquipment __instance, bool __result) => MarkDirty(__instance, __result);
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetShoulderEquipped))]
        private static class VisEquipment_SetShoulderEquipped_Patch
        {
            private static void Postfix(VisEquipment __instance, bool __result) => MarkDirty(__instance, __result);
        }

        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.SetUtilityEquipped))]
        private static class VisEquipment_SetUtilityEquipped_Patch
        {
            private static void Postfix(VisEquipment __instance, bool __result) => MarkDirty(__instance, __result);
        }
    }
}
