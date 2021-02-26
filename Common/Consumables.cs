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
        public int amount;
        public string craftingStation;
        public int minStationLevel;
        public bool enabled;
        public string repairStation;
        public List<RecipeRequirementConfig> resources = new List<RecipeRequirementConfig>();
    }

    [Serializable]
    public class ConsumableItemConfig
    {
        public string id;
        public string basePrefab;
        public string displayName;
        public string[] icons;
        public string description;
        public int maxStackSize;
        public int food;
        public int foodStamina;
        public int foodRegen;
        public int foodBurnTime;
        public string foodColor;
        public RecipeConfig RecipeConfig = new RecipeConfig();
    }

    [Serializable]
    public class ConsumablesConfig
    {
        public List<ConsumableItemConfig> items = new List<ConsumableItemConfig>();
    }
}
