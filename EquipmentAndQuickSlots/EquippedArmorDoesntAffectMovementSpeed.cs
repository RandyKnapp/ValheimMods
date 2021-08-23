using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{

    [HarmonyPatch(typeof(Player), "UpdateMovementModifier")]
    public class EquippedArmorDoesntAffectMovementSpeed_Patch
    {
        public static bool Prefix(ref float ___m_equipmentMovementModifier, ref ItemDrop.ItemData ___m_rightItem, ref ItemDrop.ItemData ___m_leftItem, ref ItemDrop.ItemData ___m_utilityItem)
        {
            if (EquipmentAndQuickSlots.EquipedArmorDoesntAffectMovementSpeed.Value)
            {
				___m_equipmentMovementModifier = 0f;
				if (___m_rightItem != null)
				{
					___m_equipmentMovementModifier += ___m_rightItem.m_shared.m_movementModifier;
				}
				if (___m_leftItem != null)
				{
					___m_equipmentMovementModifier += ___m_leftItem.m_shared.m_movementModifier;
				}
				if (___m_utilityItem != null)
				{
					___m_equipmentMovementModifier += ___m_utilityItem.m_shared.m_movementModifier;
				}
				return false;
			}
            return true;
        }
    }
}