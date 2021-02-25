using System.Collections.Generic;
using System.IO;
using BepInEx;
using UnityEngine;

namespace Jam
{
    public static class PrefabCreator
    {
        private static Dictionary<string, CraftingStation> CraftingStations;

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

        public static GameObject CreatePrefab(ConsumableItem config)
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
                Debug.Log($"[Jam] Found existing prefab for {existingItemPrefab.name}");
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
                Debug.LogError($"[Jam] Could not load basePrefab: {config.basePrefab}");
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
                Debug.LogError($"[Jam] Could not parse foodColor ({config.id}): {config.foodColor}");
            }

            var icons = new List<Sprite>();
            foreach (var icon in config.icons)
            {
                var sprite = Common.Utils.LoadSpriteFromFile(icon);
                icons.Add(sprite);
            }
            itemDrop.m_itemData.m_shared.m_icons = icons.ToArray();
            ObjectDB.instance.m_items.Add(newPrefab);

            var newRecipe = ScriptableObject.CreateInstance<Recipe>();
            newRecipe.name = $"Recipe_{config.id}";
            newRecipe.m_amount = config.recipe.amount;
            newRecipe.m_minStationLevel = config.recipe.minStationLevel;
            newRecipe.m_item = ObjectDB.instance.GetItemPrefab(config.id).GetComponent<ItemDrop>();
            newRecipe.m_enabled = config.recipe.enabled;

            if (!string.IsNullOrEmpty(config.recipe.craftingStation))
            {
                var craftingStationExists = CraftingStations.ContainsKey(config.recipe.craftingStation);
                if (!craftingStationExists)
                {
                    Debug.LogWarning($"[Jam] Could not find crafting station ({config.id}): {config.recipe.craftingStation}");
                    var stationList = string.Join(", ", CraftingStations.Keys);
                    Debug.Log($"[Jam] Available Stations: {stationList}");
                }
                else
                {
                    newRecipe.m_craftingStation = CraftingStations[config.recipe.craftingStation];
                }
            }

            if (!string.IsNullOrEmpty(config.recipe.repairStation))
            {
                var repairStationExists = CraftingStations.ContainsKey(config.recipe.repairStation);
                if (!repairStationExists)
                {
                    Debug.LogWarning($"[Jam] Could not find repair station ({config.id}): {config.recipe.repairStation}");
                    var stationList = string.Join(", ", CraftingStations.Keys);
                    Debug.Log($"[Jam] Available Stations: {stationList}");
                }
                else
                {
                    newRecipe.m_repairStation = CraftingStations[config.recipe.repairStation];
                }
            }

            var reqs = new List<Piece.Requirement>();
            foreach (var requirement in config.recipe.resources)
            {
                var reqPrefab = ObjectDB.instance.GetItemPrefab(requirement.item);
                if (reqPrefab == null)
                {
                    Debug.LogError($"[Jam] Could not load requirement item ({config.id}): {requirement.item}");
                    continue;
                }

                reqs.Add(new Piece.Requirement()
                {
                    m_amount = requirement.amount,
                    m_resItem = reqPrefab.GetComponent<ItemDrop>()
                });
            }
            newRecipe.m_resources = reqs.ToArray();

            ObjectDB.instance.m_recipes.Add(newRecipe);
            Debug.Log($"[Jam] Created {newPrefab.name}");

            return newPrefab;
        }
    }
}
