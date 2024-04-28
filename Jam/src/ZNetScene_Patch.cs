using HarmonyLib;

namespace Jam
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static bool Prefix(ZNetScene __instance)
        {
            Jam.TryRegisterPrefabs(__instance);
            return true;
        }
    }
}
