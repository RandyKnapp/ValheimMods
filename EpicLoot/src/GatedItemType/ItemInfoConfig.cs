using System;
using System.Collections.Generic;

namespace EpicLoot.GatedItemType
{
    [Serializable]
    public class ItemTypeInfo
    {
        public string Type;
        public string Fallback;
        public string ItemFallback;
        [Obsolete]
        public List<string> Items = new List<string>();
        public List<string> IgnoredItems = new List<string>();
        public Dictionary<string, List<string>> ItemsByBoss = new Dictionary<string, List<string>>();
    }

    [Serializable]
    public class ItemInfoConfig
    {
        public List<ItemTypeInfo> ItemInfo = new List<ItemTypeInfo>();
    }
}
