using HarmonyLib;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public class ModifyBuildDistance
{
    [HarmonyPatch(typeof(Player), nameof(Player.PieceRayTest))]
    public static class ModifyBuildDistance_Player_PieceRayTest_Patch
    {
        private static float originalDistance;
        
        public static void Prefix(Player __instance)
        {
            if (__instance.IsPlayer())
            {
                originalDistance = __instance.m_maxPlaceDistance;

                float effectValue =
                    __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyBuildDistance, 0.01f);
                if (effectValue > 0)
                {
                    __instance.m_maxPlaceDistance *= 1 + effectValue;
                }
            }
        }
        
        public static void Postfix(Player __instance)
        {
            if (__instance.IsPlayer())
            {
                __instance.m_maxPlaceDistance = originalDistance;
            }
        }
    }
}