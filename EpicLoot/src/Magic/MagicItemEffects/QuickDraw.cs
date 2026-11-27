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
                if (crossbowReloadSpeed > 0)
                {
                    // Scales vanilla's own time, which already carries the crossbow skill's reduction.
                    if (__instance.m_shared.m_attack.m_requiresReload)
                    {
                        __result *= 1f - crossbowReloadSpeed;
                    }
                    else if (__instance.m_shared.m_secondaryAttack.m_requiresReload)
                    {
                        // Vanilla only knows the primary attack's reload and returns a flat 1 s otherwise.
                        __result = __instance.m_shared.m_secondaryAttack.m_reloadTime * (1f - crossbowReloadSpeed);
                    }
                }
            }
        }
    }
}