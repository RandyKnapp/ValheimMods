using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using Common;
using HarmonyLib;
using UnityEngine;

namespace Jam
{
    [BepInPlugin("randyknapp.mods.jam", "Jam", "1.0.0")]
    public class Jam : BaseUnityPlugin
    {
        private Harmony _harmony;
        public static RecipesConfig Recipes;
        public static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();

        private void Awake()
        {
            var jsonFileName = Path.Combine(Paths.PluginPath, "Jam", "recipes.json");
            var jsonFile = File.ReadAllText(jsonFileName);
            Recipes = LitJson.JsonMapper.ToObject<RecipesConfig>(jsonFile);

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            var assetBundlePath = Path.Combine(Paths.PluginPath, "Jam", "jamassets");
            var assetBundle = AssetBundle.LoadFromFile(assetBundlePath);
            
            foreach (var recipe in Recipes.recipes)
            {
                if (assetBundle.Contains(recipe.item))
                {
                    var prefab = assetBundle.LoadAsset<GameObject>(recipe.item);
                    Prefabs.Add(recipe.item, prefab);
                }
            }

            assetBundle.Unload(false);
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll();
            foreach (var prefab in Prefabs.Values)
            {
                Destroy(prefab);
            }
            Prefabs.Clear();
        }

        public static void TryRegisterPrefabs(ZNetScene zNetScene)
        {
            if (zNetScene == null)
            {
                Debug.LogWarning($"[Jam] Did not register prefabs: ZNetScene.instance {ZNetScene.instance}");
                return;
            }

            foreach (var prefab in Prefabs.Values)
            {
                zNetScene.m_prefabs.Add(prefab);
            }
        }

        public static void TryRegisterItems()
        {
            if (ZNetScene.instance == null || ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                Debug.LogWarning($"[Jam] Did not register items: ZNetScene.instance {ZNetScene.instance}, ObjectDB.instance {ObjectDB.instance}, item count {ObjectDB.instance.m_items.Count}");
                return;
            }

            foreach (var prefab in Prefabs.Values)
            {
                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (ObjectDB.instance.GetItemPrefab(prefab.name.GetStableHashCode()) == null)
                    {
                        ObjectDB.instance.m_items.Add(prefab);
                    }
                }
            }
        }

        public static void TryRegisterRecipes()
        {
            if (ZNetScene.instance == null || ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                Debug.LogWarning($"[Jam] Did not register recipes: ZNetScene.instance {ZNetScene.instance}, ObjectDB.instance {ObjectDB.instance}, item count {ObjectDB.instance.m_items.Count}");
                return;
            }

            PrefabCreator.Reset();
            foreach (var recipe in Recipes.recipes)
            {
                PrefabCreator.AddNewRecipe(recipe.name, recipe.item, recipe);
            }
        }
    }
}
