using EpicLoot.Config;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// Offers to refresh base configs the player has edited once an update changes their defaults.
/// Mirrors the WelcomeMessage patch below it: a Postfix on FejdStartup.Start that instantiates a
/// prefab under the main menu.
/// </summary>
[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
public static class ConfigUpdatePrompt_FejdStartup_Start_Patch
{
    private static bool _shownThisSession;

    public static void Postfix(FejdStartup __instance)
    {
        if (!ShouldPrompt())
        {
            return;
        }

        _shownThisSession = true;

        try
        {
            ShowConfigMessage(__instance.transform);
        }
        catch (Exception e)
        {
            // Never let a cosmetic prompt break the main menu; the detection warning is already in
            // the log, so the player still has a way to find out.
            EpicLoot.LogWarningForce($"Could not show the Epic Loot config update prompt.\n{e}");
        }
    }

    private static bool ShouldPrompt()
    {
        // Declines are recorded per file during detection, so anything still listed here is both
        // player-modified and unacknowledged for the current default.
        if (_shownThisSession || !ConfigVersionManager.DetectionRan || !ConfigVersionManager.HasOutdatedConfigs)
        {
            return false;
        }

        // A dedicated server has no main menu; the detection warning in the log is its only surface.
        if (GUIManager.IsHeadless())
        {
            return false;
        }

        if (EpicAssets.ConfigMessagePrefab == null)
        {
            EpicLoot.LogWarningForce("The ConfigMessage prefab is missing from the asset bundle, " +
                "so outdated configs can only be reported in the log.");
            return false;
        }

        // Don't stack on top of the first-run welcome panel.
        return !ConfigVersionManager.WelcomeMessageWillShow;
    }

    private static void ShowConfigMessage(Transform parentTransform)
    {
        GameObject panel = UnityEngine.Object.Instantiate(EpicAssets.ConfigMessagePrefab, parentTransform, false);
        panel.name = "ConfigMessage";

        ConfigMessage configMessage = panel.AddComponent<ConfigMessage>();
        configMessage.SetMessage(
            Localization.instance.Localize("$el_configupdate_title"),
            BuildBody());
    }

    private static string BuildBody()
    {
        string fileList = string.Join("\n", ConfigVersionManager.OutdatedConfigs
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(x => $" - {x}.json"));

        return string.Format(Localization.instance.Localize("$el_configupdate_body"),
            EpicLoot.Version, fileList);
    }
}
