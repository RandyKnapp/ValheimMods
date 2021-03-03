using HarmonyLib;
using UnityEngine;

namespace Jam
{
    [HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix()
        {
            Jam.TryRegisterItems();
            Jam.TryRegisterRecipes();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), "Awake")]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix()
        {
            Jam.TryRegisterItems();
            Jam.TryRegisterRecipes();
        }
    }
}
