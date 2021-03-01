using System;

namespace ExtendedItemDataFramework
{
    [Serializable]
    public class UniqueItemDataComponent : BaseExtendedItemComponent
    {
        public string Guid;

        public UniqueItemDataComponent(ItemDrop.ItemData parent) 
            : base(typeof(UniqueItemDataComponent).AssemblyQualifiedName, parent)
        {
        }
    }
}
