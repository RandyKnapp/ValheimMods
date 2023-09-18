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
    }

    [BepInPlugin(PluginId, "Minimal Status Effects", Version)]
    [BepInIncompatibility("randyknapp.mods.auga")]
    public class MinimalStatusEffects : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.minimalstatuseffects";
        public const string Version = "1.0.5";

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

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
