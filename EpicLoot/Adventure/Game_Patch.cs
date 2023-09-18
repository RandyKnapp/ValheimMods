using HarmonyLib;

namespace EpicLoot.Adventure;

public class Game_Patch
{
    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    public static class Game_Logout_Patch
    {
        public static void Prefix(Game __instance, bool save)
        {
            if (!ZNet.instance.IsServer() || __instance.m_shuttingDown)
                return;
            BountyManagmentSystem.Instance.Shutdown(save);
        }
    }

    [HarmonyPatch(typeof(Game), nameof(Game.OnApplicationQuit))]
    public static class Game_ApplicationQuit_Patch
    {
        public static void Prefix(Game __instance)
        {
            if (!ZNet.instance.IsServer() || __instance.m_shuttingDown)
                return;
            BountyManagmentSystem.Instance.Shutdown();
        }
    }
}