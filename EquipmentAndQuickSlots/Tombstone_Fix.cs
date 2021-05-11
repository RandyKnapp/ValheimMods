using System;
using HarmonyLib;

namespace EquipmentAndQuickSlots
{
    [HarmonyPatch(typeof(Container), "Awake")]
    public static class Container_Awake_Patch
    {
        public static void Prefix(Container __instance)
        {
            if (!__instance.GetComponent<TombStone>())
            {
                return;
            }

            ZNetView nview = __instance.GetComponent<ZNetView>();
            if (!nview.IsValid())
            {
                return;
            }

            string itemData = nview.GetZDO().GetString("items", "");
            if (string.IsNullOrEmpty(itemData))
            {
                return;
            }

            ZPackage pkg = new ZPackage(itemData);

            // itemData.m_version
            pkg.ReadInt();

            // m_inventory.Count
            int numSavedItems = pkg.ReadInt();

            int capacity = __instance.m_height * __instance.m_width;
            if (numSavedItems > capacity)
            {
                int newHeight = (int)Math.Ceiling((float)numSavedItems / (float)capacity * (float)__instance.m_height);
                EquipmentAndQuickSlots.Log($"Changing tombstone height from {__instance.m_height} to {newHeight}.");
                __instance.m_height = newHeight;
            }
        }
    }
}
