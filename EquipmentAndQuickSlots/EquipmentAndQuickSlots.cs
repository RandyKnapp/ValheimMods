using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots {
    [BepInPlugin(PluginId, "Equipment and Quick Slots", Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("moreslots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("Azumatt.AzuExtendedPlayerInventory")]
    [BepInIncompatibility("aedenthorn.ExtendedPlayerInventory")]
    [BepInIncompatibility("shudnal.ExtraSlots")]
    [BepInIncompatibility("com.bruce.valheim.comfyquickslots")]
    public class EquipmentAndQuickSlots : BaseUnityPlugin {
        public const string PluginId = "randyknapp.mods.equipmentandquickslots";
        public const string Version = "3.0.0";

        public static Sprite PaperdollMale;
        public static Sprite PaperdollFemale;
        public static GameObject Paperdolls;

        public static bool HasAuga {
            get; private set;
        }

        private static EquipmentAndQuickSlots _instance;
        private Harmony _harmony;

        private void Awake() {
            _instance = this;

            HasAuga = Auga.API.IsLoaded();

            new ValConfig(Config);

            FixQuickSlotPositionForAuga();
            LoadAssets();
            Slots.InitializeSlots();
            EpicLootCompat.Initialize();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        private void Update() {
            var player = Player.m_localPlayer;
            if (player == null)
                return;

            foreach (var slot in Slots.slots) {
                if (slot.IsHotkeySlot && slot.IsShortcutDownWithItem())
                    player.UseItem(null, slot.Item, false);
            }
        }

        // Game events only mark the validators dirty; all slot-item movement drains here, outside
        // any vanilla call stack. API listeners are notified afterwards so they observe settled
        // state.
        private void LateUpdate() {
            SlotValidation.Validate();
            API.DetectSlotItemChanges();
        }

        // Auga's HUD leaves less vertical room, so a bar still sitting on the stock default is
        // nudged down to clear it. A user-moved bar is left alone.
        private static void FixQuickSlotPositionForAuga() {
            if (!HasAuga)
                return;

            var defaultPosition = (Vector2)ValConfig.QuickSlotsPosition.DefaultValue;
            if (ValConfig.QuickSlotsPosition.Value != defaultPosition)
                return;

            ValConfig.QuickSlotsPosition.Value = new Vector2(defaultPosition.x, 86);
        }

        private static void LoadAssets() {
            var assetBundle = LoadAssetBundle("eaqs");
            PaperdollMale = assetBundle.LoadAsset<Sprite>("PaperdollMale");
            PaperdollFemale = assetBundle.LoadAsset<Sprite>("PaperdollFemale");
            Paperdolls = assetBundle.LoadAsset<GameObject>("Paperdolls");
        }

        public static AssetBundle LoadAssetBundle(string filename) {
            var assembly = Assembly.GetCallingAssembly();
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
        }

        public static void Log(string message) {
            if (ValConfig.LoggingEnabled.Value) {
                _instance.Logger.LogMessage(message);
            }
        }

        // Warnings and errors are NOT gated behind the logging toggle: this mod's whole risk
        // surface is item loss, and its loss-prevention diagnostics (backup restore failures,
        // migration errors, relocation warnings) must be visible in every player's log.
        public static void LogWarning(string message) {
            _instance.Logger.LogWarning(message);
        }

        public static void LogError(string message) {
            _instance.Logger.LogError(message);
        }
    }
}
