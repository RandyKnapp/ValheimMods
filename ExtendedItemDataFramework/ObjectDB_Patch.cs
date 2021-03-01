using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix(ObjectDB __instance)
        {
            ExtendedItemDataFramework.TryConvertItemPrefabs(__instance);
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix(ObjectDB __instance)
        {
            ExtendedItemDataFramework.TryConvertItemPrefabs(__instance);
        }
    }

}
