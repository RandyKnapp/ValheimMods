using EpicLoot.Adventure;
using EpicLoot_UnityLib;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNetPatches
    {
        public static void Postfix(ZNet __instance)
        {
            AdventureDataManager.Bounties.RegisterRPC(__instance.m_routedRpc);
            EnchantingTableUpgrades.RegisterRPC(__instance.m_routedRpc, Common.Utils.IsServer());
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
    public static class ZNet_Start_Patch
    {
        public static void Postfix(ZNet __instance)
        {
            AdventureDataManager.OnZNetStart();
            EnchantingTableUpgrades.OnZNetStart();
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
    public static class ZNet_OnDestroy_Patch
    {
        public static void Postfix(ZNet __instance)
        {
            AdventureDataManager.OnZNetDestroyed();
            EnchantingTableUpgrades.OnZNetDestroyed();
        }
    }

    [HarmonyPatch(typeof(ZNet), nameof(ZNet.SaveWorld))]
    public static class ZNet_SaveWorld_Patch
    {
        public static bool Prefix(ZNet __instance)
        {
            AdventureDataManager.OnWorldSave();
            EnchantingTableUpgrades.OnWorldSave();
            return true;
        }
    }

    //ZoneSystem.RPC_GlobalKeys
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.RPC_GlobalKeys))]
    public static class ZoneSystem_RPC_GlobalKeys_Patch
    {
        public static void Postfix()
        {
            EnchantingTableUpgrades.NewGlobalKeys();
        }
    }
}
