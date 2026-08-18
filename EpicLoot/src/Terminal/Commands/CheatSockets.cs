using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicLoot;
public static partial class TerminalManager {
    private static void CheatSockets(Terminal.ConsoleEventArgs args) {
        int n = args.Length >= 2 && int.TryParse(args[1], out var v) ? v : -1;
        LootRoller.CheatSocketCount = n;
        Console.instance.Print($"> Cheat socket count set to {n} (-1 = roll from the SocketCounts table in loottables.json)");
    }
}
