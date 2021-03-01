using System;
using System.Collections.Generic;
using System.Diagnostics;
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

    public enum MagicEffectType
    {
        ModifyDamage
    }

    [Serializable]
    public class MagicItemEffect
    {
        public MagicEffectType EffectType;
        public float EffectValue;
    }

    [Serializable]
    public class MagicItem
    {
        public ItemRarity Rarity;
        public List<MagicItemEffect> Effects = new List<MagicItemEffect>();

        public string GetDisplayName(string itemName)
        {
            var color = GetColorByRarity(Rarity);
            return $"<color={color}>{itemName}</color>";
        }

        public string GetTooltip()
        {
            var color = GetColorByRarity(Rarity);
            var tooltip = $"<color={color}>";
            tooltip += $"\n\n{Rarity.ToString()}";
            foreach (var effect in Effects)
            {
                tooltip += $"\n{GetEffectText(effect)}";
            }
            tooltip += "</color>";
            return tooltip;
        }

        private static string GetEffectText(MagicItemEffect effect)
        {
            var value = effect.EffectValue;
            var intValue = (int)effect.EffectValue;
            var percentValue = Mathf.Round(effect.EffectValue * 200.0f) / 2;
            switch (effect.EffectType)
            {
                case MagicEffectType.ModifyDamage:
                    return $"Increase damage by {value}%";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string GetColorByRarity(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Magic: // Blue
                    return "#7386ff";
                case ItemRarity.Rare: // Yellow
                    return "#ffff75";
                case ItemRarity.Epic: // Purple
                    return "#ab34eb";
                case ItemRarity.Legendary: // Magenta
                    return "#e83180";
                default:
                    throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null);
            }
        }
    }
}
