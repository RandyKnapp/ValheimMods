using Common;
using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.LegendarySystem;
using EpicLoot.src.Magic.MagicItemEffects.Helpers;
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
    
    private static readonly Dictionary<string, MagicItemEffectDefinition> ExternalMagicItemEffectDefinitions = new();
    private static readonly Dictionary<string, AbilityDefinition> ExternalAbilities = new();
    // Each entry already carries its Rarities (filled from the registration's rarity when it had none).
    private static readonly List<LegendaryInfo> ExternalLegendaryItems = new();
    private static readonly List<LegendarySetInfo> ExternalLegendarySets = new();
    private static readonly Dictionary<string, UnityEngine.Object> ExternalAssets = new();
    private static readonly List<MaterialConversion> ExternalMaterialConversions = new();
    private static readonly List<RecipeConfig> ExternalRecipes = new();
    private static readonly List<DisenchantProductsConfig> ExternalSacrifices = new();
    private static readonly List<BountyTargetConfig> ExternalBountyTargets = new();
    private static readonly Dictionary<SecretStashType, List<SecretStashItemConfig>> ExternalSecretStashItems = new();
    private static readonly List<TreasureMapBiomeInfoConfig> ExternalTreasureMaps = new();
    private static readonly Dictionary<string, Dictionary<string, Delegate>> AbilityProxies = new();

    static API()
    {
        LootRoller.OnSetupLootTables += ReloadExternalLootTables;
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
        // Failures are always logged: an API call that fails returns null or false, and the log is the only place
        // the calling mod's author can see why. SetVerboseLogging only adds the routine registration chatter.
        OnError += EpicLoot.LogWarningForce;
    }

    /// <returns>True if added to <see cref="EpicAssets.AssetCache"/></returns>
    [PublicAPI]
    public static bool RegisterAsset(string name, UnityEngine.Object asset)
    {
        if (string.IsNullOrEmpty(name) || asset == null)
        {
            OnError?.Invoke("RegisterAsset needs a name and an asset.");
            return false;
        }

        if (EpicAssets.AssetCache.ContainsKey(name))
        {
            OnError?.Invoke("Duplicate asset: " + name);
            return false;
        }

        EpicAssets.AssetCache[name] = asset;
        ExternalAssets[name] = asset;
        return true;
    }

    /// <returns>true if the player has the unique item equipped</returns>
    [PublicAPI]
    public static bool HasLegendaryItem(Player player, string legendaryItemID)
    {
        if (player == null)
        {
            return false;
        }

        foreach (ItemDrop.ItemData item in player.GetMagicEquipment())
        {
            if (item.IsMagic(out var magicItem) && magicItem.LegendaryID == legendaryItemID) return true;
        }

        return false;
    }

    /// <param name="count">Set to the number of distinct set pieces equipped.</param>
    /// <returns>true if player has full set: enough pieces for every set bonus, at any rarities</returns>
    [PublicAPI]
    public static bool HasLegendarySet(Player player, string legendarySetID, ref int count)
    {
        if (!UniqueLegendaryHelper.TryGetLegendarySetInfo(legendarySetID, out LegendarySetInfo legendarySetInfo))
        {
            return false;
        }

        LegendarySetProgress progress = SetBonusEvaluator.GetSetProgress(player, legendarySetInfo);
        count = progress.Count;
        return progress.IsFull;
    }

    /// <param name="type"><see cref="MagicEffectType"/></param>
    /// <returns>the magic effect definition as JSON, or "" if there is none</returns>
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
    [PublicAPI]
    public static float GetTotalActiveMagicEffectValue(Player player,ItemDrop.ItemData item, string effectType, float scale)
    {
        return MagicEffectsHelper.GetTotalActiveMagicEffectValue(player, item, effectType, scale);
    }

    /// <param name="player">can be null</param>
    /// <param name="item">can be null</param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    [PublicAPI]
    public static float GetTotalActiveMagicEffectValueForWeapon(Player player, ItemDrop.ItemData item, string effectType, float scale)
    {
        return MagicEffectsHelper.GetTotalActiveMagicEffectValueForWeapon(player, item, effectType, scale);
    }

    /// <param name="player">can be null</param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="item">can be null</param>
    /// <param name="effectValue">set to the effect's total value</param>
    /// <returns>True if player or item has magic effect</returns>
    [PublicAPI]
    public static bool HasActiveMagicEffect(Player player, string effectType, ItemDrop.ItemData item, ref float effectValue)
    {
        return MagicEffectsHelper.HasActiveMagicEffect(player, item, effectType, out effectValue);
    }
    
    /// <param name="player">can be null</param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="effectValue">set to the effect's total value</param>
    /// <returns>True if magic effect is on item</returns>
    [PublicAPI]
    public static bool HasActiveMagicEffectOnWeapon(Player player, ItemDrop.ItemData item, string effectType, ref float effectValue)
    {
        return MagicEffectsHelper.HasActiveMagicEffectOnWeapon(player, item, effectType, out effectValue);
    }

    /// <param name="player">can be null</param>
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <returns>total value of the effect from the player's active legendary set bonuses</returns>
    [PublicAPI]
    public static float GetTotalActiveSetEffectValue(Player player, string effectType, float scale)
    {
        return MagicEffectsHelper.GetTotalActiveSetEffectValue(player, effectType, scale);
    }

    /// <param name="effectType">filter by <see cref="MagicEffectType"/></param>
    /// <returns>list of active magic effects on player</returns>
    [PublicAPI]
    public static List<string> GetAllActiveMagicEffects(Player player, string effectType = null)
    {
        if (player == null)
        {
            return new List<string>();
        }

        List<MagicItemEffect> list = player.GetAllActiveMagicEffects(effectType);
        List<string> output = new List<string>();
        foreach (MagicItemEffect item in list)
        {
            output.Add(JsonConvert.SerializeObject(item));
        }

        return output;
    }

    /// <param name="effectType">filter by <see cref="MagicEffectType"/></param>
    /// <returns>list of active magic effects on set</returns>
    [PublicAPI]
    public static List<string> GetAllActiveSetMagicEffects(Player player, string effectType = null)
    {
        if (player == null)
        {
            return new List<string>();
        }

        List<MagicItemEffect> list = player.GetAllActiveSetMagicEffects(effectType);
        List<string> output = new List<string>();
        foreach (MagicItemEffect item in list)
        {
            output.Add(JsonConvert.SerializeObject(item));
        }

        return output;
    }
    
    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <returns>total effect value found on player</returns>
    [PublicAPI]
    public static float GetTotalPlayerActiveMagicEffectValue(Player player, string effectType, float scale,
        ItemDrop.ItemData ignoreThisItem = null)
    {
        return player == null ? 0f : player.GetTotalActiveMagicEffectValue(effectType, scale, ignoreThisItem);
    }

    /// <param name="effectType"><see cref="MagicEffectType"/></param>
    /// <param name="effectValue">set to the effect's total value</param>
    /// <returns>True if player has magic effect</returns>
    [PublicAPI]
    public static bool PlayerHasActiveMagicEffect(Player player, string effectType, ref float effectValue,
        float scale = 1.0f, ItemDrop.ItemData ignoreThisItem = null)
    {
        if (player == null)
        {
            effectValue = 0f;
            return false;
        }

        return player.HasActiveMagicEffect(effectType, out effectValue, scale, ignoreThisItem);
    }

    /// <returns>the item's magic data as JSON, or null if it has none</returns>
    [PublicAPI]
    public static string GetMagicItemJson(ItemDrop.ItemData itemData)
    {
        if (itemData == null || !itemData.IsMagic()) return null;
        return JsonConvert.SerializeObject(itemData.GetMagicItem());
    }
}