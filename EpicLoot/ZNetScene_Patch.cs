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

    //public bool IsAfternoon()
    /*[HarmonyPatch(typeof(EnvMan), "IsAfternoon")]
    public static class EnvMan_IsAfternoon_Patch
    {
        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }*/
}
