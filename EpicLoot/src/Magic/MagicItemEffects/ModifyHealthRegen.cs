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

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateFood))]
    public static class AddHealthRegen_Player_UpdateFood_Patch
    {
        public static void Postfix(Player __instance)
        {
            // This works as a postfix, because on the timer is exactly zero on the same frame, 
            // then the tick just happened and the timer was reset
            if (__instance.m_foodRegenTimer != 0.0f)
            {
                return;
            }

            var regenAmount = __instance.GetTotalActiveMagicEffectValue(MagicEffectType.AddHealthRegen);
            if (regenAmount <= 0)
            {
                return;
            }

            var regenMultiplier = 1.0f;
            __instance.m_seman.ModifyHealthRegen(ref regenMultiplier);
            __instance.Heal(regenAmount * regenMultiplier);

        }
    }
}