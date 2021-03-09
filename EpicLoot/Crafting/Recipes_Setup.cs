using UnityEngine;

namespace EpicLoot.Crafting
{
    public static class Recipes_Setup
    {
        public static void SetupRecipes()
        {
            const int upgradeRatio = 10;

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
                    //recipe.m_craftingStation = ZNetScene.instance.GetPrefab(("ArtisanStation").GetStableHashCode()).GetComponent<CraftingStation>();

                    ObjectDB.instance.m_recipes.Add(recipe);
                }
            }
        }
    }
}
