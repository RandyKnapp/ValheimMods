using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

[Serializable]
[PublicAPI]
public class GuaranteedMagicEffect
{
    public string Type = "";
    public ValueDef Values = new();
    /// <summary>
    /// Optional per-rarity values, for uniques and sets that roll at several rarities. A rarity left
    /// unset uses <see cref="Values"/>. Needs an Epic Loot with per-rarity uniques; older ones ignore it.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public RarityValueOverrides? ValuesPerRarity;
    public GuaranteedMagicEffect(string type, ValueDef values)
    {
        Type = type;
        Values = values;
    }
    public GuaranteedMagicEffect(string type, float min = 1, float max = 1, float increment = 1) : this(type, new ValueDef(min, max, increment)){}

    public GuaranteedMagicEffect(){}

    /// <summary>Sets the value range this effect uses at <paramref name="rarity"/>.</summary>
    public GuaranteedMagicEffect SetValuesForRarity(ItemRarity rarity, float min, float max, float increment = 1)
    {
        ValuesPerRarity ??= new RarityValueOverrides();
        ValuesPerRarity.Set(rarity, new ValueDef(min, max, increment));
        return this;
    }
}

/// <summary>
/// Per-rarity value ranges where only the rarities that are set are sent, unlike
/// <see cref="ValuesPerRarityDef"/>, whose every rarity defaults to 0/0/0.
/// </summary>
[Serializable]
[PublicAPI]
public class RarityValueOverrides
{
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ValueDef? Magic;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ValueDef? Rare;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ValueDef? Epic;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ValueDef? Legendary;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ValueDef? Mythic;
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)] public ValueDef? Ancient;

    public void Set(ItemRarity rarity, ValueDef values)
    {
        switch (rarity)
        {
            case ItemRarity.Magic: Magic = values; break;
            case ItemRarity.Rare: Rare = values; break;
            case ItemRarity.Epic: Epic = values; break;
            case ItemRarity.Legendary: Legendary = values; break;
            case ItemRarity.Mythic: Mythic = values; break;
            case ItemRarity.Ancient: Ancient = values; break;
        }
    }
}

[Serializable]
[PublicAPI]
public class TextureReplacement
{
    public string ItemID = "";
    public string MainTexture = "";
    public string ChestTex = "";
    public string LegsTex = "";

    public TextureReplacement(string itemID, string mainTex = "", string chestTex = "", string legsTex = "")
    {
        ItemID = itemID;
        MainTexture = mainTex;
        ChestTex = chestTex;
        LegsTex = legsTex;
    }
    
    public TextureReplacement(){}
}

[Serializable]
[PublicAPI]
public class LegendaryInfo
{
    public string ID = "";
    public string Name = "";
    public string Description = "";
    /// <summary>The rarities this unique rolls at. Empty means the rarity it was registered with.</summary>
    public List<ItemRarity> Rarities = new List<ItemRarity>();
    public MagicItemEffectRequirements Requirements = new ();
    public List<GuaranteedMagicEffect> GuaranteedMagicEffects = new List<GuaranteedMagicEffect>();
    public int GuaranteedEffectCount = -1;
    public float SelectionWeight = 1;
    public string EquipFx = "";
    public FxAttachMode EquipFxMode = FxAttachMode.Player;
    public List<TextureReplacement> TextureReplacements = new List<TextureReplacement>();
    public bool IsSetItem;
    public bool Enchantable;
    public List<RecipeRequirement> EnchantCost = new List<RecipeRequirement>();

    public LegendaryInfo(LegendaryType type, string ID, string name, string description)
    {
        this.ID = ID;
        Name = name;
        Description = description;
        this.type = type.ToString();
        LegendaryItems.Add(this);
    }

    /// <summary>A unique that rolls at every rarity in <paramref name="rarities"/>.</summary>
    public LegendaryInfo(string ID, string name, string description, params ItemRarity[] rarities)
    {
        this.ID = ID;
        Name = name;
        Description = description;
        Rarities.AddRange(rarities);
        // Registered under its first rarity, so an Epic Loot without per-rarity uniques still takes it.
        type = rarities.Length > 0 ? rarities[0].ToString() : nameof(LegendaryType.Legendary);
        LegendaryItems.Add(this);
    }

    public LegendaryInfo(){}

    private string type = nameof(LegendaryType.Legendary);

    internal static readonly List<LegendaryInfo> LegendaryItems = new();
    internal static readonly Method API_AddLegendaryItem = new ("AddLegendaryItem");
    internal static readonly Method API_UpdateLegendaryItem = new ("UpdateLegendaryItem");

    public static void RegisterAll()
    {
        foreach (var item in new List<LegendaryInfo>(LegendaryItems))
        {
            item.Register();
        }
    }

    public bool Register()
    {
        string data = JsonConvert.SerializeObject(this);
        object[] result = API_AddLegendaryItem.Invoke(type, data);
        if (result[0] is not string key) return false;
        RunTimeRegistry.Register(this, key);
        LegendaryItems.Remove(this);
        EpicLoot.logger.LogDebug($"Registered legendary item: {ID}");
        return true;
    }

    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out string key)) return false;
        string data = JsonConvert.SerializeObject(this);
        object[] result = API_UpdateLegendaryItem.Invoke(key, data);
        var output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated legendary item: {ID}, {output}");
        return output;
    }
}

[PublicAPI]
public enum LegendaryType
{
    Legendary,
    Mythic
}

[Serializable]
[PublicAPI]
public class SetBonusInfo
{
    public int Count;
    public GuaranteedMagicEffect Effect = new();

    public SetBonusInfo(int count, string type, ValueDef values)
    {
        Count = count;
        Effect = new GuaranteedMagicEffect(type, values);
    }

    public SetBonusInfo(int count, string type, float min, float max, float increment) : this (count, type, new ValueDef(min, max, increment)){}
    
    public SetBonusInfo(){}
}

[Serializable]
[PublicAPI]
public class LegendarySetInfo
{
    public string ID = "";
    public string Name = "";
    /// <summary>The rarities the set's pieces roll at. Empty means the rarity it was registered with.</summary>
    public List<ItemRarity> Rarities = new List<ItemRarity>();
    public List<string> LegendaryIDs = new List<string>();
    public List<SetBonusInfo> SetBonuses = new List<SetBonusInfo>();

    public LegendarySetInfo(LegendaryType type, string ID, string name)
    {
        this.ID = ID;
        Name = name;
        this.type = type.ToString();
        LegendarySets.Add(this);
    }

    /// <summary>A set whose pieces roll at every rarity in <paramref name="rarities"/>. The pieces count
    /// together at mixed rarities; each bonus uses the rarity enough pieces reach.</summary>
    public LegendarySetInfo(string ID, string name, params ItemRarity[] rarities)
    {
        this.ID = ID;
        Name = name;
        Rarities.AddRange(rarities);
        type = rarities.Length > 0 ? rarities[0].ToString() : nameof(LegendaryType.Legendary);
        LegendarySets.Add(this);
    }

    public LegendarySetInfo(){}

    private string type = nameof(LegendaryType.Legendary);
    internal static readonly List<LegendarySetInfo> LegendarySets = new();
    internal static readonly Method API_AddLegendarySet = new ("AddLegendarySet");
    internal static readonly Method API_UpdateLegendarySet = new ("UpdateLegendarySet");
    
    public static void RegisterAll()
    {
        foreach (var set in new List<LegendarySetInfo>(LegendarySets))
        {
            set.Register();
        }
    }

    public bool Register()
    {
        string data = JsonConvert.SerializeObject(this);
        object[] result = API_AddLegendarySet.Invoke(type, data);

        if (result[0] is not string key)
        {
            return false;
        }

        RunTimeRegistry.Register(this, key);
        EpicLoot.logger.LogDebug($"Registered legendary set: {ID}");
        return true;
    }

    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out string key))
        {
            return false;
        }

        string data = JsonConvert.SerializeObject(this);   
        object[] result = API_UpdateLegendarySet.Invoke(key, data);
        bool output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated legendary set: {ID}, {output}");
        return output;
    }
}