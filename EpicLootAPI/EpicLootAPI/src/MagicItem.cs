using JetBrains.Annotations;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

[Serializable]
[PublicAPI]
public class MagicItem
{
    public int Version;
    public ItemRarity Rarity;
    public List<MagicItemEffect> Effects = new();
    public string TypeNameOverride = "";
    public int AugmentedEffectIndex;
    public List<int> AugmentedEffectIndices = new();
    public string DisplayName = "";
    public string LegendaryID = "";
    public string SetID = "";
}