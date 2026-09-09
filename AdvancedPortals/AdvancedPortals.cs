using BepInEx;
using BepInEx.Logging;
using Common;
using HarmonyLib;
using JetBrains.Annotations;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
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
        public const string Version = "1.2.0";

        private static string ConfigFileName = PluginId + ".cfg";
        private static string ConfigFileFullPath = BepInEx.Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        public static readonly ManualLogSource APLogger = BepInEx.Logging.Logger.CreateLogSource(DisplayName);
        private Harmony _harmony;

        [UsedImplicitly]
        private void Awake()
        {
            // Wire the shared Common support layer (config binder, piece loader, drawers) to this plugin
            // before anything else in Common is used -- ConfigBinder and ModLogger read it for the config
            // file and log source.
            ModContext.Initialize(this, APLogger, "AdvancedPortals");

            AssetBundle assetBundle = AssetBundleLoader.LoadIntoContext("advancedportals", typeof(AdvancedPortals).Assembly);
            if (assetBundle == null)
            {
                // Everything below needs the bundle. Bailing here reports one clear cause instead of a
                // cascade of null-reference errors from the piece registrations that follow.
                APLogger.LogError("Failed to load the 'advancedportals' asset bundle. Advanced Portals is disabled for this session.");
                return;
            }

            // SaveOnConfigSet writes the whole .cfg once per bound entry. Batch the portal registrations
            // and flush once instead; this is a measurable chunk of mod load time.
            ModContext.SaveOnSet(false);
            Portals.RegisterAll();
            ModContext.SaveOnSet(true);

            // Fix up mocked portal connect effects
            GameObject fxAncient = assetBundle.LoadAsset<GameObject>("fx_portal_connected_ancient");
            PrefabManager.Instance.AddPrefab(new CustomPrefab(fxAncient, true));
            GameObject fxBlackMarble = assetBundle.LoadAsset<GameObject>("fx_portal_connected_blackmarble");
            PrefabManager.Instance.AddPrefab(new CustomPrefab(fxBlackMarble, true));
            GameObject fxObsidian = assetBundle.LoadAsset<GameObject>("fx_portal_connected_obsidian");
            PrefabManager.Instance.AddPrefab(new CustomPrefab(fxObsidian, true));

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
            SetupWatcher();

            // Apply the teleport allow-lists once the pieces exist. PieceLoader owns the build side
            // (enabled, station, category, cost) and applies that on its own hook.
            PieceManager.OnPiecesRegistered += Portals.ApplyAllTeleportRules;

            // Add Prefabs to Portal Connections
            foreach (string portalPrefab in Portals.PrefabNames)
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
                    // Finalizer, not postfix: must clear CurrentAdvancedPortal even when the click
                    // handler throws, or the stuck static applies this portal's allow-list globally.
                    _harmony.Patch(handlePortalClickMethod, finalizer: new HarmonyMethod(
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

            // A connected client must not reload: Jotunn has already replaced these values with the
            // server's, and a reload would clobber them with whatever this machine happens to have on disk.
            // The main menu (no ZNet yet) and a singleplayer or host session both still reload.
            if (ZNet.instance != null && !ZNet.instance.IsServer()) return;

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

            // Nothing to apply by hand: Config.Reload raises SettingChanged for every entry whose value
            // actually changed, and PieceLoader and Portals both act on that themselves.
            _lastReloadTime = now;
        }
    }
}
