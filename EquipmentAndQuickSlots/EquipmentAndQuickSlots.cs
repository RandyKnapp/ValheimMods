using BepInEx;
using Common;
using HarmonyLib;
using Jotunn.Utils;
using System.Reflection;
using UnityEngine;

namespace EquipmentAndQuickSlots {
    [BepInPlugin(PluginId, "Equipment and Quick Slots", Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [SynchronizationMode(AdminOnlyStrictness.IfOnServer)]
    [BepInDependency("moreslots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.epicloot", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ItemFavoriteFrameworkAPI.ItemFavorites.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
    // Soft dependency purely for load order: Better Archery has to be loaded AND patched
    // before this Awake runs, or BetterArcheryCompat has nothing to detect and nothing to
    // unpatch (see src/Compatibility/BetterArcheryCompat.cs).
    [BepInDependency("ishid4.mods.betterarchery", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("Azumatt.AzuExtendedPlayerInventory")]
    [BepInIncompatibility("aedenthorn.ExtendedPlayerInventory")]
    [BepInIncompatibility("shudnal.ExtraSlots")]
    [BepInIncompatibility("com.bruce.valheim.comfyquickslots")]
    public class EquipmentAndQuickSlots : BaseUnityPlugin {
        public const string PluginId = "randyknapp.mods.equipmentandquickslots";
        public const string Version = "3.2.0";

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

            // True only when Auga's APIManager pointed this assembly's Auga.* references at Auga.dll as it
            // loaded (the soft dependency above opts it in); otherwise the stubs in src/Auga run.
            HasAuga = Auga.API.IsLoaded();
            if (!HasAuga && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("randyknapp.mods.auga"))
                LogWarning("Project Auga is installed but its API did not attach to Equipment and Quick Slots; the Auga layout is off. Check the log for an APIManager error.");

            new ValConfig(Config);

            FixQuickSlotPositionForAuga();
            LoadAssets();
            Slots.InitializeSlots();
            EpicLootCompat.Initialize();
            ItemFavoritesCompat.Initialize();

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            // After CreateAndPatchAll: it needs our Harmony instance to remove Better Archery's
            // inventory patches, and it claims reserved slot cells, so it must also run before any
            // other mod can take them through the API.
            BetterArcheryCompat.Initialize(_harmony);
        }

        private void Update() {
            var player = Player.m_localPlayer;
            GamepadQuickSlots.UsePending(player);
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

        // Auga keeps health, stamina and eitr in the bottom left corner, and the stock default puts
        // the bar on top of health and stamina. A bar still on a position this mod wrote moves to the
        // Auga one, and back to the stock default once Auga is gone; a bar the user moved is left
        // alone. The bar hangs below its position (lower-left pivot over Auga's top-left slots), so
        // y 74 is the highest that clears Auga's eitr bar; 3.0 wrote 86, which overlaps it.
        private const float AugaQuickSlotsY = 74f;
        private const float OldAugaQuickSlotsY = 86f;

        private static void FixQuickSlotPositionForAuga() {
            var defaultPosition = (Vector2)ValConfig.QuickSlotsPosition.DefaultValue;
            var augaPosition = new Vector2(defaultPosition.x, AugaQuickSlotsY);
            var oldAugaPosition = new Vector2(defaultPosition.x, OldAugaQuickSlotsY);
            var position = ValConfig.QuickSlotsPosition.Value;

            if (HasAuga) {
                if (position == defaultPosition || position == oldAugaPosition)
                    ValConfig.QuickSlotsPosition.Value = augaPosition;
            } else if (position == augaPosition || position == oldAugaPosition) {
                ValConfig.QuickSlotsPosition.Value = defaultPosition;
            }
        }

        private static void LoadAssets() {
            var assetBundle = LoadAssetBundle("eaqs");
            if (assetBundle == null) {
                // Only the paperdoll art lives in this bundle, so returning here costs the paperdoll
                // but leaves the slots themselves working, rather than aborting the rest of Awake.
                LogError("Failed to load the 'eaqs' asset bundle. The paperdoll will not be shown.");
                return;
            }

            PaperdollMale = assetBundle.LoadAsset<Sprite>("PaperdollMale");
            PaperdollFemale = assetBundle.LoadAsset<Sprite>("PaperdollFemale");
            Paperdolls = assetBundle.LoadAsset<GameObject>("Paperdolls");
        }

        // The assembly is named explicitly rather than taken from Assembly.GetCallingAssembly(). When
        // another mod hooks Awake, MonoMod recompiles it as a dynamic method (DMD<...::Awake>), and the
        // "calling assembly" is then that dynamic assembly, not this one. The resource lookup misses,
        // LoadFromStream(null) throws "ArgumentNullException: stream", and the mod fails to load —
        // intermittently, since it depends on which other mods are present.
        public static AssetBundle LoadAssetBundle(string filename) {
            return AssetBundleLoader.LoadFromResources(filename, typeof(EquipmentAndQuickSlots).Assembly);
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

        // Ungated as well, for the rare one-time notice that explains why a setting has no effect
        // (another mod has taken something over); those are worth more than the line they cost.
        public static void LogInfo(string message) {
            _instance.Logger.LogInfo(message);
        }
    }
}
