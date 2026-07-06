using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Jam
{
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    [BepInPlugin(PluginId, DisplayName, Version)]
    public class Jam : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.jam";
        public const string DisplayName = "Jam";
        public const string Version = "1.1.0";

        private static string ConfigFileName = PluginId + ".cfg";
        private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static readonly ManualLogSource JamLogger = BepInEx.Logging.Logger.CreateLogSource(DisplayName);

        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        public static readonly string[] Jams =
        {
            "RaspberryJam",
            "HoneyRaspberryJam",
            "BlueberryJam",
            "HoneyBlueberryJam",
            "CloudberryJam",
            "HoneyCloudberryJam",
            "KingsJam",
            "NordicJam",
            "MushroomJam",
            "AshlandsJam"
        };

        public static ConfigEntry<bool> RaspberryJamEnabled;
        public static string RaspberryJamRecipeDefault = "Raspberry:14";
        public static ConfigEntry<string> RaspberryJamRecipe = null!;
        public static ConfigEntry<float> RaspberryJamDuration = null!;
        public static ConfigEntry<float> RaspberryJamHealth = null!;
        public static ConfigEntry<float> RaspberryJamStamina = null!;
        public static ConfigEntry<float> RaspberryJamEtir = null!;
        public static ConfigEntry<float> RaspberryJamRegen = null!;

        public static ConfigEntry<bool> HoneyRaspberryJamEnabled;
        public static string HoneyRaspberryJamRecipeDefault = "Raspberry:8,Honey:4";
        public static ConfigEntry<string> HoneyRaspberryJamRecipe = null!;
        public static ConfigEntry<float> HoneyRaspberryJamDuration = null!;
        public static ConfigEntry<float> HoneyRaspberryJamHealth = null!;
        public static ConfigEntry<float> HoneyRaspberryJamStamina = null!;
        public static ConfigEntry<float> HoneyRaspberryJamEtir = null!;
        public static ConfigEntry<float> HoneyRaspberryJamRegen = null!;

        public static ConfigEntry<bool> BlueberryJamEnabled;
        public static string BlueberryJamRecipeDefault = "Blueberries:14";
        public static ConfigEntry<string> BlueberryJamRecipe = null!;
        public static ConfigEntry<float> BlueberryJamDuration = null!;
        public static ConfigEntry<float> BlueberryJamHealth = null!;
        public static ConfigEntry<float> BlueberryJamStamina = null!;
        public static ConfigEntry<float> BlueberryJamEtir = null!;
        public static ConfigEntry<float> BlueberryJamRegen = null!;

        public static ConfigEntry<bool> HoneyBlueberryJamEnabled;
        public static string HoneyBlueberryJamRecipeDefault = "Blueberries:8,Honey:4";
        public static ConfigEntry<string> HoneyBlueberryJamRecipe = null!;
        public static ConfigEntry<float> HoneyBlueberryJamDuration = null!;
        public static ConfigEntry<float> HoneyBlueberryJamHealth = null!;
        public static ConfigEntry<float> HoneyBlueberryJamStamina = null!;
        public static ConfigEntry<float> HoneyBlueberryJamEtir = null!;
        public static ConfigEntry<float> HoneyBlueberryJamRegen = null!;

        public static ConfigEntry<bool> CloudberryJamEnabled;
        public static string CloudberryJamRecipeDefault = "Cloudberry:14";
        public static ConfigEntry<string> CloudberryJamRecipe = null!;
        public static ConfigEntry<float> CloudberryJamDuration = null!;
        public static ConfigEntry<float> CloudberryJamHealth = null!;
        public static ConfigEntry<float> CloudberryJamStamina = null!;
        public static ConfigEntry<float> CloudberryJamEtir = null!;
        public static ConfigEntry<float> CloudberryJamRegen = null!;

        public static ConfigEntry<bool> HoneyCloudberryJamEnabled;
        public static string HoneyCloudberryJamRecipeDefault = "Cloudberry:8,Honey:4";
        public static ConfigEntry<string> HoneyCloudberryJamRecipe = null!;
        public static ConfigEntry<float> HoneyCloudberryJamDuration = null!;
        public static ConfigEntry<float> HoneyCloudberryJamHealth = null!;
        public static ConfigEntry<float> HoneyCloudberryJamStamina = null!;
        public static ConfigEntry<float> HoneyCloudberryJamEtir = null!;
        public static ConfigEntry<float> HoneyCloudberryJamRegen = null!;

        public static ConfigEntry<bool> KingsJamEnabled;
        public static string KingsJamRecipeDefault = "Raspberry:8,Cloudberry:6";
        public static ConfigEntry<string> KingsJamRecipe = null!;
        public static ConfigEntry<float> KingsJamDuration = null!;
        public static ConfigEntry<float> KingsJamHealth = null!;
        public static ConfigEntry<float> KingsJamStamina = null!;
        public static ConfigEntry<float> KingsJamEtir = null!;
        public static ConfigEntry<float> KingsJamRegen = null!;

        public static ConfigEntry<bool> NordicJamEnabled;
        public static string NordicJamRecipeDefault = "Blueberries:8,Cloudberry:6";
        public static ConfigEntry<string> NordicJamRecipe = null!;
        public static ConfigEntry<float> NordicJamDuration = null!;
        public static ConfigEntry<float> NordicJamHealth = null!;
        public static ConfigEntry<float> NordicJamStamina = null!;
        public static ConfigEntry<float> NordicJamEtir = null!;
        public static ConfigEntry<float> NordicJamRegen = null!;

        public static ConfigEntry<bool> MushroomJamEnabled;
        public static string MushroomJamRecipeDefault = "MushroomMagecap:4,MushroomJotunPuffs:4,Onion:2,Honey:2";
        public static ConfigEntry<string> MushroomJamRecipe = null!;
        public static ConfigEntry<float> MushroomJamDuration = null!;
        public static ConfigEntry<float> MushroomJamHealth = null!;
        public static ConfigEntry<float> MushroomJamStamina = null!;
        public static ConfigEntry<float> MushroomJamEtir = null!;
        public static ConfigEntry<float> MushroomJamRegen = null!;

        public static ConfigEntry<bool> AshlandsJamEnabled;
        public static string AshlandsJamRecipeDefault = "Vineberry:4,MushroomSmokePuff:4,Onion:2,Honey:2";
        public static ConfigEntry<string> AshlandsJamRecipe = null!;
        public static ConfigEntry<float> AshlandsJamDuration = null!;
        public static ConfigEntry<float> AshlandsJamHealth = null!;
        public static ConfigEntry<float> AshlandsJamStamina = null!;
        public static ConfigEntry<float> AshlandsJamEtir = null!;
        public static ConfigEntry<float> AshlandsJamRegen = null!;

        private static Jam _instance;
        private Harmony _harmony;

        [UsedImplicitly]
        public void Awake()
        {
            _instance = this;

            AddConfig("Jam 1 - RaspberryJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref RaspberryJamEnabled);
            AddConfig("Jam 1 - RaspberryJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, RaspberryJamRecipeDefault, ref RaspberryJamRecipe);
            AddConfig("Jam 1 - RaspberryJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1200f, ref RaspberryJamDuration);
            AddConfig("Jam 1 - RaspberryJam", "Jam Health",
                "Health value the Jam grants.",
                true, 14f, ref RaspberryJamHealth);
            AddConfig("Jam 1 - RaspberryJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 30f, ref RaspberryJamStamina);
            AddConfig("Jam 1 - RaspberryJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 0f, ref RaspberryJamEtir);
            AddConfig("Jam 1 - RaspberryJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 1f, ref RaspberryJamRegen);

            AddConfig("Jam 2 - HoneyRaspberryJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref HoneyRaspberryJamEnabled);
            AddConfig("Jam 2 - HoneyRaspberryJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, HoneyRaspberryJamRecipeDefault, ref HoneyRaspberryJamRecipe);
            AddConfig("Jam 2 - HoneyRaspberryJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1200f, ref HoneyRaspberryJamDuration);
            AddConfig("Jam 2 - HoneyRaspberryJam", "Jam Health",
                "Health value the Jam grants.",
                true, 18f, ref HoneyRaspberryJamHealth);
            AddConfig("Jam 2 - HoneyRaspberryJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 35f, ref HoneyRaspberryJamStamina);
            AddConfig("Jam 2 - HoneyRaspberryJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 0f, ref HoneyRaspberryJamEtir);
            AddConfig("Jam 2 - HoneyRaspberryJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 2f, ref HoneyRaspberryJamRegen);

            AddConfig("Jam 3 - BlueberryJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref BlueberryJamEnabled);
            AddConfig("Jam 3 - BlueberryJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, BlueberryJamRecipeDefault, ref BlueberryJamRecipe);
            AddConfig("Jam 3 - BlueberryJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1200f, ref BlueberryJamDuration);
            AddConfig("Jam 3 - BlueberryJam", "Jam Health",
                "Health value the Jam grants.",
                true, 14f, ref BlueberryJamHealth);
            AddConfig("Jam 3 - BlueberryJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 35f, ref BlueberryJamStamina);
            AddConfig("Jam 3 - BlueberryJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 0f, ref BlueberryJamEtir);
            AddConfig("Jam 3 - BlueberryJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 1f, ref BlueberryJamRegen);

            AddConfig("Jam 4 - HoneyBlueberryJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref HoneyBlueberryJamEnabled);
            AddConfig("Jam 4 - HoneyBlueberryJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, HoneyBlueberryJamRecipeDefault, ref HoneyBlueberryJamRecipe);
            AddConfig("Jam 4 - HoneyBlueberryJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1200f, ref HoneyBlueberryJamDuration);
            AddConfig("Jam 4 - HoneyBlueberryJam", "Jam Health",
                "Health value the Jam grants.",
                true, 18f, ref HoneyBlueberryJamHealth);
            AddConfig("Jam 4 - HoneyBlueberryJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 40f, ref HoneyBlueberryJamStamina);
            AddConfig("Jam 4 - HoneyBlueberryJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 0f, ref HoneyBlueberryJamEtir);
            AddConfig("Jam 4 - HoneyBlueberryJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 2f, ref HoneyBlueberryJamRegen);

            AddConfig("Jam 5 - CloudberryJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref CloudberryJamEnabled);
            AddConfig("Jam 5 - CloudberryJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, CloudberryJamRecipeDefault, ref CloudberryJamRecipe);
            AddConfig("Jam 5 - CloudberryJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1200f, ref CloudberryJamDuration);
            AddConfig("Jam 5 - CloudberryJam", "Jam Health",
                "Health value the Jam grants.",
                true, 26f, ref CloudberryJamHealth);
            AddConfig("Jam 5 - CloudberryJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 45f, ref CloudberryJamStamina);
            AddConfig("Jam 5 - CloudberryJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 0f, ref CloudberryJamEtir);
            AddConfig("Jam 5 - CloudberryJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 1f, ref CloudberryJamRegen);

            AddConfig("Jam 6 - HoneyCloudberryJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref HoneyCloudberryJamEnabled);
            AddConfig("Jam 6 - HoneyCloudberryJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, HoneyCloudberryJamRecipeDefault, ref HoneyCloudberryJamRecipe);
            AddConfig("Jam 6 - HoneyCloudberryJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1200f, ref HoneyCloudberryJamDuration);
            AddConfig("Jam 6 - HoneyCloudberryJam", "Jam Health",
                "Health value the Jam grants.",
                true, 30f, ref HoneyCloudberryJamHealth);
            AddConfig("Jam 6 - HoneyCloudberryJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 50f, ref HoneyCloudberryJamStamina);
            AddConfig("Jam 6 - HoneyCloudberryJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 0f, ref HoneyCloudberryJamEtir);
            AddConfig("Jam 6 - HoneyCloudberryJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 2f, ref HoneyCloudberryJamRegen);

            AddConfig("Jam 7 - KingsJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref KingsJamEnabled);
            AddConfig("Jam 7 - KingsJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, KingsJamRecipeDefault, ref KingsJamRecipe);
            AddConfig("Jam 7 - KingsJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1200f, ref KingsJamDuration);
            AddConfig("Jam 7 - KingsJam", "Jam Health",
                "Health value the Jam grants.",
                true, 26f, ref KingsJamHealth);
            AddConfig("Jam 7 - KingsJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 50f, ref KingsJamStamina);
            AddConfig("Jam 7 - KingsJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 0f, ref KingsJamEtir);
            AddConfig("Jam 7 - KingsJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 2f, ref KingsJamRegen);

            AddConfig("Jam 8 - NordicJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref NordicJamEnabled);
            AddConfig("Jam 8 - NordicJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, NordicJamRecipeDefault, ref NordicJamRecipe);
            AddConfig("Jam 8 - NordicJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1200f, ref NordicJamDuration);
            AddConfig("Jam 8 - NordicJam", "Jam Health",
                "Health value the Jam grants.",
                true, 26f, ref NordicJamHealth);
            AddConfig("Jam 8 - NordicJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 55f, ref NordicJamStamina);
            AddConfig("Jam 8 - NordicJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 0f, ref NordicJamEtir);
            AddConfig("Jam 8 - NordicJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 2f, ref NordicJamRegen);

            AddConfig("Jam 9 - MushroomJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref MushroomJamEnabled);
            AddConfig("Jam 9 - MushroomJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, MushroomJamRecipeDefault, ref MushroomJamRecipe);
            AddConfig("Jam 9 - MushroomJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1500f, ref MushroomJamDuration);
            AddConfig("Jam 9 - MushroomJam", "Jam Health",
                "Health value the Jam grants.",
                true, 35f, ref MushroomJamHealth);
            AddConfig("Jam 9 - MushroomJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 55f, ref MushroomJamStamina);
            AddConfig("Jam 9 - MushroomJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 30f, ref MushroomJamEtir);
            AddConfig("Jam 9 - MushroomJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 3f, ref MushroomJamRegen);

            AddConfig("Jam 10 - AshlandsJam", "Jam Enabled",
                "Enable the Jam to be crafted",
                true, true, ref AshlandsJamEnabled);
            AddConfig("Jam 10 - AshlandsJam", "Jam Recipe",
                "The items needed to craft the Jam. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, AshlandsJamRecipeDefault, ref AshlandsJamRecipe);
            AddConfig("Jam 10 - AshlandsJam", "Jam Duration",
                "Duration the Jam last in seconds.",
                true, 1500f, ref AshlandsJamDuration);
            AddConfig("Jam 10 - AshlandsJam", "Jam Health",
                "Health value the Jam grants.",
                true, 40f, ref AshlandsJamHealth);
            AddConfig("Jam 10 - AshlandsJam", "Jam Stamina",
                "Stamina value the Jam grants.",
                true, 60f, ref AshlandsJamStamina);
            AddConfig("Jam 10 - AshlandsJam", "Jam Etir",
                "Etir value the Jam grants.",
                true, 35f, ref AshlandsJamEtir);
            AddConfig("Jam 10 - AshlandsJam", "Jam Health Regen",
                "Health Regen value the Jam grants.",
                true, 4f, ref AshlandsJamRegen);

            var assetBundle = LoadAssetBundle("jamassets");
            if (assetBundle != null)
            {
                var prefabs = assetBundle.LoadAllAssets<GameObject>();
                LoadItem(assetBundle, "RaspberryJam", RaspberryJamRecipe.Value, 1);
                LoadItem(assetBundle, "HoneyRaspberryJam", HoneyRaspberryJamRecipe.Value, 1);
                LoadItem(assetBundle, "BlueberryJam", BlueberryJamRecipe.Value, 1);
                LoadItem(assetBundle, "HoneyBlueberryJam", HoneyBlueberryJamRecipe.Value, 1);
                LoadItem(assetBundle, "CloudberryJam", CloudberryJamRecipe.Value, 3);
                LoadItem(assetBundle, "HoneyCloudberryJam", HoneyCloudberryJamRecipe.Value, 3);
                LoadItem(assetBundle, "NordicJam", NordicJamRecipe.Value, 3);
                LoadItem(assetBundle, "KingsJam", KingsJamRecipe.Value, 3);
                LoadItem(assetBundle, "MushroomJam", MushroomJamRecipe.Value, 4);
                LoadItem(assetBundle, "AshlandsJam", AshlandsJamRecipe.Value, 5);
            }
            else
            {
                JamLogger.LogError("Could not load Jam assets! This mod will not load items!");
                return;
            }

            string localizedJson = AssetUtils.LoadTextFromResources("Localization.English.json", Assembly.GetExecutingAssembly());
            Localization.AddJsonFile("English", localizedJson);

            SetupWatcher();
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            // Ensure configurations apply in singleplayer games
            ItemManager.OnItemsRegistered += UpdateItemsOnGameStart;
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(BepInEx.Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private DateTime _lastReloadTime;
        private const long RELOAD_DELAY = 10000000; // One second

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            DateTime now = DateTime.Now;
            long time = now.Ticks - _lastReloadTime.Ticks;
            if (!File.Exists(ConfigFileFullPath) || time < RELOAD_DELAY) return;

            try
            {
                JamLogger.LogInfo("Attempting to reload configuration...");
                Config.Reload();
            }
            catch
            {
                JamLogger.LogWarning($"There was an issue loading {ConfigFileName}");
                return;
            }

            if (ZNet.instance != null && !ZNet.instance.IsDedicated())
            {
                UpdateJams.UpdateJamConfigruations();
            }

            _lastReloadTime = now;
        }

        private readonly ConfigurationManagerAttributes AdminConfig = new ConfigurationManagerAttributes { IsAdminOnly = true };
        private readonly ConfigurationManagerAttributes ClientConfig = new ConfigurationManagerAttributes { IsAdminOnly = false };

        private void AddConfig<T>(string section, string key, string description, bool synced, T value, ref ConfigEntry<T> configEntry)
        {
            string extendedDescription = GetExtendedDescription(description, synced);
            configEntry = Config.Bind(section, key, value,
                new ConfigDescription(extendedDescription, null, synced ? AdminConfig : ClientConfig));
        }

        public string GetExtendedDescription(string description, bool synchronizedSetting)
        {
            return description + (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]");
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            Assembly assembly = Assembly.GetCallingAssembly();
            AssetBundle assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
        }

        private static void LoadItem(AssetBundle assetBundle, string assetName, string recipe, int stationLevel)
        {
            GameObject prefab = assetBundle.LoadAsset<GameObject>(assetName);
            ItemManager.Instance.AddItem(new CustomItem(prefab, true, new ItemConfig()
            {
                Amount = 4,
                CraftingStation = CraftingStations.Cauldron,
                MinStationLevel = stationLevel,
                Requirements = RecipesHelper.MakeRecipeFromConfig(assetName, recipe).ToArray()
            }));
        }

        private static void UpdateItemsOnGameStart()
        {
            UpdateJams.UpdateJamConfigruations();
        }
    }
}
