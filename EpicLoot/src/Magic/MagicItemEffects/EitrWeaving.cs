using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using System;

namespace EpicLoot.MagicItemEffects;

[HarmonyPatch]
public static class EitrWeaving
{
    // A parry restores a percent of max eitr, and each point of the effect's value is worth this many percent.
    // The values stay on their old flat scale (15 to 100) because rolled items and runes store them: read directly
    // as a percent, an item that rolled 72 would restore 72% of max eitr. Each Balance Template sets its own.
    public const float DefaultMaxEitrPercentPerValue = 0.2f;
    private const string MaxEitrPercentPerValueKey = "MaxEitrPercentPerValue";

    private static float GetPercent(float value)
    {
        return value * EffectConfig.Get(MagicEffectType.EitrWeave, MaxEitrPercentPerValueKey, DefaultMaxEitrPercentPerValue);
    }

    // The percent of max eitr rather than the stored value. Rounded because the range text prints the raw float:
    // 80 * 0.12 otherwise reads as 9.599999.
    public static void RegisterDisplayValues()
    {
        MagicItem.RegisterDisplayValues(MagicEffectType.EitrWeave,
            value => new object[] { (float)Math.Round(GetPercent(value), 1) });
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    private static class PatchParry_EitrWeave
    {
        private static void Postfix(Humanoid __instance, Character attacker, bool __result)
        {
            // __result: vanilla returns false when there is no blocker or the attack came from
            // behind -- no block happened, so no eitr.
            if (!__result || __instance != Player.m_localPlayer)
            {
                return;
            }

            var eitrWeaveValue = Player.m_localPlayer.GetTotalActiveMagicEffectValue(MagicEffectType.EitrWeave);
            if (eitrWeaveValue > 0)
            {
                ItemDrop.ItemData currentBlocker = __instance.GetCurrentBlocker();
                if (currentBlocker == null)
                {
                    return;
                }
                bool parriedAttack = currentBlocker.m_shared.m_timedBlockBonus > 1f &&
                    __instance.m_blockTimer != -1f && __instance.m_blockTimer < 0.25f;

                if (parriedAttack == false)
                {
                    return;
                }

                Player.m_localPlayer.AddEitr(Player.m_localPlayer.GetMaxEitr() * GetPercent(eitrWeaveValue) * 0.01f);
            }
        }
    }
}
