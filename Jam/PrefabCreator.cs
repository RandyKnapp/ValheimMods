using System.Collections.Generic;
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

        public static GameObject CreatePrefab()
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

            Debug.Log(ObjectDB.instance.m_items.Count);
            var existingItemPrefab = ObjectDB.instance.GetItemPrefab("BlueberryJam");
            if (existingItemPrefab != null)
            {
                Debug.Log($"Found {existingItemPrefab.name}");
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

            var basePrefab = ObjectDB.instance.GetItemPrefab("QueensJam");
            var newPrefab = GameObject.Instantiate(basePrefab);
            newPrefab.name = "BlueberryJam";
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
                m_name = "Blueberry jam",
                m_icons = new[] { Common.Utils.LoadSpriteFromFile("Jam", "BlueberryJam.png") },
                m_description = "Test Description",
                m_itemType = ItemDrop.ItemData.ItemType.Consumable,
                m_maxStackSize = 20,
                m_food = 20,
                m_foodStamina = 25,
                m_foodRegen = 1,
                m_foodBurnTime = 1200,
                m_foodColor = new Color(0, 0, 1)
            };
            ObjectDB.instance.m_items.Add(newPrefab);

            var baseRecipe = ObjectDB.instance.m_recipes.Find(x => x.name == "Recipe_QueensJam");
            var blueberryJamRecipe = ScriptableObject.CreateInstance<Recipe>();
            blueberryJamRecipe.name = "Recipe_" + "BlueberryJam";
            blueberryJamRecipe.m_amount = 4;
            blueberryJamRecipe.m_craftingStation = CraftingStations["piece_cauldron"];
            blueberryJamRecipe.m_minStationLevel = 1;
            blueberryJamRecipe.m_item = ObjectDB.instance.GetItemPrefab("BlueberryJam").GetComponent<ItemDrop>();
            blueberryJamRecipe.m_enabled = true;
            blueberryJamRecipe.m_repairStation = null;
            blueberryJamRecipe.m_resources = new[] { new Piece.Requirement()
            {
                m_amount = 8, m_resItem = baseRecipe.m_resources[1].m_resItem
            } };
            ObjectDB.instance.m_recipes.Add(blueberryJamRecipe);
            Debug.Log($"Created {newPrefab.name}");

            return newPrefab;
        }
    }
}
