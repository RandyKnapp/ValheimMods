using EpicLoot.Data;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot.Integrations.HarmonyPatches
{
    [HarmonyPatch(typeof(ArmorStand), "DropItem")]
    public static class ArmorStand_Patch
    {
        public static bool Prefix(ArmorStand __instance, int index)
        {
            if (!__instance.HaveAttachment(index))
                return false;

            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(__instance.m_nview.GetZDO().GetString(index.ToString() + "_item"));

            if (itemPrefab != null)
            {
                GameObject gameObject = Object.Instantiate<GameObject>(itemPrefab, __instance.m_dropSpawnPoint.position, __instance.m_dropSpawnPoint.rotation);
                ItemDrop itemDrop = gameObject.GetComponent<ItemDrop>();
                ItemDrop.LoadFromZDO(index, itemDrop.m_itemData, __instance.m_nview.GetZDO());
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
                __instance.m_destroyEffects.Create(__instance.m_dropSpawnPoint.position, Quaternion.identity);
            
            }

            __instance.m_nview.GetZDO().Set(index.ToString() + "_item", "");
            __instance.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetVisualItem", index, "", 0);
            __instance.UpdateSupports();
            __instance.m_cloths = __instance.GetComponentsInChildren<Cloth>();

            return false;
        }
    }
}
