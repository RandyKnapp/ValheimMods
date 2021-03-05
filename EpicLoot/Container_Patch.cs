using HarmonyLib;

namespace EpicLoot
{
    //public void AddDefaultItems()
    [HarmonyPatch(typeof(Container), "AddDefaultItems")]
    public static class Container_AddDefaultItems_Patch
    {
        public static void Postfix(Container __instance)
        {
            if (__instance == null || __instance.m_piece == null)
            {
                return;
            }

            var containerName = __instance.m_piece.name.Replace("(Clone)", "").Trim();
            var lootTable = EpicLoot.GetLootTable(containerName, 1);
            if (lootTable != null)
            {
                var items = EpicLoot.RollLootTable(lootTable, __instance.m_piece.name, __instance.transform.position);
                foreach (var item in items)
                {
                    __instance.m_inventory.AddItem(item);
                }
            }
        }
    }
}
