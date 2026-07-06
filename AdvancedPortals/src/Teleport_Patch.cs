using HarmonyLib;
using UnityEngine;

namespace AdvancedPortals
{
    [HarmonyPatch]
    public static class Teleport_Patch
    {
        public static AdvancedPortal CurrentAdvancedPortal;

        public static void TargetPortal_HandlePortalClick_Prefix()
        {
            Vector3 playerPos = Player.m_localPlayer.transform.position;
            const float searchRadius = 2.0f;
            Collider[] colliders = Physics.OverlapSphere(playerPos, searchRadius);
            TeleportWorld closestTeleport = null;
            float minDistSquared = searchRadius * searchRadius + 1;
            foreach (Collider collider in colliders)
            {
                TeleportWorldTrigger twt = collider.gameObject.GetComponent<TeleportWorldTrigger>();
                if (twt == null)
                {
                    continue;
                }

                TeleportWorld tw = twt.GetComponentInParent<TeleportWorld>();
                if (tw == null)
                {
                    continue;
                }

                Vector3 d = collider.transform.position - playerPos;
                float distSquared = d.x * d.x + d.y * d.y + d.z * d.z;
                if (distSquared < minDistSquared)
                {
                    closestTeleport = tw;
                    minDistSquared = distSquared;
                }
            }

            if (closestTeleport != null)
            {
                Generic_Prefix(closestTeleport);
            }
        }

        public static void Generic_Prefix(TeleportWorld __instance)
        {
            CurrentAdvancedPortal = __instance.GetComponent<AdvancedPortal>();
        }

        public static void Generic_Postfix()
        {
            CurrentAdvancedPortal = null;
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.UpdatePortal))]
        [HarmonyPrefix]
        public static void TeleportWorld_UpdatePortal_Prefix(TeleportWorld __instance)
        {
            CurrentAdvancedPortal = __instance.GetComponent<AdvancedPortal>();
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.UpdatePortal))]
        [HarmonyPostfix]
        public static void TeleportWorld_UpdatePortal_Postfix()
        {
            CurrentAdvancedPortal = null;
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
        [HarmonyPrefix]
        public static void TeleportWorld_Teleport_Prefix(TeleportWorld __instance)
        {
            CurrentAdvancedPortal = __instance.GetComponent<AdvancedPortal>();
        }

        [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
        [HarmonyPostfix]
        public static void TeleportWorld_Teleport_Postfix()
        {
            CurrentAdvancedPortal = null;
        }

        // High priority to run before other mods
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.IsTeleportable))]
        [HarmonyPostfix]
        [HarmonyPriority(Priority.High)]
        public static void Inventory_IsTeleportable_Pretfix(Inventory __instance, ref bool __result)
        {
            if (CurrentAdvancedPortal == null || __result == true)
            {
                // Do not change result for non-advanced portals, or if it already allowed to teleport
                return;
            }

            if (CurrentAdvancedPortal.AllowEverything)
            {
                __result = true;
                return;
            }

            foreach (ItemDrop.ItemData itemData in __instance.GetAllItems())
            {
                if (itemData.m_dropPrefab == null)
                {
                    continue;
                }

                if (!itemData.m_shared.m_teleportable &&
                    !CurrentAdvancedPortal.AllowedItems.Contains(itemData.m_dropPrefab.name))
                {
                    return;
                }
            }

            __result = true;
        }
    }
}
