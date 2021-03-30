using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Common;
using EpicLoot;
using EpicLoot.Adventure;
using EpicLoot.Crafting;
using EpicLoot.GatedItemType;
using HarmonyLib;
using ModConfigEnforcer;

namespace EpicLoot_Addon_MCE
{
    [BepInPlugin(PluginId, "Epic Loot Addon - MCE", Version)]
    [BepInDependency("randyknapp.mods.epicloot")]
    [BepInDependency("pfhoenix.modconfigenforcer")]
    public class EpicLoot_Addon_MCE : BaseUnityPlugin
    {
        private const string PluginId = "randyknapp.mods.epicloot.addon.mce";
        private const string Version = "1.0.1";

        private static ConfigVariable<GatedItemTypeMode> _gatedItemTypeModeConfig;
        private static readonly JsonFileConfigVariable<LootConfig> _lootConfigFile = new JsonFileConfigVariable<LootConfig>("loottables.json");
        private static readonly JsonFileConfigVariable<MagicItemEffectsList> _magicEffectsConfigFile = new JsonFileConfigVariable<MagicItemEffectsList>("magiceffects.json");
        private static readonly JsonFileConfigVariable<ItemInfoConfig> _itemInfoConfigFile = new JsonFileConfigVariable<ItemInfoConfig>("iteminfo.json");
        private static readonly JsonFileConfigVariable<RecipesConfig> _recipesConfigFile = new JsonFileConfigVariable<RecipesConfig>("recipes.json");
        private static readonly JsonFileConfigVariable<EnchantingCostsConfig> _enchantCostsConfigFile = new JsonFileConfigVariable<EnchantingCostsConfig>("enchantcosts.json");
        private static readonly JsonFileConfigVariable<ItemNameConfig> _itemNamesConfigFile = new JsonFileConfigVariable<ItemNameConfig>("itemnames.json");
        private static readonly JsonFileConfigVariable<AdventureDataConfig> _adventureDataConfigFile = new JsonFileConfigVariable<AdventureDataConfig>("adventuredata.json");

        public void Awake()
        {
            var epicLootConfig = EpicLoot.EpicLoot.GetConfigObject();
            ConfigManager.RegisterMod(EpicLoot.EpicLoot.PluginId, epicLootConfig);

            ConfigManager.ServerConfigReceived += InitializeConfig;

            if (epicLootConfig.TryGetEntry<GatedItemTypeMode>("Balance", "Item Drop Limits", out var gatedItemTypeConfigEntry))
            {
                _gatedItemTypeModeConfig = ConfigManager.RegisterModConfigVariable(
                    EpicLoot.EpicLoot.PluginId, 
                    gatedItemTypeConfigEntry.Definition.Key,
                    (GatedItemTypeMode)gatedItemTypeConfigEntry.DefaultValue,
                    gatedItemTypeConfigEntry.Definition.Section,
                    gatedItemTypeConfigEntry.Description.Description,
                    false);
            }

            ConfigManager.RegisterModConfigVariable(EpicLoot.EpicLoot.PluginId, _lootConfigFile);
            ConfigManager.RegisterModConfigVariable(EpicLoot.EpicLoot.PluginId, _magicEffectsConfigFile);
            ConfigManager.RegisterModConfigVariable(EpicLoot.EpicLoot.PluginId, _itemInfoConfigFile);
            ConfigManager.RegisterModConfigVariable(EpicLoot.EpicLoot.PluginId, _recipesConfigFile);
            ConfigManager.RegisterModConfigVariable(EpicLoot.EpicLoot.PluginId, _enchantCostsConfigFile);
            ConfigManager.RegisterModConfigVariable(EpicLoot.EpicLoot.PluginId, _itemNamesConfigFile);
            ConfigManager.RegisterModConfigVariable(EpicLoot.EpicLoot.PluginId, _adventureDataConfigFile);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
        }

        public static void InitializeConfig()
        {
            LootRoller.Initialize(_lootConfigFile.Value);
            MagicItemEffectDefinitions.Initialize(_magicEffectsConfigFile.Value);
            GatedItemTypeHelper.Initialize(_itemInfoConfigFile.Value);
            RecipesHelper.Initialize(_recipesConfigFile.Value);
            EnchantCostsHelper.Initialize(_enchantCostsConfigFile.Value);
            MagicItemNames.Initialize(_itemNamesConfigFile.Value);
            AdventureDataManager.Initialize(_adventureDataConfigFile.Value);
        }

        [HarmonyPatch(typeof(EpicLoot.EpicLoot), "GetGatedItemTypeMode")]
        public static class EpicLoot_GetGatedItemTypeMode_Patch
        {
            public static bool Prefix(ref GatedItemTypeMode __result)
            {
                __result = _gatedItemTypeModeConfig.Value;
                return false;
            }
        }

        [HarmonyPatch(typeof(ZNet), "Start")]
        public static class ZNet_Start_Patch
        {
            public static void Postfix()
            {
                // This resets EpicLoot to using its local config when starting a local game
                if (ConfigManager.ShouldUseLocalConfig)
                {
                    EpicLoot.EpicLoot.InitializeConfig();
                }
            }
        }
    }
}
