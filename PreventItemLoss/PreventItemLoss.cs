using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace PreventItemLoss
{
    [BepInPlugin("randyknapp.mods.preventitemloss", "Prevent Item Loss", "1.0.0")]
    public class PreventItemLoss : BaseUnityPlugin
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
