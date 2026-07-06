using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EpicLootAPI;

[PublicAPI]
public enum FxAttachMode
{
    None,
    Player,
    ItemRoot,
    EquipRoot
}

[PublicAPI]
public enum ItemRarity
{
    Magic,
    Rare,
    Epic,
    Legendary,
    Mythic
}

[Serializable]
[PublicAPI]
public class MagicItemEffect
{
    public int Version = 1;
    public string EffectType = "";
    public float EffectValue;

    public MagicItemEffect(string type, float value = 1)
    {
        EffectType = type;
        EffectValue = value;
    }
    
    public MagicItemEffect(){}
}

[Serializable]
[PublicAPI]
public class ValueDef
{
    public float MinValue;
    public float MaxValue;
    public float Increment; // 0 means it does not roll, step from min to max

    public ValueDef(){}

    public ValueDef(float min, float max, float increment)
    {
        MinValue = min;
        MaxValue = max;
        Increment = increment;
    }

    public void Set(float min, float max, float increment = 1)
    {
        MinValue = min;
        MaxValue = max;
        Increment = increment;
    }
}

[Serializable]
[PublicAPI]
public class ValuesPerRarityDef
{
    public ValueDef Magic = new();
    public ValueDef Rare = new();
    public ValueDef Epic = new();
    public ValueDef Legendary = new();
    public ValueDef Mythic = new();
}

[Serializable]
[PublicAPI]
public class MagicItemEffectDefinition
{
    public string Type = "";
    public string DisplayText = "";
    public string Description = "";
    public MagicItemEffectRequirements Requirements = new MagicItemEffectRequirements();
    public ValuesPerRarityDef ValuesPerRarity = new ValuesPerRarityDef();
    public float SelectionWeight = 1f;
    public bool CanBeAugmented = true;
    public bool CanBeDisenchanted = true;
    public string Comment = "";
    public List<string> Prefixes = new List<string>();
    public List<string> Suffixes = new List<string>();
    public string EquipFx = "";
    public FxAttachMode EquipFxMode = FxAttachMode.Player;
    public string Ability = "";

    [Description("Adds your new magic item definition to a list, use Register to send to epic loot")]
    public MagicItemEffectDefinition(string effectType, string displayText = "", string description = "")
    {
        Type = effectType;
        DisplayText = displayText;
        Description = description;

        MagicEffects.Add(this);
    }
    
    public MagicItemEffectDefinition(){}

    internal static readonly List<MagicItemEffectDefinition> MagicEffects = new();
    internal static readonly Method API_AddMagicEffect = new("AddMagicEffect");
    internal static readonly Method API_UpdateMagicEffect = new ("UpdateMagicEffect");
    internal static readonly Method API_GetMagicEffectDefinitionCopy = new("GetMagicItemEffectDefinition");

    public static void RegisterAll()
    {
        foreach (MagicItemEffectDefinition effect in new List<MagicItemEffectDefinition>(MagicEffects))
        {
            effect.Register();
        }
    }

    /// <param name="effectType"><see cref="EffectType"/></param>
    /// <returns>simple copy of existing magic effect definition</returns>
    public static MagicItemEffectDefinition Copy(string effectType)
    {
        string result = (string)(API_GetMagicEffectDefinitionCopy.Invoke(effectType)[0] ?? "");
        if (string.IsNullOrEmpty(result))
        {
            return null;
        }

        try
        {
            MagicItemEffectDefinition copy = JsonConvert.DeserializeObject<MagicItemEffectDefinition>(result);
            return copy;
        }
        catch
        {
            EpicLoot.logger.LogWarning("Failed to parse magic item effect definition json");
            return null;
        }
    }

    /// <summary>
    /// Serialized to JSON and invokes <see cref="API_AddMagicEffect"/>
    /// </summary>
    /// <returns>true if added</returns>
    public bool Register()
    {
        MagicEffects.Remove(this);
        string data = JsonConvert.SerializeObject(this, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Include });
        object[] result = API_AddMagicEffect.Invoke(data);

        if (result[0] is not string key)
        {
            return false;
        }

        RunTimeRegistry.Register(this, key);
        EpicLoot.logger.LogDebug($"Registered magic effect: {Type}");
        return true;
    }

    /// <summary>
    /// Serialized to JSON and invokes <see cref="API_UpdateMagicEffect"/>
    /// </summary>
    /// <returns>true if updated</returns>
    public bool Update()
    {
        if (!RunTimeRegistry.TryGetValue(this, out string key))
        {
            return false;
        }

        string json = JsonConvert.SerializeObject(this);
        object[] result = API_UpdateMagicEffect.Invoke(key, json);
        var output = (bool)(result[0] ?? false);
        EpicLoot.logger.LogDebug($"Updated magic effect: {Type}, {output}");
        return output;
    }
}

[Serializable][PublicAPI]
public class MagicItemEffectRequirements
{
    private static List<string> _flags = new List<string>();
    public bool NoRoll;
    public bool ExclusiveSelf = true;
    public List<string> ExclusiveEffectTypes = new List<string>(); // if empty, no exclusives
    public List<string> MustHaveEffectTypes = new List<string>(); // if empty, no must haves
    public List<string> AllowedItemTypes = new List<string>(); // if empty, all allowed
    public List<string> ExcludedItemTypes = new List<string>(); // if empty, no excluded
    public List<ItemRarity> AllowedRarities = new List<ItemRarity>();// etc...
    public List<ItemRarity> ExcludedRarities = new List<ItemRarity>();
    public List<Skills.SkillType> AllowedSkillTypes = new List<Skills.SkillType>();
    public List<Skills.SkillType> ExcludedSkillTypes = new List<Skills.SkillType>();
    public List<string> AllowedItemNames = new List<string>();
    public List<string> ExcludedItemNames = new List<string>();
    public bool? ItemHasPhysicalDamage;
    public bool? ItemHasElementalDamage;
    public bool? ItemHasChopDamage;
    public bool? ItemUsesDurability;
    public bool? ItemHasNegativeMovementSpeedModifier;
    public bool? ItemHasBlockPower;
    public bool? ItemHasParryPower;
    public bool? ItemHasNoParryPower;
    public bool? ItemHasArmor;
    public bool? ItemHasBackstabBonus;
    public bool? ItemUsesStaminaOnAttack;
    public bool? ItemUsesEitrOnAttack;
    public bool? ItemUsesHealthOnAttack;
    public bool? ItemUsesDrawStaminaOnAttack;

    public List<string> CustomFlags = new();

    public void AddAllowedItemTypes(params ItemDrop.ItemData.ItemType[] types)
    {
        foreach (ItemDrop.ItemData.ItemType type in types)
        {
            AllowedItemTypes.Add(type.ToString());
        }
    }

    public void AddExcludedItemTypes(params ItemDrop.ItemData.ItemType[] types)
    {
        foreach (ItemDrop.ItemData.ItemType type in types)
        {
            ExcludedItemTypes.Add(type.ToString());
        }
    }
}