using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyHealthRegen(ref float regenMultiplier)
    [HarmonyPatch(typeof(SEMan), "ModifyHealthRegen")]
    public static class ModifyHealthRegen_SEMan_ModifyHealthRegen_Patch
    {
        public static void Postfix(SEMan __instance, ref float regenMultiplier)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var regenValue = 0f;
                ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyHealthRegen, effect =>
                {
	                var items = player.GetMagicEquipmentWithEffect(effect);
	                foreach (var item in items)
	                {
		                regenValue += item.GetMagicItem().GetTotalEffectValue(effect, 0.01f);
	                }
                });
                regenMultiplier += regenValue;
            }
        }
    }
}