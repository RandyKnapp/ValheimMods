using System.Collections.Generic;

namespace EpicLoot.Crafting
{
    public static class EnchantCostsHelper
    {
        public static EnchantingCostsConfig Config;

        public static void Initialize(EnchantingCostsConfig config)
        {
            Config = config;
        }

        public static List<ItemAmountConfig> GetDisenchantProducts(ItemDrop.ItemData item)
        {
            var isMagic = item.IsMagic();
            var type = item.m_shared.m_itemType;
            var name = item.m_shared.m_name;

            var configEntry = Config.DisenchantProducts.Find(x => {
                if (x.IsMagic && !isMagic)
                {
                    return false;
                }

                if (isMagic && x.Rarity != item.GetRarity())
                {
                    return false;
                }

                if (x.ItemTypes?.Count > 0 && !x.ItemTypes.Contains(type.ToString()))
                {
                    return false;
                }

                if (x.ItemNames?.Count > 0 && !x.ItemNames.Contains(name))
                {
                    return false;
                }

                return true;
            });

            return configEntry?.Products;
        }

        public static List<ItemAmountConfig> GetEnchantCost(ItemDrop.ItemData item, ItemRarity rarity)
        {
            var type = item.m_shared.m_itemType;

            var configEntry = Config.EnchantCosts.Find(x => {
                if (x.Rarity != rarity)
                {
                    return false;
                }

                if (x.ItemTypes?.Count > 0 && !x.ItemTypes.Contains(type.ToString()))
                {
                    return false;
                }

                return true;
            });

            return configEntry?.Cost;
        }

        public static List<ItemAmountConfig> GetAugmentCost(ItemDrop.ItemData item, ItemRarity rarity)
        {
            var type = item.m_shared.m_itemType;

            var configEntry = Config.AugmentCosts.Find(x => {
                if (x.Rarity != rarity)
                {
                    return false;
                }

                if (x.ItemTypes?.Count > 0 && !x.ItemTypes.Contains(type.ToString()))
                {
                    return false;
                }

                return true;
            });

            return configEntry?.Cost;
        }
    }
}
