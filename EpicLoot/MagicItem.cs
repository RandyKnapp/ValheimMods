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
        public int Version = 1;
        public string EffectType { get; set; }
        public float EffectValue;
    }

    [Serializable]
    public class MagicItem
    {
        public ItemRarity Rarity;
        public List<MagicItemEffect> Effects = new List<MagicItemEffect>();
        public string TypeNameOverride;
        public int AugmentedEffectIndex = -1;
        public string DisplayName;
        public string LegendaryID;
        public string SetID;

        public string GetItemTypeName(ExtendedItemData baseItem)
        {
            return string.IsNullOrEmpty(TypeNameOverride) ? Localization.instance.Localize(baseItem.m_shared.m_name).ToLowerInvariant() : TypeNameOverride;
        }

        public string GetRarityDisplay()
        {
            var color = GetColorString();
            return $"<color={color}>{EpicLoot.GetRarityDisplayName(Rarity)}</color>";
        }

        public string GetTooltip()
        {
            var showRange = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            var color = GetColorString();
            var tooltip = $"<color={color}>\n";
            for (var index = 0; index < Effects.Count; index++)
            {
                var effect = Effects[index];
                var bullet = (HasBeenAugmented() && index == AugmentedEffectIndex) ? "◇" : "◆";
                tooltip += $"\n{bullet} {GetEffectText(effect, Rarity, showRange)}";
            }

            tooltip += "</color>";
            return tooltip;
        }

        public Color GetColor()
        {
            if (ColorUtility.TryParseHtmlString(GetColorString(), out Color color))
            {
                return color;
            }
            return Color.white;
        }

        public string GetColorString()
        {
            return EpicLoot.GetRarityColor(Rarity);
        }

        public List<MagicItemEffect> GetEffects(string effectType)
        {
            return Effects.Where(x => x.EffectType == effectType).ToList();
        }

        public float GetTotalEffectValue(string effectType, float scale = 1)
        {
            return GetEffects(effectType).Sum(x => x.EffectValue) * scale;
        }

        public bool HasEffect(string effectType)
        {
            return Effects.Exists(x => x.EffectType == effectType);
        }

        public bool HasAnyEffect(IEnumerable<string> effectTypes)
        {
            return Effects.Any(x => effectTypes.Contains(x.EffectType));
        }

        public static string GetEffectText(MagicItemEffect effect, ItemRarity rarity, bool showRange, string legendaryID = null)
        {
            var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
            var result = string.Format(effectDef.DisplayText, effect.EffectValue);
            var values = string.IsNullOrEmpty(legendaryID) ? effectDef.GetValuesForRarity(rarity) : LootRoller.GetLegendaryEffectValues(legendaryID, effect.EffectType);
            if (showRange && values != null)
            {
                if (!Mathf.Approximately(values.MinValue, values.MaxValue))
                {
                    result += $" [{values.MinValue}-{values.MaxValue}]";
                }
            }
            return result;
        }

        public void ReplaceEffect(int index, MagicItemEffect newEffect)
        {
            if (index < 0 || index >= Effects.Count)
            {
                EpicLoot.LogError("Tried to replace effect on magic item outside of the range of the effects list!");
                return;
            }

            if (HasBeenAugmented() && AugmentedEffectIndex != index)
            {
                EpicLoot.LogError($"Tried to replace an effect on index {index} but the player has already augmented this item at index {AugmentedEffectIndex}");
                return;
            }

            AugmentedEffectIndex = index;
            Effects[index] = newEffect;
        }

        public bool HasBeenAugmented()
        {
            return AugmentedEffectIndex >= 0 && AugmentedEffectIndex < Effects.Count;
        }

        public MagicItemEffect GetAugmentedEffect()
        {
            if (HasBeenAugmented())
            {
                return Effects[AugmentedEffectIndex];
            }

            return null;
        }
    }
}
