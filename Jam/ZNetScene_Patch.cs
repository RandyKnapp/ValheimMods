using HarmonyLib;
using UnityEngine;

namespace Jam
{
    [HarmonyPatch(typeof(ZNetScene), "Awake")]
    public static class ZNetScene_Awake_Patch
    {
        public static void Postfix(ZNetScene __instance)
        {
            PrefabCreator.Reset();
            var newPrefab = PrefabCreator.CreatePrefab();
            if (newPrefab == null)
            {
                Debug.LogError($"Failure to initialize!");
            }
            else
            {
                Debug.LogWarning($"Successfully initialized in ZNetScene_Awake_Patch");
            }
        }
    }
}
