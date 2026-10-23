using Common;
using EpicLoot.Config;
using Jotunn.Managers;
using System;
using System.Linq;
using UnityEngine;

namespace EpicLoot;

/// <summary>
/// Offers to refresh base configs the player has edited once an update changes their defaults.
/// Queued on the shared startup popup queue (Common/src/Config/UI) behind the Quick Configure welcome
/// wizard, rather than opening from its own FejdStartup.Start postfix: the queue opens it once the main
/// menu has settled and the wizard, if it showed, has closed, so the two never land on top of each other.
/// </summary>
public static class ConfigUpdatePrompt
{
    private const string QueueKey = "EpicLoot.ConfigUpdate";

    private static GameObject _panel;

    /// <summary>
    /// From Awake, after ELConfig has run detection and LoadAssets has loaded the prefab. The queue waits
    /// for the main menu by itself.
    /// </summary>
    public static void Init()
    {
        if (!ShouldPrompt())
        {
            return;
        }

        ConfigUIStartupPopups.Enqueue(QueueKey, ConfigUIStartupPopups.OrderNotice, TryOpen, IsOpen);
    }

    private static bool IsOpen()
    {
        return _panel != null;
    }

    private static bool TryOpen()
    {
        // Asked again as it opens: the welcome wizard ahead of it may have rewritten some of these files.
        ConfigVersionManager.PruneResolvedOutdated();
        FejdStartup startup = FejdStartup.instance;
        if (!ShouldPrompt() || startup == null)
        {
            return false;
        }

        try
        {
            ShowConfigMessage(startup.transform);
        }
        catch (Exception e)
        {
            // Never let a cosmetic prompt break the main menu; the detection warning is already in
            // the log, so the player still has a way to find out.
            EpicLoot.LogWarningForce($"Could not show the Epic Loot config update prompt.\n{e}");
        }

        // A prompt that failed halfway may still be on screen; if so the queue waits for it like any other.
        return _panel != null;
    }

    private static bool ShouldPrompt()
    {
        // Declines are recorded per file during detection, so anything still listed here is both
        // player-modified and unacknowledged for the current default.
        if (!ConfigVersionManager.DetectionRan || !ConfigVersionManager.HasOutdatedConfigs)
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

        return true;
    }

    private static void ShowConfigMessage(Transform parentTransform)
    {
        _panel = UnityEngine.Object.Instantiate(EpicAssets.ConfigMessagePrefab, parentTransform, false);
        _panel.name = "ConfigMessage";

        ConfigMessage configMessage = _panel.AddComponent<ConfigMessage>();
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
