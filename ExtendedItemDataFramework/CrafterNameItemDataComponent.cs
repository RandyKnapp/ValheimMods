namespace ExtendedItemDataFramework
{
    public class CrafterNameItemDataComponent : BaseExtendedItemComponent
    {
        public string CrafterName;

        public CrafterNameItemDataComponent(ItemDrop.ItemData parent, string initialCrafterName) 
            : base(typeof(CrafterNameItemDataComponent).AssemblyQualifiedName, parent)
        {
            CrafterName = initialCrafterName;
        }
    }

    public static partial class ItemDataExtensions
    {
        public static string GetCrafterName(this ItemDrop.ItemData itemData)
        {
            if (itemData is ExtendedItemData extendedItemData)
            {
                return extendedItemData.GetComponent<CrafterNameItemDataComponent>().CrafterName;
            }
            else
            {
                return itemData.m_crafterName;
            }
        }
    }
}
