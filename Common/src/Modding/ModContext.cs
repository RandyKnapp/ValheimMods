using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Jotunn.Entities;
using UnityEngine;

namespace Common {
    /// <summary>
    /// Per-mod ambient state that the shared loaders in this folder read from.
    ///
    /// Common is a *shared project*: its source is compiled into each consuming mod assembly, so these
    /// statics are per-mod and never shared between plugins. Each plugin calls <see cref="Initialize"/>
    /// once from Awake, before anything else in Common is used.
    /// </summary>
    public static class ModContext {
        /// <summary>The owning plugin's log source. Prefer <see cref="ModLogger"/> over using this directly.</summary>
        public static ManualLogSource Log { get; private set; }

        /// <summary>The owning plugin's config file. <see cref="ConfigBinder"/> binds against this.</summary>
        public static ConfigFile Cfg { get; private set; }

        /// <summary>The owning plugin's BepInPlugin GUID, used to namespace Jotunn registrations.</summary>
        public static string PluginGuid { get; private set; }

        /// <summary>Subfolder under BepInEx/config that this mod owns (localizations, patch files, ...).</summary>
        public static string ConfigFolder { get; private set; }

        /// <summary>The mod's asset bundle. Set here or passed explicitly to the loaders that need one.</summary>
        public static AssetBundle AssetBundle { get; set; }

        /// <summary>The mod's Jotunn localization instance.</summary>
        public static CustomLocalization Localization { get; set; }

        /// <summary>Gates the verbose logging in the shared loaders. Never null after <see cref="Initialize"/>.</summary>
        public static ConfigEntry<bool> EnableDebugMode { get; private set; }

        /// <summary>Seconds <see cref="ConfigChangeDebouncer"/> waits before applying a settled config edit.</summary>
        public static ConfigEntry<float> ConfigApplyDelay { get; private set; }

        public static bool Initialized { get; private set; }

        /// <summary>True when debug logging is on. Safe to call before Initialize.</summary>
        public static bool DebugEnabled => EnableDebugMode != null && EnableDebugMode.Value;

        /// <summary>
        /// Wires the shared Common code to this plugin. Call once from Awake, before binding any other
        /// config or invoking <see cref="PieceLoader"/> / <see cref="ItemBatchLoader"/> / <see cref="ModLocalization"/>.
        /// </summary>
        /// <param name="plugin">The BepInEx plugin instance (supplies the config file and GUID).</param>
        /// <param name="log">The plugin's log source. BaseUnityPlugin.Logger is protected, so the plugin
        /// has to hand it over; call this from inside the plugin and pass its own <c>Logger</c>.</param>
        /// <param name="configFolder">Subfolder name under BepInEx/config owned by this mod.</param>
        /// <param name="assetBundle">Optional; may also be assigned later once the bundle is loaded.</param>
        /// <param name="localization">Optional; defaults to the mod's Jotunn localization if left null.</param>
        /// <param name="configSection">Config section the two baseline entries are bound into.</param>
        public static void Initialize(BaseUnityPlugin plugin, ManualLogSource log, string configFolder, AssetBundle assetBundle = null,
                                      CustomLocalization localization = null, string configSection = "Common") {
            if (plugin == null) { throw new System.ArgumentNullException(nameof(plugin)); }

            Log = log;
            Cfg = plugin.Config;
            PluginGuid = plugin.Info?.Metadata?.GUID ?? configFolder;
            ConfigFolder = configFolder;
            if (assetBundle != null) { AssetBundle = assetBundle; }
            if (localization != null) { Localization = localization; }

            Cfg.SaveOnConfigSet = true;

            EnableDebugMode = Cfg.Bind(configSection, "EnableDebugMode", false,
                new ConfigDescription("Enables verbose debug logging for the shared item/piece loaders.",
                    null,
                    new ConfigurationManagerAttributes { IsAdvanced = true }));
            EnableDebugMode.SettingChanged += ModLogger.OnDebugModeChanged;

            ConfigApplyDelay = ConfigBinder.BindServerConfig(configSection, "Config Apply Delay", 1f,
                "Delay in seconds before a changed config entry is applied in-game. Coalesces a burst of rapid " +
                "edits (typing, file reloads, server sync) into a single apply. Set to 0 to apply instantly.",
                true, 0f, 10f);

            Initialized = true;
            ModLogger.RefreshLevel();
        }

        /// <summary>
        /// Points the shared debug-logging gate at a config entry the mod already owns, instead of the
        /// baseline entry bound by <see cref="Initialize"/>.
        /// </summary>
        public static void UseDebugEntry(ConfigEntry<bool> entry) {
            if (entry == null) { return; }
            if (EnableDebugMode != null) { EnableDebugMode.SettingChanged -= ModLogger.OnDebugModeChanged; }
            EnableDebugMode = entry;
            EnableDebugMode.SettingChanged += ModLogger.OnDebugModeChanged;
            ModLogger.RefreshLevel();
        }

        /// <summary>
        /// Toggles write-through saving and flushes. The loaders turn this off while binding a batch of
        /// entries and back on afterwards so startup doesn't write the config file once per entry.
        /// </summary>
        public static void SaveOnSet(bool enabled) {
            if (Cfg == null) { return; }
            Cfg.SaveOnConfigSet = enabled;
            Cfg.Save();
        }
    }
}
