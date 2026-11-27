using EpicLoot.Compendium;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace EpicLoot;

[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Awake))]
internal static class TextsDialog_Awake_Patch
{
    private static void Postfix(TextsDialog __instance)
    {
        // Auga's compendium holds a tutorial and a lore dialog (AugaTextsDialogTutorial / AugaTextsDialogLore);
        // the pages belong with the tutorials only.
        if (EpicLoot.HasAuga && __instance.name.IndexOf("Lore", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return;
        }

        __instance.gameObject.AddComponent<MagicPages>();
    }
}

[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.UpdateTextsList))]
internal static class TextsDialog_UpdateTextsList_Patch
{
    private static void Postfix(TextsDialog __instance)
    {
        // Each dialog lists its own page objects: a TextInfo carries the list element built for it, so one
        // shared between two dialogs would be rewired by whichever set up last.
        if (!Player.m_localPlayer || !__instance.TryGetComponent(out MagicPages pages) || !pages.IsReady)
        {
            return;
        }

        // Vanilla lists Active Effects and Logs first; Auga lists neither, so the pages go at the top.
        int first = EpicLoot.HasAuga ? 0 : 2;
        __instance.m_texts.Insert(first, pages.MagicEffectsPage);
        __instance.m_texts.Insert(first + 1, pages.ExplainPage);
        __instance.m_texts.Insert(first + 2, pages.TreasureBountyPage);
        __instance.m_texts.Insert(first + 3, pages.SetInfos);
        __instance.m_texts.Insert(first + 4, pages.ShardStonePage);
    }
}

[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.ShowText), typeof(TextsDialog.TextInfo))]
internal static class TextsDialog_ShowText_Patch
{
    private static void Postfix(TextsDialog __instance, TextsDialog.TextInfo text)
    {
        if (!__instance.TryGetComponent(out MagicPages component))
        {
            return;
        }

        component.Reset();
        if (text is MagicTextInfo magicInfo)
        {
            component.OnSelectText(magicInfo);
        }
    }
}

[HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Setup))]
internal static class TextsDialog_Setup_Patch
{
    // Prefix, because Setup ends in ShowText(0) and a postfix would wipe the page it just built.
    private static void Prefix(TextsDialog __instance) => __instance.GetComponent<MagicPages>()?.Reset();
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

// Typing in the Compendium's search field reads as chat focus, so the game's hotkeys ignore the keystrokes.
[HarmonyPatch(typeof(Chat), nameof(Chat.HasFocus))]
internal static class Chat_HasFocus_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result |= MagicPages.InSearchField();
    }
}

