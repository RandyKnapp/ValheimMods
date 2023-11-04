using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Common;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace ItsJustWood
{
    [BepInPlugin(pluginID, pluginName, pluginVersion)]
    public class ItsJustWood : BaseUnityPlugin
    {
        const string pluginID = "randyknapp.mods.itsjustwood";
        const string pluginName = "It's Just Wood";
        const string pluginVersion = "1.1.0";

        public static ConfigEntry<bool> modEnabled;

        public static ConfigEntry<Vector2> FineWoodToWoodCount;
        public static ConfigEntry<Vector2> CoreWoodToWoodCount;
        public static ConfigEntry<Vector2> AncientBarkToWoodCount;
        public static ConfigEntry<Vector2> YggdrasilWoodToWoodCount;

        public static ConfigEntry<bool> AllowFineWoodForFuel;
        public static ConfigEntry<bool> AllowCoreWoodForFuel;
        public static ConfigEntry<bool> AllowAncientBarkForFuel;
        public static ConfigEntry<bool> AllowYggdrasilWoodForFuel;
        
        public static ConfigEntry<bool> AllowAncientBarkForCoal;
        public static ConfigEntry<bool> AllowYggdrasilWoodForCoal;

        private Harmony _harmony;

        public static ItemDrop wood;

        [UsedImplicitly]
        private void Awake()
        {
            modEnabled = Config.Bind("General", "Enabled", defaultValue: true, "Enable the mod.");

            FineWoodToWoodCount = Config.Bind("Recipes", "FineWoodToWoodCount", new Vector2(8, 8), "The Fine Wood -> Wood recipe counts. X = amount of fine wood used, Y = amount of wood created");
            CoreWoodToWoodCount = Config.Bind("Recipes", "CoreWoodToWoodCount", new Vector2(8, 8), "The Core Wood -> Wood recipe counts. X = amount of core wood used, Y = amount of wood created");
            AncientBarkToWoodCount = Config.Bind("Recipes", "AncientBarkToWoodCount", new Vector2(8, 8), "The Ancient Bark -> Wood recipe counts. X = amount of ancient bark used, Y = amount of wood created");
            YggdrasilWoodToWoodCount = Config.Bind("Recipes", "YggdrasilWoodToWoodCount", new Vector2(8, 8), "The Yggdrasil wood -> Wood recipe counts. X = amount of Yggdrasil wood used, Y = amount of wood created");

            AllowFineWoodForFuel = Config.Bind("Fuel", "AllowFineWoodForFuel", true, "Allow fine wood to be used as fuel for fires");
            AllowCoreWoodForFuel = Config.Bind("Fuel", "AllowCoreWoodForFuel", true, "Allow core wood to be used as fuel for fires");
            AllowAncientBarkForFuel = Config.Bind("Fuel", "AllowAncientBarkForFuel", true, "Allow ancient bark to be used as fuel for fires");
            AllowYggdrasilWoodForFuel = Config.Bind("Fuel", "AllowYggdrasilWoodForFuel", true, "Allow Yggdrasil wood to be used as fuel for fires");

            AllowAncientBarkForCoal = Config.Bind("Fuel", "AllowAncientBarkForCoal", true, "Allow ancient bark to be used to create coal in a charcoal kiln");
            AllowYggdrasilWoodForCoal = Config.Bind("Fuel", "AllowYggdrasilWoodForCoal", true, "Allow Yggdrasil wood to be used to create coal in a charcoal kiln");

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), pluginID);
        }

        public static bool IsObjectDBReady()
        {
            // Hack, just making sure the built-in items and prefabs have loaded
            return ObjectDB.instance != null && ObjectDB.instance.m_items.Count != 0 && ObjectDB.instance.GetItemPrefab("Amber") != null;
        }

        public static void TryRegisterRecipes()
        {
            if (!modEnabled.Value)
                return;

            if (!IsObjectDBReady())
                return;

            wood = ObjectDB.instance.GetItemPrefab("Wood").GetComponent<ItemDrop>();

            AddWoodConversionRecipe("RoundLog", CoreWoodToWoodCount.Value); // First one will be used by Recycle_N_Reclaim
            AddWoodConversionRecipe("FineWood", FineWoodToWoodCount.Value);
            AddWoodConversionRecipe("ElderBark", AncientBarkToWoodCount.Value);
            AddWoodConversionRecipe("YggdrasilWood", YggdrasilWoodToWoodCount.Value);
        }

        public static void AddWoodConversionRecipe(string prefabName, Vector2 conversion)
        {
            var recipeWoodToWood = new RecipeConfig()
            {
                amount = Mathf.CeilToInt(conversion.y),
                enabled = true,
                resources = new List<RecipeRequirementConfig>
                {
                    new RecipeRequirementConfig()
                    {
                        item = prefabName,
                        amount = Mathf.CeilToInt(conversion.x)
                    }
                }
            };

            if (recipeWoodToWood.amount > 0 && recipeWoodToWood.resources[0].amount > 0)
                PrefabCreator.AddNewRecipe($"Recipe_{prefabName}ToWood", "Wood", recipeWoodToWood);
        }

        public static ItemDrop GetReplacementFuelItem(Inventory inventory, ItemDrop builtIn)
        {
            if (builtIn != wood)
                return null;

            if (inventory.HaveItem(builtIn.m_itemData.m_shared.m_name))
            {
                return null;
            }

            var fineWood = ObjectDB.instance.GetItemPrefab("FineWood").GetComponent<ItemDrop>();
            var coreWood = ObjectDB.instance.GetItemPrefab("RoundLog").GetComponent<ItemDrop>();
            var ancientBark = ObjectDB.instance.GetItemPrefab("ElderBark").GetComponent<ItemDrop>();
            var yggdrasilWood = ObjectDB.instance.GetItemPrefab("YggdrasilWood").GetComponent<ItemDrop>();

            if (AllowAncientBarkForFuel.Value && inventory.HaveItem(ancientBark.m_itemData.m_shared.m_name))
            {
                return ancientBark;
            }

            if (AllowCoreWoodForFuel.Value && inventory.HaveItem(coreWood.m_itemData.m_shared.m_name))
            {
                return coreWood;
            }

            if (AllowFineWoodForFuel.Value && inventory.HaveItem(fineWood.m_itemData.m_shared.m_name))
            {
                return fineWood;
            }

            if (AllowYggdrasilWoodForFuel.Value && inventory.HaveItem(yggdrasilWood.m_itemData.m_shared.m_name))
            {
                return yggdrasilWood;
            }

            return null;
        }

    }
}