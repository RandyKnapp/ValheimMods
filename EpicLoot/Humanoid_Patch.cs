using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(Humanoid))]
    public class Humanoid_Patch
    {
        // Had to rewrite this method to deal with items that have no vis
        [HarmonyPatch("SetupVisEquipment")]
        [HarmonyPrefix]
        public static bool SetupVisEquipment_Prefix(Humanoid __instance, VisEquipment visEq, bool isRagdoll)
        {
            if (!isRagdoll)
            {
                visEq.SetLeftItem(__instance.m_leftItem != null ? __instance.m_leftItem?.m_dropPrefab?.name : "", __instance.m_leftItem?.m_variant ?? 0);
                visEq.SetRightItem(__instance.m_rightItem != null ? __instance.m_rightItem?.m_dropPrefab?.name : "");
                if (__instance.IsPlayer())
                {
                    visEq.SetLeftBackItem(__instance.m_hiddenLeftItem != null ? __instance.m_hiddenLeftItem.m_dropPrefab?.name : "", __instance.m_hiddenLeftItem?.m_variant ?? 0);
                    visEq.SetRightBackItem(__instance.m_hiddenRightItem != null ? __instance.m_hiddenRightItem.m_dropPrefab?.name : "");
                }
            }
            visEq.SetChestItem(__instance.m_chestItem != null ? __instance.m_chestItem?.m_dropPrefab?.name : "");
            visEq.SetLegItem(__instance.m_legItem != null ? __instance.m_legItem?.m_dropPrefab?.name : "");
            visEq.SetHelmetItem(__instance.m_helmetItem != null ? __instance.m_helmetItem?.m_dropPrefab?.name : "");
            visEq.SetShoulderItem(__instance.m_shoulderItem != null ? __instance.m_shoulderItem?.m_dropPrefab?.name : "", __instance.m_shoulderItem?.m_variant ?? 0);
            visEq.SetUtilityItem(__instance.m_utilityItem != null ? __instance.m_utilityItem?.m_dropPrefab?.name : "");
            if (!__instance.IsPlayer())
            {
                return false;
            }

            visEq.SetBeardItem(__instance.m_beardItem);
            visEq.SetHairItem(__instance.m_hairItem);

            return false;
        }
    }
}
