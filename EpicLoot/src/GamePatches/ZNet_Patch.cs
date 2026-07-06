using EpicLoot.Adventure;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNetPatches
    {
        public static void Postfix(ZNet __instance)
        {
            if (__instance.IsServer())
                __instance.gameObject.AddComponent<BountyManagmentSystem>();
            
            AdventureDataManager.Bounties.RegisterRPC(__instance.m_routedRpc);
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
    public static class ZNet_Start_Patch
    {
        public static void Postfix(ZNet __instance)
        {
            AdventureDataManager.OnZNetStart();
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
    public static class ZNet_OnDestroy_Patch
    {
        public static void Postfix(ZNet __instance)
        {
            AdventureDataManager.OnZNetDestroyed();
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorld))]
    public static class ZNet_SaveWorld_Patch
    {
        public static void Prefix(ZNet __instance)
        {
            AdventureDataManager.OnWorldSave();
        }
    }
}
