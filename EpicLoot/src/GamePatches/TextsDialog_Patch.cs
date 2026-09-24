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
        __instance.m_texts.Insert(EpicLoot.HasAuga ? 4 : 6, MagicPages.instance.ShardStonePage);
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

[HarmonyPatch(typeof(Chat), nameof(Chat.HasFocus))]
internal static class Chat_HasFocus_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result &= !MagicPages.InSearchField();
    }
}

