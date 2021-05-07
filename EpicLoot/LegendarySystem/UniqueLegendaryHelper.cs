using System.Collections.Generic;
using System.Linq;
using Common;
using ExtendedItemDataFramework;
using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.LegendarySystem
{
    public static class UniqueLegendaryHelper
    {
        public static readonly Dictionary<string, LegendaryInfo> LegendaryInfo = new Dictionary<string, LegendaryInfo>();

        private static readonly LegendaryInfo GenericLegendaryInfo = new LegendaryInfo
        {
            ID = nameof(GenericLegendaryInfo)
        };

        public static void Initialize(LegendaryItemConfig config)
        {
            LegendaryInfo.Clear();
            AddLegendaryInfo(config.LegendaryItems);
        }

        private static void AddLegendaryInfo([NotNull] IEnumerable<LegendaryInfo> legendaryItems)
        {
            foreach (var legendaryInfo in legendaryItems)
            {
                LegendaryInfo.Add(legendaryInfo.ID, legendaryInfo);
            }
        }

        public static bool TryGetLegendaryInfo(string legendaryID, out LegendaryInfo legendaryInfo)
        {
            return LegendaryInfo.TryGetValue(legendaryID, out legendaryInfo);
        }

        public static bool IsGenericLegendary(LegendaryInfo legendaryInfo)
        {
            return legendaryInfo == GenericLegendaryInfo;
        }

        public static IEnumerable<LegendaryInfo> GetAvailableLegendaries(ExtendedItemData baseItem, MagicItem magicItem)
        {
            return LegendaryInfo.Values.Where(x => x.Requirements.CheckRequirements(baseItem, magicItem)).AddItem(GenericLegendaryInfo);
        }

        public static MagicItemEffectDefinition.ValueDef GetLegendaryEffectValues(string legendaryID, string effectType)
        {
            if (LegendaryInfo.TryGetValue(legendaryID, out var legendaryInfo))
            {
                if (legendaryInfo.GuaranteedMagicEffects.TryFind(x => x.Type == effectType, out var guaranteedMagicEffect))
                {
                    return guaranteedMagicEffect.Values;
                }
            }
            return null;
        }
    }
}
