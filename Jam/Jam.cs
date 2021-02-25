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

        private void Awake()
        {
            _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchAll();
        }
    }
}
