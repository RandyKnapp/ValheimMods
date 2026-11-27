using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace EpicLoot.Compendium;

public static class MagicFontManager
{
    public enum FontOptions
    {
        [InternalName("AveriaSerifLibre-Regular")] AveriaSerifLibre
    }

    // Each entry pairs a font asset with one of ITS OWN material variants. Pairing a font with another
    // font's material is the bug this file exists to prevent -- see Apply below.
    public enum TMP_FontOptions
    {
        [TMP_Attributes("Valheim-Norsebold", "Valheim-Norsebold - Outline")] NorseBoldOutline,
        [TMP_Attributes("Valheim-AveriaSansLibre", "Valheim-AveriaSansLibre")] AveriaSansLibre,
        [TMP_Attributes("Valheim-AveriaSansLibre", "Valheim-AveriaSansLibre - Outline")] AveriaSansLibreOutline,
        [TMP_Attributes("Valheim-AveriaSerifLibre", "Valheim-AveriaSerifLibre - Outline")] AveriaSerifLibreOutline,
        [TMP_Attributes("Valheim-AveriaSerifLibre", "Valheim-AveriaSerifLibre")] AveriaSerifLibre,
    }

    private class InternalName(string internalName) : Attribute
    {
        public readonly string internalName = internalName;
    }

    private class TMP_Attributes(string fontName, string materialName) : Attribute
    {
        public readonly string fontName = fontName;
        public readonly string materialName = materialName;
    }

    private static readonly Dictionary<FontOptions, Font> m_fonts = new();
    private static readonly Dictionary<TMP_FontOptions, TMP_FontData> m_fontAssets = new();

    private static readonly HashSet<string> _warnedLookups = new HashSet<string>();
    private static readonly HashSet<string> _warnedAtlases = new HashSet<string>();

    public static Font GetFont(FontOptions option)
    {
        if (m_fonts.TryGetValue(option, out Font font))
        {
            return font;
        }

        string name = option.GetAttributeOfType<InternalName>().internalName;
        Font match = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(x => x.name == name);

        // A miss is not cached: FindObjectsOfTypeAll only sees loaded objects, so a lookup that runs
        // before the asset is loaded would otherwise stay unresolved for the whole session.
        if (match != null)
        {
            m_fonts[option] = match;
        }

        return match;
    }

    public static TMP_FontData GetTMPFont(TMP_FontOptions option)
    {
        if (!m_fontAssets.TryGetValue(option, out TMP_FontData data))
        {
            ResolvePendingOptions();
            data = m_fontAssets[option];
        }

        return data;
    }

    // Resolves every uncached option at once: the enchanting window wakes four of them in its spawn frame.
    // The slow part of a scan is Resources.FindObjectsOfTypeAll<TMP_FontAsset>, about 20 ms per call with a
    // large mod list, because a script type is matched against every loaded MonoBehaviour and
    // ScriptableObject (every material comes back in about 1 ms). So vanilla's own GUI texts are read first:
    // they reference the very font assets and presets vanilla renders with.
    private static void ResolvePendingOptions()
    {
        List<TMP_FontOptions> pending = Enum.GetValues(typeof(TMP_FontOptions)).Cast<TMP_FontOptions>()
            .Where(x => !m_fontAssets.ContainsKey(x))
            .ToList();
        HashSet<string> names = new HashSet<string>(pending
            .Select(x => x.GetAttributeOfType<TMP_Attributes>())
            .SelectMany(x => new[] { x.fontName, x.materialName }));

        HashSet<TMP_FontAsset> guiFonts = new HashSet<TMP_FontAsset>();
        HashSet<Material> guiMaterials = new HashSet<Material>();
        CollectVanillaGuiFonts(guiFonts, guiMaterials);
        Dictionary<string, List<TMP_FontAsset>> fonts = ByName(guiFonts, names);
        Dictionary<string, List<Material>> materials = ByName(guiMaterials, names);

        for (int i = pending.Count - 1; i >= 0; i--)
        {
            TMP_FontData data = PickFontAndMaterial(pending[i].GetAttributeOfType<TMP_Attributes>(), fonts, materials);
            if (data.font != null && data.font.material != null && IsOnFontAtlas(data.font, data.material))
            {
                m_fontAssets[pending[i]] = data;
                pending.RemoveAt(i);
            }
        }

        if (pending.Count == 0)
        {
            return;
        }

        EpicLoot.Log($"[fonts] {string.Join(", ", pending)} not found in vanilla's GUI, scanning loaded assets.");
        fonts = ByName(Resources.FindObjectsOfTypeAll<TMP_FontAsset>(), names);
        materials = ByName(Resources.FindObjectsOfTypeAll<Material>(), names);

        foreach (TMP_FontOptions option in pending)
        {
            TMP_Attributes attributes = option.GetAttributeOfType<TMP_Attributes>();
            int fontCount = fonts.TryGetValue(attributes.fontName, out List<TMP_FontAsset> named) ? named.Count : 0;
            TMP_FontData data = PickFontAndMaterial(attributes, fonts, materials);

            if (data.font == null && fontCount > 0)
            {
                WarnOnce(_warnedLookups, $"{attributes.fontName}|{attributes.materialName}",
                    $"{option}: {fontCount} font asset(s) named '{attributes.fontName}' are loaded, but none has a " +
                    $"default material, so TextMeshPro cannot render with any of them. Another mod most likely ships " +
                    $"a broken copy of this font. Keeping the panel's own font.");
            }
            else if (data.font == null || data.material == null)
            {
                WarnOnce(_warnedLookups, $"{attributes.fontName}|{attributes.materialName}",
                    $"{option}: font '{attributes.fontName}' {(data.font == null ? "NOT FOUND" : "ok")}, " +
                    $"material '{attributes.materialName}' {(data.material == null ? "NOT FOUND" : "ok")}");
            }

            // A partial resolve is still cached, or every text using the option would scan again.
            // RetryFailedLookups drops these entries, which is where a "not loaded yet" miss gets its
            // second chance.
            m_fontAssets[option] = data;
        }
    }

    // The in-game inventory and HUD and the main menu between them use all three faces with every preset
    // listed above. A font's own default material is added too: the plain options name it, and no text
    // may happen to use it.
    private static void CollectVanillaGuiFonts(HashSet<TMP_FontAsset> fonts, HashSet<Material> materials)
    {
        foreach (Component root in new Component[] { InventoryGui.instance, Hud.instance, FejdStartup.instance })
        {
            if (root == null)
            {
                continue;
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.font != null && fonts.Add(text.font) && text.font.material != null)
                {
                    materials.Add(text.font.material);
                }

                if (text.fontSharedMaterial != null)
                {
                    materials.Add(text.fontSharedMaterial);
                }
            }
        }
    }

    // Reads each object's name once; a name read allocates, and the fallback scan reads thousands.
    private static Dictionary<string, List<T>> ByName<T>(IEnumerable<T> objects, HashSet<string> names)
        where T : UnityEngine.Object
    {
        Dictionary<string, List<T>> found = new Dictionary<string, List<T>>();
        foreach (T obj in objects)
        {
            if (obj == null)
            {
                continue;
            }

            string name = obj.name;
            if (!names.Contains(name))
            {
                continue;
            }

            if (!found.TryGetValue(name, out List<T> list))
            {
                found[name] = list = new List<T>();
            }

            list.Add(obj);
        }

        return found;
    }

    // Other mods' asset bundles often carry their own copies of vanilla's fonts and materials under the
    // same names, and vanilla itself loads two "Valheim-Norsebold - Outline" materials on different
    // atlases. So a name only narrows the candidates, and the first match can be a copy TMP cannot use:
    // a font asset whose default material is missing throws from MaterialReference's constructor on every
    // rebuild of every text using it, whatever material the text itself is given.
    private static TMP_FontData PickFontAndMaterial(TMP_Attributes attributes,
        Dictionary<string, List<TMP_FontAsset>> fonts, Dictionary<string, List<Material>> materials)
    {
        List<TMP_FontAsset> usable = fonts.TryGetValue(attributes.fontName, out List<TMP_FontAsset> named)
            ? named.Where(x => x.material != null).ToList()
            : new List<TMP_FontAsset>();
        if (!materials.TryGetValue(attributes.materialName, out List<Material> candidates))
        {
            candidates = new List<Material>();
        }

        foreach (TMP_FontAsset font in usable)
        {
            Material onAtlas = candidates.FirstOrDefault(x => IsOnFontAtlas(font, x));
            if (onAtlas != null)
            {
                return new TMP_FontData { font = font, material = onAtlas };
            }
        }

        // No named material samples a usable font's atlas: Apply falls back to the font's own material.
        return new TMP_FontData { font = usable.FirstOrDefault(), material = candidates.FirstOrDefault() };
    }

    private static bool IsOnFontAtlas(TMP_FontAsset font, Material material)
    {
        // atlasTexture dereferences atlasTextures[0] unguarded.
        if (font == null || material == null || font.atlasTextures == null || font.atlasTextures.Length == 0)
        {
            return false;
        }

        // Public and idempotent; TMP calls it the same way before reading the cached property ids.
        ShaderUtilities.GetShaderPropertyIDs();
        if (!material.HasProperty(ShaderUtilities.ID_MainTex))
        {
            return false;
        }

        Texture materialAtlas = material.GetTexture(ShaderUtilities.ID_MainTex);
        Texture fontAtlas = font.atlasTexture;
        return materialAtlas != null && fontAtlas != null &&
               materialAtlas.GetInstanceID() == fontAtlas.GetInstanceID();
    }

    // Drops half-resolved entries so the next call looks again. A lookup that ran before vanilla's GUI or
    // the asset was loaded would otherwise stay unresolved for the session. Called once per compendium page
    // open and per temper panel build, rather than per line.
    public static void RetryFailedLookups()
    {
        List<TMP_FontOptions> failed = m_fontAssets
            .Where(x => x.Value?.font == null || x.Value.font.material == null || x.Value.material == null)
            .Select(x => x.Key)
            .ToList();

        foreach (TMP_FontOptions option in failed)
        {
            m_fontAssets.Remove(option);
        }
    }

    // TextMeshProUGUI.LoadFontAsset() only keeps a shared material whose _MainTex is the font asset's
    // current atlas texture, and falls back to the font's own material otherwise. The
    // fontSharedMaterial setter does no such check, and TMP_Text.font early-returns when the asset is
    // unchanged -- so a material assigned to a live component is never validated. Repeating TMP's check
    // here means a font paired with another font's material degrades to unstyled text instead of
    // rendering as garbage. Compendium group headings are the lines that swap material after the
    // element is live, so they are the ones TMP never validates.
    // Returns false when no usable font asset could be resolved, so a caller that latches a
    // "fonts loaded" flag can retry on its next open instead of keeping the default font forever.
    public static bool Apply(TMP_Text text, TMP_FontOptions option)
    {
        if (text == null)
        {
            return false;
        }

        // A font without a default material is never assigned (see PickFontAndMaterial): the text keeps
        // the font it already renders with. Checked again here in case the material was destroyed after
        // the lookup was cached.
        TMP_FontData data = GetTMPFont(option);
        if (data?.font == null || data.font.material == null)
        {
            return false;
        }

        text.font = data.font;
        text.fontSharedMaterial = MaterialForCurrentAtlas(data.font, data.material, option);
        return true;
    }

    public static bool ApplyAll(GameObject root, Func<TMP_Text, TMP_FontOptions> select)
    {
        bool applied = true;
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            applied &= Apply(text, select(text));
        }

        return applied;
    }

    private static Material MaterialForCurrentAtlas(TMP_FontAsset font, Material material, TMP_FontOptions option)
    {
        if (material == null)
        {
            return font.material;
        }

        if (IsOnFontAtlas(font, material))
        {
            return material;
        }

        WarnOnce(_warnedAtlases, $"{font.name}|{material.name}",
            $"{option}: material '{material.name}' is not on the current atlas of font '{font.name}'. " +
            $"{Describe(font, material)} Using the font's own material.");
        return font.material;
    }

    // The state that decides how a glyph is sampled, in one line, so the warning above says enough to
    // diagnose a recurrence from a player's log alone.
    private static string Describe(TMP_FontAsset font, Material material)
    {
        ShaderUtilities.GetShaderPropertyIDs();

        Texture fontAtlas = font == null || font.atlasTextures == null || font.atlasTextures.Length == 0
            ? null
            : font.atlasTexture;
        Texture materialAtlas = material == null ? null : material.GetTexture(ShaderUtilities.ID_MainTex);

        string fontPart = font == null
            ? "font=<null>"
            : $"font atlas id={(fontAtlas == null ? 0 : fontAtlas.GetInstanceID())} " +
              $"{font.atlasWidth}x{font.atlasHeight} actual={(fontAtlas == null ? "none" : $"{fontAtlas.width}x{fontAtlas.height}")} " +
              $"textures={font.atlasTextures.Length} mode={font.atlasPopulationMode}";

        if (material == null)
        {
            return $"{fontPart}, material=<null>";
        }

        return $"{fontPart}, material atlas id={(materialAtlas == null ? 0 : materialAtlas.GetInstanceID())} " +
               $"_TextureWidth={Prop(material, ShaderUtilities.ID_TextureWidth)} " +
               $"_TextureHeight={Prop(material, ShaderUtilities.ID_TextureHeight)} " +
               $"_GradientScale={Prop(material, ShaderUtilities.ID_GradientScale)} " +
               $"_ScaleRatioA={Prop(material, ShaderUtilities.ID_ScaleRatio_A)} " +
               $"shader='{material.shader?.name}'";
    }

    private static string Prop(Material material, int id) =>
        material.HasProperty(id) ? material.GetFloat(id).ToString("0.###") : "n/a";

    private static void WarnOnce(HashSet<string> seen, string key, string message)
    {
        if (seen.Add(key))
        {
            // Ungated on purpose: this is the diagnostic a player's log needs to explain unreadable text.
            EpicLoot.LogWarningForce($"[fonts] {message}");
        }
    }

    public class TMP_FontData
    {
        public TMP_FontAsset font;
        public Material material;
    }
}
