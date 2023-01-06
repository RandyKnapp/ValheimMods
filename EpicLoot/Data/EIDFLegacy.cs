using System;
using System.IO;
using System.Linq;
using BepInEx;

namespace EpicLoot.Data
{
    public static class EIDFLegacy
    {
        public const string StartDelimiter = "<|";
        public const string EndDelimiter = "|>";
        public const string StartDelimiterEscaped = "randyknapp.mods.extendeditemdataframework.startdelimiter";
        public const string EndDelimiterEscaped = "randyknapp.mods.extendeditemdataframework.enddelimiter";

        public const string CrafterNameType = "c";
        public const string UniqueIdType = "u";

        public static bool ExtendedItemFrameworkLoaded = false;

        public static string RestoreDataText(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Replace(StartDelimiterEscaped, StartDelimiter).Replace(EndDelimiterEscaped, EndDelimiter);
        }

        public static bool IsLegacyEIDFItem(this ItemDrop.ItemData itemData)
        {
            if (itemData == null || string.IsNullOrEmpty(itemData.m_crafterName))
                return false;

            return itemData.m_crafterName.StartsWith(StartDelimiter);
        }
        public static bool IsLegacyMagicItem(this ItemDrop.ItemData itemData)
        {
            if (itemData == null || string.IsNullOrEmpty(itemData.m_crafterName))
                return false;

            return itemData.m_crafterName.Contains($"{StartDelimiter}{MagicItemComponent.TypeID}{EndDelimiter}");
        }

        private static string _getEIDFTypeValue(string encodedCrafterName, string typeID)
        {
            if (string.IsNullOrEmpty(encodedCrafterName) || string.IsNullOrEmpty(typeID))
                return null;

            var serializedComponents = encodedCrafterName.Split(new[] { StartDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var component in serializedComponents)
            {
                var parts = component.Split(new[] { EndDelimiter }, StringSplitOptions.None);
                var typeString = RestoreDataText(parts[0]);

                if (typeString.Equals(typeID))
                {
                    var data = parts.Length == 2 ? parts[1] : string.Empty;
                    if (string.IsNullOrEmpty(data))
                        continue;

                    return RestoreDataText(data);
                }
            }

            return null;
        }

        public static string GetMagicItemFromCrafterName(ItemDrop.ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.m_crafterName))
                return null;

            return _getEIDFTypeValue(item.m_crafterName, MagicItemComponent.TypeID);
        }

        public static string GetCrafterName(this ItemDrop.ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.m_crafterName))
                return null;

            if (!item.IsLegacyEIDFItem() || ExtendedItemFrameworkLoaded)
                return item.m_crafterName;

            return _getEIDFTypeValue(item.m_crafterName, CrafterNameType) ?? string.Empty;
        }

        public static string GetUniqueId(this ItemDrop.ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.m_crafterName))
                return null;

            if (!item.IsLegacyEIDFItem()|| ExtendedItemFrameworkLoaded)
                return item.m_crafterName;

            return _getEIDFTypeValue(item.m_crafterName, UniqueIdType) ?? string.Empty;
        }

        public static void CheckForExtendedITemFrameworkLoaded()
        {
            var dirInfo = new DirectoryInfo(Paths.PluginPath);

            if (dirInfo.GetFiles("ExtendedItemDataFramework.dll", SearchOption.AllDirectories).Any())
            {
                ExtendedItemFrameworkLoaded = true;
            }
        }
    }
}
