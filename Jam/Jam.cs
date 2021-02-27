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
        public static ConsumablesConfig Consumables;

        private void Awake()
        {
            var jsonFileName = Path.Combine(Paths.PluginPath, "Jam", "consumables.json");
            var jsonFile = File.ReadAllText(jsonFileName);
            Consumables = LitJson.JsonMapper.ToObject<ConsumablesConfig>(jsonFile);

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll();
        }

        public static void TryRegisterItems()
        {
            if (Consumables == null || ZNetScene.instance == null || ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
            {
                return;
            }

            PrefabCreator.Reset();
            var success = 0;
            var fail = 0;
            foreach (var item in Consumables.items)
            {
                var newPrefab = PrefabCreator.CreatePrefab(item);
                if (newPrefab == null)
                {
                    fail++;
                }
                else
                {
                    success++;
                }
            }

            if (fail > 0)
            {
                Debug.LogError($"Failed to initialize {fail} prefabs!");
            }
            if (success > 0)
            {
                Debug.LogWarning($"Successfully initialized {success} prefabs in Jam.Update");
            }
        }
    }
}
