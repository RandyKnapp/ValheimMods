using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Game), "Awake")]
    public static class Game_Awake_Patch
    {
        public static void Postfix(Game __instance)
        {
            Debug.LogWarning("Game_Awake_Patch");
            var znetScene = __instance.GetComponent<ZNetScene>();
            if (znetScene != null)
            {
                Debug.Log("- Has ZNetScene");
                Debug.Log($"- Prefabs: {znetScene.m_prefabs.Count}");
            }
        }
    }

    [HarmonyPatch(typeof(Game), "Start")]
    public static class Game_Start_Patch
    {
        public static void Postfix(Game __instance)
        {
            EpicLoot.OnGameStart();
        }
    }

    [HarmonyPatch(typeof(Game), "Shutdown")]
    public static class Game_Shutdown_Patch
    {
        public static void Postfix(Game __instance)
        {
            if (__instance.IsShuttingDown())
            {
                return;
            }

            EpicLoot.OnGameShutdown();
        }
    }
}
