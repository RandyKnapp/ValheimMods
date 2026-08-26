using HarmonyLib;
using System;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GetAttackDrawPercentage))]
    public class QuickDrawBow_Player_GetAttackDrawPercentage_Patch
    {
        // Humanoid, not Player: this method runs for every humanoid (Draugr/Fuling archers), and
        // Harmony passes __instance through without a type check.
        private static void Postfix(Humanoid __instance, ref float __result)
        {
            if (__instance is Player player &&
                player.HasActiveMagicEffect(MagicEffectType.QuickDraw, out float bowDrawTimeReduction, 0.01f))
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
                // Parenthesized: '&&' binds tighter than '||', so a zero-value effect with a
                // reload-secondary weapon used to enter this branch and overwrite vanilla's
                // skill-lerped reload time with the raw base value.
                if (crossbowReloadSpeed > 0 &&
                    (__instance.m_shared.m_attack.m_requiresReload || __instance.m_shared.m_secondaryAttack.m_requiresReload))
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