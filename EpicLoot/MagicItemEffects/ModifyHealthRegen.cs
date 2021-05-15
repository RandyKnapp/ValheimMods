using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyHealthRegen(ref float regenMultiplier)
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyHealthRegen))]
    public static class ModifyHealthRegen_SEMan_ModifyHealthRegen_Patch
    {
        public static void Postfix(SEMan __instance, ref float regenMultiplier)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = (Player)__instance.m_character;
                var regenValue = 0f;
                ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyHealthRegen, effect =>
                {
                    regenValue = player.GetTotalActiveMagicEffectValue(effect, 0.01f);
                });
                regenMultiplier += regenValue;
            }
        }
    }
}