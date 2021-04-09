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
            _harmony.UnpatchAll(PluginId);
        }
    }

    [HarmonyPatch]
    public static class Console_Patches
    {
        private static bool _inputtingText;

        [HarmonyPatch(typeof(Console), nameof(Console.IsCheatsEnabled))]
        public static class Console_IsCheatsEnabled_Patch
        {
            public static bool Prefix(ref bool __result)
            {
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Console), nameof(Console.InputText))]
        public static class Console_InputText_Patch
        {
            public static bool Prefix()
            {
                _inputtingText = true;
                return true;
            }

            public static void Postfix()
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
