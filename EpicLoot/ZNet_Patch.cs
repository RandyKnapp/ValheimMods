using EpicLoot.Adventure;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNetPatches
    {
        public static void Postfix(ZNet __instance)
        {
            AdventureDataManager.Bounties.RegisterRPC(__instance.m_routedRpc);
        }
    }
}
