using BepInEx.Configuration;
using Common;
using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedPortals
{
    /// <summary>
    /// Declares this mod's portals and owns the teleport half of their configuration.
    ///
    /// The build side -- enabled, crafting station, build category and build cost -- is handled entirely by
    /// the shared <see cref="PieceLoader"/>, which binds those as server-synced entries that apply live
    /// without a restart. Only the rule this mod adds on top, which items a portal lets through, lives here.
    /// </summary>
    internal static class Portals
    {
        internal class PortalDefinition
        {
            /// <summary>Prefab name in the asset bundle, and the name it is registered under.</summary>
            public string PrefabName;

            /// <summary>Config section holding both the piece entries and the teleport entries below.</summary>
            public string DisplayName;

            public ConfigEntry<string> AllowedItems;
            public ConfigEntry<bool> AllowEverything;

            /// <summary>Null on the first portal, which has no earlier portal to inherit from.</summary>
            public ConfigEntry<bool> UsePreviousPortalItems;
        }

        /// <summary>
        /// Declaration order is progression order: "Use All Previous" inherits the allow-list of every
        /// portal declared before it, so adding a tier means appending one entry in <see cref="RegisterAll"/>.
        /// </summary>
        private static readonly List<PortalDefinition> Definitions = new List<PortalDefinition>();

        /// <summary>Prefab names in declaration order, for registering the portal hashes with Game.</summary>
        internal static IEnumerable<string> PrefabNames => Definitions.Select(portal => portal.PrefabName);

        // Every teleport entry debounces against this one key rather than its own ConfigEntry. The
        // allow-lists chain through "Use All Previous", so a single edit has to re-apply every portal, and
        // a shared key collapses a burst of edits (typing, a file reload, a server sync) into one pass.
        private static readonly object TeleportRulesKey = new object();

        /// <summary>
        /// Registers every portal. Call once from Awake, after <see cref="ModContext.Initialize"/> and after
        /// the asset bundle is available.
        /// </summary>
        internal static void RegisterAll()
        {
            Register("portal_ancient", "Ancient Portal",
                new List<PieceLoader.PieceCost>
                {
                    new PieceLoader.PieceCost { Prefab = "ElderBark", Amount = 20 },
                    new PieceLoader.PieceCost { Prefab = "Iron", Amount = 5 },
                    new PieceLoader.PieceCost { Prefab = "SurtlingCore", Amount = 2 }
                },
                allowedItems: "Copper, CopperOre, CopperScrap, Tin, TinOre, Bronze, BronzeScrap",
                allowEverything: false,
                usePreviousPortalItems: null);

            Register("portal_obsidian", "Obsidian Portal",
                new List<PieceLoader.PieceCost>
                {
                    new PieceLoader.PieceCost { Prefab = "Obsidian", Amount = 20 },
                    new PieceLoader.PieceCost { Prefab = "Silver", Amount = 5 },
                    new PieceLoader.PieceCost { Prefab = "SurtlingCore", Amount = 2 }
                },
                allowedItems: "Iron, IronScrap, IronOre",
                allowEverything: false,
                usePreviousPortalItems: true);

            Register("portal_blackmarble", "Black Marble Portal",
                new List<PieceLoader.PieceCost>
                {
                    new PieceLoader.PieceCost { Prefab = "BlackMarble", Amount = 20 },
                    new PieceLoader.PieceCost { Prefab = "BlackMetal", Amount = 5 },
                    new PieceLoader.PieceCost { Prefab = "Eitr", Amount = 2 }
                },
                allowedItems: "Silver, SilverOre, BlackMetal, BlackMetalScrap",
                allowEverything: true,
                usePreviousPortalItems: true);
        }

        private static void Register(string prefabName, string displayName, List<PieceLoader.PieceCost> pieceCost,
                                     string allowedItems, bool allowEverything, bool? usePreviousPortalItems)
        {
            // The build side. Name doubles as the config section, so the teleport entries bound below land
            // in the same section of the .cfg. Name, description and icon are deliberately left unset: the
            // bundle prefabs bake all three, and Jotunn's PieceConfig.Apply only overwrites them when the
            // config supplies a value.
            PieceLoader.Register(new PieceLoader.BuildPiece
            {
                Name = displayName,
                Prefab = prefabName,
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                Workbench = "piece_workbench",
                PieceCost = pieceCost
            });

            PortalDefinition portal = new PortalDefinition
            {
                PrefabName = prefabName,
                DisplayName = displayName
            };

            portal.AllowedItems = ConfigBinder.BindServerConfig(displayName, "Allowed Items", allowedItems,
                "A comma separated list of the item types allowed through this portal. Find item ids: " +
                "https://valheim.fandom.com/wiki/Item_IDs");
            portal.AllowEverything = ConfigBinder.BindServerConfig(displayName, "Allow Everything", allowEverything,
                "Allow all items through this portal. Overrides Allowed Items.");
            if (usePreviousPortalItems.HasValue)
            {
                portal.UsePreviousPortalItems = ConfigBinder.BindServerConfig(displayName, "Use All Previous",
                    usePreviousPortalItems.Value,
                    "Additionally allow everything the portals listed before this one allow.");
            }

            portal.AllowedItems.SettingChanged += TeleportRuleChanged;
            portal.AllowEverything.SettingChanged += TeleportRuleChanged;
            if (portal.UsePreviousPortalItems != null)
            {
                portal.UsePreviousPortalItems.SettingChanged += TeleportRuleChanged;
            }

            Definitions.Add(portal);
        }

        private static void TeleportRuleChanged(object sender, EventArgs e)
        {
            ConfigChangeDebouncer.Schedule(TeleportRulesKey, ApplyAllTeleportRules);
        }

        /// <summary>
        /// Pushes the configured allow-lists onto the portal prefabs' <see cref="AdvancedPortal"/> components
        /// and refreshes their build-menu descriptions. Safe to call repeatedly.
        /// </summary>
        internal static void ApplyAllTeleportRules()
        {
            for (int i = 0; i < Definitions.Count; i++)
            {
                PortalDefinition portal = Definitions[i];

                GameObject prefab = PrefabManager.Instance.GetPrefab(portal.PrefabName);
                if (prefab == null)
                {
                    ModLogger.LogError($"{portal.PrefabName} not found, could not update its teleport rules.");
                    continue;
                }

                if (!prefab.TryGetComponent(out AdvancedPortal component))
                {
                    ModLogger.LogError($"AdvancedPortal component not found on {portal.PrefabName}, " +
                        "could not update its teleport rules.");
                    continue;
                }

                component.AllowEverything = portal.AllowEverything.Value;
                component.AllowedItems = GetListFromString(portal.AllowedItems.Value);

                // Inherit from every portal declared earlier rather than from a named predecessor, so a new
                // tier appended to RegisterAll picks its ancestors up with no further code change.
                if (portal.UsePreviousPortalItems != null && portal.UsePreviousPortalItems.Value)
                {
                    for (int earlier = 0; earlier < i; earlier++)
                    {
                        component.AllowedItems.AddRange(GetListFromString(Definitions[earlier].AllowedItems.Value));
                    }
                }

                if (prefab.TryGetComponent(out Piece piece))
                {
                    piece.m_description = GetAdvancedPortalDescription(component.AllowEverything, component.AllowedItems);
                }
            }
        }

        /// <summary>
        /// Returns a UI description of the portal with the allowed teleportation rules.
        /// </summary>
        private static string GetAdvancedPortalDescription(bool allowEverything, List<string> items)
        {
            return $"$piece_portal_description Can Teleport: ({(allowEverything ? "Anything" : string.Join(", ", items))})";
        }

        private static List<string> GetListFromString(string items)
        {
            return items.Replace(" ", "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}
