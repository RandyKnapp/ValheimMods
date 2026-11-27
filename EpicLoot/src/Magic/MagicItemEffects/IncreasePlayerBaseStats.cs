using EpicLoot.MagicItemEffects.Shards;
using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class IncreasePlayerBaseStats
{
    // Two postfixes on GetTotalFoodValue: flat pool additions at Normal priority, and the percent-of-pool
    // effects at Priority.Last, so they scale the fully built pools (after BulkUp and EveryXPointsOfEitr).
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
            // Converts the finished health pool, before the eitr multipliers so they scale what it adds.
            ConvertHealthToEitr.Apply(__instance, ref hp, ref eitr);
            PercentEitr.Apply(__instance, ref eitr);
            DeepWell.Apply(__instance, ref eitr);
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
