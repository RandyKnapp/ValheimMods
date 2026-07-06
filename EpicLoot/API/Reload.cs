using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.LegendarySystem;
using System.Collections.Generic;

namespace EpicLoot;

public static partial class API
{
    /// <summary>
    /// Reloads cached external adventure data into <see cref="AdventureDataManager.Config"/>
    /// </summary>
    private static void ReloadExternalAdventureData()
    {
        ReloadExternalBounties();
        ReloadExternalSecretStashItems();
        ReloadExternalTreasures();
    }

    /// <summary>
    /// Reloads cached secret stash items into <see cref="AdventureDataManager.Config"/>
    /// </summary>
    private static void ReloadExternalSecretStashItems()
    {
        foreach (KeyValuePair<SecretStashType, List<SecretStashItemConfig>> kvp in ExternalSecretStashItems)
        {
            switch (kvp.Key)
            {
                case SecretStashType.Materials:
                    AdventureDataManager.Config.SecretStash.Materials.AddRange(kvp.Value);
                    break;
                case SecretStashType.OtherItems:
                    AdventureDataManager.Config.SecretStash.OtherItems.AddRange(kvp.Value);
                    break;
                case SecretStashType.RandomItems:
                    AdventureDataManager.Config.SecretStash.RandomItems.AddRange(kvp.Value);
                    break;
                case SecretStashType.Gamble:
                    AdventureDataManager.Config.Gamble.GambleCosts.AddRange(kvp.Value);
                    break;
                case SecretStashType.Sale:
                    AdventureDataManager.Config.TreasureMap.SaleItems.AddRange(kvp.Value);
                    break;
            }
        }
    }

    /// <summary>
    /// Reloads cached external treasure maps into <see cref="AdventureDataManager.Config"/>
    /// </summary>
    private static void ReloadExternalTreasures()
    {
        foreach (TreasureMapBiomeInfoConfig treasure in ExternalTreasureMaps)
        {
            AdventureDataManager.Config.TreasureMap.BiomeInfo.Add(treasure);
        }

        OnReload?.Invoke("Reloaded external treasures");
    }

    /// <summary>
    /// Reloads cached external bounties into <see cref="AdventureDataManager.Config"/>
    /// </summary>
    private static void ReloadExternalBounties()
    {
        AdventureDataManager.Config.Bounties.Targets.AddRange(ExternalBountyTargets);
        OnReload?.Invoke("Reloaded external bounties");
    }

    /// <summary>
    /// Reloads cached external enchanting costs into <see cref="EnchantCostsHelper.Config"/>
    /// </summary>
    private static void ReloadExternalSacrifices()
    {
        EnchantCostsHelper.Config.DisenchantProducts.AddRange(ExternalSacrifices);
        OnReload?.Invoke("Reloaded external sacrifices");
    }

    /// <summary>
    /// Reloads cached external recipes into <see cref="RecipesHelper.Config"/>
    /// </summary>
    private static void ReloadExternalRecipes()
    {
        RecipesHelper.Config.recipes.RemoveAll(ExternalRecipes);
        RecipesHelper.Config.recipes.AddRange(ExternalRecipes);
        OnReload?.Invoke("Reloaded external recipes");
    }

    /// <summary>
    /// Reloads cached external material conversions into <see cref="MaterialConversions.Conversions"/>
    /// </summary>
    private static void ReloadExternalMaterialConversions()
    {
        foreach (MaterialConversion entry in ExternalMaterialConversions)
        {
            MaterialConversions.Config.MaterialConversions.Add(entry);
        }
        OnReload?.Invoke("Reloaded external material conversions");
    }
    
    /// <summary>
    /// Reloads cached external magic effects into <see cref="MagicItemEffectDefinitions.AllDefinitions"/>
    /// </summary>
    private static void ReloadExternalMagicEffects()
    {
        foreach (MagicItemEffectDefinition effect in ExternalMagicItemEffectDefinitions.Values)
        {
            MagicItemEffectDefinitions.Add(effect);
        }

        OnReload?.Invoke("Reloaded external magic effects");
    }

    /// <summary>
    /// Reloads cached external abilities into <see cref="AbilityDefinitions.Config"/> and <see cref="AbilityDefinitions.Abilities"/>
    /// </summary>
    private static void ReloadExternalAbilities()
    {
        foreach (KeyValuePair<string, AbilityDefinition> kvp in ExternalAbilities)
        {
            AbilityDefinitions.Config.Abilities.Add(kvp.Value);
        }

        OnReload?.Invoke("Reloaded external abilities");
    }

    /// <summary>
    /// Reloads cached legendary items and sets into <see cref="UniqueLegendaryHelper"/>
    /// </summary>
    private static void ReloadExternalLegendary()
    {
        foreach (KeyValuePair<ItemRarity, List<LegendaryInfo>> kvp in ExternalLegendaryItems)
        {
            switch (kvp.Key)
            {
                case ItemRarity.Legendary:
                    UniqueLegendaryHelper.Config.LegendaryItems.AddRange(kvp.Value);
                    break;
                case ItemRarity.Mythic:
                    UniqueLegendaryHelper.Config.MythicItems.AddRange(kvp.Value);
                    break;
            }
        }

        foreach (KeyValuePair<ItemRarity, List<LegendarySetInfo>> kvp in ExternalLegendarySets)
        {
            switch (kvp.Key)
            {
                case ItemRarity.Legendary:
                    UniqueLegendaryHelper.Config.LegendarySets.AddRange(kvp.Value);
                    break;
                case ItemRarity.Mythic:
                    UniqueLegendaryHelper.Config.MythicSets.AddRange(kvp.Value);
                    break;
            }
        }

        OnReload?.Invoke("Reloaded external legendary abilities");
    }

    /// <summary>
    /// Reloads cached assets into <see cref="EpicLoot.AssetCache"/>
    /// </summary>
    public static void ReloadExternalAssets()
    {
        foreach (KeyValuePair<string, UnityEngine.Object> kvp in ExternalAssets)
        {
            EpicAssets.AssetCache[kvp.Key] = kvp.Value;
        }

        OnReload?.Invoke("Reloaded external assets");
    }
}