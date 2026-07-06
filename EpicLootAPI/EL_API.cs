using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLootAPI;
/// <summary>
/// This section is outside of EpicLoot, added into external plugins
/// Either copy this file directly into your plugin, or add .dll into your libs
/// These functions reference methods inside of EpicLoot using reflection
/// </summary>
public static class EpicLoot
{
    private static readonly List<MagicItemEffectDefinition> MagicItemEffectDefinitions = new();
    private static readonly LegendaryItemConfig LegendaryConfig = new();
    private static readonly List<AbilityDefinition> Abilities = new();

    #region Magic Item
    [Serializable]
    public class MagicItemEffect
    {
        public const float DefaultValue = 1;

        public int Version = 1;
        public string EffectType = "";
        public float EffectValue;

        public MagicItemEffect()
        {
        }

        public MagicItemEffect(string type, float value = DefaultValue)
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
        public string TypeNameOverride = "";
        public int AugmentedEffectIndex = -1;
        public List<int> AugmentedEffectIndices = new List<int>();
        public string DisplayName = "";
        public string LegendaryID = "";
        public string SetID = "";
    }

    [Serializable]
    public class MagicItemEffectDefinition
    {
        [Serializable]
        public class ValueDef
        {
            public float MinValue;
            public float MaxValue;
            public float Increment;

            public ValueDef() { }

            public ValueDef(float min, float max, float increment)
            {
                MinValue = min;
                MaxValue = max;
                Increment = increment;
            }
        }

        [Serializable]
        public class ValuesPerRarityDef
        {
            public ValueDef? Magic;
            public ValueDef? Rare;
            public ValueDef? Epic;
            public ValueDef? Legendary;
            public ValueDef? Mythic;
        }

        public string Type;
        public string DisplayText;
        public string Description;
        public MagicItemEffectRequirements Requirements = new MagicItemEffectRequirements();
        public ValuesPerRarityDef ValuesPerRarity = new ValuesPerRarityDef();
        public float SelectionWeight = 1;
        public bool CanBeAugmented = true;
        public bool CanBeDisenchanted = true;
        public string Comment = "";
        public List<string> Prefixes = new List<string>();
        public List<string> Suffixes = new List<string>();
        public string EquipFx = "";
        public FxAttachMode EquipFxMode = FxAttachMode.Player;
        public string Ability = "";

        [Description("Adds your new magic item definition to a list, use RegisterMagicItems() to send to epic loot")]
        public MagicItemEffectDefinition(string effectType, string displayText = "", string description = "")
        {
            Type = effectType;
            DisplayText = displayText;
            Description = description;

            MagicItemEffectDefinitions.Add(this);
        }
    }

    [Serializable]
    public class MagicItemEffectRequirements
    {
        private static List<string> _flags = new List<string>();
        public bool NoRoll;
        public bool ExclusiveSelf = true;
        public List<string> ExclusiveEffectTypes = new List<string>();
        public List<string> MustHaveEffectTypes = new List<string>();
        public List<string> AllowedItemTypes = new List<string>();
        public List<string> ExcludedItemTypes = new List<string>();
        public List<ItemRarity> AllowedRarities = new List<ItemRarity>();
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
    }
    public enum FxAttachMode
    {
        None,
        Player,
        ItemRoot,
        EquipRoot
    }
    public enum ItemRarity
    {
        Magic,
        Rare,
        Epic,
        Legendary,
        Mythic
    }

    #endregion

    #region Legendary
    [Serializable]
    public class GuaranteedMagicEffect
    {
        public string Type;
        public MagicItemEffectDefinition.ValueDef? Values;

        public GuaranteedMagicEffect(string type, MagicItemEffectDefinition.ValueDef values)
        {
            Type = type;
            Values = values;
        }

        public GuaranteedMagicEffect(string type, float min, float max, float increment) : this(type, new MagicItemEffectDefinition.ValueDef(min, max, increment)) { }
    }

    [Serializable]
    public class TextureReplacement
    {
        public string ItemID;
        public string MainTexture;
        public string ChestTex;
        public string LegsTex;

        public TextureReplacement(string itemID, string mainTex = "", string chestTex = "", string legsTex = "")
        {
            ItemID = itemID;
            MainTexture = mainTex;
            ChestTex = chestTex;
            LegsTex = legsTex;
        }
    }

    [Serializable]
    public class LegendaryInfo
    {
        public string ID;
        public string Name;
        public string Description;
        public MagicItemEffectRequirements Requirements = new();
        public List<GuaranteedMagicEffect> GuaranteedMagicEffects = new List<GuaranteedMagicEffect>();
        public int GuaranteedEffectCount = -1;
        public float SelectionWeight = 1;
        public string EquipFx = "";
        public FxAttachMode EquipFxMode = FxAttachMode.Player;
        public List<TextureReplacement> TextureReplacements = new List<TextureReplacement>();
        public bool IsSetItem;
        public bool Enchantable;
        public List<RecipeRequirementConfig> EnchantCost = new List<RecipeRequirementConfig>();

        public LegendaryInfo(LegendaryType type, string ID, string name, string description)
        {
            this.ID = ID;
            Name = name;
            Description = description;

            switch (type)
            {
                case LegendaryType.Legend:
                    LegendaryConfig.LegendaryItems.Add(this);
                    break;
                case LegendaryType.Mythic:
                    LegendaryConfig.MythicItems.Add(this);
                    break;
            }
        }
    }

    public enum LegendaryType { Legend, Mythic }

    [Serializable]
    public class SetBonusInfo
    {
        public int Count;
        public GuaranteedMagicEffect Effect;

        public SetBonusInfo(int count, string type, MagicItemEffectDefinition.ValueDef values)
        {
            Count = count;
            Effect = new GuaranteedMagicEffect(type, values);
        }

        public SetBonusInfo(int count, string type, float min, float max, float increment) : this(count, type, new MagicItemEffectDefinition.ValueDef(min, max, increment)) { }
    }

    [Serializable]
    public class LegendarySetInfo
    {
        public string ID;
        public string Name;
        public List<string> LegendaryIDs = new List<string>();
        public List<SetBonusInfo> SetBonuses = new List<SetBonusInfo>();

        public LegendarySetInfo(LegendaryType type, string ID, string name)
        {
            this.ID = ID;
            Name = name;

            switch (type)
            {
                case LegendaryType.Legend:
                    LegendaryConfig.LegendarySets.Add(this);
                    break;
                case LegendaryType.Mythic:
                    LegendaryConfig.MythicSets.Add(this);
                    break;
            }
        }
    }

    [Serializable]
    public class LegendaryItemConfig
    {
        public List<LegendaryInfo> LegendaryItems = new List<LegendaryInfo>();
        public List<LegendarySetInfo> LegendarySets = new List<LegendarySetInfo>();
        public List<LegendaryInfo> MythicItems = new List<LegendaryInfo>();
        public List<LegendarySetInfo> MythicSets = new List<LegendarySetInfo>();
    }
    #endregion

    #region common
    [Serializable]
    public class RecipeRequirementConfig
    {
        public string item = "";
        public int amount = 1;
    }

    [Serializable]
    public class RecipeConfig
    {
        public string name = "";
        public string item = "";
        public int amount = 1;
        public string craftingStation = "";
        public int minStationLevel = 1;
        public bool enabled = true;
        public string repairStation = "";
        public List<RecipeRequirementConfig> resources = new List<RecipeRequirementConfig>();
    }

    [Serializable]
    public class RecipesConfig
    {
        public List<RecipeConfig> recipes = new List<RecipeConfig>();
    }
    #endregion

    #region Ability
    [Serializable]
    public enum AbilityActivationMode
    {
        Passive,
        Triggerable,
        Activated
    }

    [Serializable]
    public enum AbilityAction
    {
        Custom,
        StatusEffect
    }

    [Serializable]
    public class AbilityDefinition
    {
        public string ID;
        public string IconAsset; // Will need to tweak EpicLoot class to allow for custom icons to be passed
        public AbilityActivationMode ActivationMode;
        public float Cooldown;
        public AbilityAction Action;
        public List<string> ActionParams = new List<string>();

        public AbilityDefinition(string ID, string iconAsset, AbilityActivationMode mode, float cooldown, AbilityAction type)
        {
            this.ID = ID;
            IconAsset = iconAsset;
            ActivationMode = mode;
            Cooldown = cooldown;
            Action = type;

            Abilities.Add(this);
        }
    }

    #endregion

    public static void Add<T>(this List<T> list, params T[] items) => list.AddRange(items);

    public static void Add(this List<GuaranteedMagicEffect> list, string type, float min = 1, float max = 1,
        float increment = 1) => list.Add(new GuaranteedMagicEffect(type, min, max, increment));

    public static void Set(this MagicItemEffectDefinition.ValueDef? def, float min, float max, float increment)
    {
        def ??= new MagicItemEffectDefinition.ValueDef();
        def.MinValue = min;
        def.MaxValue = max;
        def.Increment = increment;
    }

    private static readonly Method API_AddMagicEffect = new("EpicLoot.API, EpicLoot", "AddMagicEffect");
    private static readonly Method API_GetTotalActiveMagicEffectValue = new("EpicLoot.API, EpicLoot", "GetTotalActiveMagicEffectValue");
    private static readonly Method API_GetTotalActiveMagicEffectValueForWeapon = new("EpicLoot.API, EpicLoot", "GetTotalActiveMagicEffectValueForWeapon");
    private static readonly Method API_HasActiveMagicEffect = new("EpicLoot.API, EpicLoot", "HasActiveMagicEffect");
    private static readonly Method API_HasActiveMagicEffectOnWeapon = new("EpicLoot.API, EpicLoot", "HasActiveMagicEffectOnWeapon");
    private static readonly Method API_GetTotalActiveSetEffectValue = new("EpicLoot.API, EpicLoot", "GetTotalActiveSetEffectValue");
    private static readonly Method API_GetMagicEffectDefinitionCopy = new("EpicLoot.API, EpicLoot", "GetMagicItemEffectDefinition");
    private static readonly Method API_GetAllActiveMagicEffects = new("EpicLoot.API, EpicLoot", "GetAllActiveMagicEffects");
    private static readonly Method API_GetAllSetMagicEffects = new("EpicLoot.API, EpicLoot", "GetAllActiveSetMagicEffects");
    private static readonly Method API_GetPlayerTotalActiveMagicEffectValue = new("EpicLoot.API, EpicLoot", "GetTotalPlayerActiveMagicEffectValue");
    private static readonly Method API_PlayerHasActiveMagicEffect = new("EpicLoot.API, EpicLoot", "PlayerHasActiveMagicEffect");
    private static readonly Method API_AddLegendaryItemConfig = new("EpicLoot.API, EpicLoot", "AddLegendaryItemConfig");
    private static readonly Method API_AddAbility = new("EpicLoot.API, EpicLoot", "AddAbility");
    private static readonly Method API_HasLegendaryItem = new("EpicLoot.API, EpicLoot", "HasLegendaryItem");
    private static readonly Method API_HasLegendarySet = new("EpicLoot.API, EpicLoot", "HasLegendarySet");

    public static MagicItemEffectDefinition? GetMagicEffectDefinitionCopy(string effectType)
    {
        string result = (string)(API_GetMagicEffectDefinitionCopy.Invoke(effectType) ?? "");
        if (string.IsNullOrEmpty(result)) return null;
        try
        {
            MagicItemEffectDefinition? copy = JsonConvert.DeserializeObject<MagicItemEffectDefinition>(result);
            return copy;
        }
        catch
        {
            Debug.LogWarning("[EpicLoot API] Failed to parse magic item effect definition json");
            return null;
        }
    }

    public static void RegisterMagicItems()
    {
        foreach (var item in new List<MagicItemEffectDefinition>(MagicItemEffectDefinitions)) AddMagicEffect(item);
    }

    [Description("Register a new magic effect")]
    public static bool AddMagicEffect(MagicItemEffectDefinition definition)
    {
        MagicItemEffectDefinitions.Remove(definition);
        string data = JsonConvert.SerializeObject(definition);
        object? result = API_AddMagicEffect.Invoke(data);

        return (bool)(result ?? false);
    }

    public static bool HasActiveMagicEffectOnWeapon(Player player, ItemDrop.ItemData item, string effectType, out float effectValue)
    {
        effectValue = 0f;
        object? output = API_HasActiveMagicEffectOnWeapon.Invoke(player, item, effectType, effectValue);

        return (bool)(output ?? false);
    }

    public static float GetTotalActiveMagicEffectValue(Player player, ItemDrop.ItemData item, string effectType,
        float scale = 1.0f)
    {
        object? result = API_GetTotalActiveMagicEffectValue.Invoke(player, item, effectType);
        return (float)(result ?? 0f);
    }

    public static float GetTotalActiveMagicEffectValueForWeapon(Player player, ItemDrop.ItemData item,
        string effectType, float scale = 1.0f)
    {
        object? result = API_GetTotalActiveMagicEffectValueForWeapon.Invoke(player, item, effectType, scale);
        return (float)(result ?? 0f);
    }

    public static bool HasActiveMagicEffect(Player player, ItemDrop.ItemData item, string effectType, float scale = 1.0f)
    {
        object? result = API_HasActiveMagicEffect.Invoke(player, item, effectType, scale);
        return (bool)(result ?? false);
    }

    public static float GetTotalActiveSetEffectValue(Player player, string effectType, float scale = 1.0f)
    {
        object? result = API_GetTotalActiveSetEffectValue.Invoke(player, effectType, scale);
        return (float)(result ?? 0f);
    }

    public static List<MagicItemEffect> GetAllActiveMagicEffects(this Player player, string? effectType = null)
    {
        object? result = API_GetAllActiveMagicEffects.Invoke(player, effectType);
        List<string> list = (List<string>)(result ?? new List<string>());
        if (list.Count <= 0) return new List<MagicItemEffect>();
        List<MagicItemEffect> output = new List<MagicItemEffect>();
        foreach (var item in list)
        {
            try
            {
                MagicItemEffect? magicItemEffect = JsonConvert.DeserializeObject<MagicItemEffect>(item);
                if (magicItemEffect == null) continue;
                output.Add(magicItemEffect);
            }
            catch
            {
                Debug.LogWarning("Failed to parse magic item effect");
            }
        }

        return output;
    }

    public static List<MagicItemEffect> GetAllActiveSetMagicEffects(this Player player, string? effectType = null)
    {
        object? result = API_GetAllSetMagicEffects.Invoke(player, effectType);
        List<string> list = (List<string>)(result ?? new List<string>());
        if (list.Count <= 0) return new List<MagicItemEffect>();
        List<MagicItemEffect> output = new List<MagicItemEffect>();
        foreach (var item in list)
        {
            try
            {
                MagicItemEffect? magicItemEffect = JsonConvert.DeserializeObject<MagicItemEffect>(item);
                if (magicItemEffect == null) continue;
                output.Add(magicItemEffect);
            }
            catch
            {
                Debug.LogWarning("[EpicLoot API] Failed to parse magic item effect");
            }
        }

        return output;
    }

    public static float GetTotalActiveMagicEffectValue(this Player player, string effectType, float scale = 1.0f,
        ItemDrop.ItemData? ignoreThisItem = null)
    {
        object? result = API_GetPlayerTotalActiveMagicEffectValue.Invoke(player, effectType, scale, ignoreThisItem);
        return (float)(result ?? 0f);
    }

    public static bool HasActiveMagicEffect(this Player player, string effectType, out float effectValue,
        float scale = 1.0f, ItemDrop.ItemData? ignoreThisItem = null)
    {
        effectValue = 0f;
        object? result = API_PlayerHasActiveMagicEffect.Invoke(player, effectType, effectValue, scale, ignoreThisItem);
        return (bool)(result ?? false);
    }

    public static bool RegisterLegendaryItems()
    {
        string data = JsonConvert.SerializeObject(LegendaryConfig);
        object? result = API_AddLegendaryItemConfig.Invoke(data);
        return (bool)(result ?? false);
    }

    public static void RegisterAbilities()
    {
        foreach (var ability in new List<AbilityDefinition>(Abilities))
        {
            AddAbility(ability);
        }
    }

    public static bool AddAbility(AbilityDefinition ability)
    {
        Abilities.Remove(ability);
        string data = JsonConvert.SerializeObject(ability);
        object? result = API_AddAbility.Invoke(data);
        return (bool)(result ?? false);
    }

    public static bool HasLegendaryItem(this Player player, string legendaryItemID)
    {
        var result = API_HasLegendaryItem.Invoke(player, legendaryItemID);
        return (bool)(result ?? false);
    }

    public static bool HasLegendarySet(this Player player, string legendarySetID)
    {
        var result = API_HasLegendarySet.Invoke(player, legendarySetID);
        return (bool)(result ?? false);
    }

    private class Method
    {
        private static readonly Dictionary<string, Type> CachedTypes = new();
        private readonly MethodInfo? info;

        public object? Invoke(params object?[] args) => info?.Invoke(null, args);

        public Method(string definition, string function)
        {
            if (!CachedTypes.TryGetValue(definition, out Type type))
            {
                if (Type.GetType(definition) is not { } source)
                {
                    Debug.LogWarning("[EpicLoot API] Failed to find type: " + definition);
                    return;
                }

                type = source;
                CachedTypes[definition] = source;
            }

            if (type == null)
            {
                Debug.LogWarning("[EpicLoot API] Failed to find type: " + definition);
                return;
            }
            info = type.GetMethod(function, BindingFlags.Public | BindingFlags.Static);
            if (info == null)
            {
                Debug.LogWarning("[EpicLoot API] Failed to find method: " + function);
            }
        }
    }
}