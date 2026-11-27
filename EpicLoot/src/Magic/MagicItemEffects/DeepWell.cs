using EpicLoot.src.Magic.MagicItemEffects.Helpers;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    public static partial class MagicEffectType
    {
        public static string DeepWell = nameof(DeepWell);
    }
}

namespace EpicLoot.MagicItemEffects
{
    // The pool half is called from IncreasePlayerBaseStats.PostfixPercentOfPool right after PercentEitr, so it also
    // grows the eitr ConvertHealthToEitr added. The regen half uses ModifyEitrRegen's additive convention (+15 there
    // means +0.15 on the multiplier), so 30 here means -0.30.
    public static class DeepWell
    {
        public const float DefaultEitrRegenReduction = 30f;
        private const string EitrRegenReductionKey = "EitrRegenReduction";

        // Player-editable; a negative value would turn the penalty into a bonus.
        private static float EitrRegenReduction => EffectConfig.GetClamped(MagicEffectType.DeepWell,
            EitrRegenReductionKey, DefaultEitrRegenReduction, 0f, 100f);

        public static void RegisterDisplayValues()
        {
            MagicItem.RegisterDisplayValues(MagicEffectType.DeepWell, value => new object[] { value, EitrRegenReduction });
        }

        public static void Apply(Player player, ref float eitr)
        {
            if (player != Player.m_localPlayer || eitr <= 0f)
            {
                return;
            }

            float fraction = player.GetTotalActiveMagicEffectValue(MagicEffectType.DeepWell, 0.01f);
            if (fraction > 0f)
            {
                eitr += eitr * fraction;
            }
        }

        // Last, so it subtracts from the multiplier after ModifyEitrRegen and DartingThoughts have added to it, and the
        // clamp sees their total. Clamped like BulkUp's health regen cut, so the multiplier never goes negative and
        // turns regen into a drain.
        [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
        private static class SEMan_ModifyEitrRegen_Patch
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(SEMan __instance, ref float eitrMultiplier)
            {
                Player player = Player.m_localPlayer;
                if (player == null || !ReferenceEquals(__instance.m_character, player) ||
                    !player.HasActiveMagicEffect(MagicEffectType.DeepWell, out float _))
                {
                    return;
                }

                float reduction = EitrRegenReduction * 0.01f;
                eitrMultiplier -= Mathf.Min(reduction, Mathf.Max(eitrMultiplier, 0f));
            }
        }
    }
}
