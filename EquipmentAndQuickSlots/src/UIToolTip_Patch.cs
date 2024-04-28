using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    public static class UIToolTip_Patch
    {
        [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.LateUpdate))]
        public static class UIToolTip_LateUpdate_Patch
        {
            public static void Postfix(UITooltip __instance)
            {
                if (!EquipmentAndQuickSlots.EquipmentSlotsEnabled.Value && !EquipmentAndQuickSlots.QuickSlotsEnabled.Value) return;
                
                if (ZInput.IsGamepadActive() && !ZInput.IsMouseActive())
                {
                    if (!(UITooltip.m_current == __instance) || !(UITooltip.m_tooltip != null))
                        return;
                    
                    if ( __instance.m_anchor != null)
                    {
                        UITooltip.m_tooltip.transform.localPosition = new Vector3(__instance.m_fixedPosition.x+200, __instance.m_fixedPosition.y, 0);
                    } 
                } 
            }
        }
    }
}