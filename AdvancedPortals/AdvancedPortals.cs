using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using Common;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

namespace AdvancedPortals
{
    public class PieceDef
    {
        public delegate void OnPrefabAddedDelegate(GameObject prefab);

        public string Table;
        public string CraftingStation;
        public OnPrefabAddedDelegate OnPrefabAdded;
        public List<RecipeRequirementConfig> Resources = new List<RecipeRequirementConfig>();
    }

    [BepInPlugin(PluginId, DisplayName, Version)]
    [BepInIncompatibility("com.github.xafflict.UnrestrictedPortals")]
    [BepInDependency("org.bepinex.plugins.targetportal", BepInDependency.DependencyFlags.SoftDependency)]
    public class AdvancedPortals : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.advancedportals";
        public const string DisplayName = "Advanced Portals";
        public const string Version = "1.0.10";
        public static readonly string[] _portalPrefabs = { "portal_ancient", "portal_obsidian", "portal_blackmarble" };
        
        public static readonly List<GameObject> RegisteredPrefabs = new List<GameObject>();
        public static readonly Dictionary<GameObject, PieceDef> RegisteredPieces = new Dictionary<GameObject, PieceDef>();

        private readonly ConfigSync _configSync = new ConfigSync(PluginId) { DisplayName = DisplayName, CurrentVersion = Version, MinimumRequiredVersion = Version };
        private static ConfigEntry<bool> _serverConfigLocked;
        private static ConfigEntry<bool> _ancientPortalEnabled;
        private static ConfigEntry<string> _ancientPortalRecipe;
        private static ConfigEntry<string> _ancientPortalAllowedItems;
        private static ConfigEntry<bool> _ancientPortalAllowEverything;
        private static ConfigEntry<bool> _obsidianPortalEnabled;
        private static ConfigEntry<string> _obsidianPortalRecipe;
        private static ConfigEntry<string> _obsidianPortalAllowedItems;
        private static ConfigEntry<bool> _obsidianPortalAllowEverything;
        private static ConfigEntry<bool> _obsidianPortalAllowPreviousPortalItems;
        private static ConfigEntry<bool> _blackMarblePortalEnabled;
        private static ConfigEntry<string> _blackMarblePortalRecipe;
        private static ConfigEntry<string> _blackMarblePortalAllowedItems;
        private static ConfigEntry<bool> _blackMarblePortalAllowEverything;
        private static ConfigEntry<bool> _blackMarblePortalAllowPreviousPortalItems;

        private static AdvancedPortals _instance;
        private Harmony _harmony;

        [UsedImplicitly]
        private void Awake()
        {
            _instance = this;

            _serverConfigLocked = SyncedConfig("Config Sync", "Lock Config", false, "[Server Only] The configuration is locked and may not be changed by clients once it has been synced from the server. Only valid for server config, will have no effect on clients.");

            _ancientPortalEnabled = SyncedConfig("Portal 1 - Ancient", "Ancient Portal Enabled", true, "Enable the Ancient Portal");
            _ancientPortalRecipe = SyncedConfig("Portal 1 - Ancient", "Ancient Portal Recipe", "ElderBark:20,Iron:5,SurtlingCore:2", "The items needed to build the Ancient Portal. A comma separated list of ITEM:QUANTITY pairs separated by a colon.");
            _ancientPortalAllowedItems = SyncedConfig("Portal 1 - Ancient", "Ancient Portal Allowed Items", "Copper, CopperOre, CopperScrap, Tin, TinOre, Bronze", "A comma separated list of the item types allowed through the Ancient Portal");
            _ancientPortalAllowEverything = SyncedConfig("Portal 1 - Ancient", "Ancient Portal Allow Everything", false, "Allow all items through the Ancient Portal (overrides Allowed Items)");

            _obsidianPortalEnabled = SyncedConfig("Portal 2 - Obsidian", "Obsidian Portal Enabled", true, "Enable the Obsidian Portal");
            _obsidianPortalRecipe = SyncedConfig("Portal 2 - Obsidian", "Obsidian Portal Recipe", "Obsidian:20,Silver:5,SurtlingCore:2", "The items needed to build the Obsidian Portal. A comma separated list of ITEM:QUANTITY pairs separated by a colon.");
            _obsidianPortalAllowedItems = SyncedConfig("Portal 2 - Obsidian", "Obsidian Portal Allowed Items", "Iron, IronScrap", "A comma separated list of the item types allowed through the Obsidian Portal");
            _obsidianPortalAllowEverything = SyncedConfig("Portal 2 - Obsidian", "Obsidian Portal Allow Everything", false, "Allow all items through the Obsidian Portal (overrides Allowed Items)");
            _obsidianPortalAllowPreviousPortalItems = SyncedConfig("Portal 2 - Obsidian", "Obsidian Portal Use All Previous", true, "Additionally allow all items from the Ancient Portal");

            _blackMarblePortalEnabled = SyncedConfig("Portal 3 - Black Marble", "Black Marble Portal Enabled", true, "Enable the Black Marble Portal");
            _blackMarblePortalRecipe = SyncedConfig("Portal 3 - Black Marble", "Black Marble Portal Recipe", "BlackMarble:20,BlackMetal:5,Eitr:2", "The items needed to build the Black Marble Portal. A comma separated list of ITEM:QUANTITY pairs separated by a colon.");
            _blackMarblePortalAllowedItems = SyncedConfig("Portal 3 - Black Marble", "Black Marble Portal Allowed Items", "Silver, SilverOre", "A comma separated list of the item types allowed through the Black Marble Portal");
            _blackMarblePortalAllowEverything = SyncedConfig("Portal 3 - Black Marble", "Black Marble Portal Allow Everything", true, "Allow all items through the Black Marble Portal (overrides Allowed Items)");
            _blackMarblePortalAllowPreviousPortalItems = SyncedConfig("Portal 3 - Black Marble", "Black Marble Portal Use All Previous", true, "Additionally allow all items from the Obsidian and Ancient Portal");

            _configSync.AddLockingConfigEntry(_serverConfigLocked);

            var assetBundle = LoadAssetBundle("advancedportals");

            var allowedTypesAncient = _ancientPortalAllowedItems.Value.Replace(" ", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var allowedTypesObsidian = _obsidianPortalAllowedItems.Value.Replace(" ", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var allowedTypesBlackMarble = _blackMarblePortalAllowedItems.Value.Replace(" ", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (_ancientPortalEnabled.Value)
            {
                LoadBuildPiece(assetBundle, "portal_ancient", new PieceDef()
                {
                    Table = "_HammerPieceTable",
                    CraftingStation = "piece_workbench",
                    Resources = MakeRecipeFromConfig("Ancient Portal", _ancientPortalRecipe.Value),
                    OnPrefabAdded = (prefab =>
                    {
                        if (!prefab.TryGetComponent<TeleportWorld>(out var teleport))
                        {
                            teleport = prefab.AddComponent<TeleportWorld>();
                        }
                        var advPortal = prefab.AddComponent<AdvancedPortal>();
                        advPortal.DefaultName = "ancient";
                        advPortal.AllowedItems = allowedTypesAncient;
                        advPortal.AllowEverything = _ancientPortalAllowEverything.Value;

                        var itemDrop = prefab.GetComponent<Piece>();
                        itemDrop.m_description += $" Can Teleport: ({(advPortal.AllowEverything ? "Anything" : string.Join(", ", advPortal.AllowedItems))})";
                    })
                });
            }

            if (_obsidianPortalEnabled.Value)
            {
                LoadBuildPiece(assetBundle, "portal_obsidian", new PieceDef() {
                    Table = "_HammerPieceTable",
                    CraftingStation = "piece_workbench",
                    Resources = MakeRecipeFromConfig("Obsidian Portal", _obsidianPortalRecipe.Value),
                    OnPrefabAdded = (prefab => {
                        if (!prefab.TryGetComponent<TeleportWorld>(out var teleport))
                        {
                            teleport = prefab.AddComponent<TeleportWorld>();
                        }
                        
                        var advPortal = prefab.AddComponent<AdvancedPortal>();
                        advPortal.DefaultName = "obsidian"; 
                        advPortal.AllowedItems = allowedTypesObsidian.ToList();
                        if (_obsidianPortalAllowPreviousPortalItems.Value)
                            advPortal.AllowedItems.AddRange(allowedTypesAncient);
                        advPortal.AllowEverything = _obsidianPortalAllowEverything.Value;

                        var itemDrop = prefab.GetComponent<Piece>();
                        itemDrop.m_description += $" Can Teleport: ({(advPortal.AllowEverything ? "Anything" : string.Join(", ", advPortal.AllowedItems))})";
                    })
                });
            }

            if (_blackMarblePortalEnabled.Value)
            {
                LoadBuildPiece(assetBundle, "portal_blackmarble", new PieceDef() {
                    Table = "_HammerPieceTable",
                    CraftingStation = "piece_workbench",
                    Resources = MakeRecipeFromConfig("Black Marble Portal", _blackMarblePortalRecipe.Value),
                    OnPrefabAdded = (prefab => {
                        if (!prefab.TryGetComponent<TeleportWorld>(out var teleport))
                        {
                            teleport = prefab.AddComponent<TeleportWorld>();
                        }
                        var advPortal = prefab.AddComponent<AdvancedPortal>();
                        advPortal.DefaultName = "blackmarble"; 
                        advPortal.AllowedItems = allowedTypesBlackMarble.ToList();
                        if (_blackMarblePortalAllowPreviousPortalItems.Value)
                        {
                            advPortal.AllowedItems.AddRange(allowedTypesAncient);
                            advPortal.AllowedItems.AddRange(allowedTypesObsidian);
                        }
                        advPortal.AllowEverything = _blackMarblePortalAllowEverything.Value;

                        var itemDrop = prefab.GetComponent<Piece>();
                        itemDrop.m_description += $" Can Teleport: ({(advPortal.AllowEverything ? "Anything" : string.Join(", ", advPortal.AllowedItems))})";
                    })
                });
            }

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);

            //Add Prefabs to Portal Connections
            foreach (var portalPrefab in _portalPrefabs)
            {
                AddPortal.Hashes.Add(portalPrefab.GetStableHashCode());    
            }
            
            // Patch TargetPortal's handle click method, since it does not directly call TeleportWorld.Teleport
            var targetPortal = gameObject.GetComponent("TargetPortal.TargetPortal");
            if (targetPortal)
            {
                var pluginType = targetPortal.GetType();
                var mapType = pluginType.Assembly.GetType("TargetPortal.Map");
                var handlePortalClickMethod = AccessTools.DeclaredMethod(mapType, "HandlePortalClick");
                if (handlePortalClickMethod != null)
                {
                    _harmony.Patch(handlePortalClickMethod, new HarmonyMethod(typeof(AdvancedPortals), nameof(TargetPortal_HandlePortalClick_Prefix)));
                    _harmony.Patch(handlePortalClickMethod, null, new HarmonyMethod(typeof(Teleport_Patch), nameof(Teleport_Patch.Generic_Postfix)));
                }
            }
        }

        private static void TargetPortal_HandlePortalClick_Prefix()
        {
            var playerPos = Player.m_localPlayer.transform.position;
            const float searchRadius = 2.0f;
            var colliders = Physics.OverlapSphere(playerPos, searchRadius);
            TeleportWorld closestTeleport = null;
            var minDistSquared = searchRadius * searchRadius + 1;
            foreach (var collider in colliders)
            {
                var twt = collider.gameObject.GetComponent<TeleportWorldTrigger>();
                if (twt == null)
                    continue;

                var tw = twt.GetComponentInParent<TeleportWorld>();
                if (tw == null)
                    continue;

                var d = collider.transform.position - playerPos;
                var distSquared = d.x * d.x + d.y * d.y + d.z * d.z;
                if (distSquared < minDistSquared)
                {
                    closestTeleport = tw;
                    minDistSquared = distSquared;
                }
            }

            if (closestTeleport != null)
                Teleport_Patch.Generic_Prefix(closestTeleport);
        }

        private static List<RecipeRequirementConfig> MakeRecipeFromConfig(string portalName, string configString)
        {
            var recipe = new List<RecipeRequirementConfig>();

            var entries = configString.Replace(" ", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var entry in entries)
            {
                var parts = entry.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    _instance.Logger.LogError($"Incorrectly formatted recipe for {portalName}! Should be 'ITEM:QUANITY,ITEM2:QUANTITY' etc.");
                    return new List<RecipeRequirementConfig>();
                }
                var item = parts[0];
                var amountString = parts[1];
                if (!int.TryParse(amountString, out var amount))
                {
                    _instance.Logger.LogError($"Incorrectly formatted recipe for {portalName}! Should be 'ITEM:QUANITY,ITEM2:QUANTITY' etc.");
                    return new List<RecipeRequirementConfig>();
                }
                recipe.Add(new RecipeRequirementConfig() {item = item, amount = amount});
            }

            if (recipe.Count == 0)
            {
                _instance.Logger.LogError($"Incorrectly formatted recipe for {portalName}! Must have at least one entry, should be 'ITEM:QUANITY,ITEM2:QUANTITY' etc.");
            }
            return recipe;
        }

        [UsedImplicitly]
        private void OnDestroy()
        {
            _instance = null;
            _harmony?.UnpatchSelf();
        }

        private ConfigEntry<T> SyncedConfig<T>(string group, string configName, T value, string description, bool synchronizedSetting = true) => SyncedConfig(group, configName, value, new ConfigDescription(description), synchronizedSetting);

        private ConfigEntry<T> SyncedConfig<T>(string group, string configName, T value, ConfigDescription description, bool synchronizedSetting = true)
        {
            var configEntry = Config.Bind(group, configName, value, description);

            var syncedConfigEntry = _configSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        public static AssetBundle LoadAssetBundle(string filename)
        {
            var assembly = Assembly.GetCallingAssembly();
            var assetBundle = AssetBundle.LoadFromStream(assembly.GetManifestResourceStream($"{assembly.GetName().Name}.{filename}"));

            return assetBundle;
        }

        private static void LoadBuildPiece(AssetBundle assetBundle, string assetName, PieceDef pieceDef)
        {
            var prefab = assetBundle.LoadAsset<GameObject>(assetName);
            RegisteredPieces.Add(prefab, pieceDef);
            RegisteredPrefabs.Add(prefab);
        }

        public static void TryRegisterPrefabs(ZNetScene zNetScene)
        {
            if (zNetScene == null || zNetScene.m_prefabs == null || zNetScene.m_prefabs.Count <= 0)
            {
                return;
            }

            foreach (var prefab in RegisteredPrefabs)
            {
                if (!zNetScene.m_prefabs.Contains(prefab))
                {
                    zNetScene.m_prefabs.Add(prefab);
                }
            }
        }

        public static void TryRegisterPieces(List<PieceTable> pieceTables, List<CraftingStation> craftingStations)
        {
            foreach (var entry in RegisteredPieces)
            {
                var prefab = entry.Key;
                if (prefab == null)
                {
                    _instance.Logger.LogError($"Tried to register piece but prefab was null!");
                    continue;
                }

                var pieceDef = entry.Value;
                if (pieceDef == null)
                {
                    _instance.Logger.LogError($"Tried to register piece ({prefab}) but pieceDef was null!");
                    continue;
                }

                var piece = prefab.GetComponent<Piece>();
                if (piece == null)
                {
                    _instance.Logger.LogError($"Tried to register piece ({prefab}) but Piece component was missing!");
                    continue;
                }

                var pieceTable = pieceTables.Find(x => x.name == pieceDef.Table);
                if (pieceTable == null)
                {
                    _instance.Logger.LogError($"Tried to register piece ({prefab}) but could not find piece table ({pieceDef.Table}) (pieceTables({pieceTables.Count})= {string.Join(", ", pieceTables.Select(x => x.name))})!");
                    continue;
                }

                if (pieceTable.m_pieces.Contains(prefab))
                {
                    continue;
                }

                pieceTable.m_pieces.Add(prefab);

                var pieceStation = craftingStations.Find(x => x.name == pieceDef.CraftingStation);
                piece.m_craftingStation = pieceStation;

                var resources = new List<Piece.Requirement>();
                foreach (var resource in pieceDef.Resources)
                {
                    var resourcePrefab = ObjectDB.instance.GetItemPrefab(resource.item);
                    resources.Add(new Piece.Requirement() {
                        m_resItem = resourcePrefab.GetComponent<ItemDrop>(),
                        m_amount = resource.amount
                    });
                }
                piece.m_resources = resources.ToArray();

                var portalPrefab = pieceTables.SelectMany(x => x.m_pieces).FirstOrDefault(x => x.name == "portal_wood");
                if (portalPrefab != null)
                {
                    if (portalPrefab.GetComponent<Piece>() is Piece otherPiece)
                    {
                        piece.m_placeEffect.m_effectPrefabs = otherPiece.m_placeEffect.m_effectPrefabs.ToArray();
                    }

                    if (portalPrefab.GetComponent<WearNTear>() is WearNTear otherWearTear)
                    {
                        var wearTear = prefab.GetComponent<WearNTear>();
                        if (wearTear)
                        {
                            wearTear.m_destroyedEffect.m_effectPrefabs = otherWearTear.m_destroyedEffect.m_effectPrefabs.ToArray();
                            wearTear.m_hitEffect.m_effectPrefabs = otherWearTear.m_hitEffect.m_effectPrefabs.ToArray();
                        }
                    }
                }
                else
                {
                    _instance.Logger.LogError("No portal_wood prefab!");
                }

                pieceDef.OnPrefabAdded?.Invoke(prefab);
            }
        }

        public static bool IsObjectDBReady()
        {
            // Hack, just making sure the built-in items and prefabs have loaded
            return ObjectDB.instance != null && ObjectDB.instance.m_items.Count != 0 && ObjectDB.instance.GetItemPrefab("Amber") != null;
        }

        public static void TryRegisterObjects()
        {
            if (!IsObjectDBReady())
            {
                return;
            }

            ObjectDB.instance.UpdateItemHashes();

            var pieceTables = new List<PieceTable>();
            foreach (var itemPrefab in ObjectDB.instance.m_items)
            {
                var itemDrop = itemPrefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    _instance.Logger.LogError($"An item without an ItemDrop ({itemPrefab}) exists in ObjectDB.instance.m_items! Don't do this!");
                    continue;
                }
                var item = itemDrop.m_itemData;
                if (item != null && item.m_shared.m_buildPieces != null && !pieceTables.Contains(item.m_shared.m_buildPieces))
                {
                    pieceTables.Add(item.m_shared.m_buildPieces);
                }
            }

            var craftingStations = new List<CraftingStation>();
            foreach (var pieceTable in pieceTables)
            {
                craftingStations.AddRange(pieceTable.m_pieces
                    .Where(x => x.GetComponent<CraftingStation>() != null)
                    .Select(x => x.GetComponent<CraftingStation>()));
            }

            TryRegisterPieces(pieceTables, craftingStations);
        }
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    public static class ZNetScene_Awake_Patch
    {
        public static bool Prefix(ZNetScene __instance)
        {
            AdvancedPortals.TryRegisterPrefabs(__instance);
            return true;
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.CopyOtherDB))]
    public static class ObjectDB_CopyOtherDB_Patch
    {
        public static void Postfix()
        {
            AdvancedPortals.TryRegisterObjects();
        }
    }

    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    public static class ObjectDB_Awake_Patch
    {
        public static void Postfix()
        {
            AdvancedPortals.TryRegisterObjects();
        }
    }
}
