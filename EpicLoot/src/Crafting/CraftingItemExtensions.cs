using BepInEx;
using System;

namespace EpicLoot.Crafting
{
    public static class CraftingItemExtensions
    {
        const string magicMat = "MagicCraftingMaterial";
        const string magicUnidentified = "Unidentified";

        public static bool IsMagicCraftingMaterial(this ItemDrop.ItemData item)
        {
            if (item.m_shared == null)
            {
                return false;
            }

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material &&
                item.m_shared.m_ammoType.EndsWith(magicMat);
        }

        public static bool IsUnidentifiedMaterial(this ItemDrop.ItemData item)
        {
            if (item.m_shared == null)
            {
                return false;
            }

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material &&
                item.m_shared.m_ammoType.EndsWith(magicUnidentified);
        }

        public static ItemRarity GetCraftingMaterialRarity(this ItemDrop.ItemData item)
        {
            if (item.m_shared == null || item.m_shared.m_ammoType.IsNullOrWhiteSpace())
            {
                return ItemRarity.Magic;
            }

            string[] typeParts = item.m_shared.m_ammoType.Split(new [] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (typeParts.Length == 0 || typeParts.Length > 2)
            {
                return ItemRarity.Magic;
            }

            var rarityString = typeParts[0];
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

        public static bool IsRunestone(this ItemDrop.ItemData item)
        {
            if (item.m_shared == null)
            {
                return false;
            }

            return item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material &&
                item.m_shared.m_ammoType.EndsWith("Runestone");
        }

        public static ItemRarity GetRunestoneRarity(this ItemDrop.ItemData item)
        {
            return item.GetCraftingMaterialRarity();
        }

        public static string GetRunestoneRarityColor(this ItemDrop.ItemData item)
        {
            return item.GetCraftingMaterialRarityColor();
        }
    }
}
