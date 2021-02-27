using System;
using System.Collections.Generic;

namespace Common
{
    [Serializable]
    public class RecipeRequirementConfig
    {
        public string item;
        public int amount;
    }

    [Serializable]
    public class RecipeConfig
    {
        public string name;
        public string item;
        public int amount;
        public string craftingStation;
        public int minStationLevel;
        public bool enabled;
        public string repairStation;
        public List<RecipeRequirementConfig> resources = new List<RecipeRequirementConfig>();
    }

    [Serializable]
    public class RecipesConfig
    {
        public List<RecipeConfig> recipes = new List<RecipeConfig>();
    }
}
