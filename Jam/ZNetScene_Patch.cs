using HarmonyLib;
using UnityEngine;

namespace Jam
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static void Postfix(ZNetScene __instance)
        {
            
        }
    }
}
