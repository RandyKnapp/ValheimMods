using System;
using System.Collections.Generic;
using System.Linq;
using ExtendedItemDataFramework;
using UnityEngine;

namespace EpicLoot
{
    public enum ItemRarity
    {
        Magic,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public class MagicItemEffect
    {
        public MagicEffectType EffectType
        {
            get => (MagicEffectType) _effectId;
            set => _effectId = (int)value;
        }
        public int IntType
        {
            get => _effectId;
            set => _effectId = value;
        }
        public float EffectValue;

        private int _effectId;
    }

    [Serializable]
    public class MagicItem
    {
        public ItemRarity Rarity;
        public List<MagicItemEffect> Effects = new List<MagicItemEffect>();
        public string DisplayNameOverride;

        public string GetDisplayName(ExtendedItemData baseItem)
        {
            return string.IsNullOrEmpty(DisplayNameOverride) ? baseItem.m_shared.m_name.ToLowerInvariant() : DisplayNameOverride;
        }

        public string GetRarityDisplay()
        {
            var color = GetColorByRarity(Rarity);
            return $"<color={color}>{Rarity.ToString()}</color>";
        }

        public string GetTooltip()
        {
            var color = GetColorByRarity(Rarity);
            var tooltip = $"<color={color}>\n";
            foreach (var effect in Effects)
            {
                tooltip += $"\n‣ {GetEffectText(effect)}";
            }
            tooltip += "</color>";
            return tooltip;
        }

        public Color GetColor()
        {
            if (ColorUtility.TryParseHtmlString(GetColorByRarity(Rarity), out Color color))
            {
                return color;
            }
            return Color.white;
        }

        public string GetColorString()
        {
            return GetColorByRarity(Rarity);
        }

        public Sprite GetBgSprite()
        {
            return GetBgSpriteByRarity(Rarity);
        }

        public List<MagicItemEffect> GetEffects(MagicEffectType effectType)
        {
            return Effects.Where(x => x.EffectType == effectType).ToList();
        }

        public List<MagicItemEffect> GetEffects(int effectType)
        {
            return Effects.Where(x => x.IntType == effectType).ToList();
        }

        public float GetTotalEffectValue(MagicEffectType effectType, float scale = 1)
        {
            return GetEffects(effectType).Sum(x => x.EffectValue) * scale;
        }

        public float GetTotalEffectValue(int effectType, float scale = 1)
        {
            return GetEffects(effectType).Sum(x => x.EffectValue) * scale;
        }

        public bool HasEffect(MagicEffectType effectType)
        {
            return Effects.Exists(x => x.EffectType == effectType);
        }

        public bool HasEffect(int effectType)
        {
            return Effects.Exists(x => x.IntType == effectType);
        }

        public bool HasAnyEffect(IEnumerable<MagicEffectType> effectTypes)
        {
            return Effects.Any(x => effectTypes.Contains(x.EffectType));
        }

        public bool HasAnyEffect(IEnumerable<int> effectTypes)
        {
            return Effects.Any(x => effectTypes.Contains(x.IntType));
        }

        private static string GetEffectText(MagicItemEffect effect)
        {
            var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
            return string.Format(effectDef.DisplayText, effect.EffectValue);
        }

        public static string GetColorByRarity(ItemRarity rarity)
        {
            return EpicLoot.GetRarityColor(rarity);
        }

        public static Sprite GetBgSpriteByRarity(ItemRarity rarity)
        {
            return EpicLoot.Assets.GenericItemBgSprite;
        }
    }
}
