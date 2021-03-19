using System;
using System.Collections.Generic;

namespace EpicLoot.GatedItemType
{
    [Serializable]
    public class ItemTypeInfo
    {
        public string Type;
        public string Fallback;
        public List<string> Items = new List<string>();
    }

    [Serializable]
    public class ItemInfoConfig
    {
        public List<ItemTypeInfo> ItemInfo = new List<ItemTypeInfo>();
    }
}
