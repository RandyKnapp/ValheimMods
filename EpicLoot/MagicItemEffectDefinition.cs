using System;
using System.Collections.Generic;
using System.Linq;
using ExtendedItemDataFramework;

namespace EpicLoot
{
    public class MagicItemEffectDefinition
    {
        public class ValueDef
        {
            public float MinValue;
            public float MaxValue;
            public float Increment;
        }

        public MagicEffectType Type
        {
            get => (MagicEffectType) IntType;
            set => IntType = (int) value;
        }
        public int IntType { get; set; }

        public string DisplayText = "";
        public List<ItemDrop.ItemData.ItemType> AllowedItemTypes = new List<ItemDrop.ItemData.ItemType>();
        public Dictionary<ItemRarity, ValueDef> ValuesPerRarity = new Dictionary<ItemRarity, ValueDef>();
        public float SelectionWeight = 1;
        public string Comment;
        public Func<ExtendedItemData, MagicItem, bool> Requirement;
    }

    public static partial class MagicItemEffectDefinitions
    {
        private static readonly List<ItemDrop.ItemData.ItemType> Weapons = new List<ItemDrop.ItemData.ItemType>()
        {
            ItemDrop.ItemData.ItemType.OneHandedWeapon, ItemDrop.ItemData.ItemType.TwoHandedWeapon, ItemDrop.ItemData.ItemType.Bow, ItemDrop.ItemData.ItemType.Torch
        };
        private static readonly List<ItemDrop.ItemData.ItemType> Shields = new List<ItemDrop.ItemData.ItemType>()
        {
            ItemDrop.ItemData.ItemType.Shield
        };
        private static readonly List<ItemDrop.ItemData.ItemType> Armor = new List<ItemDrop.ItemData.ItemType>()
        {
            ItemDrop.ItemData.ItemType.Helmet, ItemDrop.ItemData.ItemType.Chest, ItemDrop.ItemData.ItemType.Legs, ItemDrop.ItemData.ItemType.Shoulder, ItemDrop.ItemData.ItemType.Utility
        };
        private static readonly List<ItemDrop.ItemData.ItemType> Tools = new List<ItemDrop.ItemData.ItemType>()
        {
            ItemDrop.ItemData.ItemType.Tool
        };

        public static readonly Dictionary<int, MagicItemEffectDefinition> AllDefinitions = new Dictionary<int, MagicItemEffectDefinition>();

        private static readonly Dictionary<ItemDrop.ItemData.ItemType, Dictionary<ItemRarity, Dictionary<int, MagicItemEffectDefinition>>> SortedDefinitions
            = new Dictionary<ItemDrop.ItemData.ItemType, Dictionary<ItemRarity, Dictionary<int, MagicItemEffectDefinition>>>();

        public static void Add(MagicItemEffectDefinition effectDef)
        {
            if (AllDefinitions.ContainsKey(effectDef.IntType))
            {
                AllDefinitions.Remove(effectDef.IntType);
            }
            AllDefinitions.Add(effectDef.IntType, effectDef);

            foreach (var itemType in effectDef.AllowedItemTypes)
            {
                foreach (var rarity in effectDef.ValuesPerRarity.Keys)
                {
                    SortedDefinitions[itemType][rarity].Add(effectDef.IntType, effectDef);
                }
            }
        }

        public static MagicItemEffectDefinition Get(MagicEffectType type)
        {
            return Get((int) type);
        }

        public static MagicItemEffectDefinition Get(int type)
        {
            AllDefinitions.TryGetValue(type, out MagicItemEffectDefinition effectDef);
            return effectDef;
        }

        public static List<MagicItemEffectDefinition> GetAvailableEffects(ExtendedItemData itemData, MagicItem magicItem)
        {
            var itemType = itemData.m_shared.m_itemType;
            var itemRarity = magicItem.Rarity;
            if (SortedDefinitions.ContainsKey(itemType) && SortedDefinitions[itemType].ContainsKey(itemRarity))
            {
                var availableDefs = SortedDefinitions[itemType][itemRarity].Values.ToList();
                availableDefs.RemoveAll(x => magicItem.HasEffect(x.Type));
                availableDefs.RemoveAll(x => x.Requirement != null && !x.Requirement(itemData, magicItem));
                return availableDefs;
            }

            return new List<MagicItemEffectDefinition>();
        }

        private static void InitializeSortedDefs()
        {
            foreach (ItemDrop.ItemData.ItemType itemType in Enum.GetValues(typeof(ItemDrop.ItemData.ItemType)))
            {
                var rarityDict = new Dictionary<ItemRarity, Dictionary<int, MagicItemEffectDefinition>>();
                SortedDefinitions.Add(itemType, rarityDict);

                foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
                {
                    rarityDict.Add(rarity, new Dictionary<int, MagicItemEffectDefinition>());
                }
            }
        }
    }
}
