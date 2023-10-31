using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Auga;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace EquipmentAndQuickSlots
{
    [BepInPlugin(PluginId, "Equipment and Quick Slots", "2.1.12")]
    [BepInDependency("moreslots", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("randyknapp.mods.auga", BepInDependency.DependencyFlags.SoftDependency)]
    public class EquipmentAndQuickSlots : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.equipmentandquickslots";

        public const int QuickSlotCount = 3;
        public static int EquipSlotCount => EquipSlotTypes.Count;
        public static readonly ConfigEntry<KeyboardShortcut>[] KeyCodes = new ConfigEntry<KeyboardShortcut>[QuickSlotCount];
        public static readonly ConfigEntry<string>[] HotkeyLabels = new ConfigEntry<string>[QuickSlotCount];
        public static readonly List<ItemDrop.ItemData.ItemType> EquipSlotTypes = new List<ItemDrop.ItemData.ItemType>() {
            ItemDrop.ItemData.ItemType.Helmet,
            ItemDrop.ItemData.ItemType.Chest,
            ItemDrop.ItemData.ItemType.Legs,
            ItemDrop.ItemData.ItemType.Shoulder,
            ItemDrop.ItemData.ItemType.Utility
        };

        public static ConfigEntry<bool> EquipmentSlotsEnabled;
        public static ConfigEntry<bool> QuickSlotsEnabled;
        public static ConfigEntry<bool> ViewDebugSaveData;
        private static ConfigEntry<bool> _loggingEnabled;
        public static ConfigEntry<TextAnchor> QuickSlotsAnchor;
        public static ConfigEntry<Vector2> QuickSlotsPosition;
        public static ConfigEntry<bool> DontDropEquipmentOnDeath;
        public static ConfigEntry<bool> DontDropQuickslotsOnDeath;
        public static ConfigEntry<bool> InstantlyReequipArmorOnPickup;
        public static ConfigEntry<bool> InstantlyReequipQuickslotsOnPickup;

        public static Sprite PaperdollMale;
        public static Sprite PaperdollFemale;
        public static GameObject Paperdolls;

        public static bool HasAuga { get; private set; }

        private static EquipmentAndQuickSlots _instance;
        private Harmony _harmony;

        private void Awake()
        {
            _instance = this;
            
            HasAuga = Auga.API.IsLoaded();
            
            _loggingEnabled = Config.Bind("Logging", "Logging Enabled", false, "Enable logging");
            KeyCodes[0] = Config.Bind("Hotkeys", "Quick slot hotkey 1", new KeyboardShortcut(KeyCode.Z), "Hotkey for Quick Slot 1.");
            if (KeyCodes[0].Value.MainKey == KeyCode.None)
                KeyCodes[0].Value = new KeyboardShortcut(KeyCode.Z);
            
            KeyCodes[1] = Config.Bind("Hotkeys", "Quick slot hotkey 2", new KeyboardShortcut(KeyCode.V), "Hotkey for Quick Slot 2.");
            if (KeyCodes[1].Value.MainKey == KeyCode.None)
                KeyCodes[1].Value = new KeyboardShortcut(KeyCode.V);
            
            KeyCodes[2] = Config.Bind("Hotkeys", "Quick slot hotkey 3", new KeyboardShortcut(KeyCode.B), "Hotkey for Quick Slot 3.");
            if (KeyCodes[2].Value.MainKey == KeyCode.None)
                KeyCodes[2].Value = new KeyboardShortcut(KeyCode.B);
            
            HotkeyLabels[0] = Config.Bind("Hotkeys", "Quick slot hotkey label 1", "", "Hotkey Label for Quick Slot 1. Leave blank to use the hotkey itself.");
            HotkeyLabels[1] = Config.Bind("Hotkeys", "Quick slot hotkey label 2", "", "Hotkey Label for Quick Slot 2. Leave blank to use the hotkey itself.");
            HotkeyLabels[2] = Config.Bind("Hotkeys", "Quick slot hotkey label 3", "", "Hotkey Label for Quick Slot 3. Leave blank to use the hotkey itself.");
            EquipmentSlotsEnabled = Config.Bind("Toggles", "Enable Equipment Slots", true, "Enable the equipment slots. Disabling this while items are equipped with attempt to move them to your inventory.");
            QuickSlotsEnabled = Config.Bind("Toggles", "Enable Quick Slots", true, "Enable the quick slots. Disabling this while items are in the slots with attempt to move them to your inventory.");
            ViewDebugSaveData = Config.Bind("Toggles", "View Debug Save Data", false, "Enable to view the raw save data in the compendium.");
            QuickSlotsAnchor = Config.Bind("Quick Slots", "Quick Slots Anchor", TextAnchor.LowerLeft, "The point on the HUD to anchor the Quick Slots bar. Changing this also changes the pivot of the Quick Slots to that corner.");
            QuickSlotsPosition = Config.Bind("Quick Slots", "Quick Slots Position", GetDefaultPosition(new Vector2(216, 150)), "The position offset from the Quick Slots Anchor at which to place the Quick Slots.");
            DontDropEquipmentOnDeath = Config.Bind("Gravestone", "Dont drop equipment on death", false, "If set to true, your equipment will not be dropped when you die. Only valid when Equipment Slots are enabled.");
            DontDropQuickslotsOnDeath = Config.Bind("Gravestone", "Dont drop quickslot items on death", false, "If set to true, the items in the quickslots will not be dropped when you die. Only valid when Quick Slots are enabled.");
            InstantlyReequipArmorOnPickup = Config.Bind("Gravestone", "Instantly re-equip armor on pickup", false, "If set to true, when you pickup your equipment gravestone your armor will be instantly re-equipped, if possible. Only valid when Equipment Slots are enabled.");
            InstantlyReequipQuickslotsOnPickup = Config.Bind("Gravestone", "Instantly re-equip quickslot items on pickup", true, "If set to true, when you pickup your equipment gravestone your quick slot items will be instantly re-equipped, if possible. Only valid when Quick Slots are enabled.");

            FixQuickSlotPositionForAuga((Vector2)QuickSlotsPosition.DefaultValue, QuickSlotsPosition.Value);
            LoadAssets();
            

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            Debug.LogWarning($"Has Auga API: {HasAuga}");
        }

        private static Vector2 GetDefaultPosition(Vector2 defaultPosition)
        {
            return !HasAuga ? defaultPosition : new Vector2(defaultPosition.x, 86);
        }

        private static void FixQuickSlotPositionForAuga(Vector2 defaultPosition, Vector2 currentPosition)
        {
            if (!HasAuga)
                return;

            if (defaultPosition != currentPosition) return;
            
            var newPosition = GetDefaultPosition(currentPosition);
            QuickSlotsPosition.Value = newPosition;
        }

        private static void LoadAssets()
        {
            var assetBundle = LoadAssetBundle("eaqs");
            PaperdollMale = assetBundle.LoadAsset<Sprite>("PaperdollMale");
            PaperdollFemale = assetBundle.LoadAsset<Sprite>("PaperdollFemale");
            Paperdolls = assetBundle.LoadAsset<GameObject>("Paperdolls");
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void Update()
        {
            if (QuickSlotsEnabled.Value)
            {
                var player = Player.m_localPlayer;
                if (player != null && player.TakeInput())
                {
                    for (int i = 0; i < QuickSlotCount; ++i)
                    {
                        CheckQuickUseInput(player, i);
                    }
                }
            }
        }

        public static void Log(string message)
        {
            if (_loggingEnabled.Value)
            {
                _instance.Logger.LogMessage(message);
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

        public static string GetBindingLabel(int index)
        {
            index = Mathf.Clamp(index, 0, QuickSlotCount - 1);
            
            var keycode = GetBindingKeycode(index);
            var label = HotkeyLabels[index]?.Value;
            
            return string.IsNullOrEmpty(label) ? keycode.ToString() : label;
        }

        public static KeyCode GetBindingKeycode(int index)
        {
            index = Mathf.Clamp(index, 0, QuickSlotCount - 1);
            var keycodeValue = KeyCodes[index].Value.MainKey;

            return keycodeValue;
        }

        public static void CheckQuickUseInput(Player player, int index)
        {
            var keyCode = GetBindingKeycode(index);
            if (ZInput.GetKeyDown(keyCode))
            {
                var item = player.GetQuickSlotItem(index);
                if (item != null)
                {
                    player.UseItem(null, item, false);
                }
            }
        }

        public static ItemDrop.ItemData.ItemType GetEquipmentTypeForSlot(int index)
        {
            if (index < 0 || index >= EquipSlotTypes.Count)
            {
                return ItemDrop.ItemData.ItemType.None;
            }

            return EquipSlotTypes[index];
        }

        public static Vector2i GetEquipmentSlotForType(ItemDrop.ItemData.ItemType type)
        {
            return new Vector2i(EquipSlotTypes.IndexOf(type), 0);
        }

        public static bool IsSlotEquippable(ItemDrop.ItemData item)
        {
            return EquipSlotTypes.Contains(item.m_shared.m_itemType);
        }
        
        public static CodeInstruction LogMessage(CodeInstruction instruction, int counter)
        {
            EquipmentAndQuickSlots.Log($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
            return instruction;
        }
            
        public static CodeInstruction FindInstructionWithLabel(List<CodeInstruction> codeInstructions, int index, Label label)
        {
            if (index >= codeInstructions.Count)
                return null;
                
            if (codeInstructions[index].labels.Contains(label))
                return codeInstructions[index];
                
            return FindInstructionWithLabel(codeInstructions, index + 1, label);
        }

    }
}
