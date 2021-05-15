using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void ModifyMaxCarryWeight(float baseLimit, ref float limit)
    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyMaxCarryWeight))]
    public static class AddCarryWeight_SEMan_ModifyMaxCarryWeight_Patch
    {
        public static void Postfix(SEMan __instance, ref float limit)
        {
            if (__instance.m_character.IsPlayer())
            {
                var player = __instance.m_character as Player;
                var carryWeightBonus = player.GetTotalActiveMagicEffectValue(MagicEffectType.AddCarryWeight);
                limit += carryWeightBonus;
            }
        }
    }
}
