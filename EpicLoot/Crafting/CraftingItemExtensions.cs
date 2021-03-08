using System;

namespace EpicLoot.Crafting
{
    public static class CraftingItemExtensions
    {
        public static bool IsMagicCraftingMaterial(this ItemDrop.ItemData item)
        {
            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material && item.m_shared.m_ammoType == "MagicCraftingMaterial";
        }

        public static ItemRarity GetCraftingMaterialRarity(this ItemDrop.ItemData item)
        {
            var rarityString = item.m_shared.m_holdAnimationState;
            if (Enum.TryParse(rarityString, out ItemRarity rarity))
            {
                return rarity;
            }

            return ItemRarity.Magic;
        }

        public static string GetCraftingMaterialRarityColor(this ItemDrop.ItemData item)
        {
            return EpicLoot.GetRarityColor(item.GetCraftingMaterialRarity());
        }
    }
}
