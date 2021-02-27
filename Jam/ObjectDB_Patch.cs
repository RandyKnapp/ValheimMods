using HarmonyLib;
using UnityEngine;

namespace Jam
{
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix()
        {
            Debug.LogWarning($"[Jam] ObjectDB_CopyOtherDB_Patch ({ObjectDB.instance.m_items.Count})");
            Jam.TryRegisterItems();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix()
        {
            Debug.LogWarning($"[Jam] ObjectDB_Awake_Patch ({ObjectDB.instance.m_items.Count})");
            Jam.TryRegisterItems();
        }
    }
}
