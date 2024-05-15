using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyJumpStaminaUsage))]
    public static class ModifyJumpStaminaUse_SEMan_ModifyJumpStaminaUsage_Patch
    {
        public static void Postfix(SEMan __instance, ref float staminaUse)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = (Player)__instance.m_character;
                var jumpStaminaUse = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyJumpStaminaUse, 0.01f);
                staminaUse *= 1 - jumpStaminaUse;
            }
        }
    }
}