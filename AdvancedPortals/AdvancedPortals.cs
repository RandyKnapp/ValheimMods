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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AdvancedPortals
{
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Patch)]
    [BepInPlugin(PluginId, DisplayName, Version)]
    [BepInIncompatibility("com.github.xafflict.UnrestrictedPortals")]
    [BepInDependency("org.bepinex.plugins.targetportal", BepInDependency.DependencyFlags.SoftDependency)]
    public class AdvancedPortals : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.advancedportals";
        public const string DisplayName = "Advanced Portals";
        public const string Version = "1.1.3";

        private static string ConfigFileName = PluginId + ".cfg";
        private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static readonly string[] PortalPrefabs = { "portal_ancient", "portal_obsidian", "portal_blackmarble" };

        public static readonly List<GameObject> RegisteredPrefabs = new List<GameObject>();

        public static ConfigEntry<bool> AncientPortalEnabled;
        public static string AncientPortalRecipeDefault = "ElderBark:20,Iron:5,SurtlingCore:2";
        public static ConfigEntry<string> AncientPortalRecipe = null!;
        public static ConfigEntry<string> AncientPortalAllowedItems = null!;
        public static ConfigEntry<bool> AncientPortalAllowEverything = null!;

        public static ConfigEntry<bool> ObsidianPortalEnabled;
        public static string ObsidianPortalRecipeDefault = "Obsidian:20,Silver:5,SurtlingCore:2";
        public static ConfigEntry<string> ObsidianPortalRecipe;
        public static ConfigEntry<string> ObsidianPortalAllowedItems = null!;
        public static ConfigEntry<bool> ObsidianPortalAllowEverything = null!;
        public static ConfigEntry<bool> ObsidianPortalAllowPreviousPortalItems = null!;

        public static ConfigEntry<bool> BlackMarblePortalEnabled;
        public static string BlackMarblePortalRecipeDefault = "BlackMarble:20,BlackMetal:5,Eitr:2";
        public static ConfigEntry<string> BlackMarblePortalRecipe;
        public static ConfigEntry<string> BlackMarblePortalAllowedItems = null!;
        public static ConfigEntry<bool> BlackMarblePortalAllowEverything = null!;
        public static ConfigEntry<bool> BlackMarblePortalAllowPreviousPortalItems = null!;

        public static readonly ManualLogSource APLogger = BepInEx.Logging.Logger.CreateLogSource(DisplayName);
        private Harmony _harmony;

        [UsedImplicitly]
        private void Awake()
        {
            AddConfig("Portal 1 - Ancient", "Ancient Portal Enabled",
                "Enable the Ancient Portal",
                true, true, ref AncientPortalEnabled);
            AddConfig("Portal 1 - Ancient", "Ancient Portal Recipe",
                "The items needed to build the Ancient Portal. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, AncientPortalRecipeDefault, ref AncientPortalRecipe);
            AddConfig("Portal 1 - Ancient", "Ancient Portal Allowed Items",
                "A comma separated list of the item types allowed through the Ancient Portal",
                true, "Copper, CopperOre, CopperScrap, Tin, TinOre, Bronze, BronzeScrap", ref AncientPortalAllowedItems);
            AddConfig("Portal 1 - Ancient", "Ancient Portal Allow Everything",
                "Allow all items through the Ancient Portal (overrides Allowed Items)",
                true, false, ref AncientPortalAllowEverything);

            AddConfig("Portal 2 - Obsidian", "Obsidian Portal Enabled",
                "Enable the Obsidian Portal",
                true, true, ref ObsidianPortalEnabled);
            AddConfig("Portal 2 - Obsidian", "Obsidian Portal Recipe",
                "The items needed to build the Obsidian Portal. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, ObsidianPortalRecipeDefault, ref ObsidianPortalRecipe);
            AddConfig("Portal 2 - Obsidian", "Obsidian Portal Allowed Items",
                "A comma separated list of the item types allowed through the Obsidian Portal",
                true, "Iron, IronScrap, IronOre", ref ObsidianPortalAllowedItems);
            AddConfig("Portal 2 - Obsidian", "Obsidian Portal Allow Everything",
                "Allow all items through the Obsidian Portal (overrides Allowed Items)",
                true, false, ref ObsidianPortalAllowEverything);
            AddConfig("Portal 2 - Obsidian", "Obsidian Portal Use All Previous",
                "Additionally allow all items from the Ancient Portal",
                true, true, ref ObsidianPortalAllowPreviousPortalItems);

            AddConfig("Portal 3 - Black Marble", "Black Marble Portal Enabled",
                "Enable the Black Marble Portal",
                true, true, ref BlackMarblePortalEnabled);
            AddConfig("Portal 3 - Black Marble", "Black Marble Portal Recipe",
                "The items needed to build the Black Marble Portal. A comma separated list of ITEM:QUANTITY pairs separated by a colon.",
                true, BlackMarblePortalRecipeDefault, ref BlackMarblePortalRecipe);
            AddConfig("Portal 3 - Black Marble", "Black Marble Portal Allowed Items",
                "A comma separated list of the item types allowed through the Black Marble Portal",
                true, "Silver, SilverOre, BlackMetal, BlackMetalScrap", ref BlackMarblePortalAllowedItems);
            AddConfig("Portal 3 - Black Marble", "Black Marble Portal Allow Everything",
                "Allow all items through the Black Marble Portal (overrides Allowed Items)",
                true, true, ref BlackMarblePortalAllowEverything);
            AddConfig("Portal 3 - Black Marble", "Black Marble Portal Use All Previous",
                "Additionally allow all items from the Obsidian and Ancient Portal",
                true, true, ref BlackMarblePortalAllowPreviousPortalItems);

            AssetBundle assetBundle = LoadAssetBundle("advancedportals");

            LoadBuildPiece(assetBundle, "portal_ancient", new PieceConfig()
            {
                Name = "$item_elderbark $piece_portal",
                Description = "$piece_portal_description",
                PieceTable = "Hammer",
                Category = "Misc",
                CraftingStation = "piece_workbench",
                Requirements = UpdatePortals.MakeRecipeFromConfig("Ancient Portal", AncientPortalRecipe.Value).ToArray()
            });

            LoadBuildPiece(assetBundle, "portal_obsidian", new PieceConfig()
            {
                Name = "$item_obsidian $piece_portal",
                Description = "$piece_portal_description",
                PieceTable = "Hammer",
                Category = "Misc",
                CraftingStation = "piece_workbench",
                Requirements = UpdatePortals.MakeRecipeFromConfig("Obsidian Portal", ObsidianPortalRecipe.Value).ToArray()
            });

            LoadBuildPiece(assetBundle, "portal_blackmarble", new PieceConfig()
            {
                Name = "$item_blackmarble $piece_portal",
                Description = "$piece_portal_description",
                PieceTable = "Hammer",
                Category = "Misc",
                CraftingStation = "piece_workbench",
                Requirements = UpdatePortals.MakeRecipeFromConfig("Black Marble Portal", BlackMarblePortalRecipe.Value).ToArray()
            });

            // Fix up mocked portal connect effects
            GameObject fxAncient = assetBundle.LoadAsset<GameObject>("fx_portal_connected_ancient");
            PrefabManager.Instance.AddPrefab(new CustomPrefab(fxAncient, true));
            GameObject fxBlackMarble = assetBundle.LoadAsset<GameObject>("fx_portal_connected_blackmarble");
            PrefabManager.Instance.AddPrefab(new CustomPrefab(fxBlackMarble, true));
            GameObject fxObsidian = assetBundle.LoadAsset<GameObject>("fx_portal_connected_obsidian");
            PrefabManager.Instance.AddPrefab(new CustomPrefab(fxObsidian, true));

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
            SetupWatcher();

            // Ensure configurations apply in singleplayer games
            PieceManager.OnPiecesRegistered += UpdatePortalsOnGameStart;

            // Add Prefabs to Portal Connections
            foreach (string portalPrefab in PortalPrefabs)
            {
                AddPortal.Hashes.Add(portalPrefab.GetStableHashCode());
            }

            // Patch TargetPortal's handle click method, since it does not directly call TeleportWorld.Teleport
            Component targetPortal = gameObject.GetComponent("TargetPortal.TargetPortal");
            if (targetPortal)
            {
                Type pluginType = targetPortal.GetType();
                Type mapType = pluginType.Assembly.GetType("TargetPortal.Map");
                MethodInfo handlePortalClickMethod = AccessTools.DeclaredMethod(mapType, "HandlePortalClick");
                if (handlePortalClickMethod != null)
                {
                    _harmony.Patch(handlePortalClickMethod, new HarmonyMethod(
                        typeof(Teleport_Patch), nameof(Teleport_Patch.TargetPortal_HandlePortalClick_Prefix)));
                    _harmony.Patch(handlePortalClickMethod, null, new HarmonyMethod(
                        typeof(Teleport_Patch), nameof(Teleport_Patch.Generic_Postfix)));
                }
            }
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
                APLogger.LogInfo("Attempting to reload configuration...");
                Config.Reload();
            }
            catch
            {
                APLogger.LogWarning($"There was an issue loading {ConfigFileName}");
                return;
            }

            if (ZNet.instance != null && !ZNet.instance.IsDedicated())
            {
                UpdatePortals.UpdatePortalConfigurations();
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

        private static void LoadBuildPiece(AssetBundle assetBundle, string assetName, PieceConfig piececonfig)
        {
            GameObject prefab = assetBundle.LoadAsset<GameObject>(assetName);
            PieceManager.Instance.AddPiece(new CustomPiece(prefab, true, piececonfig));
        }

        private static void UpdatePortalsOnGameStart()
        {
            UpdatePortals.UpdatePortalConfigurations();
        }
    }
}
