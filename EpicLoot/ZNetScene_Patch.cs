using HarmonyLib;
using UnityEngine;

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

    [HarmonyPatch(typeof(ZNet), "Start")]
    public static class ZNet_Start_Patch
    {
        public static void Postfix()
        {
            EpicLoot.LogError("ZNet Start");
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "Awake")]
    public static class FejdStartup_Awake_Patch
    {
        public static void Postfix()
        {
            EpicLoot.LogError("FejdStartup Awake");
            EpicLoot.InitializeConfig();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), "Start")]
    public static class FejdStartup_Start_Patch
    {
        public static void Postfix()
        {
            EpicLoot.LogError("FejdStartup Start");
        }
    }
}
