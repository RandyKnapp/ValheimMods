using System.Linq;
using HarmonyLib;

namespace EpicLoot_Addon_Helheim
{
    [HarmonyPatch(typeof(Console), "InputText")]
    public static class Console_Patch
    {
        public static bool Prefix(Console __instance)
        {
            var input = __instance.m_input.text;
            var args = input.Split(' ');
            if (args.Length == 0)
            {
                return true;
            }

            var player = Player.m_localPlayer;

            if (Command("helheim", args))
            {
                var level = args.Length >= 2 ? int.Parse(args[1]) : 0;
                Helheim.SetLevel(level);
            }

            return true;
        }

        private static bool Command(string command, params string[] args)
        {
            return args.Contains(command);
        }
    }
}
