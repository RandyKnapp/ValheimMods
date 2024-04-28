using Common;

namespace Jam
{
    public static class RecipesHelper
    {
        public static RecipesConfig Config;

        public static void Initialize(RecipesConfig config)
        {
            Config = config;

            if (Jam.IsObjectDBReady())
            {
                SetupRecipes();
            }
        }

        public static void SetupRecipes()
        {
            PrefabCreator.Reset();
            foreach (var recipe in Config.recipes)
            {
                PrefabCreator.AddNewRecipe(recipe.name, recipe.item, recipe);
            }
        }
    }
}
