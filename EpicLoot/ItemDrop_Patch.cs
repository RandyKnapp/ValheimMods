using EpicLoot.LootBeams;
using ExtendedItemDataFramework;
using HarmonyLib;

namespace EpicLoot
{
    [HarmonyPatch(typeof(ItemDrop), "Awake")]
    public static class ItemDrop_Awake_Patch
    {
        public static void Postfix(ItemDrop __instance)
        {
            __instance.gameObject.AddComponent<LootBeam>();

            // This code probably needs to go into EIDF
            var prefab = __instance.m_itemData.m_dropPrefab;
            if (prefab != null)
            {
                var itemDropPrefab = prefab.GetComponent<ItemDrop>();
                if (itemDropPrefab != null && itemDropPrefab.m_itemData.IsExtended() && !__instance.m_itemData.IsExtended())
                {
                    __instance.m_itemData = new ExtendedItemData(itemDropPrefab.m_itemData);
                    __instance.m_itemData.m_dropPrefab = itemDropPrefab.gameObject;
                }
            }
        }
    }
}
