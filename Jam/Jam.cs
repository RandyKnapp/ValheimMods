using BepInEx;
using BepInEx.Logging;
using Common;
using JetBrains.Annotations;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
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
        public const string Version = "2.0.0";

        public static readonly ManualLogSource JamLogger = BepInEx.Logging.Logger.CreateLogSource(DisplayName);

        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        [UsedImplicitly]
        public void Awake()
        {
            // Wires the shared Common layer (ConfigBinder, ModLogger, ConfigChangeDebouncer) to this
            // plugin's config file and logger. Must run before anything binds a config entry.
            ModContext.Initialize(this, JamLogger, DisplayName);

            AssetBundle assetBundle = LoadAssetBundle("jamassets");
            if (assetBundle == null)
            {
                JamLogger.LogError("Could not load Jam assets! This mod will not load items!");
                return;
            }

            string localizedJson = AssetUtils.LoadTextFromResources("Localization.English.json", Assembly.GetExecutingAssembly());
            Localization.AddJsonFile("English", localizedJson);

            new JamItems(assetBundle);
            ServingTray.Register();
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            Config.Save();
        }

        // The assembly is named explicitly rather than taken from Assembly.GetCallingAssembly(). When
        // another mod hooks Awake, MonoMod recompiles it as a dynamic method (DMD<...::Awake>), and the
        // "calling assembly" is then that dynamic assembly, not this one. The resource lookup misses,
        // LoadFromStream(null) throws "ArgumentNullException: stream", and the mod fails to load —
        // intermittently, since it depends on which other mods are present.
        public static AssetBundle LoadAssetBundle(string filename)
        {
            return AssetBundleLoader.LoadFromResources(filename, typeof(Jam).Assembly);
        }
    }
}
