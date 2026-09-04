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
        [InternalName("Norse")] Norse,
        [InternalName("Norsebold")] NorseBold,
        [InternalName("AveriaSerifLibre-Regular")] AveriaSerifLibre,
        [InternalName("AveriaSerifLibre-Bold")] AveriaSerifLibreBold,
        [InternalName("AveriaSerifLibre-Light")] AveriaSerifLibreLight,
        [InternalName("LegacyRuntime")] LegacyRuntime
    }

    // Each entry pairs a font asset with one of ITS OWN material variants. Pairing a font with another
    // font's material is the bug this file exists to prevent -- see Apply below.
    public enum TMP_FontOptions
    {
        [TMP_Attributes("Valheim-Norse", "Valheim-Norse - Outline")] Norse,
        [TMP_Attributes("Valheim-Norsebold", "Valheim-Norsebold - Outline")] NorseBoldOutline,
        [TMP_Attributes("Valheim-AveriaSansLibre", "Valheim-AveriaSansLibre")] AveriaSansLibre,
        [TMP_Attributes("Valheim-AveriaSansLibre", "Valheim-AveriaSansLibre - Outline")] AveriaSansLibreOutline,
        [TMP_Attributes("Valheim-AveriaSerifLibre", "Valheim-AveriaSerifLibre - Outline")] AveriaSerifLibreOutline,
        [TMP_Attributes("Valheim-AveriaSerifLibre", "Valheim-AveriaSerifLibre")] AveriaSerifLibre,
        [TMP_Attributes("Valheim-Rune", "Valheim-Rune")] Rune,
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

        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        Font match = fonts.FirstOrDefault(x => x.name == option.GetAttributeOfType<InternalName>().internalName);

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
        if (m_fontAssets.TryGetValue(option, out TMP_FontData asset)) return asset;

        TMP_Attributes attributes = option.GetAttributeOfType<TMP_Attributes>();
        TMP_FontAsset[] assets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        Material[] materials = Resources.FindObjectsOfTypeAll<Material>();

        TMP_FontAsset matchFont = assets.FirstOrDefault(x => x.name == attributes.fontName);
        Material matchMaterial = materials.FirstOrDefault(x => x.name == attributes.materialName);
        TMP_FontData data = new TMP_FontData { font = matchFont, material = matchMaterial};

        // A partial resolve is still cached: Apply runs once per compendium line, and the two
        // FindObjectsOfTypeAll scans above walk every loaded object. RetryFailedLookups drops these
        // entries once per page open, which is where a "not loaded yet" miss gets its second chance.
        if (matchFont == null || matchMaterial == null)
        {
            WarnOnce(_warnedLookups, $"{attributes.fontName}|{attributes.materialName}",
                $"{option}: font '{attributes.fontName}' {(matchFont == null ? "NOT FOUND" : "ok")}, " +
                $"material '{attributes.materialName}' {(matchMaterial == null ? "NOT FOUND" : "ok")}");
        }

        m_fontAssets[option] = data;
        return data;
    }

    // Drops half-resolved entries so the next call rescans. FindObjectsOfTypeAll only sees loaded
    // objects, so a lookup that ran before an asset was loaded would otherwise stay unresolved for the
    // session. Called once per compendium page open rather than per line.
    public static void RetryFailedLookups()
    {
        List<TMP_FontOptions> failed = m_fontAssets
            .Where(x => x.Value?.font == null || x.Value.material == null)
            .Select(x => x.Key)
            .ToList();

        foreach (TMP_FontOptions option in failed)
        {
            m_fontAssets.Remove(option);
        }
    }

    // Applies a font asset and its material together, with the atlas check TMP itself performs.
    //
    // TextMeshProUGUI.LoadFontAsset() only keeps a shared material whose _MainTex is the font asset's
    // current atlas texture, and falls back to the font's own material otherwise. The
    // fontSharedMaterial setter does no such check, and TMP_Text.font early-returns when the asset is
    // unchanged -- so a material assigned to a live component is never validated. Repeating TMP's check
    // here means a font paired with another font's material (the shape of the deleted
    // AveriaSansLibre-Bold entry) degrades to unstyled text instead of rendering as garbage.
    //
    // This is what made compendium group headings render as a few unreadable specks on the first open
    // of a session and correctly on every open after: they are the only lines that swap material after
    // the element is live, so they were the only ones TMP never got to validate.
    // Returns false when the font asset could not be resolved, so a caller that latches a
    // "fonts loaded" flag can retry on its next open instead of keeping the default font forever.
    public static bool Apply(TMP_Text text, TMP_FontOptions option)
    {
        if (text == null)
        {
            return false;
        }

        TMP_FontData data = GetTMPFont(option);
        if (data?.font == null)
        {
            return false;
        }

        text.font = data.font;
        text.fontSharedMaterial = MaterialForCurrentAtlas(data.font, data.material, option);
        return true;
    }

    private static Material MaterialForCurrentAtlas(TMP_FontAsset font, Material material, TMP_FontOptions option)
    {
        // atlasTexture dereferences atlasTextures[0] unguarded, and this runs per compendium line.
        if (material == null || font.atlasTextures == null || font.atlasTextures.Length == 0)
        {
            return font.material;
        }

        // Public and idempotent; TMP calls it the same way before reading the cached property ids.
        ShaderUtilities.GetShaderPropertyIDs();

        Texture materialAtlas = material.GetTexture(ShaderUtilities.ID_MainTex);
        Texture fontAtlas = font.atlasTexture;
        if (materialAtlas != null && fontAtlas != null &&
            materialAtlas.GetInstanceID() == fontAtlas.GetInstanceID())
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

// ################## TMP Fonts:
// Opensans - Fallback
// AveriaSansLibre-Bold SDF
// NotoSansJP-Regular SDF
// NotoSansSC-Regular SDF
// Valheim-AveriaSansLibre
// NotoSansThai-Regular SDF
// Valheim-AveriaSerifLibre
// NotoSerifJP-Regular SDF
// NotoSerifArmenian-Regular SDF
// NotoSerifDevanagari-Regular SDF
// NotoSerifGeorgian-Regular SDF
// NotoSerifThai-Regular SDF
// NotoEmoji-Regular SDF
// NotoSerifMalayalam-Regular SDF
// NotoSerifBengali-Regular SDF
// NotoSansHebrew-Regular SDF
// NotoSansArabic-Regular SDF
// Fallback-NotoSerifNormal
// NotoSerifSC-Regular SDF
// NotoSerifKR-Regular SDF
// NotoSansBengali-Regular SDF
// Valheim-Prstartk
// NotoSansGeorgian-Regular SDF
// NotoSansArmenian-Regular SDF
// Fallback-NotoSansNormal
// NotoSansKR-Regular SDF
// NotoSansMalayalam-Regular SDF
// NotoSansDevanagari-Regular SDF
// Valheim-Norsebold
// NotoSansSC-Thin SDF
// NotoSansJP-Thin SDF
// NotoSansDevanagari-ExtraLight SDF
// NotoSansThai-ExtraLight SDF
// NotoSansBengali-ExtraLight SDF
// Valheim-Norse
// NotoSansArmenian-ExtraLight SDF
// NotoSansKR-Thin SDF
// NotoSansHebrew-Light SDF
// NotoEmoji-Light SDF
// NotoSansGeorgian-ExtraLight SDF
// NotoSansArabic-Light SDF
// NotoSansMalayalam-ExtraLight SDF
// Fallback-NotoSansThin
// Valheim-Rune


// ################## Materials:
// Valheim-AveriaSansLibre - Outline
// Valheim-AveriaSerifLibre - Outline
// Valheim-Prstartk
// Valheim-AveriaSansLibre
// Valheim-Prstartk - Outline
// Valheim-AveriaSerifLibre
// Valheim-Norsebold
// Valheim-Norsebold - Outline (Thin)
// Valheim-Norsebold - Outline
// Valheim-AveriaSerifLibre - Outline (Thin)
// Valheim-Norse
// Valheim-Norse - Sign Lit
// Valheim-Rune
// Valheim-Prstartk - Outline (Thin)
// Valheim-Norse - Outline
// Valheim-Norse - Outline (Thin)
// Valheim-AveriaSansLibre - Outline (Thin)
// Valheim-AveriaSansLibre - Outline (Thick)