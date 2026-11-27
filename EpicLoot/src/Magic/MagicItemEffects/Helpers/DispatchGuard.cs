using System;
using System.Collections.Generic;

namespace EpicLoot.src.Magic.MagicItemEffects.Helpers
{
    // The shared dispatchers sit on vanilla's hottest methods (every hit, every stamina use, every carry-weight read), so
    // a handler that throws is caught at its stage and vanilla carries on: an exception escaping RPC_Damage's prefix
    // would stop the hit landing. Each stage and exception is logged once, since the same fault repeats every frame.
    internal static class DispatchGuard
    {
        private static readonly HashSet<string> Reported = new HashSet<string>();

        public static void Report(string stage, Exception e)
        {
            if (Reported.Count < 256 && Reported.Add($"{stage}|{e.GetType().Name}|{e.Message}"))
            {
                EpicLoot.LogErrorForce($"[{stage}] an Epic Loot effect handler failed and was skipped: {e}");
            }
        }
    }
}
