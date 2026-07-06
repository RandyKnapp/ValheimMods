using Common;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLoot;

public static partial class API
{
    private static bool ShowLogs = false;
    private static event Action<string> OnReload;
    private static event Action<string> OnError;
    private static event Action<string> OnDebug;
    
    private static readonly Dictionary<string, MagicItemEffectDefinition> ExternalMagicItemEffectDefinitions = new();
    private static readonly Dictionary<string, AbilityDefinition> ExternalAbilities = new();
    private static readonly Dictionary<ItemRarity, List<LegendaryInfo>> ExternalLegendaryItems = new();
    private static readonly Dictionary<ItemRarity, List<LegendarySetInfo>> ExternalLegendarySets = new();
    private static readonly Dictionary<string, UnityEngine.Object> ExternalAssets = new();
    private static readonly List<MaterialConversion> ExternalMaterialConversions = new();
    private static readonly List<RecipeConfig> ExternalRecipes = new();
    private static readonly List<DisenchantProductsConfig> ExternalSacrifices = new();
    private static readonly List<BountyTargetConfig> ExternalBountyTargets = new();
    private static readonly Dictionary<SecretStashType, List<SecretStashItemConfig>> ExternalSecretStashItems = new();
    private static readonly List<TreasureMapBiomeInfoConfig> ExternalTreasureMaps = new();
    private static readonly Dictionary<string, Dictionary<string, Delegate>> AbilityProxies = new();

    /// <summary>
    /// Static constructor, runs automatically once before the API class is first used.
    /// </summary>
    static API()
    {
        MagicItemEffectDefinitions.OnSetupMagicItemEffectDefinitions += ReloadExternalMagicEffects;
        UniqueLegendaryHelper.OnSetupLegendaryItemConfig += ReloadExternalLegendary;
        AbilityDefinitions.OnSetupAbilityDefinitions += ReloadExternalAbilities;
        MaterialConversions.OnSetupMaterialConversions += ReloadExternalMaterialConversions;
        RecipesHelper.OnSetupRecipeConfig += ReloadExternalRecipes;
        EnchantCostsHelper.OnSetupEnchantingCosts += ReloadExternalSacrifices;
        AdventureDataManager.OnSetupAdventureData += ReloadExternalAdventureData;

        OnReload += message =>
        {
            if (!ShowLogs) return;
            EpicLoot.Log(message);
        };
        OnError += message =>
        {
            if (!ShowLogs) return;
            EpicLoot.LogWarning(message);
        };
        OnDebug += message =>
        {
            if (!ShowLogs) return;
            EpicLoot.LogWarningForce(message);
        };
    }

    /// <param name="name"><see cref="string"/></param>
    /// <param name="asset"><see cref="Object"/></param>
    /// <returns>True if added to <see cref="EpicAssets.AssetCache"/></returns>
    [PublicAPI]
    public static bool RegisterAsset(string name, UnityEngine.Object asset)
    {
        if (EpicAssets.AssetCache.ContainsKey(name))
        {
            OnError?.Invoke("Duplicate asset: " + name);
            return false;
        }

        EpicAssets.AssetCache[name] = asset;
        ExternalAssets[name] = asset;
        return true;
    }

     /// <remarks>
    /// Can be useful for external plugins to know, so they can design features around it.
    /// </remarks>
    /// <param name="player"></param>
    /// <param name="legendaryItemID"></param>
    /// <returns>true if player has item</returns>
    [PublicAPI]
    public static bool HasLegendaryItem(Player player, string legendaryItemID)
    {
        foreach (ItemDrop.ItemData item in player.GetInventory().GetEquippedItems())
        {
            if (item.IsMagic(out var magicItem) && magicItem.LegendaryID == legendaryItemID) return true;
        }

        return false;
    }

    /// <remarks>
    /// Can be useful for external plugins to know, so they can design features around it.
    /// </remarks>
    /// <param name="player"></param>
    /// <param name="legendarySetID"></param>
    /// <param name="count"></param>
    /// <returns>true if player has full set</returns>
    [PublicAPI]
    public static bool HasLegendarySet(Player player, string legendarySetID, ref int count)
    {
        if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(legendarySetID, out LegendarySetInfo legendarySetInfo, out ItemRarity _))
        {
            return false;
        }

        count = player.GetMagicEquippedSetPieces(legendarySetID).Count;
        return count >= legendarySetInfo.LegendaryIDs.Count;
    }
    /// <param name="type"><see cref="MagicEffectType"/></param>
    /// <returns>serialized object of magic effect definition if found</returns>
    [PublicAPI]
    public static string GetMagicItemEffectDefinition(string type)
    {
        if (!MagicItemEffectDefinitions.AllDefinitions.TryGetValue(type, out MagicItemEffectDefinition definition))
        {
            return "";
        }

        return JsonConvert.SerializeObject(definition);
    }
    
    /// <param name="player">can be null</param>
    /// <param name="item">can be null</param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    [PublicAPI]
    public static float GetTotalActiveMagicEffectValue(Player player,ItemDrop.ItemData item, string effectType, float scale)
    {
        return MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, item, effectType, scale);
    }

    /// <param name="player">can be null</param>
    /// <param name="item">can be null</param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    [PublicAPI]
    public static float GetTotalActiveMagicEffectValueForWeapon(Player player, ItemDrop.ItemData item, string effectType, float scale)
    {
        return MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, item, effectType, scale);
    }

    /// <param name="player">can be null</param>
    /// <param name="effectType"></param>
    /// <param name="item">can be null</param>
    /// <param name="effectValue"><see cref="MagicEffectType"/></param>
    /// <returns>True if player or item has magic effect</returns>
    [PublicAPI]
    public static bool HasActiveMagicEffect(Player player, string effectType, ItemDrop.ItemData item, ref float effectValue)
    {
        return MagicEffectsHelper.HasActiveMagicEffect(player, item, effectType, out effectValue);
    }
    
    /// <param name="player">can be null</param>
    /// <param name="item"></param>
    /// <param name="effectType"></param>
    /// <param name="effectValue"><see cref="MagicEffectType"/></param>
    /// <returns>True if magic effect is on item</returns>
    [PublicAPI]
    public static bool HasActiveMagicEffectOnWeapon(Player player, ItemDrop.ItemData item, string effectType, ref float effectValue)
    {
        return MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item, effectType, out effectValue);
    }

    /// <remarks>
    /// Currently hard coded to Modify Armor effect type ???
    /// </remarks>
    /// <param name="player"></param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    [PublicAPI]
    public static float GetTotalActiveSetEffectValue(Player player, string effectType, float scale)
    {
        return MagicEffectsHelper.GetTotalActiveSetEffectValue(player, effectType, scale);
    }

    /// <param name="player"></param>
    /// <param name="effectType">filter by <see cref="MagicEffectType"/></param>
    /// <returns>list of active magic effects on player</returns>
    [PublicAPI]
    public static List<string> GetAllActiveMagicEffects(Player player, string effectType = null)
    {
        List<MagicItemEffect> list = player.GetAllActiveMagicEffects(effectType);
        List<string> output = new List<string>();
        foreach (MagicItemEffect item in list)
        {
            output.Add(JsonConvert.SerializeObject(item));
        }

        return output;
    }

    /// <param name="player"></param>
    /// <param name="effectType">filter by <see cref="MagicEffectType"/></param>
    /// <returns>list of active magic effects on set</returns>
    [PublicAPI]
    public static List<string> GetAllActiveSetMagicEffects(Player player, string effectType = null)
    {
        List<MagicItemEffect> list = player.GetAllActiveSetMagicEffects(effectType);
        List<string> output = new List<string>();
        foreach (MagicItemEffect item in list)
        {
            output.Add(JsonConvert.SerializeObject(item));
        }

        return output;
    }
    
    /// <param name="player"></param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="scale"></param>
    /// <param name="ignoreThisItem"></param>
    /// <returns>total effect value found on player</returns>
    [PublicAPI]
    public static float GetTotalPlayerActiveMagicEffectValue(Player player, string effectType, float scale,
        ItemDrop.ItemData ignoreThisItem = null)
    {
        return player.GetTotalActiveMagicEffectValue(effectType, scale, ignoreThisItem);
    }

    /// <param name="player"></param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="effectValue"></param>
    /// <param name="scale"></param>
    /// <param name="ignoreThisItem"></param>
    /// <returns>True if player has magic effect</returns>
    [PublicAPI]
    public static bool PlayerHasActiveMagicEffect(Player player, string effectType, ref float effectValue,
        float scale = 1.0f, ItemDrop.ItemData ignoreThisItem = null)
    {
        return player.HasActiveMagicEffect(effectType, out effectValue, scale, ignoreThisItem);
    }

    /// <param name="itemData"></param>
    /// <returns></returns>
    [PublicAPI]
    public static string GetMagicItemJson(ItemDrop.ItemData itemData)
    {
        if (!itemData.IsMagic()) return null;
        return JsonConvert.SerializeObject(itemData.GetMagicItem());
    }
}