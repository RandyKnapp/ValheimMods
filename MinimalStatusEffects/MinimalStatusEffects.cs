using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MinimalStatusEffects
{
    public static class MinimalStatusEffectConfig
    {
        public static ConfigEntry<Vector2> ScreenPosition;
        public static ConfigEntry<Vector2> ListSize;
        public static ConfigEntry<float> EntrySpacing;
        public static ConfigEntry<float> IconSize;
        public static ConfigEntry<int> FontSize;

        public static ConfigEntry<Vector2> SailingWindIndicatorPosition;
        public static ConfigEntry<Vector2> SailingPowerIndicatorPosition;
        public static ConfigEntry<float> SailingPowerIndicatorScale;

        public static ConfigEntry<bool> EnabledInNomap;
        public static ConfigEntry<Vector2> NomapScreenPosition;
        public static ConfigEntry<Vector2> NomapListSize;
        public static ConfigEntry<float> NomapEntrySpacing;
        public static ConfigEntry<float> NomapIconSize;
        public static ConfigEntry<int> NomapFontSize;

        public static ConfigEntry<Vector2> NomapSailingWindIndicatorPosition;
        public static ConfigEntry<Vector2> NomapSailingPowerIndicatorPosition;
        public static ConfigEntry<float> NomapSailingPowerIndicatorScale;
    }

    [BepInPlugin(PluginId, "Minimal Status Effects", Version)]
    public class MinimalStatusEffects : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.minimalstatuseffects";
        public const string Version = "1.0.4";

        private Harmony _harmony;

        private void Awake()
        {
            MinimalStatusEffectConfig.ScreenPosition = Config.Bind("General", "ScreenPosition", new Vector2(-230, -290),
                "The position offset from the top right corner of the screen.");
            MinimalStatusEffectConfig.ListSize = Config.Bind("General", "ListSize", new Vector2(200, 400),
                "The size of the list box.");
            MinimalStatusEffectConfig.EntrySpacing = Config.Bind<float>("General", "EntrySpacing", 42,
                "The number of units between the top of each entry in the status effects list.");
            MinimalStatusEffectConfig.IconSize = Config.Bind<float>("General", "IconSize", 32, "The size of the square icons.");
            MinimalStatusEffectConfig.FontSize = Config.Bind("General", "FontSize", 20, "The size of the text on the label.");

            MinimalStatusEffectConfig.SailingWindIndicatorPosition = Config.Bind("SailingIndicator", "SailingWindIndicatorPosition", new Vector2(-350, -140), "Location of the sailing wind direction indicator on screen.");
            MinimalStatusEffectConfig.SailingPowerIndicatorPosition = Config.Bind("SailingIndicator", "SailingPowerIndicatorPosition", new Vector2(-270, -215), "Location of the sailing power indicator on screen.");
            MinimalStatusEffectConfig.SailingPowerIndicatorScale = Config.Bind("SailingIndicator", "SailingPowerIndicatorScale", 0.65f, "Scale of the sailing power indicator.");

            MinimalStatusEffectConfig.EnabledInNomap = Config.Bind("Nomap", "Enabled", true, "Enabled in nomap mode");
            MinimalStatusEffectConfig.NomapScreenPosition = Config.Bind("Nomap", "ScreenPosition", new Vector2(-230, -290), "For nomap mode. The position offset from the top right corner of the screen.");
            MinimalStatusEffectConfig.NomapListSize = Config.Bind("Nomap", "ListSize", new Vector2(200, 400), "For nomap mode. The size of the list box.");
            MinimalStatusEffectConfig.NomapEntrySpacing = Config.Bind<float>("Nomap", "EntrySpacing", 42, "For nomap mode. The number of units between the top of each entry in the status effects list.");
            MinimalStatusEffectConfig.NomapIconSize = Config.Bind<float>("Nomap", "IconSize", 32, "For nomap mode. The size of the square icons.");
            MinimalStatusEffectConfig.NomapFontSize = Config.Bind("Nomap", "FontSize", 20, "For nomap mode. The size of the text on the label.");

            MinimalStatusEffectConfig.NomapSailingWindIndicatorPosition = Config.Bind("SailingIndicator Nomap", "SailingWindIndicatorPosition", new Vector2(-350, -140), "For nomap mode. Location of the sailing wind direction indicator on screen.");
            MinimalStatusEffectConfig.NomapSailingPowerIndicatorPosition = Config.Bind("SailingIndicator Nomap", "SailingPowerIndicatorPosition", new Vector2(-270, -215), "For nomap mode. Location of the sailing power indicator on screen.");
            MinimalStatusEffectConfig.NomapSailingPowerIndicatorScale = Config.Bind("SailingIndicator Nomap", "SailingPowerIndicatorScale", 0.65f, "For nomap mode. Scale of the sailing power indicator.");

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
