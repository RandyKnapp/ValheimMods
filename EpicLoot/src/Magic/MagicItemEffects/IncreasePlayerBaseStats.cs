using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class IncreasePlayerBaseStats
{
    // Consolidated GetTotalFoodValue patch for the flat and percent-of-pool magic/shard effects. Two
    // postfixes preserve the ordering the effects used to get from their individual [HarmonyPriority]s:
    //  * Normal priority -- flat pool additions, then the Normal-priority shard stamina effects.
    //  * Priority.Last   -- the percent-of-pool shard effects, so they layer on the fully-built pools
    //    (after the flat adds here and the lower-priority BulkUp / EveryXPointsOfEitr patches).
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    public static class Player_GetTotalFoodValue_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
        {
            hp += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseHealth);
            stamina += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseStamina);
            eitr += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseEitr);

            GainMaxStaminaBasedOnPlayerMaxHealth.Apply(__instance, hp, ref stamina);
            StaminaIncreaseForMovementPenalty.Apply(__instance, ref stamina);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void PostfixPercentOfPool(Player __instance, ref float hp, ref float stamina, ref float eitr)
        {
            PercentHealth.Apply(__instance, ref hp);
            PercentStamina.Apply(__instance, ref stamina);
            PercentEitr.Apply(__instance, ref eitr);
            HeartyEitr.Apply(__instance, hp, ref eitr);
            EnergeticEitr.Apply(__instance, stamina, ref eitr);
        }
    }

    /// <summary>
    /// Sets the hud display to the correct size
    /// </summary>
    [HarmonyPatch(typeof(Player), nameof(Player.GetBaseFoodHP))]
    public static class IncreaseHealth_Player_GetBaseFoodHP_Patch
    {
        public static void Postfix(Player __instance, ref float __result)
        {
            __result += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseHealth);
        }
    }
}
