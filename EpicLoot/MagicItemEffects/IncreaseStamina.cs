using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void GetTotalFoodValue(out float hp, out float stamina)
    [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
    public static class IncreaseStamina_Player_GetTotalFoodValue_Patch
    {
        public static void Postfix(Player __instance, ref float stamina)
        {
            var items = __instance.GetMagicEquipmentWithEffect(MagicEffectType.IncreaseStamina);
            foreach (var item in items)
            {
                stamina += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.IncreaseStamina);
            }
        }
    }
}