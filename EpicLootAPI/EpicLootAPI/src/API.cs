using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;

namespace EpicLootAPI;
public static class EpicLoot
{
    public static readonly Logger logger = new Logger();
    
    private static readonly Method API_GetTotalActiveMagicEffectValue = new ("GetTotalActiveMagicEffectValue");
    private static readonly Method API_GetTotalActiveMagicEffectValueForWeapon = new("GetTotalActiveMagicEffectValueForWeapon");
    private static readonly Method API_HasActiveMagicEffect = new("HasActiveMagicEffect");
    private static readonly Method API_HasActiveMagicEffectOnWeapon = new("HasActiveMagicEffectOnWeapon");
    private static readonly Method API_GetTotalActiveSetEffectValue = new("GetTotalActiveSetEffectValue");
    private static readonly Method API_GetAllActiveMagicEffects = new("GetAllActiveMagicEffects");
    private static readonly Method API_GetAllSetMagicEffects = new("GetAllActiveSetMagicEffects");
    private static readonly Method API_GetPlayerTotalActiveMagicEffectValue = new("GetTotalPlayerActiveMagicEffectValue");
    private static readonly Method API_PlayerHasActiveMagicEffect = new("PlayerHasActiveMagicEffect");
    private static readonly Method API_HasLegendaryItem = new("HasLegendaryItem");
    private static readonly Method API_HasLegendarySet = new("HasLegendarySet");
    private static readonly Method API_RegisterAsset = new("RegisterAsset");
    private static readonly Method API_GetMagicItemJson = new("GetMagicItemJson");

    [PublicAPI][Description("Send all your custom conversions, effects, item definitions, etc... to Epic Loot")]
    public static void RegisterAll()
    {
        MaterialConversion.RegisterAll();
        MagicItemEffectDefinition.RegisterAll();
        AbilityDefinition.RegisterAll();
        CustomRecipe.RegisterAll();
        Sacrifice.RegisterAll();
        TreasureMap.RegisterAll();
        SecretStashItem.RegisterAll();
        LegendaryInfo.RegisterAll();
        LegendarySetInfo.RegisterAll();
        BountyTarget.RegisterAll();
        AbilityProxyDefinition.RegisterAll();
    }
    
    /// <summary>
    /// Register asset into EpicLoot in order to target them in your definitions
    /// </summary>
    /// <param name="name"><see cref="string"/></param>
    /// <param name="asset"><see cref="Object"/></param>
    /// <returns></returns>
    [PublicAPI][Description("Register asset into EpicLoot in order to target them in your definitions")]
    public static bool RegisterAsset(string name, object asset)
    {
        object[] result = API_RegisterAsset.Invoke(name, asset);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Registered asset: {name}, {output}");
        return output;
    }
    
    /// <param name="player"></param>
    /// <param name="legendaryItemID"></param>
    /// <returns>true if player has legendary item</returns>
    [PublicAPI]
    public static bool HasLegendaryItem(this Player player, string legendaryItemID)
    {
        object[] result = API_HasLegendaryItem.Invoke(player, legendaryItemID);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Has legendary item: {legendaryItemID}, {output} ");
        return output;
    }

    /// <param name="player"></param>
    /// <param name="legendarySetID"></param>
    /// <param name="count"></param>
    /// <returns>true if player has full legendary set</returns>
    [PublicAPI]
    public static bool HasLegendarySet(this Player player, string legendarySetID, out int count)
    {
        count = 0;
        object[] result = API_HasLegendarySet.Invoke(player, legendarySetID, count);
        count = (int)(result[3] ?? 0);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Has legendary set: {legendarySetID}, {output}, count: {count}");
        return output;
    }

    /// <summary>
    /// ⚠️ Conditional behavior: Returns different results based on player parameter,
    /// </summary>
    /// <param name="player"></param>
    /// <param name="item"></param>
    /// <param name="effectType"><see cref="EffectType"/></param>
    /// <param name="effectValue"></param>
    /// <returns>
    /// Player null → Checks if item has effect,
    /// Player provided → Checks if player has effect 
    /// </returns>
    [PublicAPI]
    public static bool HasActiveMagicEffectOnWeapon(Player player, ItemDrop.ItemData item, string effectType, out float effectValue)
    {
        effectValue = 0f;
        object[] output = API_HasActiveMagicEffectOnWeapon.Invoke(player, item, effectType, effectValue);
        effectValue = (float)(output[4] ?? 0f);
        bool result = (bool)(output[0] ?? false);
        logger.LogDebug($"Has magic effect on weapon: {effectType}, {result}, value: {effectValue}");
        return result;
    }

    /// <summary>
    /// ⚠️ Conditional behavior: Returns different results based on player parameter,
    /// </summary>
    /// <param name="player"></param>
    /// <param name="item"></param>
    /// <param name="effectType"></param>
    /// <param name="scale"></param>
    /// <returns>
    /// Player null → returns item effect values,
    /// Player provided → returns player effect values
    /// </returns>
    [PublicAPI]
    public static float GetTotalActiveMagicEffectValue(Player player, ItemDrop.ItemData item, string effectType,
        float scale = 1.0f)
    {
        object[] result = API_GetTotalActiveMagicEffectValue.Invoke(player, item, effectType);
        float output = (float)(result[0] ?? 0f);
        logger.LogDebug($"Total magic effect value: {effectType}, amount: {output}");
        return output;
    }

    /// <summary>
    /// ⚠️ Conditional behavior: Returns different results based on player parameter
    /// </summary>
    /// <param name="player"></param>
    /// <param name="item"></param>
    /// <param name="effectType"></param>
    /// <param name="scale"></param>
    /// <returns>
    /// Player null → returns item effect values,
    /// Player provided → returns effect value without item
    /// </returns>
    [PublicAPI]
    public static float GetTotalActiveMagicEffectValueForWeapon(Player player, ItemDrop.ItemData item,
        string effectType, float scale = 1.0f)
    {
        object[] result = API_GetTotalActiveMagicEffectValueForWeapon.Invoke(player, item, effectType, scale);
        float output = (float)(result[0] ?? 0f);
        logger.LogDebug($"Total effect on weapon: {effectType}, amount: {output}");
        return output;
    }
    
    /// <param name="player"></param>
    /// <param name="effectType"><see cref="EffectType"/></param>
    /// <param name="ignoreThisItem"></param>
    /// <param name="scale"></param>
    /// <returns>true if player has effect</returns>
    [PublicAPI]
    public static bool HasActiveMagicEffect(Player player, string effectType, ItemDrop.ItemData ignoreThisItem = null, float scale = 1.0f)
    {
        object[] result = API_HasActiveMagicEffect.Invoke(player, ignoreThisItem, effectType, scale);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Has active effect: {effectType}, {output}");
        return output;
    }

    /// <summary>
    /// ⚠️ Currently hard-coded to ModifyArmor, 
    /// </summary>
    /// <param name="player"></param>
    /// <param name="effectType"><see cref="EffectType"/></param>
    /// <param name="scale"></param>
    /// <returns>total effect on set</returns>
    [PublicAPI]
    public static float GetTotalActiveSetEffectValue(Player player, string effectType, float scale = 1.0f)
    {
        object[] result = API_GetTotalActiveSetEffectValue.Invoke(player, effectType, scale);
        float output = (float)(result[0] ?? 0f);
        logger.LogDebug($"Total effect value: {effectType}, amount: {output}");
        return output;
    }
    
    /// <param name="player"></param>
    /// <param name="effectType"></param>
    /// <returns>list of effects on player</returns>
    [PublicAPI]
    public static List<MagicItemEffect> GetAllActiveMagicEffects(this Player player, string effectType = null)
    {
        object[] result = API_GetAllActiveMagicEffects.Invoke(player, effectType);
        List<string> list = (List<string>)(result[0] ?? new List<string>());

        if (list.Count <= 0)
        {
            return new List<MagicItemEffect>();
        }

        List<MagicItemEffect> output = new List<MagicItemEffect>();
        output.DeserializeList(list);
        return output;
    }
    
    /// <param name="player"></param>
    /// <param name="effectType"><see cref="EffectType"/></param>
    /// <returns>list of effects on active set</returns>
    [PublicAPI]
    public static List<MagicItemEffect> GetAllActiveSetMagicEffects(this Player player, string effectType = null)
    {
        object[] result = API_GetAllSetMagicEffects.Invoke(player, effectType);
        List<string> list = (List<string>)(result[0] ?? new List<string>());

        if (list.Count <= 0)
        {
            return new List<MagicItemEffect>();
        }

        List<MagicItemEffect> output = new List<MagicItemEffect>();
        output.DeserializeList(list);
        return output;
    }
    /// <summary>
    /// Helper function to JSON deserialize entire list
    /// </summary>
    /// <param name="output"></param>
    /// <param name="input"></param>
    /// <typeparam name="T"></typeparam>
    private static void DeserializeList<T>(this List<T> output, List<string> input)
    {
        foreach (string item in input)
        {
            try
            {
                T result = JsonConvert.DeserializeObject<T>(item);
                if (result == null)
                {
                    continue;
                }

                output.Add(result);
            }
            catch
            {
                logger.LogWarning($"Failed to parse {typeof(T).Name}");
            }
        }
    }
    
    /// <param name="player"></param>
    /// <param name="effectType"><see cref="EffectType"/></param>
    /// <param name="scale"></param>
    /// <param name="ignoreThisItem"></param>
    /// <returns>total effect value on player</returns>
    [PublicAPI]
    public static float GetTotalActiveMagicEffectValue(this Player player, string effectType, float scale = 1.0f,
        ItemDrop.ItemData ignoreThisItem = null)
    {
        object[] result = API_GetPlayerTotalActiveMagicEffectValue.Invoke(player, effectType, scale, ignoreThisItem);
        float output = (float)(result[0] ?? 0f);
        logger.LogDebug($"Total magic effect: {effectType}, value: {output}");
        return output;
    }
    
    /// <param name="player"></param>
    /// <param name="effectType"><see cref="EffectType"/></param>
    /// <param name="effectValue"></param>
    /// <param name="scale"></param>
    /// <param name="ignoreThisItem"></param>
    /// <returns>true if player has effect</returns>
    [PublicAPI]
    public static bool HasActiveMagicEffect(this Player player, string effectType, out float effectValue,
        float scale = 1.0f, ItemDrop.ItemData ignoreThisItem = null)
    {
        effectValue = 0f;
        object[] result = API_PlayerHasActiveMagicEffect.Invoke(player, effectType, effectValue, scale, ignoreThisItem);
        effectValue = (float)(result[3] ?? 0f);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Has active magic effect: {effectType}, value: {output}");
        return output;
    }
    
    /// <summary>
    /// Retrieves the MagicItem data for an ItemData by deserializing from JSON.
    /// JSON is used as an intermediate format to avoid direct type dependencies between EpicLoot and EpicLootAPI assemblies.
    /// </summary>
    /// <param name="itemData">The item to get magic item data from.</param>
    /// <returns>The MagicItem if it exists, otherwise null.</returns>
    [PublicAPI]
    public static MagicItem GetMagicItem(this ItemDrop.ItemData itemData)
    {
        object[] result = API_GetMagicItemJson.Invoke(itemData);
        string json = (string)(result[0] ?? "");

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonConvert.DeserializeObject<MagicItem>(json);
    }
}