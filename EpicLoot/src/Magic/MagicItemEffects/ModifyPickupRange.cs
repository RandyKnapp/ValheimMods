using HarmonyLib;

namespace EpicLoot.MagicItemEffects;

public class ModifyPickupRange
{
    [HarmonyPatch(typeof(Player), nameof(Player.AutoPickup))]
    public static class ModifyPickupRange_Player_AutoPickup_Patch
    {
        private static float originalDistance;
        
        public static void Prefix(Player __instance)
        {
            if (__instance.IsPlayer())
            {
                originalDistance = __instance.m_autoPickupRange;

                float effectValue =
                    __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyPickupRange, 0.01f);
                if (effectValue > 0)
                {
                    __instance.m_autoPickupRange *= 1 + effectValue;
                }
            }
        }
        
        public static void Postfix(Player __instance)
        {
            if (__instance.IsPlayer())
            {
                __instance.m_autoPickupRange = originalDistance;
            }
        }
    }
}