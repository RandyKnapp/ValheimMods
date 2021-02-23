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
    }

    [BepInPlugin("randyknapp.mods.minimalstatuseffects", "Minimal Status Effects", "1.0.1")]
    [BepInProcess("valheim.exe")]
    public class MinimalStatusEffects : BaseUnityPlugin
    {
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
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void OnDestroy()
        {
            _harmony.UnpatchAll();
        }
    }
}
