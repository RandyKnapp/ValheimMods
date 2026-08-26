using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace ConsoleHelper
{
    [BepInPlugin(PluginId, PluginId, "1.0.0")]
    public class ConsoleHelper : BaseUnityPlugin
    {
        private const string PluginId = "ConsoleHelper";
        private Harmony _harmony;

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }

    [HarmonyPatch]
    public static class Terminal_Patches
    {
        private static bool _inputtingText;

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.IsCheatsEnabled))]
        public static class Terminal_IsCheatsEnabled_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Terminal), nameof(Terminal.InputText))]
        public static class Terminal_InputText_Patch
        {
            public static bool Prefix()
            {
                _inputtingText = true;
                return true;
            }

            // Finalizer, not postfix: InputText runs the typed command, and a throwing command
            // (mistyped or from another mod) would skip a postfix -- leaving the flag latched and
            // ZNet.IsServer patched-true on this client for the rest of the session.
            public static void Finalizer()
            {
                _inputtingText = false;
            }
        }

        [HarmonyPatch(typeof(ZNet), nameof(ZNet.IsServer))]
        public static class ZNet_IsServer_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                if (_inputtingText)
                {
                    __result = true;
                    return false;
                }

                return true;
            }
        }
    }

    
}
