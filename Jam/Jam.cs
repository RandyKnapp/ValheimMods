using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Common;
using HarmonyLib;
using JetBrains.Annotations;
using Newtonsoft.Json;
using ServerSync;
using UnityEngine;

namespace Jam
{
    [BepInPlugin(PluginId, DisplayName, Version)]
    public class Jam : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.jam";
        public const string DisplayName = "Jam";
        public const string Version = "1.0.6";

        private readonly ConfigSync _configSync = new ConfigSync(PluginId) { DisplayName = DisplayName, CurrentVersion = Version, MinimumRequiredVersion = Version };
        private static ConfigEntry<bool> _serverConfigLocked;

        private static Jam _instance;
        private Harmony _harmony;
        public static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        public static Dictionary<string, CustomSyncedValue<string>> SyncedJsonFiles = new Dictionary<string, CustomSyncedValue<string>>();

        [UsedImplicitly]
        public void Awake()
        {
            _instance = this;
            PrefabCreator.Logger = Logger;

            _serverConfigLocked = SyncedConfig("Config Sync", "Lock Config", false, "[Server Only] The configuration is locked and may not be changed by clients once it has been synced from the server. Only valid for server config, will have no effect on clients.");
            _configSync.AddLockingConfigEntry(_serverConfigLocked);

            var assetBundle = LoadAssetBundle("jamassets");
            if (assetBundle != null)
            {
                var prefabs = assetBundle.LoadAllAssets<GameObject>();
                foreach (var prefab in prefabs)
                {
                    Prefabs.Add(prefab.name, prefab);
                }
            }

            LoadJsonFile<RecipesConfig>("recipes.json", RecipesHelper.Initialize);

            assetBundle?.Unload(false);

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        public static string LoadJsonText(string filename)
        {
            var jsonFilePath = GetAssetPath(filename);
            if (string.IsNullOrEmpty(jsonFilePath))
                return null;

            return File.ReadAllText(jsonFilePath);
        }

        public static void LoadJsonFile<T>(string filename, Action<T> onFileLoad, bool update = false) where T : class
        {
            var jsonFile = LoadJsonText(filename);

            if (!update)
            {
                SyncedJsonFiles.Add(filename, new CustomSyncedValue<string>(_instance._configSync, filename, jsonFile));
            }

            void Process()
            {
                T result;
                try
                {
                    result = string.IsNullOrEmpty(SyncedJsonFiles[filename].Value) ? null : JsonConvert.DeserializeObject<T>(SyncedJsonFiles[filename].Value);
                }
                catch (Exception)
                {
                    LogError($"Could not parse file '{filename}'! Errors in JSON!");
                    throw;
                }

                onFileLoad(result);
            }

            SyncedJsonFiles[filename].ValueChanged += Process;
            Process();

            if (jsonFile != null)
            {
                //Primary JSON Watcher
                void ConsumeConfigFileEvent(object s, FileSystemEventArgs e)
                {
                    SyncedJsonFiles[filename].AssignLocalValue(LoadJsonText(filename));
                }

                var filePath = GetAssetPath(filename);
                var directoryName = Path.GetDirectoryName(filePath);
                if (directoryName == null)
                    return;
                var watcher = new FileSystemWatcher(directoryName, Path.GetFileName(filePath));
                watcher.Changed += ConsumeConfigFileEvent;
                watcher.Created += ConsumeConfigFileEvent;
                watcher.Renamed += ConsumeConfigFileEvent;
                watcher.IncludeSubdirectories = true;
                watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
                watcher.EnableRaisingEvents = true;
            }
        }

        public static void LogError(string message)
        {
            _instance.Logger.LogError(message);
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
        }

        public static string GenerateAssetPathAtAssembly(string assetName)
        {
            var assembly = typeof(Jam).Assembly;
            return Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, assetName);
        }

        public static string GetAssetPath(string assetName)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "Jam", assetName);
            if (!File.Exists(assetFileName))
            {
                assetFileName = GenerateAssetPathAtAssembly(assetName);
                if (!File.Exists(assetFileName))
                {
                    _instance.Logger.LogError($"Could not find asset ({assetName})");
                    return null;
                }
            }

            return assetFileName;
        }

        public ConfigEntry<T> SyncedConfig<T>(string group, string configName, T value, string description, bool synchronizedSetting = true)
        {
            var configEntry = Config.Bind(group, configName, value, new ConfigDescription(description));

            var syncedConfigEntry = _configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        [UsedImplicitly]
        public void OnDestroy()
        {
            _instance = null;
            _harmony?.UnpatchSelf();
        }

        public static void TryRegisterPrefabs(ZNetScene zNetScene)
        {
            if (zNetScene == null)
            {
                return;
            }

            foreach (var prefab in Prefabs.Values)
            {
                zNetScene.m_prefabs.Add(prefab);
            }
        }

        public static bool IsObjectDBReady()
        {
            return ObjectDB.instance != null && ObjectDB.instance.m_items.Count != 0 && ObjectDB.instance.GetItemPrefab("Amber") != null;
        }

        public static void TryRegisterItems()
        {
            if (!IsObjectDBReady())
            {
                return;
            }

            foreach (var prefab in Prefabs.Values)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (ObjectDB.instance.GetItemPrefab(prefab.name.GetStableHashCode()) == null)
                    {
                        itemDrop.m_itemData.m_dropPrefab = prefab;
                        ObjectDB.instance.m_items.Add(prefab);
                    }
                }
            }

            ObjectDB.instance.UpdateItemHashes();
        }

        public static void TryRegisterRecipes()
        {
            if (!IsObjectDBReady())
            {
                return;
            }

            RecipesHelper.SetupRecipes();
        }
    }
}
