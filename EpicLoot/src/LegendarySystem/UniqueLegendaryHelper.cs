using Common;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot.LegendarySystem
{
    public static class UniqueLegendaryHelper
    {
        internal static LegendaryItemConfig Config;
        public static readonly Dictionary<string, LegendaryInfo> LegendaryInfo = new Dictionary<string, LegendaryInfo>();
        public static readonly Dictionary<string, LegendarySetInfo> LegendarySets = new Dictionary<string, LegendarySetInfo>();
        public static readonly Dictionary<string, LegendaryInfo> MythicInfo = new Dictionary<string, LegendaryInfo>();
        public static readonly Dictionary<string, LegendarySetInfo> MythicSets = new Dictionary<string, LegendarySetInfo>();

        public static readonly Dictionary<string, LegendarySetInfo> LegendaryItemsToSetMap = new Dictionary<string, LegendarySetInfo>();
        public static readonly Dictionary<string, LegendarySetInfo> MythicItemsToSetMap = new Dictionary<string, LegendarySetInfo>();

        public static readonly LegendaryInfo GenericLegendaryInfo = new LegendaryInfo
        {
            ID = nameof(GenericLegendaryInfo)
        };
        
        public static event Action OnSetupLegendaryItemConfig;

        public static void Initialize(LegendaryItemConfig config)
        {
            Config = config;
            OnSetupLegendaryItemConfig?.Invoke();
            LegendaryInfo.Clear();
            AddLegendaryInfo(config.LegendaryItems);

            LegendarySets.Clear();
            LegendaryItemsToSetMap.Clear();
            AddLegendarySets(config.LegendarySets);

            MythicInfo.Clear();
            AddMythicInfo(config.MythicItems);

            MythicSets.Clear();
            MythicItemsToSetMap.Clear();
            AddMythicSets(config.MythicSets);
            
        }

        public static LegendaryItemConfig GetCFG()
        {
            return Config;
        }

        private static void AddLegendaryInfo([NotNull] IEnumerable<LegendaryInfo> legendaryItems)
        {
            foreach (var legendaryInfo in legendaryItems)
            {
                if (!LegendaryInfo.ContainsKey(legendaryInfo.ID))
                {
                    LegendaryInfo.Add(legendaryInfo.ID, legendaryInfo);
                }
                else
                {
                    EpicLoot.LogWarning($"Duplicate entry found for LegendaryInfo: {legendaryInfo.ID}. " +
                        $"Please fix your configuration.");
                }
            }
        }

        private static void AddMythicInfo([NotNull] IEnumerable<LegendaryInfo> mythicItems)
        {
            foreach (var mythicInfo in mythicItems)
            {
                if (!MythicInfo.ContainsKey(mythicInfo.ID))
                {
                    MythicInfo.Add(mythicInfo.ID, mythicInfo);
                }
                else
                {
                    EpicLoot.LogWarning($"Duplicate entry found for MythicInfo: {mythicInfo.ID}. " +
                        $"Please fix your configuration.");
                }
            }
        }

        private static void AddLegendarySets(List<LegendarySetInfo> legendarySets)
        {
            foreach (var legendarySetInfo in legendarySets)
            {
                if (!LegendarySets.ContainsKey(legendarySetInfo.ID))
                {
                    LegendarySets.Add(legendarySetInfo.ID, legendarySetInfo);
                }
                else
                {
                    EpicLoot.LogWarning($"Duplicate entry found for LegendarySetInfo: {legendarySetInfo.ID}. " +
                        $"Please fix your configuration.");
                    continue;
                }

                foreach (var legendaryID in legendarySetInfo.LegendaryIDs)
                {
                    if (!LegendaryItemsToSetMap.ContainsKey(legendaryID))
                    {
                        LegendaryItemsToSetMap.Add(legendaryID, legendarySetInfo);
                    }
                    else
                    {
                        EpicLoot.LogWarning($"Duplicate entry found for LegendarySet {legendarySetInfo.ID}: {legendaryID}. " +
                            $"Please fix your configuration.");
                    }
                }
            }
        }

        private static void AddMythicSets(List<LegendarySetInfo> mythicSets)
        {
            foreach (var mythicSetInfo in mythicSets)
            {
                if (!MythicSets.ContainsKey(mythicSetInfo.ID))
                {
                    MythicSets.Add(mythicSetInfo.ID, mythicSetInfo);
                }
                else
                {
                    EpicLoot.LogWarning($"Duplicate entry found for MythicSetInfo: {mythicSetInfo.ID}. " +
                        $"Please fix your configuration.");
                    continue;
                }

                foreach (var mythicID in mythicSetInfo.LegendaryIDs)
                {
                    if (!MythicItemsToSetMap.ContainsKey(mythicID))
                    {
                        MythicItemsToSetMap.Add(mythicID, mythicSetInfo);
                    }
                    else
                    {
                        EpicLoot.LogWarning($"Duplicate entry found for MythicSet {mythicSetInfo.ID}: {mythicID}. " +
                            $"Please fix your configuration.");
                    }
                }
            }
        }

        public static bool TryGetLegendaryInfo(string legendaryID, out LegendaryInfo legendaryInfo)
        {
            if (MythicInfo.TryGetValue(legendaryID, out legendaryInfo))
            {
                return true;
            }

            return LegendaryInfo.TryGetValue(legendaryID, out legendaryInfo);
        }

        public static bool IsGenericLegendary(LegendaryInfo legendaryInfo)
        {
            return legendaryInfo == GenericLegendaryInfo;
        }

        public static IList<LegendaryInfo> GetAvailableLegendaries(ItemDrop.ItemData baseItem, MagicItem magicItem, bool rollSetItem)
        {
            var availableLegendaries = LegendaryInfo.Values
                .Where(x => x.IsSetItem == rollSetItem && x.Requirements.CheckRequirements(baseItem, magicItem))
                .AddItem(GenericLegendaryInfo).ToList();
            if (rollSetItem && availableLegendaries.Count > 1)
            {
                availableLegendaries.Remove(UniqueLegendaryHelper.GenericLegendaryInfo);
            }

            return availableLegendaries;
        }

        public static IList<LegendaryInfo> GetAvailableMythics(ItemDrop.ItemData baseItem, MagicItem magicItem, bool rollSetItem)
        {
            var availableMythics = MythicInfo.Values
                .Where(x => x.IsSetItem == rollSetItem && x.Requirements.CheckRequirements(baseItem, magicItem))
                .AddItem(GenericLegendaryInfo).ToList();
            if (rollSetItem && availableMythics.Count > 1)
            {
                availableMythics.Remove(UniqueLegendaryHelper.GenericLegendaryInfo);
            }

            return availableMythics;
        }

        public static MagicItemEffectDefinition.ValueDef GetLegendaryEffectValues(string legendaryID, string effectType)
        {
            if (MythicInfo.TryGetValue(legendaryID, out var mythicInfo))
            {
                if (mythicInfo.GuaranteedMagicEffects.TryFind(x => x.Type == effectType, out var guaranteedMagicEffect))
                {
                    return guaranteedMagicEffect.Values;
                }
            }
            else if (LegendaryInfo.TryGetValue(legendaryID, out var legendaryInfo))
            {
                if (legendaryInfo.GuaranteedMagicEffects.TryFind(x => x.Type == effectType, out var guaranteedMagicEffect))
                {
                    return guaranteedMagicEffect.Values;
                }
            }

            return null;
        }

        public static bool TryGetLegendarySetInfo(string setID, out LegendarySetInfo legendarySetInfo, out ItemRarity rarity)
        {
            if (string.IsNullOrEmpty(setID))
            {
                legendarySetInfo = null;
                rarity = ItemRarity.Magic;
                return false;
            }

            if (MythicSets.TryGetValue(setID, out legendarySetInfo))
            {
                rarity = ItemRarity.Mythic;
                return true;
            }

            rarity = ItemRarity.Legendary;
            return LegendarySets.TryGetValue(setID, out legendarySetInfo);
        }

        public static string GetSetForLegendaryItem(LegendaryInfo legendary)
        {
            if (legendary != null && legendary.IsSetItem && !string.IsNullOrEmpty(legendary.ID))
            {
                if (MythicItemsToSetMap.TryGetValue(legendary.ID, out var mythicSetInfo))
                {
                    return mythicSetInfo.ID;
                }
                else if (LegendaryItemsToSetMap.TryGetValue(legendary.ID, out var setInfo))
                {
                    return setInfo.ID;
                }
            }

            return null;
        }
    }
}
