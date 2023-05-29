using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace AdvancedPortals
{
    [HarmonyPatch]
    public static class ConnectPortals_Patch
    {
        //public bool GetAllZDOsWithPrefabIterative(string prefab, List<ZDO> zdos, ref int index)
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.GetAllZDOsWithPrefabIterative))]
        [HarmonyPrefix]
        public static bool GetPortalsZDOs_Prefix(ZDOMan __instance, ref bool __result, string prefab, List<ZDO> zdos, ref int index)
        {
            if (Game.instance == null || Game.instance.m_portalPrefab == null || __instance == null)
                return true;

            var prefabPortalName = Game.instance.m_portalPrefab.name;
            if (prefab != prefabPortalName)
                return true;

            __result = GetAllZDOsWithMultiplePrefabsIterative(__instance, new []{ prefabPortalName, "portal_ancient", "portal_obsidian", "portal_blackmarble" }, zdos, ref index);
            return false;
        }

        public static bool GetAllZDOsWithMultiplePrefabsIterative(ZDOMan __instance, string[] prefabs, List<ZDO> zdos, ref int index)
        {
            var stableHashCode = prefabs.Select(x => x.GetStableHashCode()).ToArray();
            if (index >= __instance.m_objectsBySector.Length)
            {
                foreach (var zdoList in __instance.m_objectsByOutsideSector.Values)
                {
                    foreach (var zdo in zdoList)
                    {
                        foreach (var nameHash in stableHashCode)
                        {
                            if (zdo.GetPrefab() == nameHash)
                            {
                                zdos.Add(zdo);
                                break;
                            }
                        }
                    }
                }
                zdos.RemoveAll(new Predicate<ZDO>(ZDOMan.InvalidZDO));
                return true;
            }
            var num = 0;
            while (index < __instance.m_objectsBySector.Length)
            {
                var zdoList = __instance.m_objectsBySector[index];
                if (zdoList != null)
                {
                    foreach (var zdo in zdoList)
                    {
                        foreach (var nameHash in stableHashCode)
                        {
                            if (zdo.GetPrefab() == nameHash)
                            {
                                zdos.Add(zdo);
                                break;
                            }
                        }
                    }
                    ++num;
                    if (num > 400)
                        break;
                }
                ++index;
            }
            return false;
        }

        public static void GetAllZDOsWithPrefab(ZDOMan __instance, string[] prefabs, List<ZDO> zdos)
        {
            var stableHashCode = prefabs.Select(x => x.GetStableHashCode()).ToArray();
            foreach (ZDO zdo in __instance.m_objectsByID.Values)
            {
                foreach (var nameHash in stableHashCode)
                {
                    if (zdo.GetPrefab() == nameHash)
                    {
                        zdos.Add(zdo);
                        break;
                    }
                }
            }
        }
    }
}
