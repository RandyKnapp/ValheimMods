using System.Collections.Generic;
using System.Globalization;
using EpicLoot.Transformation;

namespace EpicLoot;

public static partial class TerminalManager
{
    private static void Hexen(Terminal.ConsoleEventArgs args)
    {
        var player = Player.m_localPlayer;
        var form = player != null ? player.GetComponent<HexenForm>() : null;
        if (form == null)
        {
            Console.instance.Print("> No local player to transform");
            return;
        }

        float? scale;
        if (args.Length < 2)
        {
            scale = HexenForm.DebugScale.HasValue ? null : HexenForm.DefaultScale;
        }
        else if (args[1].Equals("set", System.StringComparison.OrdinalIgnoreCase))
        {
            scale = null;
        }
        else if (args[1].Equals("off", System.StringComparison.OrdinalIgnoreCase))
        {
            scale = 0f;
        }
        else if (!float.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0f)
        {
            Console.instance.Print("> Usage: hexen [off|set|scale] (scale is a multiplier on her native size, 1 = full Hexen; set lets the set bonus decide)");
            return;
        }
        else
        {
            scale = parsed;
        }

        HexenForm.DebugScale = scale;
        form.Refresh();

        if (!scale.HasValue)
        {
            Console.instance.Print(form.Active ? "> The set keeps you a Hexen" : "> Back to your own body");
            return;
        }

        if (scale.Value <= 0f)
        {
            Console.instance.Print("> Back to your own body, set or not");
            return;
        }

        if (!form.Active)
        {
            Console.instance.Print("> Could not become a Hexen; see the log");
            return;
        }

        Console.instance.Print($"> You are a Hexen at {scale.Value.ToString(CultureInfo.InvariantCulture)}x her size");
        Console.instance.Print(">   attack: magic blast | secondary attack: lightning bolt");
        Console.instance.Print(">   jump: take off, then hold jump/crouch to rise/sink; flight drains Eitr and you sink when it runs dry");
        Console.instance.Print(">   block+jump or sneak+jump: blink in the move direction");
    }

    private static List<string> GetHexenOptions(string[] args)
    {
        return args.Length == 2 ? ["off", "set", "0.6", "1"] : [];
    }
}
