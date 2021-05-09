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
                var regenValue = 0f;
                ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyStaminaRegen, effect =>
                {
					var items = player.GetMagicEquipmentWithEffect(effect);
	                foreach (var item in items)
	                {
		                regenValue += item.GetMagicItem().GetTotalEffectValue(effect, 0.01f);
	                }
                });
                staminaMultiplier += regenValue;
            }
        }
    }
}