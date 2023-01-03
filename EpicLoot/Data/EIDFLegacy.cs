
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
    }
}
