using Common;
using System;

namespace EpicLoot.Crafting
{
    public static class RecipesHelper
    {
        public static RecipesConfig Config;
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
