using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateModifiers))]
    public static class RemoveSpeedPenalty_Player_UpdateMovementModifier_Patch
    {
        public static void Postfix(Player __instance)
        {
            float penalty = GetSpeedPenalty(__instance);
            for (int i = 0; i < __instance.m_equipmentModifierValues.Length; i++)
            {
                __instance.m_equipmentModifierValues[i] -= penalty;

                ModifyWithLowHealth.Apply(__instance, MagicEffectType.ModifyMovementSpeed, effect =>
                {
                    __instance.m_equipmentModifierValues[i] += __instance.GetTotalActiveMagicEffectValue(effect, 0.01f);
                });
            }
            
        }

        public static float GetSpeedPenalty(Player __instance)
        {
            float penalty = 0f;
            foreach (var itemData in __instance.GetEquipment())
            {
                if (itemData != null && itemData.HasMagicEffect(MagicEffectType.RemoveSpeedPenalty))
                {
                    penalty += itemData.m_shared.m_movementModifier;
                }
            }

            return penalty;
        }
    }
}
