using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AdvancedPortals
{
    internal class UpdatePortals
    {
        public static List<RequirementConfig> MakeRecipeFromConfig(string portalName, string configString)
        {
            List<RequirementConfig> recipe = new List<RequirementConfig>();

            string[] entries = configString.Replace(" ", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string entry in entries)
            {
                string[] parts = entry.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    AdvancedPortals.APLogger.LogError($"Incorrectly formatted recipe for {portalName}! " +
                        $"Should be 'ITEM:QUANITY,ITEM2:QUANTITY' etc.");
                    continue;
                }

                string item = parts[0];
                string amountString = parts[1];
                if (!int.TryParse(amountString, out int amount))
                {
                    AdvancedPortals.APLogger.LogError($"Incorrectly formatted recipe for {portalName}! " +
                        $"Should be 'ITEM:QUANITY,ITEM2:QUANTITY' etc.");
                    continue;
                }

                recipe.Add(new RequirementConfig
                {
                    Item = item,
                    Amount = amount,
                    Recover = true
                });
            }

            return recipe;
        }

        public static void UpdatePortalConfigurations()
        {
            GameObject hammerPrefab = PrefabManager.Instance.GetPrefab("Hammer");

            if (hammerPrefab == null)
            {
                AdvancedPortals.APLogger.LogError($"Hammer not found, could not update portal configurations.");
                return;
            }

            if (!hammerPrefab.TryGetComponent<ItemDrop>(out ItemDrop itemdrop))
            {
                AdvancedPortals.APLogger.LogError($"Hammer not found, could not update portal configurations.");
                return;
            }

            var pieceTable = itemdrop.m_itemData.m_shared.m_buildPieces;

            foreach (string portal in AdvancedPortals.PortalPrefabs)
            {
                GameObject portalPrefab = PrefabManager.Instance.GetPrefab(portal);

                if (portalPrefab == null)
                {
                    AdvancedPortals.APLogger.LogError($"{portal} not found, could not update configurations.");
                    continue;
                }

                if (!portalPrefab.TryGetComponent<AdvancedPortal>(out AdvancedPortal component))
                {
                    AdvancedPortals.APLogger.LogError($"AdvancedPortal not found, could not update {portal} configurations.");
                    continue;
                }

                string name = string.Empty;
                string recipeString = string.Empty;
                bool enabled = true;

                switch (portal)
                {
                    case "portal_ancient":
                        enabled = AdvancedPortals.AncientPortalEnabled.Value;
                        name = "Ancient Portal";
                        recipeString = AdvancedPortals.AncientPortalRecipe.Value;
                        component.AllowEverything = AdvancedPortals.AncientPortalAllowEverything.Value;
                        component.AllowedItems = GetListFromString(AdvancedPortals.AncientPortalAllowedItems.Value);
                        break;
                    case "portal_obsidian":
                        enabled = AdvancedPortals.ObsidianPortalEnabled.Value;
                        name = "Obsidian Portal";
                        recipeString = AdvancedPortals.ObsidianPortalRecipe.Value;
                        component.AllowEverything = AdvancedPortals.ObsidianPortalAllowEverything.Value;
                        component.AllowedItems = GetListFromString(AdvancedPortals.ObsidianPortalAllowedItems.Value);
                        if (AdvancedPortals.ObsidianPortalAllowPreviousPortalItems.Value)
                        {
                            component.AllowedItems.AddRange(GetListFromString(AdvancedPortals.AncientPortalAllowedItems.Value));
                        }
                        break;
                    case "portal_blackmarble":
                        enabled = AdvancedPortals.BlackMarblePortalEnabled.Value;
                        name = "Black Marble Portal";
                        recipeString = AdvancedPortals.BlackMarblePortalRecipe.Value;
                        component.AllowEverything = AdvancedPortals.BlackMarblePortalAllowEverything.Value;
                        component.AllowedItems = GetListFromString(AdvancedPortals.BlackMarblePortalAllowedItems.Value);
                        if (AdvancedPortals.BlackMarblePortalAllowPreviousPortalItems.Value)
                        {
                            component.AllowedItems.AddRange(GetListFromString(AdvancedPortals.AncientPortalAllowedItems.Value));
                            component.AllowedItems.AddRange(GetListFromString(AdvancedPortals.ObsidianPortalAllowedItems.Value));
                        }
                        break;
                    default:
                        AdvancedPortals.APLogger.LogError($"Could not update {portal} configurations. Not a configurable advance portal.");
                        continue;
                }

                Piece piece = portalPrefab.GetComponent<Piece>();
                if (piece == null)
                {
                    AdvancedPortals.APLogger.LogError($"Could not update portal configurations for {portal}. Piece not found.");
                }

                GameObject pieceTablePiece = pieceTable.m_pieces.Find(x => x.name == Utils.GetPrefabName(portalPrefab.name));
                if (!enabled)
                {
                    if (pieceTablePiece != null)
                    {
                        // Remove existing
                        pieceTable.m_pieces.Remove(pieceTablePiece);
                    }
                }
                else
                {
                    List<RequirementConfig> config = MakeRecipeFromConfig(name, recipeString);
                    List<Piece.Requirement> reqs = new List<Piece.Requirement>();
                    foreach (RequirementConfig req in config)
                    {
                        var newReq = req.GetRequirement();
                        GameObject resource = PrefabManager.Instance.GetPrefab(req.Item);
                        ItemDrop resourceItemDrop = resource.GetComponent<ItemDrop>();
                        if (resourceItemDrop != null)
                        {
                            newReq.m_resItem = resourceItemDrop;
                            reqs.Add(newReq);
                        }
                        else
                        {
                            AdvancedPortals.APLogger.LogError($"Could not add requirement {req.Item}, for {portal}");
                        }
                    }

                    piece.m_resources = reqs.ToArray();
                    piece.m_description = GetAdvancedPortalDescription(component.AllowEverything, component.AllowedItems);

                    if (pieceTablePiece != null)
                    {
                        // Update existing
                        var tablePiece = pieceTablePiece.GetComponent<Piece>();
                        tablePiece.m_resources = reqs.ToArray();
                    }
                    else
                    {
                        // Add new
                        pieceTable.m_pieces.Add(portalPrefab);
                    }
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
