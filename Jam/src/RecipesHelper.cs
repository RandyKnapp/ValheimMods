using Jotunn.Configs;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jam
{
    public static class RecipesHelper
    {
        public static List<RequirementConfig> MakeRecipeFromConfig(string itemName, string configString)
        {
            List<RequirementConfig> recipe = new List<RequirementConfig>();

            string[] entries = configString.Replace(" ", "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string entry in entries)
            {
                string[] parts = entry.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    Jam.JamLogger.LogError($"Incorrectly formatted recipe for {itemName}! " +
                        $"Should be 'ITEM:QUANITY,ITEM2:QUANTITY' etc.");
                    continue;
                }

                string item = parts[0];
                string amountString = parts[1];
                if (!int.TryParse(amountString, out int amount))
                {
                    Jam.JamLogger.LogError($"Incorrectly formatted recipe for {itemName}! " +
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

        public static List<Piece.Requirement> MakeRequirementFromConfig(string itemName, string configString)
        {
            List<RequirementConfig> config = MakeRecipeFromConfig(itemName, configString);
            List<Piece.Requirement> reqs = new List<Piece.Requirement>();
            
            foreach (RequirementConfig req in config)
            {
                var newReq = req.GetRequirement();
                GameObject resource = PrefabManager.Instance.GetPrefab(req.Item);

                if (resource == null)
                {
                    Jam.JamLogger.LogError($"Could not add requirement {req.Item}, for {itemName}. Prefab not found.");
                    continue;
                }

                ItemDrop resourceItemDrop = resource.GetComponent<ItemDrop>();
                if (resourceItemDrop != null)
                {
                    newReq.m_resItem = resourceItemDrop;
                    reqs.Add(newReq);
                }
                else
                {
                    Jam.JamLogger.LogError($"Could not add requirement {req.Item}, for {itemName}.");
                }
            }

            return reqs;
        }
    }
}
