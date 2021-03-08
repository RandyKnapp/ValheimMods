using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static bool Prefix(ZNetScene __instance)
        {
            EpicLoot.TryRegisterPrefabs(__instance);
            return true;
        }
    }
}
