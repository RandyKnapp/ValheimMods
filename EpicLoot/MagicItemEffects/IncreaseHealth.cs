using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void GetTotalFoodValue(out float hp, out float stamina)
    [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
    public static class IncreaseHealth_Player_GetTotalFoodValue_Patch
    {
        public static void Postfix(Player __instance, ref float hp)
        {
            var items = __instance.GetMagicEquipmentWithEffect(MagicEffectType.IncreaseHealth);
            foreach (var item in items)
            {
                hp += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.IncreaseHealth);
            }
        }
    }

    //public float GetBaseFoodHP() => 25f;
    [HarmonyPatch(typeof(Player), "GetBaseFoodHP")]
    public static class IncreaseHealth_Player_GetBaseFoodHP_Patch
    {
        public static void Postfix(Player __instance, ref float __result)
        {
            var items = __instance.GetMagicEquipmentWithEffect(MagicEffectType.IncreaseHealth);
            foreach (var item in items)
            {
                __result += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.IncreaseHealth);
            }
        }
    }
}
