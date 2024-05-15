using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.Explore), typeof(Vector3), typeof(float))]
    public class DiscoveryRadiusIncrease_Minimap_Explore_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Minimap __instance, ref float radius)
        {
            if (Player.m_localPlayer != null)
            {
                var skillBonus = 1 + Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyDiscoveryRadius, 0.01f);
                radius *= skillBonus;
            }
        }
    }
}