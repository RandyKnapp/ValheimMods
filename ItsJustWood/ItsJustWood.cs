using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Common;
using HarmonyLib;
using UnityEngine;

namespace ItsJustWood
{
    [BepInPlugin(PluginId, "It's Just Wood", "1.0.1")]
    public class ItsJustWood : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.itsjustwood";

        public static ConfigEntry<Vector2> FineWoodToWoodCount;
        public static ConfigEntry<Vector2> CoreWoodToWoodCount;
        public static ConfigEntry<Vector2> AncientBarkToWoodCount;
        public static ConfigEntry<bool> AllowFineWoodForFuel;
        public static ConfigEntry<bool> AllowCoreWoodForFuel;
        public static ConfigEntry<bool> AllowAncientBarkForFuel;
        public static ConfigEntry<bool> AllowAncientBarkForCoal;

        private Harmony _harmony;
        //private bool _initialized;

        private void Awake()
        {
            FineWoodToWoodCount = Config.Bind("Recipes", "FineWoodToWoodCount", new Vector2(8, 8), "The Fine Wood -> Wood recipe counts. X = amount of fine wood used, Y = amount of wood created");
            CoreWoodToWoodCount = Config.Bind("Recipes", "CoreWoodToWoodCount", new Vector2(8, 8), "The Core Wood -> Wood recipe counts. X = amount of core wood used, Y = amount of wood created");
            AncientBarkToWoodCount = Config.Bind("Recipes", "AncientBarkToWoodCount", new Vector2(8, 8), "The Ancient Bark -> Wood recipe counts. X = amount of ancient bark used, Y = amount of wood created");

            AllowFineWoodForFuel = Config.Bind("Fuel", "AllowFineWoodForFuel", true, "Allow fine wood to be used as fuel for fires");
            AllowCoreWoodForFuel = Config.Bind("Fuel", "AllowCoreWoodForFuel", true, "Allow core wood to be used as fuel for fires");
            AllowAncientBarkForFuel = Config.Bind("Fuel", "AllowAncientBarkForFuel", true, "Allow ancient bark to be used as fuel for fires");

            AllowAncientBarkForCoal = Config.Bind("Fuel", "AllowAncientBarkForCoal", true, "Allow ancient bark to be used to create coal in a charcoal kiln");

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll(PluginId);
        }

        public static void TryRegisterRecipes()
        {
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                return;
            }

            var fineWoodToWood = new RecipeConfig()
            {
                amount = (int)FineWoodToWoodCount.Value.y,
                enabled = true,
                resources = new List<RecipeRequirementConfig>
                {
                    new RecipeRequirementConfig()
                    {
                        item = "FineWood",
                        amount = (int)FineWoodToWoodCount.Value.x
                    }
                }
            };
            PrefabCreator.AddNewRecipe("Recipe_FineWoodToWood", "Wood", fineWoodToWood);

            var coreWoodToWood = new RecipeConfig()
            {
                amount = (int)CoreWoodToWoodCount.Value.y,
                enabled = true,
                resources = new List<RecipeRequirementConfig>
                {
                    new RecipeRequirementConfig()
                    {
                        item = "RoundLog",
                        amount = (int)CoreWoodToWoodCount.Value.x
                    }
                }
            };
            PrefabCreator.AddNewRecipe("Recipe_CoreWoodToWood", "Wood", coreWoodToWood);

            var ancientBarkToWood = new RecipeConfig()
            {
                amount = (int)AncientBarkToWoodCount.Value.y,
                enabled = true,
                resources = new List<RecipeRequirementConfig>
                {
                    new RecipeRequirementConfig()
                    {
                        item = "ElderBark",
                        amount = (int)AncientBarkToWoodCount.Value.x
                    }
                }
            };
            PrefabCreator.AddNewRecipe("Recipe_AncientBarkWoodToWood", "Wood", ancientBarkToWood);
        }
    }
}
