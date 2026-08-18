using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Common {
    /// <summary>
    /// Registers a build piece with Jotunn and exposes its workbench requirement, build category and
    /// build cost as server-synced config entries that apply live, without a restart.
    ///
    /// Call <see cref="Register"/> once per piece after <see cref="ModContext.Initialize"/> and after the
    /// asset bundle is available.
    /// </summary>
    public static class PieceLoader {
        public class LoadedGameObjects {
            public GameObject Prefab { get; set; }
            public Sprite Sprite { get; set; }
            public GameObject ScenePrefab { get; set; }
            /// <summary>The resolved prefab's Piece component. Everything applied at runtime goes through this.</summary>
            public Piece ScenePiece { get; set; }
        }

        public class PieceCost {
            public string Prefab { get; set; }
            public int Amount { get; set; }
            public bool Refundable { get; set; } = true;
        }

        public class PieceConfigs {
            public ConfigEntry<bool> Enabled { get; set; }
            public ConfigEntry<bool> RequiresWorkbench { get; set; }
            public ConfigEntry<string> Workbench { get; set; }
            public ConfigEntry<string> PieceCategory { get; set; }
            public ConfigEntry<string> PieceCost { get; set; }
            public List<PieceCost> UpdatedCost { get; set; } = new List<PieceCost>();
        }

        /// <summary>Authored definition of one build piece.</summary>
        public class BuildPiece {
            /// <summary>Config section name for this piece.</summary>
            public string Name { get; set; }
            public bool Enabled { get; set; } = true;
            /// <summary>Prefab name in the asset bundle (with or without a .prefab suffix).</summary>
            public string Prefab { get; set; }
            /// <summary>Optional icon name in the asset bundle. Null uses the prefab's own icon.</summary>
            public string Sprite { get; set; }
            public string Category { get; set; } = PieceCategories.Misc;
            public string PieceTable { get; set; } = PieceTables.Hammer;
            /// <summary>Crafting station prefab required to build. Ignored when RequiresWorkbench is false.</summary>
            public string Workbench { get; set; } = "piece_workbench";
            public bool RequiresWorkbench { get; set; } = true;
            public bool AllowedInDungeons { get; set; } = false;
            public List<PieceCost> PieceCost { get; set; } = new List<PieceCost>();

            // Populated by in-game related runtime objects
            public LoadedGameObjects Objs { get; set; }
            public PieceConfigs Cfgs { get; set; }
        }

        private static readonly List<BuildPiece> BuildPieces = new List<BuildPiece>();
        private static bool PiecesReady = false;

        public static void Register(BuildPiece jbuildpiece, AssetBundle assetBundle = null) {
            AssetBundle bundle = assetBundle ?? ModContext.AssetBundle;
            if (bundle == null) {
                ModLogger.LogError($"No asset bundle available; piece '{jbuildpiece.Name}' will not be registered.");
                return;
            }

            LoadedGameObjects LGos = new LoadedGameObjects();
            LGos.Prefab = LoadBundleAsset<GameObject>(bundle, jbuildpiece.Prefab, ".prefab");
            if (LGos.Prefab == null) {
                ModLogger.LogError($"Could not find prefab '{jbuildpiece.Prefab}' in the asset bundle; piece '{jbuildpiece.Name}' will not be registered.");
                return;
            }
            if (!string.IsNullOrEmpty(jbuildpiece.Sprite)) {
                LGos.Sprite = LoadBundleAsset<Sprite>(bundle, jbuildpiece.Sprite, ".png");
            }
            jbuildpiece.Objs = LGos;
            jbuildpiece.Cfgs = new PieceConfigs();

            InitialPieceSetup(jbuildpiece);

            BuildPieces.Add(jbuildpiece);

            void ResolveAndApplyScenePrefab() {
                GameObject scenePrefab = ResolveScenePrefab(jbuildpiece.Prefab);
                if (scenePrefab == null) {
                    ModLogger.LogWarning($"Could not find a scene prefab named '{jbuildpiece.Prefab}' with a Piece component after prefab registration; skipping in-place setup for {jbuildpiece.Name}.");
                    return;
                }
                jbuildpiece.Objs.ScenePrefab = scenePrefab;
                jbuildpiece.Objs.ScenePiece = scenePrefab.GetComponent<Piece>();
                PiecesReady = true;
                // Bring the current config (default or server-synced) into effect a single time now that
                // every mod prefab is resolvable. This also covers values that arrived early via config sync.
                ApplyWorkbench(jbuildpiece);
                ApplyCategory(jbuildpiece);
                ApplyRecipe(jbuildpiece);
            }
            PrefabManager.OnPrefabsRegistered += ResolveAndApplyScenePrefab;
        }

        // Finds the registered prefab to apply config to. Jotunn's PrefabManager is authoritative, so ask it
        // first; the Resources scan is only a fallback for prefabs injected into ZNetScene by other means.
        //
        // The scan must filter on the Piece component: several loaded GameObjects can share a prefab's name
        // (the bundle asset, Jotunn's registered clone, child objects), Resources.FindObjectsOfTypeAll returns
        // them in no defined order, and picking one without a Piece made every Apply* below throw an NRE.
        private static GameObject ResolveScenePrefab(string prefabName) {
            GameObject registered = PrefabManager.Instance.GetPrefab(prefabName);
            if (registered != null && registered.GetComponent<Piece>() != null) {
                return registered;
            }

            List<GameObject> candidates = Resources.FindObjectsOfTypeAll<GameObject>()
                .Where(obj => obj.name == prefabName && obj.GetComponent<Piece>() != null)
                .ToList();
            if (ModContext.DebugEnabled) {
                ModLogger.LogInfo($"Found {candidates.Count} scene object(s) named '{prefabName}' with a Piece component.");
            }
            return candidates.FirstOrDefault();
        }

        // Bundles differ by mod: the template authors assets under Assets/Custom/... with a file extension,
        // while EpicLoot/Jam/AdvancedPortals bundles use bare names. Try the bare name first, then suffixed.
        private static T LoadBundleAsset<T>(AssetBundle bundle, string name, string suffix) where T : UnityEngine.Object {
            T asset = bundle.LoadAsset<T>(name);
            if (asset != null) { return asset; }
            return name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) ? null : bundle.LoadAsset<T>(name + suffix);
        }

        private static void InitialPieceSetup(BuildPiece jbuildpiece) {
            // Set where the recipe can be crafted. Gated on PiecesReady so an early config sync / file
            // reload (both fire before ZNetScene.Awake) can't run before the scene prefab is resolved.
            void RequiredBench_SettingChanged(object sender, EventArgs e) {
                if (!PiecesReady || jbuildpiece.Objs.ScenePiece == null) { return; }
                ConfigChangeDebouncer.Schedule(jbuildpiece.Cfgs.Workbench, () => ApplyWorkbench(jbuildpiece));
            }
            jbuildpiece.Cfgs.RequiresWorkbench = ConfigBinder.BindServerConfig(jbuildpiece.Name, "Requires Workbench", jbuildpiece.RequiresWorkbench, $"Whether {jbuildpiece.Name} requires a crafting station to be built at all.");
            jbuildpiece.Cfgs.RequiresWorkbench.SettingChanged += RequiredBench_SettingChanged;
            jbuildpiece.Cfgs.Workbench = ConfigBinder.BindServerConfig(jbuildpiece.Name, "Workbench", jbuildpiece.Workbench, "The table required to allow building this piece, eg: 'forge', 'piece_workbench', 'blackforge', 'piece_artisanstation'.");
            jbuildpiece.Cfgs.Workbench.SettingChanged += RequiredBench_SettingChanged;

            // Crafting cost change
            void BuildRecipeChanged_SettingChanged(object sender, EventArgs e) {
                if (!PiecesReady || jbuildpiece.Objs.ScenePiece == null) { return; }
                ConfigChangeDebouncer.Schedule(sender, () => {
                    if (sender is ConfigEntry<string> sendEntry) {
                        if (ModContext.DebugEnabled) { ModLogger.LogInfo($"Recieved new piece config {sendEntry.Value}"); }
                        // return if its an invalid change
                        if (PieceRecipeConfigUpdater(jbuildpiece, sendEntry.Value) == false) { return; }
                    }
                    ApplyRecipe(jbuildpiece);
                });
            }

            // Setup enable/disable
            jbuildpiece.Cfgs.Enabled = ConfigBinder.BindServerConfig(jbuildpiece.Name, "Enabled", jbuildpiece.Enabled, $"Enable/Disable the {jbuildpiece.Name}.");
            jbuildpiece.Cfgs.Enabled.SettingChanged += BuildRecipeChanged_SettingChanged;
            // Setup piece category
            jbuildpiece.Cfgs.PieceCategory = ConfigBinder.BindServerConfig(jbuildpiece.Name, "Piece Category", jbuildpiece.Category, "Piece category for building.", PieceCategories.GetAcceptableValueList());
            void CraftingCategory_SettingChanged(object sender, EventArgs e) {
                if (!PiecesReady || jbuildpiece.Objs.ScenePiece == null) { return; }
                ConfigChangeDebouncer.Schedule(jbuildpiece.Cfgs.PieceCategory, () => ApplyCategory(jbuildpiece));
            }
            jbuildpiece.Cfgs.PieceCategory.SettingChanged += CraftingCategory_SettingChanged;

            // Build out the internal default recipe
            List<string> raw_recipe_default = new List<string>();
            foreach (PieceCost entry in jbuildpiece.PieceCost) { raw_recipe_default.Add($"{entry.Prefab},{entry.Amount},{entry.Refundable}"); }
            string recipe_cfg_default = string.Join("|", raw_recipe_default.ToArray());
            // Wire up the config and on-change for piece costs
            jbuildpiece.Cfgs.PieceCost = ConfigBinder.BindServerConfig(jbuildpiece.Name, "Building Cost", recipe_cfg_default, "Cost to build. Find item ids: https://valheim.fandom.com/wiki/Item_IDs Format: resouce_id,amount,refund eg: Wood,8,true|LeatherScraps,4,false", advanced: true);
            if (PieceRecipeConfigUpdater(jbuildpiece, jbuildpiece.Cfgs.PieceCost.Value, false) == false) {
                ModLogger.LogWarning($"{jbuildpiece.Name} has an invalid piece cost. The default will be used instead.");
                PieceRecipeConfigUpdater(jbuildpiece, recipe_cfg_default, false);
            }

            jbuildpiece.Cfgs.PieceCost.SettingChanged += BuildRecipeChanged_SettingChanged;

            // Collapse this piece's entries into a single grouped custom drawer to keep the in-game
            // Configuration Manager responsive (one visible row per piece under "Building Pieces").
            PieceConfigDrawer.Attach(jbuildpiece);

            List<RequirementConfig> recipe = new List<RequirementConfig>();
            foreach (PieceCost entry in jbuildpiece.Cfgs.UpdatedCost) {
                recipe.Add(new RequirementConfig { Item = entry.Prefab, Amount = entry.Amount, Recover = entry.Refundable });
            }

            // Build the jotunn piece definition
            PieceConfig piececfg = new PieceConfig() {
                CraftingStation = ResolveWorkbenchName(jbuildpiece),
                PieceTable = jbuildpiece.PieceTable,
                Category = jbuildpiece.Cfgs.PieceCategory.Value,
                AllowedInDungeons = jbuildpiece.AllowedInDungeons,
                Icon = jbuildpiece.Objs.Sprite,
                Requirements = recipe.ToArray()
            };
            // Add the updated piece to the piece manager
            PieceManager.Instance.AddPiece(new CustomPiece(jbuildpiece.Objs.Prefab, fixReference: true, piececfg));
        }

        // Empty string / "none" / RequiresWorkbench=false all mean "buildable anywhere"; Jotunn wants null.
        private static string ResolveWorkbenchName(BuildPiece jbuildpiece) {
            bool requiresWorkbench = jbuildpiece.Cfgs.RequiresWorkbench?.Value ?? true;
            string workbench = jbuildpiece.Cfgs.Workbench.Value;
            if (!requiresWorkbench || string.IsNullOrEmpty(workbench) || workbench.ToLower() == "none") { return null; }
            return workbench;
        }

        // Applies the configured crafting station to the in-scene piece. Callers must ensure the scene
        // prefab is resolved (PiecesReady) before invoking this.
        private static void ApplyWorkbench(BuildPiece jbuildpiece) {
            Piece piece = jbuildpiece.Objs.ScenePiece;
            if (piece == null) { return; }

            string workbench = ResolveWorkbenchName(jbuildpiece);
            if (workbench == null) {
                if (ModContext.DebugEnabled) { ModLogger.LogInfo($"Setting required crafting station for {jbuildpiece.Name} to none."); }
                piece.m_craftingStation = null;
                return;
            }

            CraftingStation craftable_at = PrefabManager.Instance.GetPrefab(workbench)?.GetComponent<CraftingStation>();
            if (craftable_at == null) {
                ModLogger.LogWarning($"Required crafting station does not exist or does not have a crafting station component, check your prefab name ({workbench}).");
                return;
            }

            if (ModContext.DebugEnabled) { ModLogger.LogInfo($"Setting crafting station to {workbench}."); }
            piece.m_craftingStation = craftable_at;
        }

        // Applies the configured build category to the in-scene piece.
        private static void ApplyCategory(BuildPiece jbuildpiece) {
            Piece piece = jbuildpiece.Objs.ScenePiece;
            if (piece == null) { return; }

            Piece.PieceCategory? category = PieceManager.Instance.GetPieceCategory(jbuildpiece.Cfgs.PieceCategory.Value);
            if (category == null) {
                category = PieceManager.Instance.AddPieceCategory(jbuildpiece.Cfgs.PieceCategory.Value);
            }
            piece.m_category = (Piece.PieceCategory)category;
        }

        // Resolves the recipe in UpdatedCost against the live prefab database and applies it to the
        // in-scene piece. Bails out (leaving the existing recipe intact) if any requirement prefab is not
        // yet resolvable, so it is safe even if a dependency mod registered its items late.
        private static void ApplyRecipe(BuildPiece jbuildpiece) {
            Piece piece = jbuildpiece.Objs.ScenePiece;
            if (piece == null) { return; }

            if (jbuildpiece.Cfgs.Enabled.Value == false) {
                // Set this piece not craftable
                piece.m_enabled = false;
                return;
            }

            List<RequirementConfig> recipe = new List<RequirementConfig>();
            if (ModContext.DebugEnabled) { ModLogger.LogInfo("Validating and building requirementsConfig"); }
            foreach (PieceCost entry in jbuildpiece.Cfgs.UpdatedCost) {
                if (PrefabManager.Instance.GetPrefab(entry.Prefab) == null) {
                    if (ModContext.DebugEnabled) { ModLogger.LogInfo($"{entry.Prefab} is not a valid prefab, skipping recipe update."); }
                    return;
                }
                if (ModContext.DebugEnabled) { ModLogger.LogInfo($"Checking entry {entry.Prefab} amount:{entry.Amount} refund?:{entry.Refundable}"); }
                recipe.Add(new RequirementConfig { Item = entry.Prefab, Amount = entry.Amount, Recover = entry.Refundable });
            }

            if (ModContext.DebugEnabled) { ModLogger.LogInfo("Updating Piece."); }
            List<Piece.Requirement> newRequirements = new List<Piece.Requirement>();
            foreach (RequirementConfig recipe_entry in recipe) {
                Piece.Requirement piece_req = new Piece.Requirement();
                piece_req.m_resItem = PrefabManager.Instance.GetPrefab(recipe_entry.Item.Replace("JVLmock_", ""))?.GetComponent<ItemDrop>();
                piece_req.m_amount = recipe_entry.Amount;
                piece_req.m_recover = recipe_entry.Recover;
                newRequirements.Add(piece_req);
            }
            if (ModContext.DebugEnabled) { ModLogger.LogInfo($"Fixed mock requirements {newRequirements.Count}."); }
            piece.m_resources = newRequirements.ToArray();
            // Re-enable as well as disable, so toggling Enabled back on takes effect without a restart.
            piece.m_enabled = true;
        }

        private static bool PieceRecipeConfigUpdater(BuildPiece jBuildPiece, string rawRecipe, bool during_runtime = true) {
            string[] RawRecipeEntries = rawRecipe.Split('|');
            List<PieceCost> updated_pieceRecipe = new List<PieceCost>();
            foreach (string recipe_entry in RawRecipeEntries) {
                string[] recipe_segments = recipe_entry.Split(',');
                if (recipe_segments.Length != 3) {
                    ModLogger.LogWarning($"{recipe_entry} is invalid, it does not have enough segments. Proper format is: PREFABNAME,COST,REFUND_BOOL eg: Wood,8,false");
                    return false;
                }
                // Add a sanity check to ensure the prefab we are trying to use exists
                // This can only happen during runtime after pieces are available otherwise it will cause errors
                if (during_runtime) {
                    if (PrefabManager.Instance.GetPrefab(recipe_segments[0]) == null) {
                        ModLogger.LogWarning($"{recipe_segments[0]} is an invalid prefab and does not exist.");
                        return false;
                    }
                }
                if (recipe_segments[0].Length == 0 || recipe_segments[1].Length == 0 || recipe_segments[2].Length == 0) {
                    ModLogger.LogWarning($"{recipe_entry} is invalid, one segment does not have enough data. Proper format is: PREFABNAME,CRAFT_COST,REFUND_BOOL eg: Wood,8,false");
                    return false;
                }
                if (bool.TryParse(recipe_segments[2], out bool refund_flag_parse) == false) {
                    ModLogger.LogWarning($"{recipe_entry} is invalid, the REFUND_BOOL could not be parsed to (true/false). Proper format is: PREFABNAME,CRAFT_COST,REFUND_BOOL eg: Wood,8,false");
                    return false;
                }
                if (int.TryParse(recipe_segments[1], out int amount_parse) == false) {
                    ModLogger.LogWarning($"{recipe_entry} is invalid, the CRAFT_COST could not be parsed to a whole number. Proper format is: PREFABNAME,CRAFT_COST,REFUND_BOOL eg: Wood,8,false");
                    return false;
                }

                if (ModContext.DebugEnabled) {
                    ModLogger.LogInfo($"prefab: {recipe_segments[0]} c:{recipe_segments[1]} u:{recipe_segments[2]}");
                }
                updated_pieceRecipe.Add(new PieceCost() { Prefab = recipe_segments[0], Amount = amount_parse, Refundable = refund_flag_parse });
            }

            jBuildPiece.Cfgs.UpdatedCost.Clear();
            foreach (PieceCost entry in updated_pieceRecipe) { jBuildPiece.Cfgs.UpdatedCost.Add(entry); }
            if (ModContext.DebugEnabled) {
                string recipe_string = "";
                foreach (PieceCost entry in updated_pieceRecipe) {
                    recipe_string += $" {entry.Prefab} c:{entry.Amount} r:{entry.Refundable}";
                }
                ModLogger.LogInfo($"Updated recipe:{recipe_string}");
            }
            return true;
        }
    }
}
