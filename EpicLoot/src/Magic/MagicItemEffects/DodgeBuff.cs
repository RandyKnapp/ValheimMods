using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class DodgeBuff
{
    [HarmonyPatch(typeof(Player), nameof(Player.RPC_HitWhileDodging))]
    private static class DodgeBuff_RPC__HitWhileDodging_Patch
    {
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer)
            {
                return;
            }

            if (Player.m_localPlayer.GetSEMan().GetStatusEffect(EpicAssets.DodgeBuff_SE_Name.GetStableHashCode()) == null &&
                Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.DodgeBuff))
            {
                Player.m_localPlayer.GetSEMan().AddStatusEffect(EpicAssets.DodgeBuff_SE_Name.GetStableHashCode());
                return;
            }
        }
    }
}