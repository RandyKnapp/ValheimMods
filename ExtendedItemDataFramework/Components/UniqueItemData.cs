using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ExtendedItemDataFramework
{
    public class UniqueItemData : BaseExtendedItemComponent
    {
        public static readonly List<ItemDrop.ItemData.ItemType> UniqueItemTypes = new List<ItemDrop.ItemData.ItemType>()
        {
            ItemDrop.ItemData.ItemType.OneHandedWeapon,
            ItemDrop.ItemData.ItemType.Bow,
            ItemDrop.ItemData.ItemType.Shield,
            ItemDrop.ItemData.ItemType.Helmet,
            ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs,
            ItemDrop.ItemData.ItemType.Hands,
            ItemDrop.ItemData.ItemType.TwoHandedWeapon,
            ItemDrop.ItemData.ItemType.Torch,
            ItemDrop.ItemData.ItemType.Shoulder,
            ItemDrop.ItemData.ItemType.Utility,
            ItemDrop.ItemData.ItemType.Tool
        };

        public string Guid;

        public UniqueItemData(ExtendedItemData parent) 
            : base(typeof(UniqueItemData).FullName, parent)
        {
        }

        private void CreateNewGuid()
        {
            Guid = System.Guid.NewGuid().ToString();
            ItemData.Save();
        }

        public override string Serialize()
        {
            return Guid;
        }

        public override void Deserialize(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                CreateNewGuid();
            }
            else
            {
                Guid = data;
            }
        }

        public override BaseExtendedItemComponent Clone()
        {
            return MemberwiseClone() as BaseExtendedItemComponent;
        }

        public static void OnNewExtendedItemData(ExtendedItemData itemdata)
        {
            if (UniqueItemTypes.Contains(itemdata.m_shared.m_itemType))
            {
                var uniqueItemData = itemdata.AddComponent<UniqueItemData>();
                uniqueItemData.CreateNewGuid();
            }
        }

        public static void OnLoadExtendedItemData(ExtendedItemData itemdata)
        {
            if (UniqueItemTypes.Contains(itemdata.m_shared.m_itemType) && !itemdata.IsUnique())
            {
                var uniqueItemData = itemdata.AddComponent<UniqueItemData>();
                uniqueItemData.CreateNewGuid();
            }
        }
    }

    public static partial class ItemDataExtensions
    {
        public static bool IsUnique(this ItemDrop.ItemData itemData)
        {
            if (itemData.Extended() is ExtendedItemData extendedItemData)
            {
                var uniqueItemData = extendedItemData.GetComponent<UniqueItemData>();
                return uniqueItemData != null;
            }

            return false;
        }

        public static string GetUniqueId(this ItemDrop.ItemData itemData)
        {
            if (itemData.Extended() is ExtendedItemData extendedItemData)
            {
                var uniqueItemData = extendedItemData.GetComponent<UniqueItemData>();
                return uniqueItemData != null ? uniqueItemData.Guid : string.Empty;
            }

            return string.Empty;
        }
    }

    //public static string GetTooltip(ItemDrop.ItemData item, int qualityLevel, bool crafting)
    [HarmonyPatch(typeof(ItemDrop.ItemData), "GetTooltip", typeof(ItemDrop.ItemData), typeof(int), typeof(bool))]
    public static class ItemData_GetTooltip_UniqueItemData_Patch
    {
        public static void Postfix(ItemDrop.ItemData item, ref string __result)
        {
            var uniqueId = item.GetUniqueId();
            if (!string.IsNullOrEmpty(uniqueId))
            {
                __result += $"\n<color=magenta>{uniqueId}</color>";
            }
        }
    }
}
