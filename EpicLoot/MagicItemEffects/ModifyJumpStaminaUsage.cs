using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
    [HarmonyPatch(typeof(SEMan), "ModifyJumpStaminaUsage")]
    public static class ModifyJumpStaminaUse_SEMan_ModifyJumpStaminaUsage_Patch
    {
        public static void Postfix(SEMan __instance, ref float staminaUse)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var items = player.GetMagicEquipmentWithEffect(MagicEffectType.ModifyJumpStaminaUse);
                foreach (var item in items)
                {
                    var jumpStaminaUse = item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyJumpStaminaUse, 0.01f);
                    staminaUse *= 1 - jumpStaminaUse;
                }
            }
        }
    }
}