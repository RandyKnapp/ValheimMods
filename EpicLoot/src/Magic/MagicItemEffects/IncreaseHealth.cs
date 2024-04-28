using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void GetTotalFoodValue(out float hp, out float stamina)
    [HarmonyPatch(typeof(Player), nameof(Player.GetTotalFoodValue))]
    public static class IncreaseHealth_Player_GetTotalFoodValue_Patch
    {
        public static void Postfix(Player __instance, ref float hp)
        {
            hp += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseHealth);
        }
    }

    //public float GetBaseFoodHP() => 25f;
    [HarmonyPatch(typeof(Player), nameof(Player.GetBaseFoodHP))]
    public static class IncreaseHealth_Player_GetBaseFoodHP_Patch
    {
        public static void Postfix(Player __instance, ref float __result)
        {
            __result += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.IncreaseHealth);
        }
    }
}
