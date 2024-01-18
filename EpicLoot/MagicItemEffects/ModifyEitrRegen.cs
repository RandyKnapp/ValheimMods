using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyEitrRegen(ref float eitrMultiplier)
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyEitrRegen))]
    public static class ModifyEitrRegen_SEMan_ModifyEitrRegen_Patch
    {
        public static void Postfix(SEMan __instance, ref float eitrMultiplier)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var regenValue = 0f;
                ModifyWithLowHealth.Apply(player, MagicEffectType.ModifyEitrRegen, effect =>
                {
                    regenValue += player.GetTotalActiveMagicEffectValue(effect, 0.01f);
                });
                eitrMultiplier += regenValue;
            }
        }
    }
}