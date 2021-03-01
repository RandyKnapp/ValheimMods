namespace ExtendedItemDataFramework
{
    public static partial class ItemDataExtensions
    {
        public static ExtendedItemData Extended(this ItemDrop.ItemData itemData)
        {
            if (itemData.IsExtended())
            {
                return itemData as ExtendedItemData;
            }

            return null;
        }

        public static bool IsExtended(this ItemDrop.ItemData itemData)
        {
            return itemData is ExtendedItemData;
        }
    }
}
