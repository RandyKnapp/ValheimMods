using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    [HarmonyPatch]
    public static class Humanoid_Patch
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        public class AttachExtendedDataToZDO_Humanoid_EquipItem_Patch
        {
            [UsedImplicitly]
            private static void Postfix(ItemDrop.ItemData item, Humanoid __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    item.Extended()?.UpdatePlayerZDOForEquipment();
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        public class AttachExtendedDataToZDO_Humanoid_UnequipItem_Patch
        {
            [UsedImplicitly]
            private static void Prefix(Humanoid __instance, ItemDrop.ItemData item)
            {
                if (__instance == Player.m_localPlayer)
                {
                    item.Extended()?.UpdatePlayerZDOForEquipment(false);
                }
            }
        }
    }
}
