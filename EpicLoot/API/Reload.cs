using EpicLoot.Abilities;
using EpicLoot.Adventure;
using EpicLoot.Crafting;
using EpicLoot.CraftingV2;
using EpicLoot.LegendarySystem;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace EpicLoot;

public static partial class API
{
    // A config the server pushed already holds the server's API entries, and the mod registering them may run on this
    // client too: an entry is added only when an identical one is not there. Compared by content, since the pushed
    // copies are new objects.
    private static void AddMissing<T>(List<T> target, IEnumerable<T> items) where T : class
    {
        if (target == null || items == null)
        {
            return;
        }

        HashSet<string> present = new HashSet<string>(target.Where(x => x != null).Select(ContentKey).Where(x => x != null));
        foreach (T item in items)
        {
            if (item == null)
            {
                continue;
            }

            string key = ContentKey(item);
            if (key != null ? present.Add(key) : !target.Contains(item))
            {
                target.Add(item);
            }
        }
    }

    private static string ContentKey(object item)
    {
        try
        {
            return JsonConvert.SerializeObject(item);
        }
        catch (System.Exception)
        {
            return null;
        }
    }

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
        if (AdventureDataManager.Config?.SecretStash == null)
        {
            return;
        }

        foreach (KeyValuePair<SecretStashType, List<SecretStashItemConfig>> kvp in ExternalSecretStashItems)
        {
            switch (kvp.Key)
            {
                case SecretStashType.Materials:
                    AddMissing(AdventureDataManager.Config.SecretStash.Materials, kvp.Value);
                    break;
                case SecretStashType.OtherItems:
                    AddMissing(AdventureDataManager.Config.SecretStash.OtherItems, kvp.Value);
                    break;
                case SecretStashType.RandomItems:
                    AddMissing(AdventureDataManager.Config.SecretStash.RandomItems, kvp.Value);
                    break;
                case SecretStashType.Gamble:
                    AddMissing(AdventureDataManager.Config.Gamble?.GambleCosts, kvp.Value);
                    break;
                case SecretStashType.Sale:
                    AddMissing(AdventureDataManager.Config.TreasureMap?.SaleItems, kvp.Value);
                    break;
            }
        }
    }

    /// <summary>
    /// Reloads cached external treasure maps into <see cref="AdventureDataManager.Config"/>
    /// </summary>
    private static void ReloadExternalTreasures()
    {
        if (AdventureDataManager.Config?.TreasureMap == null)
        {
            return;
        }

        AddMissing(AdventureDataManager.Config.TreasureMap.BiomeInfo, ExternalTreasureMaps);
        AdventureDataManager.Config.TreasureMap.UpdateBiomeList();
        OnReload?.Invoke("Reloaded external treasures");
    }

    /// <summary>
    /// Reloads cached external bounties into <see cref="AdventureDataManager.Config"/>
    /// </summary>
    private static void ReloadExternalBounties()
    {
        AddMissing(AdventureDataManager.Config?.Bounties?.Targets, ExternalBountyTargets);
        OnReload?.Invoke("Reloaded external bounties");
    }

    /// <summary>
    /// Reloads cached external sacrifice products into <see cref="EnchantCostsHelper.Config"/>
    /// </summary>
    private static void ReloadExternalSacrifices()
    {
        AddMissing(EnchantCostsHelper.Config?.DisenchantProducts, ExternalSacrifices);
        OnReload?.Invoke("Reloaded external sacrifices");
    }

    /// <summary>
    /// Reloads cached external recipes into <see cref="RecipesHelper.Config"/>
    /// </summary>
    private static void ReloadExternalRecipes()
    {
        if (RecipesHelper.Config?.recipes == null)
        {
            return;
        }

        RecipesHelper.Config.recipes.RemoveAll(ExternalRecipes);
        AddMissing(RecipesHelper.Config.recipes, ExternalRecipes);
        OnReload?.Invoke("Reloaded external recipes");
    }

    /// <summary>
    /// Reloads cached external material conversions into <see cref="MaterialConversions.Config"/>
    /// </summary>
    private static void ReloadExternalMaterialConversions()
    {
        AddMissing(MaterialConversions.Config?.MaterialConversions, ExternalMaterialConversions);
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
        List<AbilityDefinition> abilities = AbilityDefinitions.Config?.Abilities;
        if (abilities == null)
        {
            return;
        }

        // By ID: an ability is one entry whatever its fields, so a pushed copy is replaced by this client's.
        foreach (KeyValuePair<string, AbilityDefinition> kvp in ExternalAbilities)
        {
            abilities.RemoveAll(x => x != null && x.ID == kvp.Value.ID);
            abilities.Add(kvp.Value);
        }

        OnReload?.Invoke("Reloaded external abilities");
    }

    /// <summary>
    /// Reloads cached legendary items and sets into <see cref="UniqueLegendaryHelper"/>
    /// </summary>
    private static void ReloadExternalLegendary()
    {
        // Every rarity shares the one pool; each entry's Rarities says where it rolls.
        AddMissing(UniqueLegendaryHelper.Config?.LegendaryItems, ExternalLegendaryItems);
        AddMissing(UniqueLegendaryHelper.Config?.LegendarySets, ExternalLegendarySets);

        OnReload?.Invoke("Reloaded external legendary items and sets");
    }

    /// <summary>
    /// Reloads cached assets into <see cref="EpicAssets.AssetCache"/>
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