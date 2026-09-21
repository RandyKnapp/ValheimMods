using HarmonyLib;
using JetBrains.Annotations;

namespace EpicLoot.Adventure;

[HarmonyPatch(typeof(Minimap))]
public static class MinimapPatch
{
    [HarmonyPatch(nameof(Minimap.Awake))]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void Postfix(Minimap __instance)
    {
        __instance.gameObject.AddComponent<MinimapController>();
    }

    [HarmonyPatch(nameof(Minimap.ToggleIconFilter))]
    [HarmonyPostfix]
    [UsedImplicitly]
    private static void ToggleIconFilterPostfix(Minimap.PinType type)
    {
        MinimapController.OnPinFilterToggled(type);
    }

    [HarmonyPatch(nameof(Minimap.ShowPinNameInput))]
    [HarmonyPrefix]
    [UsedImplicitly]
    private static bool ShowPinNameInputPrefix(Minimap __instance)
    {
        return !MinimapController.IsAdventurePinType(__instance.m_selectedType);
    }
}
