using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyStaminaRegen(ref float staminaMultiplier)
    [HarmonyPatch(typeof(SEMan), "ModifyStaminaRegen")]
    public static class ModifyStaminaRegen_SEMan_ModifyStaminaRegen_Patch
    {
        public static void Postfix(SEMan __instance, ref float staminaMultiplier)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var items = player.GetMagicEquipmentWithEffect(MagicEffectType.ModifyStaminaRegen);
                foreach (var item in items)
                {
                    var regenValue = item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyStaminaRegen, 0.01f);
                    staminaMultiplier += regenValue;
                }
            }
        }
    }
}