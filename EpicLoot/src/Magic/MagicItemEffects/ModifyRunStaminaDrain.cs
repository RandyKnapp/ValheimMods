using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyHealthRegen(ref float regenMultiplier)
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyRunStaminaDrain))]
    public static class ModifySprintStaminaUse_SEMan_ModifyRunStaminaDrain_Patch
    {
        public static void Postfix(SEMan __instance, float baseDrain, ref float drain)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = (Player)__instance.m_character;
                drain *= 1 - player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifySprintStaminaUse, 0.01f);
            }
        }
    }
}