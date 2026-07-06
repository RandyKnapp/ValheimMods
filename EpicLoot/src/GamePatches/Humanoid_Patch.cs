using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Humanoid))]
    public static class Humanoid_Patch
    {
        // Handle ItemDrop.ItemData that have null m_dropPrefab values to prevent NRE in method.
        // TODO: Validate if this is needed, or can be fixed in a better way.
        [HarmonyPatch(nameof(Humanoid.SetupVisEquipment))]
        [HarmonyPrefix]
        public static void SetupVisEquipment_Prefix(Humanoid __instance, VisEquipment visEq, bool isRagdoll)
        {
            if (EpicAssets.DummyPrefab() == null)
            {
                EpicLoot.LogWarning("Unable to find empty object, may cause unexpected errors for Humanoid.SetupVisEquipment method.");
                return;
            }

            AssignEmptyToNull(ref __instance.m_leftItem);
            AssignEmptyToNull(ref __instance.m_rightItem);
            AssignEmptyToNull(ref __instance.m_hiddenLeftItem);
            AssignEmptyToNull(ref __instance.m_hiddenRightItem);
            AssignEmptyToNull(ref __instance.m_chestItem);
            AssignEmptyToNull(ref __instance.m_legItem);
            AssignEmptyToNull(ref __instance.m_helmetItem);
            AssignEmptyToNull(ref __instance.m_shoulderItem);
            AssignEmptyToNull(ref __instance.m_utilityItem);
            AssignEmptyToNull(ref __instance.m_trinketItem);
        }

        private static void AssignEmptyToNull(ref ItemDrop.ItemData data)
        {
            if (data != null && data.m_dropPrefab == null)
            {
                data.m_dropPrefab = EpicAssets.DummyPrefab();
            }
        }
    }
}
