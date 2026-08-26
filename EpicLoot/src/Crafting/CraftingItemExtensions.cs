using BepInEx;
using System;

namespace EpicLoot.Crafting
{
    public static class CraftingItemExtensions
    {
        const string magicMat = "MagicCraftingMaterial";
        const string magicUnidentified = "Unidentified";
        const string shardSlotChisel = "|ShardSlotChisel";

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

        // Brokkr's Gift: the consumable that adds shard slots to a magic item. Deliberately matches on
        // the ammoType suffix alone, with no ItemType check -- unlike every other predicate here, its
        // prefabs are authored as Misc rather than Material, and the suffix is already unique.
        public static bool IsShardSlotChisel(this ItemDrop.ItemData item)
        {
            if (item.m_shared == null || item.m_shared.m_ammoType.IsNullOrWhiteSpace())
            {
                return false;
            }

            return item.m_shared.m_ammoType.EndsWith(shardSlotChisel);
        }

        public static ItemRarity GetShardSlotChiselRarity(this ItemDrop.ItemData item)
        {
            return item.GetCraftingMaterialRarity();
        }
    }
}
