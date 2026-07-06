using HarmonyLib;
using System;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
    public class QuickDrawBow_Player_GetAttackDrawPercentage_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (__instance.HasActiveMagicEffect(MagicEffectType.QuickDraw, out float bowDrawTimeReduction, 0.01f))
            {
                float reduction = Math.Min(1, __result *= (1 + bowDrawTimeReduction));
                __result = reduction;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
    public class Quickdraw_Player_GetWeaponLoadingTime
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (Player.m_localPlayer != null && Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.QuickDraw, 0.01f) is float crossbowReloadSpeed)
            {
                if (crossbowReloadSpeed > 0 && __instance.m_shared.m_attack.m_requiresReload || __instance.m_shared.m_secondaryAttack.m_requiresReload)
                {
                    if (__instance.m_shared.m_attack.m_requiresReload)
                    {
                        __result = __instance.m_shared.m_attack.m_reloadTime * (1f - crossbowReloadSpeed);
                    }

                    if (__instance.m_shared.m_secondaryAttack.m_requiresReload)
                    {
                        __result = __instance.m_shared.m_secondaryAttack.m_reloadTime * (1f - crossbowReloadSpeed);
                    }
                }
            }
        }
    }
}