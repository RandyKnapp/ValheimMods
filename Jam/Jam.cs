using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using Common;
using HarmonyLib;
using LitJson;
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
            Recipes = LoadJsonFile<RecipesConfig>("recipes.json");
            var assetBundle = LoadAssetBundle("jamassets");
            if (Recipes != null && assetBundle != null)
            {
                foreach (var recipe in Recipes.recipes)
                {
                    if (assetBundle.Contains(recipe.item))
                    {
                        var prefab = assetBundle.LoadAsset<GameObject>(recipe.item);
                        Prefabs.Add(recipe.item, prefab);
                    }
                }
            }

            assetBundle?.Unload(false);

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private static T LoadJsonFile<T>(string filename) where T : class
        {
            var jsonFileName = GetAssetPath(filename);
            if (!string.IsNullOrEmpty(jsonFileName))
            {
                var jsonFile = File.ReadAllText(jsonFileName);
                return JsonMapper.ToObject<T>(jsonFile);
            }

            return null;
        }

        private static AssetBundle LoadAssetBundle(string filename)
        {
            var assetBundlePath = GetAssetPath(filename);
            if (!string.IsNullOrEmpty(assetBundlePath))
            {
                return AssetBundle.LoadFromFile(assetBundlePath);
            }

            return null;
        }

        private static string GetAssetPath(string assetName)
        {
            var assetFileName = Path.Combine(Paths.PluginPath, "Jam", assetName);
            if (!File.Exists(assetFileName))
            {
                Assembly assembly = typeof(Jam).Assembly;
                assetFileName = Path.Combine(assembly.Location, assetName);
                if (!File.Exists(assetFileName))
                {
                    Debug.LogError($"Could not find asset ({assetName})");
                    return null;
                }
            }

            return assetFileName;
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
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
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
            if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
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
