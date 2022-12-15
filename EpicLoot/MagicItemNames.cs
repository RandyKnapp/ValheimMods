using System;
using System.Collections.Generic;
using System.Globalization;
using Random = UnityEngine.Random;

namespace EpicLoot
{
    [Serializable]
    public class ItemNameEntry
    {
        public string Name = "";
        public List<ItemDrop.ItemData.ItemType> Types = new List<ItemDrop.ItemData.ItemType>();
        public List<Skills.SkillType> Skills = new List<Skills.SkillType>();
        public List<string> MagicEffects = new List<string>();
    }

    [Serializable]
    public class EpicItemNameConfig
    {
        public List<ItemNameEntry> Adjectives = new List<ItemNameEntry>();
        public List<ItemNameEntry> Names = new List<ItemNameEntry>();
    }

    [Serializable]
    public class RareItemNameConfig
    {
        public List<string> GenericPrefixes = new List<string>();
        public List<string> GenericSuffixes = new List<string>();
    }

    [Serializable]
    public class ItemNameConfig
    {
        public RareItemNameConfig Rare;
        public EpicItemNameConfig Epic;
        public List<ItemNameEntry> Legendary;
    }

    public static class MagicItemNames
    {
        public static ItemNameConfig Config;

        public static void Initialize(ItemNameConfig config)
        {
            Config = config;
        }

        public static string GetNameForItem(ItemDrop.ItemData item, MagicItem magicItem)
        {
            var baseName = TranslateAndCapitalize(item.m_shared.m_name);
            if (!EpicLoot.UseGeneratedMagicItemNames.Value || magicItem == null)
            {
                return null;
            }

            var rarity = magicItem.Rarity;
            switch (rarity)
            {
                case ItemRarity.Magic:
                    var magicFormat = Localization.instance.Localize("$mod_epicloot_basicmagicnameformat");
                    return string.Format(magicFormat, baseName);

                case ItemRarity.Rare:
                    var prefix = GetPrefix(magicItem);
                    var suffix = GetSuffix(magicItem);
                    var fullNameFormat = Localization.instance.Localize("$mod_epicloot_fullnameformat");

                    return string.Format(fullNameFormat, prefix, baseName, suffix);

                case ItemRarity.Epic:
                    return BuildEpicName(item, magicItem);

                case ItemRarity.Legendary:
                    return GetLegendaryName(item, magicItem);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string TranslateAndCapitalize(string baseName)
        {
            var name = Localization.instance.Localize(baseName);
            var capName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
            return capName;
        }

        public static object GetPrefix(MagicItem magicItem)
        {
            if (magicItem.Effects.Count == 0)
            {
                return null;
            }

            var firstEffect = magicItem.Effects[0];
            var effectDef = MagicItemEffectDefinitions.Get(firstEffect.EffectType);
            if (effectDef == null)
            {
                return null;
            }

            var prefixes = effectDef.Prefixes ?? Config.Rare.GenericPrefixes;
            var randomPrefix = GetRandomStringFromList(prefixes);

            // Include trailing space
            var format = Localization.instance.Localize("$mod_epicloot_prefixformat");
            return string.IsNullOrEmpty(randomPrefix) ? null : string.Format(format, randomPrefix);
        }

        public static object GetSuffix(MagicItem magicItem)
        {
            if (magicItem.Effects.Count < 2)
            {
                return null;
            }

            var secondEffect = magicItem.Effects[1];
            var effectDef = MagicItemEffectDefinitions.Get(secondEffect.EffectType);
            if (effectDef == null)
            {
                return null;
            }

            var suffixes = effectDef.Suffixes ?? Config.Rare.GenericSuffixes;
            var randomSuffix = GetRandomStringFromList(suffixes);

            // Include " of "
            var format = Localization.instance.Localize("$mod_epicloot_suffixformat");
            return string.IsNullOrEmpty(randomSuffix) ? null : string.Format(format, randomSuffix);
        }

        public static string BuildEpicName(ItemDrop.ItemData item, MagicItem magicItem)
        {
            var adjective = GetAdjectivePartForItem(item, magicItem);
            var name = GetNamePartForItem(item, magicItem);
            var format = Localization.instance.Localize("$mod_epicloot_epicnameformat");
            return string.Format(format, adjective, name);
        }

        public static string GetRandomStringFromList(List<string> list)
        {
            if (list == null || list.Count == 0)
            {
                return null;
            }

            return list[Random.Range(0, list.Count)];
        }

        public static string GetAdjectivePartForItem(ItemDrop.ItemData item, MagicItem magicItem)
        {
            var adjectives = GetAllowedNamesFromList(Config.Epic.Adjectives, item, magicItem);
            var adjectivePart = GetRandomStringFromList(adjectives);
            return string.IsNullOrEmpty(adjectivePart) ? EpicLoot.GetRarityDisplayName(ItemRarity.Epic) : adjectivePart;
        }

        public static string GetNamePartForItem(ItemDrop.ItemData item, MagicItem magicItem)
        {
            var names = GetAllowedNamesFromList(Config.Epic.Names, item, magicItem);
            var namePart = GetRandomStringFromList(names);
            return string.IsNullOrEmpty(namePart) ? TranslateAndCapitalize(item.m_shared.m_name) : namePart;
        }

        public static List<string> GetAllowedNamesFromList(List<ItemNameEntry> nameEntries, ItemDrop.ItemData item, MagicItem magicItem)
        {
            var results = new List<string>();

            foreach (var nameEntry in nameEntries)
            {
                var itemType = item.m_shared.m_itemType;
                if (nameEntry.Types.Count > 0 && nameEntry.Types.Contains(itemType))
                {
                    results.Add(nameEntry.Name);
                    continue;
                }

                if (IsValidTypeForSkillCheck(itemType) && nameEntry.Skills.Count > 0 && nameEntry.Skills.Contains(item.m_shared.m_skillType))
                {
                    results.Add(nameEntry.Name);
                    continue;
                }

                if (nameEntry.MagicEffects.Count > 0 && magicItem.HasAnyEffect(nameEntry.MagicEffects))
                {
                    results.Add(nameEntry.Name);
                    continue;
                }

                if (nameEntry.Types.Count == 0 && nameEntry.Skills.Count == 0 && nameEntry.MagicEffects.Count == 0)
                {
                    results.Add(nameEntry.Name);
                }
            }

            return results;
        }

        private static string GetLegendaryName(ItemDrop.ItemData item, MagicItem magicItem)
        {
            var allowedNames = GetAllowedNamesFromListAllRequired(Config.Legendary, item, magicItem);
            var name = GetRandomStringFromList(allowedNames);

            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            var format = Localization.instance.Localize("$mod_epicloot_basiclegendarynameformat");
            var baseName = TranslateAndCapitalize(item.m_shared.m_name);
            return string.Format(format, baseName);
        }

        public static List<string> GetAllowedNamesFromListAllRequired(List<ItemNameEntry> nameEntries, ItemDrop.ItemData item, MagicItem magicItem)
        {
            var results = new List<string>();

            foreach (var nameEntry in nameEntries)
            {
                var itemType = item.m_shared.m_itemType;
                if (nameEntry.Types.Count > 0 && !nameEntry.Types.Contains(itemType))
                {
                    continue;
                }

                if (IsValidTypeForSkillCheck(itemType) && nameEntry.Skills.Count > 0 && !nameEntry.Skills.Contains(item.m_shared.m_skillType))
                {
                    continue;
                }

                if (nameEntry.MagicEffects.Count > 0 && !magicItem.HasAnyEffect(nameEntry.MagicEffects))
                {
                    continue;
                }

                results.Add(nameEntry.Name);
            }

            return results;
        }

        // Everything that isn't a weapon or shield has Swords set as its skill... wtf
        public static bool IsValidTypeForSkillCheck(ItemDrop.ItemData.ItemType itemType)
        {
            return itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon
                   || itemType == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                   || itemType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft
                   || itemType == ItemDrop.ItemData.ItemType.Bow
                   || itemType == ItemDrop.ItemData.ItemType.Shield
                   || itemType == ItemDrop.ItemData.ItemType.Torch;
        }
    }
}
