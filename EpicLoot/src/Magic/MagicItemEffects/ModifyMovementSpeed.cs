using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateMovementModifier))]
    public static class RemoveSpeedPenalty_Player_UpdateMovementModifier_Patch
    {
        public static void Postfix(Player __instance)
        {
            RemoveSpeedPenalties(__instance);

            ModifyWithLowHealth.Apply(__instance, MagicEffectType.ModifyMovementSpeed, effect =>
            {
                __instance.m_equipmentMovementModifier += __instance.GetTotalActiveMagicEffectValue(effect, 0.01f);
            });
        }

        public static void RemoveSpeedPenalties(Player __instance)
        {
            foreach (var itemData in __instance.GetEquipment())
            {
                if (itemData != null && itemData.HasMagicEffect(MagicEffectType.RemoveSpeedPenalty))
                {
                    __instance.m_equipmentMovementModifier -= itemData.m_shared.m_movementModifier;
                }
            }
        }
    }
}
