using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public static class PrefabCreator
    {
        public static Dictionary<string, CraftingStation> CraftingStations;

        public static T RequireComponent<T>(GameObject go) where T:Component
        {
            var c = go.GetComponent<T>();
            if (c == null)
            {
                c = go.AddComponent<T>();
            }

            return c;
        }

        public static void Reset()
        {
            CraftingStations = null;
        }

        public static GameObject CreatePrefab(ConsumableItemConfig config)
        {
            if (CraftingStations == null)
            {
                CraftingStations = new Dictionary<string, CraftingStation>();
                foreach (var recipe in ObjectDB.instance.m_recipes)
                {
                    if (recipe.m_craftingStation != null && !CraftingStations.ContainsKey(recipe.m_craftingStation.name))
                    {
                        CraftingStations.Add(recipe.m_craftingStation.name, recipe.m_craftingStation);
                    }
                }
            }

            var existingItemPrefab = ObjectDB.instance.GetItemPrefab(config.id);
            if (existingItemPrefab != null)
            {
                Debug.Log($"[PrefabCreator] Found existing prefab for {existingItemPrefab.name}");
                return existingItemPrefab;
                /*var existingRecipe = ObjectDB.instance.GetRecipe(existingItemPrefab.GetComponent<ItemDrop>().m_itemData);
                if (existingRecipe != null)
                {
                    ObjectDB.instance.m_recipes.Remove(existingRecipe);
                }

                ZNetScene.instance.m_namedPrefabs.Remove(existingItemPrefab.name.GetStableHashCode());
                ObjectDB.instance.m_items.Remove(existingItemPrefab);
                Object.DestroyImmediate(existingItemPrefab);*/
            }

            var basePrefab = ObjectDB.instance.GetItemPrefab(config.basePrefab);
            if (basePrefab == null)
            {
                Debug.LogError($"[PrefabCreator] Could not load basePrefab: {config.basePrefab}");
                return null;
            }

            var newPrefab = GameObject.Instantiate(basePrefab);
            newPrefab.name = config.id;
            newPrefab.layer = 12;

            ZNetView zNetView = RequireComponent<ZNetView>(newPrefab);
            zNetView.m_persistent = true;
            zNetView.m_distant = false;

            newPrefab.transform.SetParent(null);
            Object.DontDestroyOnLoad(newPrefab);
            ZNetScene.instance.m_instances.Remove(zNetView.GetZDO());
            ZNetScene.instance.m_namedPrefabs.Add(newPrefab.name.GetStableHashCode(), newPrefab);
            //newPrefab.SetActive(false);

            var rigidBody = newPrefab.GetComponent<Rigidbody>();
            rigidBody.centerOfMass = Vector3.zero;
            rigidBody.inertiaTensor = new Vector3(1, 1, 1);
            rigidBody.useGravity = false;
            rigidBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidBody.isKinematic = true;

            var itemDrop = RequireComponent<ItemDrop>(newPrefab);
            itemDrop.m_itemData = new ItemDrop.ItemData();
            itemDrop.m_itemData.m_shared = new ItemDrop.ItemData.SharedData()
            {
                m_name = config.displayName,
                m_description = config.description,
                m_itemType = ItemDrop.ItemData.ItemType.Consumable,
                m_maxStackSize = config.maxStackSize,
                m_food = config.food,
                m_foodStamina = config.foodStamina,
                m_foodRegen = config.foodRegen,
                m_foodBurnTime = config.foodBurnTime
            };

            if (!ColorUtility.TryParseHtmlString(config.foodColor, out itemDrop.m_itemData.m_shared.m_foodColor))
            {
                Debug.LogError($"[PrefabCreator] Could not parse foodColor ({config.id}): {config.foodColor}");
            }

            var icons = new List<Sprite>();
            foreach (var icon in config.icons)
            {
                var sprite = Common.Utils.LoadSpriteFromFile(icon);
                icons.Add(sprite);
            }
            itemDrop.m_itemData.m_shared.m_icons = icons.ToArray();
            ObjectDB.instance.m_items.Add(newPrefab);

            AddNewRecipe($"Recipe_{config.id}", config.id, config.RecipeConfig);

            Debug.Log($"[PrefabCreator] Created {newPrefab.name}");

            return newPrefab;
        }

        public static Recipe CreateRecipe(string name, string itemId, RecipeConfig recipeConfig)
        {
            var newRecipe = ScriptableObject.CreateInstance<Recipe>();
            newRecipe.name = name;
            newRecipe.m_amount = recipeConfig.amount;
            newRecipe.m_minStationLevel = recipeConfig.minStationLevel;
            newRecipe.m_item = ObjectDB.instance.GetItemPrefab(itemId).GetComponent<ItemDrop>();
            newRecipe.m_enabled = recipeConfig.enabled;

            if (!string.IsNullOrEmpty(recipeConfig.craftingStation))
            {
                var craftingStationExists = CraftingStations.ContainsKey(recipeConfig.craftingStation);
                if (!craftingStationExists)
                {
                    Debug.LogWarning($"[PrefabCreator] Could not find crafting station ({itemId}): {recipeConfig.craftingStation}");
                    var stationList = string.Join(", ", CraftingStations.Keys);
                    Debug.Log($"[PrefabCreator] Available Stations: {stationList}");
                }
                else
                {
                    newRecipe.m_craftingStation = CraftingStations[recipeConfig.craftingStation];
                }
            }

            if (!string.IsNullOrEmpty(recipeConfig.repairStation))
            {
                var repairStationExists = CraftingStations.ContainsKey(recipeConfig.repairStation);
                if (!repairStationExists)
                {
                    Debug.LogWarning($"[PrefabCreator] Could not find repair station ({itemId}): {recipeConfig.repairStation}");
                    var stationList = string.Join(", ", CraftingStations.Keys);
                    Debug.Log($"[PrefabCreator] Available Stations: {stationList}");
                }
                else
                {
                    newRecipe.m_repairStation = CraftingStations[recipeConfig.repairStation];
                }
            }

            var reqs = new List<Piece.Requirement>();
            foreach (var requirement in recipeConfig.resources)
            {
                var reqPrefab = ObjectDB.instance.GetItemPrefab(requirement.item);
                if (reqPrefab == null)
                {
                    Debug.LogError($"[PrefabCreator] Could not load requirement item ({itemId}): {requirement.item}");
                    continue;
                }

                reqs.Add(new Piece.Requirement()
                {
                    m_amount = requirement.amount,
                    m_resItem = reqPrefab.GetComponent<ItemDrop>()
                });
            }
            newRecipe.m_resources = reqs.ToArray();

            return newRecipe;
        }

        public static Recipe AddNewRecipe(string name, string itemId, RecipeConfig recipeConfig)
        {
            var recipe = CreateRecipe(name, itemId, recipeConfig);
            return AddNewRecipe(recipe);
        }

        public static Recipe AddNewRecipe(Recipe recipe)
        {
            var removed = ObjectDB.instance.m_recipes.RemoveAll(x => x.name == recipe.name);
            if (removed > 0)
            {
                Debug.Log($"[PrefabCreator] Removed recipes ({recipe.name}): {removed}");
            }

            ObjectDB.instance.m_recipes.Add(recipe);
            Debug.Log($"[PrefabCreator] Added recipe: {recipe.name}");

            return recipe;
        }
    }
}
