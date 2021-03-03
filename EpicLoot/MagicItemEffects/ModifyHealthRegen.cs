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
                var items = player.GetMagicEquipmentWithEffect(MagicEffectType.ModifyHealthRegen);
                foreach (var item in items)
                {
                    var regenValue = item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyHealthRegen);
                    regenMultiplier += regenValue;
                }
            }
        }
    }
}