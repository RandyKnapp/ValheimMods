using Common;
using System;

namespace EpicLoot.Crafting
{
    /// <summary>
    /// Holds the item recipes Epic Loot adds to the ObjectDB. Epic Loot no longer ships any of its own -
    /// its craftable items are registered through the shared item batch loader instead - so this is now
    /// purely the backing store for recipes other mods register via <see cref="API.AddRecipe"/>.
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
