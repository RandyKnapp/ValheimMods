using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix()
        {
            EpicLoot.TryRegisterItems();
            EpicLoot.TryRegisterRecipes();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix()
        {
            EpicLoot.TryRegisterItems();
            EpicLoot.TryRegisterRecipes();
        }
    }
}
