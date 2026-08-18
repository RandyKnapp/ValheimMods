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

        // Runs after every equipment change (Humanoid.SetupEquipment routes here), so it also covers
        // mods that equip into their own slots and finish by calling SetupEquipment themselves.
        [HarmonyPatch(nameof(Humanoid.SetupVisEquipment))]
        [HarmonyPostfix]
        public static void SetupVisEquipment_Postfix(Humanoid __instance, bool isRagdoll)
        {
            if (!isRagdoll && __instance is Player player)
            {
                VisEquipment_Patch.RefreshPlayerFx(player);
            }
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
