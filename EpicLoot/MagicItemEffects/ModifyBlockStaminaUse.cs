using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public override bool BlockAttack(HitData hit, Character attacker)
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    public static class ModifyBlockStaminaUse_Humanoid_BlockAttack_Patch
    {
        public static bool Override;
        public static float OriginalValue;

        public static bool Prefix(Humanoid __instance, HitData hit, Character attacker)
        {
            Override = false;
            OriginalValue = -1;

            if (__instance.IsPlayer())
            {
                var player = (Player)__instance;

                Override = true;
                OriginalValue = __instance.m_blockStaminaDrain;

                var totalBlockStaminaUseMod = player.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyBlockStaminaUse, 0.01f);
                __instance.m_blockStaminaDrain *= 1.0f - totalBlockStaminaUseMod;
            }

            return true;
        }

        public static void Postfix(Humanoid __instance, HitData hit, Character attacker)
        {
            if (Override)
            {
                __instance.m_blockStaminaDrain = OriginalValue;
            }

            Override = false;
            OriginalValue = -1;
        }
    }
}
