using System;
using System.Collections.Generic;
using System.Linq;
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

    private class InternalName(string internalName) : Attribute
    {
        public readonly string internalName = internalName;
    }

    private static readonly Dictionary<FontOptions, Font> m_fonts = new();

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
}