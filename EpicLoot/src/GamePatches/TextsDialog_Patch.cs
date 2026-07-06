using EpicLoot.Compendium;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot;

[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Awake))]
internal static class TextsDialog_Awake_Patch
{
    private static void Postfix(TextsDialog __instance) => __instance.gameObject.AddComponent<MagicPages>();
}

[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
internal static class TextsDialog_UpdateTextsList_Patch
{
    private static void Postfix(TextsDialog __instance)
    {
        if (!Player.m_localPlayer || MagicPages.instance == null)
        {
            return;
        }

        __instance.m_texts.Insert(EpicLoot.HasAuga ? 0 : 2, MagicPages.instance.MagicEffectsPage);
        __instance.m_texts.Insert(EpicLoot.HasAuga ? 1 : 3, MagicPages.instance.ExplainPage);
        __instance.m_texts.Insert(EpicLoot.HasAuga ? 2 : 4, MagicPages.instance.TreasureBountyPage);
        __instance.m_texts.Insert(EpicLoot.HasAuga ? 3 : 5, MagicPages.instance.SetInfos);
    }
}

[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.OnSelectText))]
internal static class TextsDialog_OnSelectText_Patch
{
    private static bool Prefix(TextsDialog __instance, TextsDialog.TextInfo text)
    {
        if (!__instance.TryGetComponent(out MagicPages component))
        {
            return true;
        }

        component.Reset();
        if (text is not MagicTextInfo magicInfo)
        {
            return true;
        }

        component.OnSelectText(magicInfo);

        foreach (TextsDialog.TextInfo element in __instance.m_texts)
        {
            element.m_selected.SetActive(false);
        }

        magicInfo.m_selected.SetActive(true);
        
        __instance.StartCoroutine(__instance.FocusOnCurrentLevel(__instance.m_leftScrollRect,
            __instance.m_listRoot, magicInfo.m_selected.transform as RectTransform));
        return false;
    }
}

[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Setup))]
internal static class TextsDialog_Setup_Patch
{
    private static void Postfix(TextsDialog __instance) => __instance.GetComponent<MagicPages>()?.Reset();
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
internal static class InventoryGui_Hide_Prefix
{
    [UsedImplicitly]
    private static bool Prefix() => !MagicPages.InSearchField();
}

[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
internal static class PlayerController_TakeInput_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result &= !MagicPages.InSearchField();
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
internal static class PlayerTakeInput_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result &= !MagicPages.InSearchField();
    }
}

[HarmonyPatch(typeof(Chat), nameof(Chat.HasFocus))]
internal static class Chat_HasFocus_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result &= !MagicPages.InSearchField();
    }
}

