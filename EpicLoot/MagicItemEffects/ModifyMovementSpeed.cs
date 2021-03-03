using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void UpdateMovementModifier()
    [HarmonyPatch(typeof(Player), "UpdateMovementModifier")]
    public static class RemoveSpeedPenalty_Player_UpdateMovementModifier_Patch
    {
        public static void Postfix(Player __instance)
        {
            DoSpeedCalc(__instance, __instance.m_rightItem);
            DoSpeedCalc(__instance, __instance.m_leftItem);
            DoSpeedCalc(__instance, __instance.m_chestItem);
            DoSpeedCalc(__instance, __instance.m_legItem);
            DoSpeedCalc(__instance, __instance.m_helmetItem);
            DoSpeedCalc(__instance, __instance.m_shoulderItem);
            DoSpeedCalc(__instance, __instance.m_utilityItem);
        }

        public static void DoSpeedCalc(Player __instance, ItemDrop.ItemData item)
        {
            if (item != null)
            {
                if (item.HasMagicEffect(MagicEffectType.RemoveSpeedPenalty))
                {
                    __instance.m_equipmentMovementModifier -= item.m_shared.m_movementModifier;
                }

                if (item.HasMagicEffect(MagicEffectType.ModifyMovementSpeed))
                {
                    __instance.m_equipmentMovementModifier += item.GetMagicItem().GetTotalEffectValue(MagicEffectType.ModifyMovementSpeed, 0.01f);
                }
            }
        }
    }
}
