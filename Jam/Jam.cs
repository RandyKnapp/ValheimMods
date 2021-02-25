using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Jam
{
    [BepInPlugin("randyknapp.mods.jam", "Jam", "1.0.0")]
    public class Jam : BaseUnityPlugin
    {
        private Harmony _harmony;
        public ConsumablesConfig Consumables;
        private bool _initialized;

        private void Awake()
        {
            var jsonFileName = Path.Combine(Paths.PluginPath, "Jam", "consumables.json");
            var jsonFile = File.ReadAllText(jsonFileName);
            Consumables = LitJson.JsonMapper.ToObject<ConsumablesConfig>(jsonFile);

            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

            _initialized = false;
        }

        private void OnDestroy()
        {
            _initialized = false;
            _harmony?.UnpatchAll();
        }

        private void Update()
        {
            if (_initialized)
            {
                return;
            }

            if (ZNetScene.instance == null || ObjectDB.instance == null)
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

            _initialized = true;
        }
    }
}
