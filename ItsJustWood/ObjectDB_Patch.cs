using HarmonyLib;
using UnityEngine;

namespace ItsJustWood
{
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix()
        {
            Debug.LogWarning($"ItsJustWood - ObjectDB_CopyOtherDB_Patch ({ObjectDB.instance.m_items.Count})");
            ItsJustWood.TryRegisterRecipes();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix()
        {
            Debug.LogWarning($"ItsJustWood - ObjectDB_Awake_Patch ({ObjectDB.instance.m_items.Count})");
            ItsJustWood.TryRegisterRecipes();
        }
    }
}
