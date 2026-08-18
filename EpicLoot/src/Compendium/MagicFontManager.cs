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

    public enum TMP_FontOptions
    {
        [TMP_Attributes("Valheim-Norse", "Valheim-Norse - Outline")] Norse,
        [TMP_Attributes("Valheim-Norsebold", "Valheim-Norsebold - Outline")] NorseBoldOutline,
        [TMP_Attributes("Valheim-AveriaSansLibre", "Valheim-AveriaSansLibre")] AveriaSansLibre,
        [TMP_Attributes("Valheim-AveriaSansLibre", "Valheim-AveriaSansLibre - Outline")] AveriaSansLibreOutline,
        [TMP_Attributes("AveriaSansLibre-Bold SDF", "Valheim-AveriaSansLibre - Outline")] AveriaSansLibreBoldOutline,
        [TMP_Attributes("Valheim-AveriaSerifLibre", "Valheim-AveriaSerifLibre - Outline")] AveriaSerifLibreOutline,
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

    public static Font GetFont(FontOptions option)
    {
        if (m_fonts.TryGetValue(option, out Font font))
        {
            return font;
        }

        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        Font match = fonts.FirstOrDefault(x => x.name == option.GetAttributeOfType<InternalName>().internalName);
        m_fonts[option] = match;
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
        m_fontAssets[option] = data;
        return data;
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