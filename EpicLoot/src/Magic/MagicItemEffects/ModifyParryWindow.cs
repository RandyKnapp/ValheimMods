using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot.MagicItemEffects;

public static class ModifyParryWindow
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    private class ModifyParryWindow_Humanoid_BlockAttack_Patch
    {
        private const float DEFAULT_VALUE = -1f;

        [UsedImplicitly]
        private static void Prefix(Humanoid __instance, ref float __state)
        {
            __state = DEFAULT_VALUE;
            if (__instance == Player.m_localPlayer &&
                Player.m_localPlayer.HasActiveMagicEffect(MagicEffectType.ModifyParryWindow, out float effectValue))
            {
                __state = Player.m_localPlayer.m_blockTimer;
                Player.m_localPlayer.m_blockTimer = GetBlockTimer(Player.m_localPlayer.m_blockTimer, effectValue);
            }
        }

        [UsedImplicitly]
        private static void Postfix(Humanoid __instance, float __state)
        {
            if (__state != DEFAULT_VALUE)
            {
                Player.m_localPlayer.m_blockTimer = __state;
            }
        }
    }

    public static float GetBlockTimer(float original, float effectValue)
    {
        if (original <= 0f)
        {
            return original;
        }

        return Mathf.Clamp(original - (effectValue / 1000), 0f, original);
    }

    /// <summary>
    /// Helper method to determine if current block is a parry.
    /// </summary>
    public static bool IsParry()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return false;
        }

        // Vanilla's own test (Humanoid.BlockAttack): only a blocker with a parry bonus parries, so an early
        // block with a tower shield is a plain block.
        ItemDrop.ItemData blocker = player.GetCurrentBlocker();
        return blocker != null && blocker.m_shared.m_timedBlockBonus > 1f &&
            player.m_blockTimer != -1f && player.m_blockTimer < 0.25f;
    }
}
