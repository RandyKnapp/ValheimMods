using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public static class IncreasePlayerBaseStats
{
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    public static class Player_GetTotalFoodValue_Patch
    {
        public static void Postfix(Player __instance, ref float hp, ref float stamina, ref float eitr)
        {
            hp += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseHealth);
            stamina += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseStamina);
            eitr += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseEitr);
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
