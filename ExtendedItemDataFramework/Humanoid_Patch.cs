using HarmonyLib;

namespace ExtendedItemDataFramework
{
    public static class Humanoid_Patch
    {
        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.EquipItem))]
        class AttachExtendedDataToZDO_Humanoid_EquipItem_Patch
        {
            private static void Postfix(ItemDrop.ItemData item, Humanoid __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    item.Extended()?.UpdatePlayerZDOForEquipment();
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UnequipItem))]
        class AttachExtendedDataToZDO_Humanoid_UnequipItem_Patch
        {
            private static void Postfix(ItemDrop.ItemData item, Humanoid __instance)
            {
                if (__instance == Player.m_localPlayer)
                {
                    item.Extended()?.UpdatePlayerZDOForEquipment(false);
                }
            }
        }
    }
}
