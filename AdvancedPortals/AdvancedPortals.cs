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

        private FileSystemWatcher _configWatcher;

        private void SetupWatcher()
        {
            // The reload is driven by ConfigFileReloader watching the file's timestamp; the events
            // below only ask it to look sooner than its own poll would. See that class for why the
            // events on their own are not enough on a Linux server, and why a connected client does
            // not reload.
            //
            // Nothing to apply by hand once it does: Config.Reload raises SettingChanged for every
            // entry whose value actually changed, and PieceLoader and Portals both act on that
            // themselves.
            ConfigFileReloader.Begin(Config, ConfigFileFullPath);

            FileSystemWatcher watcher = new(BepInEx.Paths.ConfigPath, ConfigFileName);
            watcher.Changed += OnConfigFileEvent;
            watcher.Created += OnConfigFileEvent;
            watcher.Renamed += OnConfigFileEvent;
            // FileName included so an editor that saves by writing a temp file and renaming it over
            // the config still reports; LastWrite alone only covers writes made in place.
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
            // Not IncludeSubdirectories: the file only ever lives directly in BepInEx/config, and
            // recursing made Mono register an inotify watch for every subdirectory under it - every
            // other mod's config folder included - for a filter that can only ever match at the top.
            //
            // Held in a field so it can be disposed or replaced later. Mono's watcher backends keep
            // every instance rooted until Dispose, so this is not what keeps events flowing.
            _configWatcher = watcher;
        }

        private void OnConfigFileEvent(object sender, FileSystemEventArgs e)
        {
            ConfigFileReloader.CheckSoon();
        }
    }
}
