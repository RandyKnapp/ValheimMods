using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace Common
{
    public static class PrefabCreator
    {
        public static ManualLogSource Logger;
        public static Dictionary<string, CraftingStation> CraftingStations;

        public static T RequireComponent<T>(GameObject go) where T:Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
            }

            return c;
        }

        public static void Reset()
        {
            CraftingStations = null;
        }

        private static void InitCraftingStations()
        {
            if (CraftingStations == null)
            {
                CraftingStations = new Dictionary<string, CraftingStation>();
                foreach (var recipe in ObjectDB.instance.m_recipes)
                {
                    if (recipe.m_craftingStation != null && !CraftingStations.ContainsKey(recipe.m_craftingStation.name))
                    {
                        CraftingStations.Add(recipe.m_craftingStation.name, recipe.m_craftingStation);
                    }
                }
            }
        }

        public static Recipe CreateRecipe(string name, string itemId, RecipeConfig recipeConfig)
        {
            InitCraftingStations();

            var itemPrefab = ObjectDB.instance.GetItemPrefab(itemId);
            if (itemPrefab == null)
            {
                Logger?.LogWarning($"[PrefabCreator] Could not find item prefab ({itemId})");
                return null;
            }

            var newRecipe = ScriptableObject.CreateInstance<Recipe>();
            newRecipe.name = name;
            newRecipe.m_amount = recipeConfig.amount;
            newRecipe.m_minStationLevel = recipeConfig.minStationLevel;
            newRecipe.m_item = itemPrefab.GetComponent<ItemDrop>();
            newRecipe.m_enabled = recipeConfig.enabled;

            if (!string.IsNullOrEmpty(recipeConfig.craftingStation))
            {
                var craftingStationExists = CraftingStations.ContainsKey(recipeConfig.craftingStation);
                if (!craftingStationExists)
                {
                    Logger?.LogWarning($"[PrefabCreator] Could not find crafting station ({itemId}): {recipeConfig.craftingStation}");
                    var stationList = string.Join(", ", CraftingStations.Keys);
                    Logger?.LogInfo($"[PrefabCreator] Available Stations: {stationList}");
                }
                else
                {
                    newRecipe.m_craftingStation = CraftingStations[recipeConfig.craftingStation];
                }
            }

            if (!string.IsNullOrEmpty(recipeConfig.repairStation))
            {
                var repairStationExists = CraftingStations.ContainsKey(recipeConfig.repairStation);
                if (!repairStationExists)
                {
                    Logger?.LogWarning($"[PrefabCreator] Could not find repair station ({itemId}): {recipeConfig.repairStation}");
                    var stationList = string.Join(", ", CraftingStations.Keys);
                    Logger?.LogInfo($"[PrefabCreator] Available Stations: {stationList}");
                }
                else
                {
                    newRecipe.m_repairStation = CraftingStations[recipeConfig.repairStation];
                }
            }

            var reqs = new List<Piece.Requirement>();
            foreach (var requirement in recipeConfig.resources)
            {
                var reqPrefab = ObjectDB.instance.GetItemPrefab(requirement.item);
                if (reqPrefab == null)
                {
                    Logger?.LogError($"[PrefabCreator] Could not find requirement item ({itemId}): {requirement.item}");
                    continue;
                }

                reqs.Add(new Piece.Requirement()
                {
                    m_amount = requirement.amount,
                    m_resItem = reqPrefab.GetComponent<ItemDrop>()
                });
            }
            newRecipe.m_resources = reqs.ToArray();

            return newRecipe;
        }

        public static Recipe AddNewRecipe(string name, string itemId, RecipeConfig recipeConfig)
        {
            var recipe = CreateRecipe(name, itemId, recipeConfig);
            if (recipe == null)
            {
                Logger?.LogError($"[PrefabCreator] Failed to create recipe ({name})");
                return null;
            }
            return AddNewRecipe(recipe);
        }

        public static Recipe AddNewRecipe(Recipe recipe)
        {
            var removed = ObjectDB.instance.m_recipes.RemoveAll(x => x.name == recipe.name);
            if (removed > 0)
            {
                Logger?.LogInfo($"[PrefabCreator] Removed recipe ({recipe.name}): {removed}");
            }

            ObjectDB.instance.m_recipes.Add(recipe);
            Logger?.LogInfo($"[PrefabCreator] Added recipe: {recipe.name}");

            return recipe;
        }
    }
}
