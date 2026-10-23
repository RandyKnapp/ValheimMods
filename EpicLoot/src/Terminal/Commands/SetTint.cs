using EpicLoot.LegendarySystem;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static readonly ArmorTintInfo[] DemoTints =
    {
        new ArmorTintInfo { Color = "#3FA34D" },
        new ArmorTintInfo { Color = "#2E7D32", Value = -0.1f },
        new ArmorTintInfo { Color = "#9CCC65", Strength = 0.7f },
        new ArmorTintInfo { Color = "#4FC3F7" },
        new ArmorTintInfo { Color = "#1E5BB8" },
        new ArmorTintInfo { Color = "#E6F4FF", Strength = 0.5f, Value = 0.1f },
        new ArmorTintInfo { Color = "#FF7A2A" },
        new ArmorTintInfo { Color = "#C62828" },
        new ArmorTintInfo { Color = "#B388FF" },
        new ArmorTintInfo { Color = "#6A1B9A" },
        new ArmorTintInfo { Color = "#00E5FF" },
        new ArmorTintInfo { Color = "#FFD54F" },
        new ArmorTintInfo { Color = "#B8860B", Strength = 0.8f },
        new ArmorTintInfo { Color = "#9E9E9E", Value = -0.15f },
        new ArmorTintInfo { Color = "#4A2C5A", Value = -0.1f },
    };

    private static Coroutine _tintDemo;

    private static void SetTint(Terminal.ConsoleEventArgs args)
    {
        if (args.Length >= 2 && args[1].Equals("off", System.StringComparison.OrdinalIgnoreCase))
        {
            StopTintDemo();
            SetArmorTint.SetOverride(null);
            Console.instance.Print("> Armor tint preview off; set tints apply again");
            return;
        }

        if (args.Length >= 2 && args[1].Equals("demo", System.StringComparison.OrdinalIgnoreCase))
        {
            if (_tintDemo != null)
            {
                StopTintDemo();
                Console.instance.Print("> Armor tint demo stopped; the last colour stays until 'settint off'");
                return;
            }

            if (Player.m_localPlayer == null)
            {
                Console.instance.Print("> No local player");
                return;
            }

            float interval = 0.5f;
            if (args.Length >= 3 && float.TryParse(args[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
            {
                interval = Mathf.Max(0.1f, seconds);
            }

            _tintDemo = Player.m_localPlayer.StartCoroutine(TintDemo(interval));
            Console.instance.Print($"> Cycling {DemoTints.Length} armor tints every {interval:0.##}s; 'settint demo' again to stop");
            return;
        }

        if (args.Length >= 5 && args[1].Equals("shift", System.StringComparison.OrdinalIgnoreCase) &&
            TryParseFloat(args[2], out float hue) && TryParseFloat(args[3], out float saturation) && TryParseFloat(args[4], out float value))
        {
            Apply(new ArmorTintInfo { Hue = hue, Saturation = saturation, Value = value });
            return;
        }

        if (args.Length >= 2 && TryParseColor(args[1], out string color))
        {
            ArmorTintInfo tint = new ArmorTintInfo { Color = color };
            if (args.Length >= 3 && TryParseFloat(args[2], out float strength))
            {
                tint.Strength = Mathf.Clamp01(strength);
            }

            if (args.Length >= 4 && TryParseFloat(args[3], out float valueShift))
            {
                tint.Value = Mathf.Clamp(valueShift, -1f, 1f);
            }

            Apply(tint);
            return;
        }

        ArmorTintInfo current = SetArmorTint.Override;
        Console.instance.Print(current == null ? "> No armor tint preview active" : $"> Armor tint preview: {Describe(current)}");
        Console.instance.Print("> usage: settint <#rrggbb> [strength 0-1] [value -1..1] | settint shift <hue> <saturation> <value> | settint demo [seconds] | settint off");
    }

    private static void Apply(ArmorTintInfo tint)
    {
        StopTintDemo();
        SetArmorTint.SetOverride(tint);
        Console.instance.Print($"> Armor tint preview: {Describe(tint)}");
    }

    private static bool TryParseFloat(string text, out float value)
    {
        return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseColor(string text, out string color)
    {
        color = text.StartsWith("#") ? text : "#" + text;
        return ColorUtility.TryParseHtmlString(color, out _);
    }

    private static System.Collections.IEnumerator TintDemo(float interval)
    {
        int index = 0;
        while (Player.m_localPlayer != null)
        {
            ArmorTintInfo tint = DemoTints[index];
            SetArmorTint.SetOverride(tint);
            Console.instance.Print($"> tint {index + 1}/{DemoTints.Length}: {Describe(tint)}");
            index = (index + 1) % DemoTints.Length;
            yield return new WaitForSeconds(interval);
        }

        _tintDemo = null;
    }

    private static void StopTintDemo()
    {
        if (_tintDemo != null && Player.m_localPlayer != null)
        {
            Player.m_localPlayer.StopCoroutine(_tintDemo);
        }

        _tintDemo = null;
    }

    private static string Describe(ArmorTintInfo tint)
    {
        string f(float x) => x.ToString("0.##", CultureInfo.InvariantCulture);
        List<string> parts = new List<string>();
        if (!string.IsNullOrEmpty(tint.Color))
        {
            parts.Add($"\"Color\": \"{tint.Color}\"");
            if (!Mathf.Approximately(tint.Strength, 1f))
            {
                parts.Add($"\"Strength\": {f(tint.Strength)}");
            }
        }

        if (tint.Hue != 0f) parts.Add($"\"Hue\": {f(tint.Hue)}");
        if (tint.Saturation != 0f) parts.Add($"\"Saturation\": {f(tint.Saturation)}");
        if (tint.Value != 0f) parts.Add($"\"Value\": {f(tint.Value)}");
        return "{ " + string.Join(", ", parts) + " }";
    }

    private static List<string> GetSetTintOptions(string[] args)
    {
        return args.Length switch
        {
            2 => ["off", "demo", "shift", "#3FA34D", "#2E7D32", "#1E5BB8", "#C62828", "#6A1B9A", "#FFD54F"],
            3 => ["1", "0.8", "0.6", "0.4"],
            4 => ["0", "0.1", "-0.1", "-0.2"],
            _ => []
        };
    }
}
