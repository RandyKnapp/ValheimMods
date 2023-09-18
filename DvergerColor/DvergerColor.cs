using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace DvergerColor
{
    [BepInPlugin(PluginId, "Dverger Color", "1.0.5")]
    [BepInProcess("valheim.exe")]
    public class DvergerColor : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.dvergercolor";

        public static ConfigEntry<Color> Color;
        public static ConfigEntry<int> MaxSteps;
        public static ConfigEntry<float> MinAngle;
        public static ConfigEntry<float> MaxAngle;
        public static ConfigEntry<float> MinIntensity;
        public static ConfigEntry<float> MaxIntensity;
        public static ConfigEntry<float> MinRange;
        public static ConfigEntry<float> MaxRange;
        public static ConfigEntry<float> PointIntensity;
        public static ConfigEntry<float> PointRange;
        public static ConfigEntry<string> NarrowBeamHotkey;
        public static ConfigEntry<string> WidenBeamHotkey;
        public static ConfigEntry<string> ToggleHotkey;
        public static ConfigEntry<bool> TurnOffInBed;

        public const string StepDataKey = "randyknapp.mods.dvergercolor.step";
        public const string OnDataKey = "randyknapp.mods.dvergercolor.on";

        private Harmony _harmony;

        private void Awake()
        {
            Color = Config.Bind("General", "Color", UnityEngine.Color.white, "The color of the Dverger light beam.");
            MaxSteps = Config.Bind("General", "Max Steps", 3, "Define how many steps of focus the Dverger light beam has. Must be at least 2.");
            MinAngle = Config.Bind("General", "Min Angle", 30.0f, "The angle of the beam at the narrowest setting.");
            MaxAngle = Config.Bind("General", "Max Angle", 110.0f, "The angle of the beam at the widest setting.");
            MinIntensity = Config.Bind("General", "Min Intensity", 1.4f, "The intensity of the beam at the widest setting.");
            MaxIntensity = Config.Bind("General", "Max Intensity", 2.2f, "The intensity of the beam at the narrowest setting");
            MinRange = Config.Bind("General", "Min Range", 45.0f, "The range of the beam at the narrowest setting.");
            MaxRange = Config.Bind("General", "Max Range", 15.0f, "The range of the beam at the widest setting");
            PointIntensity = Config.Bind("General", "Point Intensity", 1.1f, "The intensity of the Dverger light pool on the point light setting.");
            PointRange = Config.Bind("General", "Point Range", 10.0f, "The range of the Dverger light pool on the point light setting.");
            NarrowBeamHotkey = Config.Bind("Hotkeys", "Narrow Beam Hotkey", "[", "Press this button to narrow the Dverger light beam.");
            WidenBeamHotkey = Config.Bind("Hotkeys", "Widen Beam Hotkey", "]", "Press this button to widen the Dverger light beam.");
            ToggleHotkey = Config.Bind("Hotkeys", "Toggle Light On/Off", "\\", "Press this button to turn the light beam on or off.");
            TurnOffInBed = Config.Bind("General", "Turn Off In Bed", true, "Whether the circlet should be turned off when you get into bed.");

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        private void OnDestroy()
        {
            _harmony.UnpatchSelf();
        }
    }
}
