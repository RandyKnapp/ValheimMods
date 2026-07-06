using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

[Serializable]
[PublicAPI]
public class RecipeRequirement
{
    public string item;
    public int amount;

    public RecipeRequirement(string item, int amount = 1)
    {
        this.item = item;
        this.amount = amount;
    }
}

[Serializable]
[PublicAPI]
public class CustomRecipe
{
    public string name = "";
    public string item = "";
    public int amount;
    public string craftingStation = "";
    public int minStationLevel = 1;
    public bool enabled = true;
    public string repairStation = "";
    public List<RecipeRequirement> resources = new List<RecipeRequirement>();
    
    public CustomRecipe(string name, string item, CraftingTable craftingTable, int amount = 1)
    {
        this.name = name;
        this.item = item;
        this.amount = amount;
        craftingStation = craftingTable.GetInternalName();
        Recipes.Add(this);
    }
    
    public CustomRecipe(){}

    internal static readonly List<CustomRecipe> Recipes = new();
    internal static readonly Method API_AddRecipe = new ("AddRecipe");

    /// <summary>
    /// Invokes <see cref="API_AddRecipe"/> with serialized List <see cref="CustomRecipe"/>
    /// </summary>
    /// <returns>Unique key if added</returns>
    [PublicAPI]
    public static void RegisterAll()
    {
        foreach (var recipe in new List<CustomRecipe>(Recipes))
        {
            recipe.Register();
        }
    }

    public bool Register()
    {
        string json = JsonConvert.SerializeObject(this);
        object[] result = API_AddRecipe.Invoke(json);

        if (result[0] is not string key)
        {
            return false;
        }

        RunTimeRegistry.Register(this, key);
        Recipes.Remove(this);
        EpicLoot.logger.LogDebug($"Registered recipe: {name}");
        return true;
    }
}

[PublicAPI]
public enum CraftingTable
{
    [InternalName("piece_workbench")] Workbench,
    [InternalName("piece_cauldron")] Cauldron,
    [InternalName("forge")] Forge,
    [InternalName("piece_artisanstation")] ArtisanTable,
    [InternalName("piece_stonecutter")] StoneCutter,
    [InternalName("piece_magetable")] MageTable,
    [InternalName("blackforge")] BlackForge,
    [InternalName("piece_preptable")] FoodPreparationTable,
    [InternalName("piece_MeadCauldron")] MeadKetill,
}

internal class InternalName : Attribute
{
    public readonly string internalName;
    public InternalName(string internalName) => this.internalName = internalName;
}