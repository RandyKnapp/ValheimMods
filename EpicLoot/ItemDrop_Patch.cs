using EpicLoot.Data;
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


            var prefab = __instance.m_itemData.m_dropPrefab;


            if (prefab != null)
            {
                var itemDropPrefab = prefab.GetComponent<ItemDrop>();

                if ((__instance.m_itemData.IsLegacyEIDFItem() || itemDropPrefab.m_itemData.IsExtended()) && !__instance.m_itemData.IsExtended())
                {
                        var instanceData = __instance.m_itemData.Data().Add<MagicItemComponent>();

                    if (itemDropPrefab.m_itemData.IsExtended())
                    {
                        var prefabData = itemDropPrefab.m_itemData.Data().Get<MagicItemComponent>();

                        if (instanceData != null && prefabData != null)
                        {
                            instanceData.Save(prefabData.MagicItem);
                        }
                    }

                    __instance.m_itemData.m_dropPrefab = itemDropPrefab.gameObject;
                }
            }
        }
    }
}
