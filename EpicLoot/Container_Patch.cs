using System.Linq;
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
            var lootTables = LootRoller.GetLootTable(containerName);
            if (lootTables != null && lootTables.Count > 0)
            {
                var items = LootRoller.RollLootTable(lootTables, 1, __instance.m_piece.name, __instance.transform.position);
                EpicLoot.Log($"Rolling on loot table: {containerName}, spawned {items.Count} items at drop point({__instance.transform.position.ToString("0")}).");
                foreach (var item in items)
                {
                    __instance.m_inventory.AddItem(item);
                    EpicLoot.Log($"  - {item.m_shared.m_name}" + (item.IsMagic() ? $": {string.Join(", ", item.GetMagicItem().Effects.Select(x => x.EffectType.ToString()))}" : ""));
                }
            }
        }
    }
}
