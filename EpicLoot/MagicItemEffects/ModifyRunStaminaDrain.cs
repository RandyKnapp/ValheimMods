using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyHealthRegen(ref float regenMultiplier)
    [HarmonyPatch(typeof(SEMan), "ModifyRunStaminaDrain")]
    public static class ModifySprintStaminaUse_SEMan_ModifyRunStaminaDrain_Patch
    {
        public static void Postfix(SEMan __instance, float baseDrain, ref float drain)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var items = player.GetMagicEquipmentWithEffect(MagicEffectType.ModifySprintStaminaUse);
                foreach (var item in items)
                {
                    var sprintStaminaUse = item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifySprintStaminaUse, 0.01f);
                    drain *= 1 - sprintStaminaUse;
                }
            }
        }
    }
}