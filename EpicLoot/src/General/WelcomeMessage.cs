using EpicLoot.Config;
using HarmonyLib;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

public sealed class WelcomeMessage : MonoBehaviour
{
    public Text TitleText { get; private set; }
    public Text ContentText { get; private set; }

    public Button DiscordButton { get; private set; }
    public Button PatchNotesButton { get; private set; }
    public Button CloseButton { get; private set; }

    public Button OverhaulMinimalButton { get; private set; }
    public Button OverhaulBalancedButton { get; private set; }
    public Button OverhaulLegendaryButton { get; private set; }

    void Awake()
    {
        SetupUI();
    }

    void SetupUI()
    {
        TitleText = transform.Find("Title").GetComponent<Text>();
        ContentText = transform.Find("Content").GetComponent<Text>();

        DiscordButton = transform.Find("DiscordButton").GetComponent<Button>();
        PatchNotesButton = transform.Find("PatchNotesButton").GetComponent<Button>();
        CloseButton = transform.Find("CloseButton").GetComponent<Button>();

        OverhaulMinimalButton = transform.Find("overhaul_minimal").GetComponent<Button>();
        OverhaulBalancedButton = transform.Find("overhaul_balanced").GetComponent<Button>();
        OverhaulLegendaryButton = transform.Find("overhaul_legendary").GetComponent<Button>();

        if (EpicLoot.HasAuga)
        {
            ApplyAugaUI();
        }

        DiscordButton.onClick.AddListener(OnJoinDiscordClick);
        PatchNotesButton.onClick.AddListener(OnPatchNotesClick);
        CloseButton.onClick.AddListener(Close);

        OverhaulMinimalButton.onClick.AddListener(SetOverhaulMinimalAndClick);
        OverhaulBalancedButton.onClick.AddListener(SetOverhaulBalancedAndClick);
        OverhaulLegendaryButton.onClick.AddListener(SetOverhaulLegendaryAndClick);
    }

    void ApplyAugaUI()
    {
        EpicLootAuga.ReplaceBackground(gameObject, withCornerDecoration: true);
        EpicLootAuga.FixFonts(gameObject);

        EpicLootAuga.ReplaceButton(DiscordButton);
        EpicLootAuga.ReplaceButton(PatchNotesButton);
        EpicLootAuga.ReplaceButton(CloseButton);
    }

    public void OnJoinDiscordClick()
    {
        Application.OpenURL("https://discord.gg/ZNhYeavv3C");
        Close();
    }

    public void OnPatchNotesClick()
    {
        Application.OpenURL("https://thunderstore.io/c/valheim/p/RandyKnapp/EpicLoot/changelog/");
        Close();
    }

    public void Close()
    {
        ELConfig.AlwaysShowWelcomeMessage.Value = false;
        Destroy(gameObject);
    }

    public void SetOverhaulBalancedAndClick()
    {
        ELConfig.BalanceConfigurationType.Value = "balanced";
        ELConfig.ItemsUnidentifiedDropRatio.Value = 0.8f;
        ELConfig.ItemsToMaterialsDropRatio.Value = 0.95f;

        OnOverhaulButtonClick();
        Close();
    }

    public void SetOverhaulMinimalAndClick()
    {
        ELConfig.BalanceConfigurationType.Value = "minimal";
        ELConfig.ItemsToMaterialsDropRatio.Value = 1.0f;

        OnOverhaulButtonClick();
        Close();
    }

    public void SetOverhaulLegendaryAndClick()
    {
        ELConfig.BalanceConfigurationType.Value = "legendary";
        ELConfig.ItemsUnidentifiedDropRatio.Value = 0.2f;
        ELConfig.ItemsToMaterialsDropRatio.Value = 0.1f;

        OnOverhaulButtonClick();
        Close();
    }

    void OnOverhaulButtonClick()
    {
        string basecfglocation =
            Path.Combine(ELConfig.GetOverhaulDirectoryPath(), "magiceffects.json");
        string overhaulfiledata =
            EpicLoot.ReadEmbeddedResourceFile(ELConfig.GetDefaultEmbeddedFileLocation("magiceffects.json"));

        File.WriteAllText(basecfglocation, overhaulfiledata);
    }
}

[HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
static class WelcomeMessage_FejdStartup_Start_Patch
{
    static void Postfix(FejdStartup __instance)
    {
        if (ELConfig.AlwaysShowWelcomeMessage.Value)
        {
            ShowWelcomeMessage(__instance.transform);
        }
    }

    static void ShowWelcomeMessage(Transform parentTransform)
    {
        GameObject welcomeMessage = Object.Instantiate(EpicAssets.WelcomMessagePrefab, parentTransform, false);
        welcomeMessage.name = "WelcomeMessage";
        welcomeMessage.AddComponent<WelcomeMessage>();
    
        ELConfig.AlwaysShowWelcomeMessage.Value = false;
    }
}
