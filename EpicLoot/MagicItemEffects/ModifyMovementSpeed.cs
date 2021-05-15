using HarmonyLib;

namespace EpicLoot.MagicItemEffects
{
    //public void UpdateMovementModifier()
    [HarmonyPatch(typeof(Player), nameof(Player.UpdateMovementModifier))]
    public static class RemoveSpeedPenalty_Player_UpdateMovementModifier_Patch
    {
        public static void Postfix(Player __instance)
        {
            foreach (var itemData in __instance.GetEquipment())
            {
                DoSpeedCalc(__instance, itemData);
            }

            ModifyWithLowHealth.Apply(__instance, MagicEffectType.ModifyMovementSpeed, effect =>
            {
                __instance.m_equipmentMovementModifier += __instance.GetTotalActiveMagicEffectValue(MagicEffectType.ModifyMovementSpeed, 0.01f);
            });
        }

        public static void DoSpeedCalc(Player __instance, ItemDrop.ItemData item)
        {
            if (item != null && item.HasMagicEffect(MagicEffectType.RemoveSpeedPenalty))
            {
                __instance.m_equipmentMovementModifier -= item.m_shared.m_movementModifier;
            }
        }
    }
}
