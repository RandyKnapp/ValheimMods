using HarmonyLib;
using UnityEngine;

namespace EpicLoot
{
    [HarmonyPatch]
    public static class VisEquipment_Patch
    {
        [HarmonyPatch(typeof(VisEquipment), nameof(VisEquipment.AttachItem))]
        [HarmonyPostfix]
        public static void AttachItem_Postfix(VisEquipment __instance, GameObject __result, int itemHash)
        {
            var player = __instance.GetComponent<Player>();
            if (__result == null || player == null)
            {
                return;
            }

            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemHash);
            if (itemPrefab == null)
            {
                return;
            }

            var itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                return;
            }

            var itemData = itemDrop.m_itemData;
            var equippedItem = player.GetEquipmentOfType(itemData.m_shared.m_itemType);
            if (equippedItem == null)
            {
                return;
            }

            if (equippedItem.IsMagic() && equippedItem.GetMagicItem().IsUniqueLegendary())
            {
                var legendaryInfo = equippedItem.GetMagicItem().GetLegendaryInfo();
                if (legendaryInfo == null || string.IsNullOrEmpty(legendaryInfo.ItemEffect))
                {
                    return;
                }

                var asset = EpicLoot.LoadAsset<GameObject>(legendaryInfo.ItemEffect);
                if (asset != null)
                {
                    var attachObject = __result.transform;
                    var equipEffects = attachObject.Find("equiped");
                    if (equipEffects != null)
                    {
                        attachObject = equipEffects;
                    }

                    Object.Instantiate(asset, attachObject, false);
                }
            }
        }
    }
}
