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
    public class GuaranteedMagicEffect
    {
        public string Type;
        public MagicItemEffectDefinition.ValueDef Values;
    }

    [Serializable]
    public class LegendaryInfo
    {
        public string ID;
        public string Name;
        public string Description;
        public MagicItemEffectRequirements Requirements;
        public List<GuaranteedMagicEffect> GuaranteedMagicEffects;
        public float SelectionWeight = 1;
        public string ItemEffect;
    }

    [Serializable]
    public class ItemInfoConfig
    {
        public List<ItemTypeInfo> ItemInfo = new List<ItemTypeInfo>();
        public List<LegendaryInfo> LegendaryItems = new List<LegendaryInfo>();
    }
}
