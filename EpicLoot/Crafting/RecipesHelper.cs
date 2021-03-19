using Common;

namespace EpicLoot.Crafting
{
    public static class RecipesHelper
    {
        public static RecipesConfig Config;

        public static void Initialize(RecipesConfig config)
        {
            Config = config;
        }

        public static void SetupRecipes()
        {
            // Recipes from file
            PrefabCreator.Reset();
            foreach (var recipe in Config.recipes)
            {
                PrefabCreator.AddNewRecipe(recipe.name, recipe.item, recipe);
            }
        }
    }
}
