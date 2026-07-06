using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.Adventure;

[HarmonyPatch(typeof(Minimap))]
public static class MinimapPatch
{
    [HarmonyPatch(nameof(Minimap.Awake))]
    [UsedImplicitly]
    private static void Postfix(Minimap __instance)
    {
        __instance.gameObject.AddComponent<MinimapController>();
    }
}