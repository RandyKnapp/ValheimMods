using EpicLoot.LootBeams;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Awake))]
    public static class ItemDrop_Awake_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            __instance.gameObject.AddComponent<LootBeam>();

            var prefabData = __instance.m_itemData.InitializeCustomData();
            if (prefabData != null)
            {
                __instance.m_itemData.m_dropPrefab = prefabData;
                __instance.Save();
            }
        }
    }

    [HarmonyPatch(typeof(Inventory), nameof(Inventory.Load))]
    public static class Inventory_Load_Patch
    {
        public static void Postfix(Inventory __instance)
        {
            foreach (var itemData in __instance.m_inventory)
            {
                var prefabData = itemData.InitializeCustomData();
                if (prefabData != null)
                    itemData.m_dropPrefab = prefabData;
            }
        }
    }
}