using Common;
using EpicLoot.Crafting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EpicLoot;

public static partial class API
{
    private const string RecipeApiObsolete = "Epic Loot's recipes.json was removed in 0.13.0 and it no longer ships the recipes.json file. ";

    /// <param name="json">JSON serialized <see cref="RecipeConfig"/></param>
    /// <returns>unique key if successfully added</returns>
    [PublicAPI]
    [Obsolete(RecipeApiObsolete)]
    public static string AddRecipe(string json)
    {
        try
        {
            var recipe = JsonConvert.DeserializeObject<RecipeConfig>(json);
            if (recipe == null)
            {
                return null;
            }

            ExternalRecipes.Add(recipe);
            RecipesHelper.Config.recipes.Add(recipe);
            return RuntimeRegistry.Register(recipe);
        }
        catch
        {
            OnError?.Invoke("Failed to parse recipe passed in through external plugin.");
            return null;
        }
    }
    
    /// <param name="json">JSON serialized List of <see cref="RecipeConfig"/></param>
    /// <returns>unique key if successfully added</returns>
    [PublicAPI]
    [Obsolete(RecipeApiObsolete)]
    public static string AddRecipes(string json)
    {
        try
        {
            List<RecipeConfig> recipes = JsonConvert.DeserializeObject<List<RecipeConfig>>(json);

            if (recipes == null)
            {
                return null;
            }

            ExternalRecipes.AddRange(recipes);
            RecipesHelper.Config.recipes.AddRange(recipes);
            return RuntimeRegistry.Register(recipes);
        }
        catch
        {
            OnError?.Invoke("Failed to parse recipe from external plugin");
            return null;
        }
    }
    
    /// <param name="key">the key AddRecipes returned</param>
    /// <param name="json">JSON serialized List of <see cref="RecipeConfig"/></param>
    /// <returns>True if updated</returns>
    [PublicAPI]
    [Obsolete(RecipeApiObsolete)]
    public static bool UpdateRecipes(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out List<RecipeConfig> list))
        {
            return false;
        }

        List<RecipeConfig> recipes;
        try
        {
            recipes = JsonConvert.DeserializeObject<List<RecipeConfig>>(json);
        }
        catch (Exception e)
        {
            OnError?.Invoke($"Failed to parse recipes passed in through external plugin: {e.Message}");
            return false;
        }

        if (recipes == null)
        {
            return false;
        }

        ExternalRecipes.ReplaceThenAdd(list, recipes);
        RecipesHelper.Config.recipes.ReplaceThenAdd(list, recipes);
        return true;
    }

    private static void AddOrSet<T, V>(this Dictionary<T, List<V>> dict, T key, V value)
    {
        if (!dict.ContainsKey(key))
        {
            dict[key] = new List<V>();
        }

        dict[key].Add(value);
    }

    // Null fields of source are left as target has them.
    private static void CopyFieldsFrom<T>(this T target, T source)
    {
        foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            object value = field.GetValue(source);
            if (value == null) continue;
            field.SetValue(target, value);
        }
    }

    private static void ReplaceThenAdd<T>(this List<T> list, List<T> original, List<T> replacements)
    {
        list.RemoveAll(original);
        list.AddRange(replacements);
    }

    private static void RemoveAll<T>(this List<T> list, List<T> itemsToRemove)
    {
        foreach (var item in itemsToRemove) list.Remove(item);
    }
}