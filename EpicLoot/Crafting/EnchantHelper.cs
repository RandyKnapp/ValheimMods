using System.Collections.Generic;

namespace EpicLoot.Crafting
{
    public class EnchantHelper
    {
        public static List<KeyValuePair<ItemDrop, int>> GetEnchantCosts(ItemDrop.ItemData item, ItemRarity rarity)
        {
            var costList = new List<KeyValuePair<ItemDrop, int>>();

            var enchantCostDef = EnchantCostsHelper.GetEnchantCost(item, rarity);
            if (enchantCostDef == null)
            {
                return costList;
            }

            foreach (var itemAmountConfig in enchantCostDef)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item).GetComponent<ItemDrop>();
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Tried to add unknown item ({itemAmountConfig.Item}) to enchant cost for item ({item.m_shared.m_name})");
                    continue;
                }
                costList.Add(new KeyValuePair<ItemDrop, int>(prefab, itemAmountConfig.Amount));
            }

            return costList;
        }
    }
}
