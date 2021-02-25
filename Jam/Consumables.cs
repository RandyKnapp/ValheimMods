using System;
using System.Collections.Generic;

namespace Jam
{
    [Serializable]
    public class ConsumableItemRecipeRequirement
    {
        public string item;
        public int amount;
    }

    [Serializable]
    public class ConsumableItemRecipe
    {
        public int amount;
        public string craftingStation;
        public int minStationLevel;
        public bool enabled;
        public string repairStation;
        public List<ConsumableItemRecipeRequirement> resources = new List<ConsumableItemRecipeRequirement>();
    }

    [Serializable]
    public class ConsumableItem
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
        public ConsumableItemRecipe recipe = new ConsumableItemRecipe();
    }

    [Serializable]
    public class ConsumablesConfig
    {
        public List<ConsumableItem> items = new List<ConsumableItem>();
    }
}
