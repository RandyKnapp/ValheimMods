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
        public bool Augmentable;
    }

    [Serializable]
    public class TextureReplacement
    {
        public string ItemID;
        public string MainTexture;
        public string ChestTex;
        public string LegsTex;
    }

    [Serializable]
    public class LegendaryInfo
    {
        public string ID;
        public string Name;
        public string Description;
        public MagicItemEffectRequirements Requirements;
        public List<GuaranteedMagicEffect> GuaranteedMagicEffects = new List<GuaranteedMagicEffect>();
        public int GuaranteedEffectCount = -1;
        public float SelectionWeight = 1;
        public string EquipFx;
        public FxAttachMode EquipFxMode = FxAttachMode.Player;
        public List<TextureReplacement> TextureReplacements = new List<TextureReplacement>();
        public bool IsSetItem;
        public bool Enchantable;
        public List<RecipeRequirementConfig> EnchantCost = new List<RecipeRequirementConfig>();
    }

    [Serializable]
    public class SetBonusInfo
    {
        public int Count;
        public GuaranteedMagicEffect Effect;
    }

    [Serializable]
    public class LegendarySetInfo
    {
        public string ID;
        public string Name;
        public List<string> LegendaryIDs = new List<string>();
        public List<SetBonusInfo> SetBonuses = new List<SetBonusInfo>();
    }

    [Serializable]
    public class LegendaryItemConfig
    {
        public List<LegendaryInfo> LegendaryItems = new List<LegendaryInfo>();
        public List<LegendarySetInfo> LegendarySets = new List<LegendarySetInfo>();
    }
}
