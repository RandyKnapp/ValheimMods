using System;
using UnityEngine;

namespace EpicLoot.Crafting
{
    public static class Recipes_Setup
    {
        public static void SetupRecipes()
        {
            const int upgradeRatio = 5;

            foreach (var materialPrefabsEntry in EpicLoot.Assets.CraftingMaterialPrefabs)
            {
                var type = materialPrefabsEntry.Key;
                var byRarity = materialPrefabsEntry.Value;
                for (var index = 0; index < byRarity.Length; index++)
                {
                    var rarity = (ItemRarity) index;
                    if (rarity == ItemRarity.Magic)
                    {
                        continue;
                    }

                    var prefab = byRarity[index];
                    var resourcePrefab = byRarity[index - 1];

                    var recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.name = $"Recipe_{type}{rarity}Upgrade";
                    recipe.m_item = prefab.GetComponent<ItemDrop>();
                    var resource = resourcePrefab.GetComponent<ItemDrop>();
                    recipe.m_resources = new[] { new Piece.Requirement() { m_resItem = resource, m_amount = upgradeRatio } };
                    recipe.m_minStationLevel = 1;
                    recipe.m_enabled = true;

                    ObjectDB.instance.m_recipes.Add(recipe);
                }
            }

            // Shard to Dust, Essence, Reagent at 2:1
            const int conversionRatio = 2;
            var toTypes = new [] { "Dust", "Essence", "Reagent" };
            foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
            {
                foreach (var otherType in toTypes)
                {
                    var prefab = EpicLoot.Assets.CraftingMaterialPrefabs[otherType][(int)rarity];
                    var resourcePrefab = EpicLoot.Assets.CraftingMaterialPrefabs["Shard"][(int) rarity];

                    var recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.name = $"Recipe_{rarity}ShardTo{otherType}_Conversion";
                    recipe.m_item = prefab.GetComponent<ItemDrop>();
                    var resource = resourcePrefab.GetComponent<ItemDrop>();
                    recipe.m_resources = new[] { new Piece.Requirement() { m_resItem = resource, m_amount = conversionRatio } };
                    recipe.m_minStationLevel = 1;
                    recipe.m_enabled = true;

                    ObjectDB.instance.m_recipes.Add(recipe);
                }
            }
        }
    }
}
