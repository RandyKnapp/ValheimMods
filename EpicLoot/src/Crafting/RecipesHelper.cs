using Common;
using System;

namespace EpicLoot.Crafting
{
    /// <summary>
    /// The backing store for recipes other mods register via <see cref="API.AddRecipe"/>. Epic Loot's own
    /// craftable items are registered through the shared item batch loader instead.
    /// </summary>
    public static class RecipesHelper
    {
        // Non-null from the start so the API can add recipes before the game finishes loading.
        public static RecipesConfig Config = new RecipesConfig();
        public static event Action OnSetupRecipeConfig;

        public static void Initialize(RecipesConfig config)
        {
            Config = config;
            OnSetupRecipeConfig?.Invoke();

            if (EpicLoot.IsObjectDBReady())
            {
                SetupRecipes();
            }
        }

        public static RecipesConfig GetCFG()
        {
            return Config;
        }

        public static void SetupRecipes()
        {
            PrefabCreator.Reset();
            foreach (var recipe in Config.recipes)
            {
                if (!String.IsNullOrEmpty(recipe.craftingStation))
                {
                    PrefabCreator.AddNewRecipe(recipe.name, recipe.item, recipe);
                }
            }
        }
    }
}
