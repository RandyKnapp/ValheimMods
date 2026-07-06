using System;
using System.Collections.Generic;

namespace Common
{
    [Serializable]
    public class RecipeRequirementConfig
    {
        public string item = "";
        public int amount = 1;
    }

    [Serializable]
    public class RecipeConfig
    {
        public string name = "";
        public string item = "";
        public int amount = 1;
        public string craftingStation = "";
        public int minStationLevel = 1;
        public bool enabled = true;
        public string repairStation = "";
        public List<RecipeRequirementConfig> resources = new List<RecipeRequirementConfig>();
    }

    [Serializable]
    public class RecipesConfig
    {
        public List<RecipeConfig> recipes = new List<RecipeConfig>();
    }
}
