using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ZNetView), nameof(ZNetView.Awake))]
    public class SkipAwakeInit_ZNetView_Awake_Patch
    {
        public static bool skipAwake;

        [UsedImplicitly]
        private static bool Prefix() => !skipAwake;
    }
}
