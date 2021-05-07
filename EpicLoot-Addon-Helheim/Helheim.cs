using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using fastJSON;
using HarmonyLib;
using UnityEngine;

namespace EpicLoot_Addon_Helheim
{
    public class Assets
    {
        public GameObject HelheimTextPrefab;
    }

    [BepInPlugin(PluginId, "Epic Loot Addon - Helheim", Version)]
    [BepInDependency("randyknapp.mods.epicloot")]
    [BepInDependency("org.bepinex.plugins.creaturelevelcontrol")]
    public class Helheim : BaseUnityPlugin
    {
        private const string PluginId = "randyknapp.mods.epicloot.addon.helheim";
        private const string Version = "1.0.0";

        private static ConfigEntry<bool> _loggingEnabled;

        private static Helheim _instance;

        public const int HelheimLevelCount = 5;
        public static int HelheimLevel { get; private set; }
        public static readonly Assets Assets = new Assets();

        public static event Action<int> HelheimLevelChanged;

        public HelheimConfig HelheimConfig;

        public Helheim()
        {
            LoadEmbeddedAssembly("fastJSON.dll");
        }

        public void Awake()
        {
            _instance = this;
            _loggingEnabled = Config.Bind("Logging", "Logging Enabled", false, "Enable logging");

            var assetBundle = LoadAssetBundle("helheim");
            Assets.HelheimTextPrefab = assetBundle.LoadAsset<GameObject>("HelheimText");

            HelheimConfig = LoadJsonFile<HelheimConfig>("helheim.json");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            CreatureLevelControl.CreatureLevelControl.difficulty.Value = CreatureLevelControl.CreatureLevelControl.Difficulty.Custom;
        }

        private static void LoadEmbeddedAssembly(string assemblyName)
        {
            var stream = GetManifestResourceStream(assemblyName);
            if (stream == null)
            {
                LogError($"Could not load embedded assembly ({assemblyName})!");
                return;
            }

            using (stream)
            {
                var data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                Assembly.Load(data);
            }
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            using (var stream = GetManifestResourceStream(filename))
            {
                return AssetBundle.LoadFromStream(stream);
            }
        }

        public static Stream GetManifestResourceStream(string filename)
        {
            var assembly = Assembly.GetCallingAssembly();
            var fullname = assembly.GetManifestResourceNames().SingleOrDefault(x => x.EndsWith(filename));
            if (!string.IsNullOrEmpty(fullname))
            {
                return assembly.GetManifestResourceStream(fullname);
            }

            return null;
        }

        public static T LoadJsonFile<T>(string filename) where T : class
        {
            var jsonFile = LoadJsonText(filename);
            T result;
            try
            {
                result = string.IsNullOrEmpty(jsonFile) ? null : JSON.ToObject<T>(jsonFile);
            }
            catch (Exception)
            {
                LogError($"Could not parse file '{filename}'! Errors in JSON!");
                throw;
            }

            return result;
        }

        public static string LoadJsonText(string filename)
        {
            var jsonFileName = GetAssetPath(filename);
            return !string.IsNullOrEmpty(jsonFileName) ? File.ReadAllText(jsonFileName) : null;
        }

        public static string GetAssetPath(string assetName)
        {
            var assembly = typeof(Helheim).Assembly;
            var assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, assetName);
            if (!File.Exists(assetFileName))
            {
                LogError($"Could not find asset ({assetName})");
                return null;
            }

            return assetFileName;
        }

        public static void SetLevel(int level)
        {
            if (HelheimLevel != level)
            {
                HelheimLevel = Mathf.Clamp(level, 0, HelheimLevelCount);
                HelheimLevelChanged?.Invoke(HelheimLevel);
                LogWarning($"Helheim level: {HelheimLevel}");
            }
        }

        public static void Log(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogWarning(message);
            }
        }

        public static void LogError(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogError(message);
            }
        }
    }
}
