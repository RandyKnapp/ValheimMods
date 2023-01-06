using EpicLoot.Data;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Integrations.HarmonyPatches
{
    [HarmonyPatch(typeof(ItemStand), "DropItem")]
    public static class ItemStand_Patch
    {
        public static bool Prefix(ItemStand __instance)
        {
            if (!__instance.HaveAttachment())
            {
                return false;
            }

            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(__instance.m_nview.GetZDO().GetString("item"));
            if (itemPrefab != null)
            {
                GameObject gameObject = Object.Instantiate(itemPrefab, __instance.m_dropSpawnPoint.position, __instance.m_dropSpawnPoint.rotation);
                ItemDrop itemDrop = gameObject.GetComponent<ItemDrop>();
                ItemDrop.LoadFromZDO(itemDrop.m_itemData, __instance.m_nview.GetZDO());
                var itemData = itemDrop.m_itemData;

                var instanceData = itemData.Data().Get<MagicItemComponent>();

                if (instanceData != null)
                {
                    itemDrop.m_itemData = instanceData.Item;
                    instanceData.Deserialize();
                    itemDrop.m_itemData.SaveMagicItem(instanceData.MagicItem);
                }

                itemDrop.Save();
                
                gameObject.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
                __instance.m_effects.Create(__instance.m_dropSpawnPoint.position, Quaternion.identity);

            }
            __instance.m_nview.GetZDO().Set("item", "");
            __instance.m_nview.InvokeRPC(ZNetView.Everybody, "SetVisualItem", "", 0, 0);
            return false;
        }
    }
}
