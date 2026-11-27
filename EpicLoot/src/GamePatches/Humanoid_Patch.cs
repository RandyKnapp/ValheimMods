using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Humanoid))]
    public static class Humanoid_Patch
    {
        // Vanilla reads m_dropPrefab.name of every equipped item with no null check, and an item built
        // without ItemDrop.Awake (another mod's, or a code clone) can carry a null m_dropPrefab.
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
            if (data == null || data.m_dropPrefab != null)
            {
                return;
            }

            // Try for the real prefab first. The dummy exists only to keep SetupVisEquipment from throwing,
            // but it is an empty stand-in: an item wearing it renders as nothing, and being non-null while
            // carrying no ItemDrop it slips past every `m_dropPrefab == null` guard downstream. Stamping it
            // is the fallback, not the goal.
            if (data.HealDropPrefab())
            {
                return;
            }

            data.m_dropPrefab = EpicAssets.DummyPrefab();
        }
    }
}
