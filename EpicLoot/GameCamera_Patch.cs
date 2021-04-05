using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateCamera))]
    public static class GameCamera_UpdateCamera_Patch
    {
        public static bool Prefix(GameCamera __instance)
        {
            if (__instance.m_freeFly)
            {
                const float dt = 1.0f / 30.0f;
                __instance.UpdateFreeFly(dt);
                __instance.UpdateCameraShake(dt);
                return false;
            }

            return true;
        }
    }
}
