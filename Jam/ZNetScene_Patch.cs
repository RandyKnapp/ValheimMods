using HarmonyLib;
using UnityEngine;

namespace Jam
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static bool Prefix(ZNetScene __instance)
        {
            Debug.LogWarning($"[Jam] ZNetScene_Awake_Patch");
            Jam.TryRegisterPrefabs(__instance);
            return true;
        }
    }
}
