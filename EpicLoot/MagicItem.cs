using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.LegendarySystem;
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

        public MagicItemEffect()
        {
        }

        public MagicItemEffect(string type, float value = 0)
        {
            EffectType = type;
            EffectValue = value;
        }
    }

    [Serializable]
    public class MagicItem
    {
        public int Version = 2;
        public ItemRarity Rarity;
        public List<MagicItemEffect> Effects = new List<MagicItemEffect>();
        public string TypeNameOverride;
        public int AugmentedEffectIndex = -1;
        public List<int> AugmentedEffectIndices = new List<int>();
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
                var bullet = IsEffectAugmented(index) ? "◇" : "◆";
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

        public List<MagicItemEffect> GetEffects(string effectType = null)
        {
            return effectType == null ? Effects.ToList() : Effects.Where(x => x.EffectType == effectType).ToList();
        }

        public float GetTotalEffectValue(string effectType, float scale = 1.0f)
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

        public static string GetEffectText(MagicItemEffectDefinition effectDef, float value)
        {
            var localizedDisplayText = Localization.instance.Localize(effectDef.DisplayText);
            var result = string.Format(localizedDisplayText, value);
            return result;
        }

        public static string GetEffectText(MagicItemEffect effect, ItemRarity rarity, bool showRange, string legendaryID, MagicItemEffectDefinition.ValueDef valuesOverride)
        {
            var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
            var result = GetEffectText(effectDef, effect.EffectValue);
            var values = valuesOverride ?? (string.IsNullOrEmpty(legendaryID) ? effectDef.GetValuesForRarity(rarity) : UniqueLegendaryHelper.GetLegendaryEffectValues(legendaryID, effect.EffectType));
            if (showRange && values != null)
            {
                if (!Mathf.Approximately(values.MinValue, values.MaxValue))
                {
                    result += $" [{values.MinValue}-{values.MaxValue}]";
                }
            }
            return result;
        }

        public static string GetEffectText(MagicItemEffect effect, ItemRarity rarity, bool showRange, string legendaryID = null)
        {
            return GetEffectText(effect, rarity, showRange, legendaryID, null);
        }

        public static string GetEffectText(MagicItemEffect effect, MagicItemEffectDefinition.ValueDef valuesOverride)
        {
            return GetEffectText(effect, ItemRarity.Legendary, false, null, valuesOverride);
        }

        public void ReplaceEffect(int index, MagicItemEffect newEffect)
        {
            if (index < 0 || index >= Effects.Count)
            {
                EpicLoot.LogError("Tried to replace effect on magic item outside of the range of the effects list!");
                return;
            }

            SetEffectAsAugmented(index);

            Effects[index] = newEffect;
        }

        public bool HasBeenAugmented()
        {
            return AugmentedEffectIndex >= 0 && AugmentedEffectIndex < Effects.Count || AugmentedEffectIndices.Count > 0;
        }

        public bool IsEffectAugmented(int index)
        {
            return AugmentedEffectIndex == index || AugmentedEffectIndices.Contains(index);
        }

        public void SetEffectAsAugmented(int index)
        {
            if (AugmentedEffectIndex == index)
            {
                return;
            }

            if (!IsEffectAugmented(index))
            {
                AugmentedEffectIndices.Add(index);
            }
        }

        public int GetAugmentCount()
        {
            var old = AugmentedEffectIndex >= 0 ? 1 : 0;
            return old + AugmentedEffectIndices.Count;
        }

        public bool IsUniqueLegendary()
        {
            return !string.IsNullOrEmpty(LegendaryID);
        }

        public LegendaryInfo GetLegendaryInfo()
        {
            if (IsUniqueLegendary())
            {
                UniqueLegendaryHelper.TryGetLegendaryInfo(LegendaryID, out var legendaryInfo);
                return legendaryInfo;
            }

            return null;
        }

        public string GetFirstEquipEffect(out FxAttachMode mode)
        {
            foreach (var effect in Effects)
            {
                var effectDef = MagicItemEffectDefinitions.Get(effect.EffectType);
                if (effectDef != null && !string.IsNullOrEmpty(effectDef.EquipFx))
                {
                    mode = effectDef.EquipFxMode;
                    return effectDef.EquipFx;
                }
            }

            mode = FxAttachMode.None;
            return null;
        }

        public bool IsLegendarySetItem()
        {
            return !string.IsNullOrEmpty(SetID);
        }
    }
}
