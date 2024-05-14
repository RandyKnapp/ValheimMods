using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Skills), nameof(Skills.RaiseSkill))]
    public class QuickLearner_Skills_RaiseSkill_Patch
    {
        [UsedImplicitly]
        private static void Prefix(Skills __instance, ref float factor)
        {
            var skillBonus = 1 + __instance.m_player.GetTotalActiveMagicEffectValue(MagicEffectType.QuickLearner, 0.01f);
            factor *= skillBonus;
        }
    }
}