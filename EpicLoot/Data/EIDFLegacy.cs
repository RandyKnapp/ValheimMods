
using System;
using System.Linq;

namespace EpicLoot.Data
{
    public static class EIDFLegacy
    {
        public const string StartDelimiter = "<|";
        public const string EndDelimiter = "|>";
        public const string StartDelimiterEscaped = "randyknapp.mods.extendeditemdataframework.startdelimiter";
        public const string EndDelimiterEscaped = "randyknapp.mods.extendeditemdataframework.enddelimiter";

        public static string RestoreDataText(string text)
        {
            return string.IsNullOrEmpty(text) ? text : text.Replace(StartDelimiterEscaped, StartDelimiter).Replace(EndDelimiterEscaped, EndDelimiter);
        }

        public static bool IsLegacyEIDFItem(this ItemDrop.ItemData itemData)
        {
            return itemData.m_crafterName.StartsWith(StartDelimiter);
        }
        public static bool IsLegacyMagicItem(this ItemDrop.ItemData itemData)
        {
            return itemData.m_crafterName.Contains($"{StartDelimiter}{MagicItemComponent.TypeID}{EndDelimiter}");
        }

        public static string GetMagicItemFromCrafterName(ItemDrop.ItemData item)
        {
            var serializedComponents = item.m_crafterName.Split(new[] { StartDelimiter }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var component in serializedComponents)
            {
                var parts = component.Split(new[] { EndDelimiter }, StringSplitOptions.None);
                var typeString = RestoreDataText(parts[0]);

                if (typeString.Equals(MagicItemComponent.TypeID))
                {
                    var data = parts.Length == 2 ? parts[1] : string.Empty;
                    if (string.IsNullOrEmpty(data))
                        continue;

                    return RestoreDataText(data);
                }
            }
            return null;
        }
    }
}
