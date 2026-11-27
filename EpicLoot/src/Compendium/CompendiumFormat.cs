using System;
using System.Collections.Generic;
using UnityEngine;

namespace EpicLoot.Compendium;

internal static class CompendiumFormat
{
    public const string BetterColor = "#8aff8aff";
    public const string WorseColor = "#ff8a8aff";
    public const string NeutralColor = "#c0c0c0ff";
    public const string Dash = "<color=#808080ff>-</color>";

    public static MagicTextTable AddStatTable(MagicPages instance, string firstHeader = "$mod_epicloot_vitals_col_stat") =>
        instance.MagicPagesTextArea.AddTable(
            new MagicTableColumn { Header = firstHeader, Weight = 0.34f, Outline = true },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_base", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_epicloot", Weight = 0.22f, Alignment = TextAnchor.MiddleCenter },
            new MagicTableColumn { Header = "$mod_epicloot_vitals_col_total", Weight = 0.22f, Alignment = TextAnchor.MiddleRight });

    public static void AddRow(MagicTextTable table, string name, string baseText, float magicDelta, string totalText,
        List<string> detail, Func<float, string> formatDelta, bool higherIsBetter = true)
    {
        bool neutral = Mathf.Abs(magicDelta) < 0.005f;
        bool better = higherIsBetter ? magicDelta > 0f : magicDelta < 0f;
        string color = neutral ? NeutralColor : better ? BetterColor : WorseColor;
        string[] cells =
        {
            name,
            baseText,
            neutral ? Dash : formatDelta(magicDelta),
            $"<color={color}>{totalText}</color>",
        };
        table.AddRow(cells, detail, true);
    }

    public static string Grey(string text) => StatSourceLines.Grey(text);
    public static string Whole(float value) => Mathf.RoundToInt(value).ToString();
    public static string OneDecimal(float value) => value.ToString("0.#");
    public static string Times(float value) => value.ToString("0.#") + "x";
    public static string Percent(float fraction) => $"{Mathf.RoundToInt(fraction * 100f)}%";
    public static string SignedPercent(float fraction) => Signed(fraction * 100f) + "%";

    public static string Signed(float value, string format = "0")
    {
        string text = Mathf.Abs(value).ToString(format);
        return value < 0f ? $"-{text}" : $"+{text}";
    }
}
