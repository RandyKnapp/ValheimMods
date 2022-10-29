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
    [BepInPlugin(PluginId, "Jam", "1.0.3")]
    public class Jam : BaseUnityPlugin
    {
        public const string PluginId = "randyknapp.mods.jam";

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

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PluginId);
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
                assetFileName = Path.Combine(Path.GetDirectoryName(assembly.Location), assetName);
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
            _harmony?.UnpatchSelf();
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
                return;
            }

            foreach (var prefab in Prefabs.Values)
            {
                zNetScene.m_prefabs.Add(prefab);
            }
        }

        public static bool IsObjectDBReady()
        {
            // Hack, just making sure the built-in items and prefabs have loaded
            return ObjectDB.instance != null && ObjectDB.instance.m_items.Count != 0 && ObjectDB.instance.GetItemPrefab("Amber") != null;
        }

        public static void TryRegisterItems()
        {
            if (!IsObjectDBReady())
            {
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
            if (!IsObjectDBReady())
            {
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
