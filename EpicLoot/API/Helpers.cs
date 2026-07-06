using Common;
using EpicLoot.Crafting;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;

namespace EpicLoot;

public static partial class API
{
    [PublicAPI]
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
    public static string AddRecipes(string json)
    {
        // TODO: Figure out why it looks like recipes are added twice
        // PRIORITY: Low
        // Some interesting logic about re-initializing recipes after item manager on items registered ??
        // Current fix, remove external recipes, then add again on reload
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
    
    /// <param name="key">unique identifier <see cref="string"/></param>
    /// <param name="json">JSON serialized List of <see cref="MaterialConversion"/></param>
    /// <returns>True if updated</returns>
    [PublicAPI]
    public static bool UpdateRecipes(string key, string json)
    {
        if (!RuntimeRegistry.TryGetValue(key, out List<RecipeConfig> list))
        {
            return false;
        }

        List<RecipeConfig> recipes = JsonConvert.DeserializeObject<List<RecipeConfig>>(json);

        if (recipes == null)
        {
            return false;
        }

        ExternalRecipes.ReplaceThenAdd(list, recipes);
        RecipesHelper.Config.recipes.ReplaceThenAdd(list, recipes);
        return true;
    }

    /// <summary>
    /// Helper function to add into dictionary of lists
    /// </summary>
    /// <param name="dict">Dictionary T key, List V values</param>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    private static void AddOrSet<T, V>(this Dictionary<T, List<V>> dict, T key, V value)
    {
        if (!dict.ContainsKey(key))
        {
            dict[key] = new List<V>();
        }

        dict[key].Add(value);
    }
    /// <summary>
    /// Helper function to copy all fields from one instance to the other
    /// </summary>
    /// <param name="target"><see cref="T"/></param>
    /// <param name="source"><see cref="T"/></param>
    /// <typeparam name="T"><see cref="T"/></typeparam>
    private static void CopyFieldsFrom<T>(this T target, T source)
    {
        foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            object value = field.GetValue(source);
            if (value == null) continue;
            field.SetValue(target, value);
        }
    }

    /// <summary>
    /// Helper function, removes all from list, then adds new items into list
    /// </summary>
    /// <param name="list"></param>
    /// <param name="original"></param>
    /// <param name="replacements"></param>
    /// <typeparam name="T"></typeparam>
    private static void ReplaceThenAdd<T>(this List<T> list, List<T> original, List<T> replacements)
    {
        list.RemoveAll(original);
        list.AddRange(replacements);
    }

    /// <summary>
    /// Helper function, removes all instances from one list in the other list
    /// </summary>
    /// <param name="list"></param>
    /// <param name="itemsToRemove"></param>
    /// <typeparam name="T"></typeparam>
    private static void RemoveAll<T>(this List<T> list, List<T> itemsToRemove)
    {
        foreach (var item in itemsToRemove) list.Remove(item);
    }
}