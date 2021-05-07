using System;
using System.Collections.Generic;
using Common;

namespace EpicLoot.LegendarySystem
{
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
        public bool Enchantable;
        public List<RecipeRequirementConfig> EnchantCost = new List<RecipeRequirementConfig>();
    }

    [Serializable]
    public class LegendaryItemConfig
    {
        public List<LegendaryInfo> LegendaryItems = new List<LegendaryInfo>();
    }
}
