using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot
{
    public class WelcomeMessage : MonoBehaviour
    {
        public const string PlayerPrefKey = "el-wm";
        public const int VersionNumber = 1;

        public static bool PlayerHasSeenMessage()
        {
            return PlayerPrefs.GetInt(PlayerPrefKey, 0) >= VersionNumber;
        }

        public static void SetPlayerHasSeenMessage()
        {
            PlayerPrefs.SetInt(PlayerPrefKey, VersionNumber);
        }

        public void Awake()
        {
            var discordButton = transform.Find("DiscordButton")?.GetComponent<Button>();
            var patchNotesButton = transform.Find("PatchNotesButton")?.GetComponent<Button>();
            var closeButton = transform.Find("CloseButton")?.GetComponent<Button>();

            if (EpicLoot.HasAuga)
            {
                EpicLootAuga.ReplaceBackground(gameObject, true);
                EpicLootAuga.FixFonts(gameObject);
                if (discordButton != null)
                    discordButton = EpicLootAuga.ReplaceButton(discordButton);
                if (patchNotesButton != null)
                    patchNotesButton = EpicLootAuga.ReplaceButton(patchNotesButton);
                if (closeButton != null)
                    closeButton = EpicLootAuga.ReplaceButton(closeButton);
            }

            if (discordButton != null)
                discordButton.onClick.AddListener(OnJoinDiscordClick);
            if (patchNotesButton != null)
                patchNotesButton.onClick.AddListener(OnPatchNotesClick);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        public void OnJoinDiscordClick()
        {
            Application.OpenURL("https://discord.gg/randyknappmods");
            SetPlayerHasSeenMessage();
            Destroy(gameObject);
        }

        public static void OnPatchNotesClick()
        {
            Application.OpenURL("https://github.com/RandyKnapp/ValheimMods/blob/main/EpicLoot/patchnotes.md");
        }

        public void Close()
        {
            SetPlayerHasSeenMessage();
            Destroy(gameObject);
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Start))]
    public class WelcomeMessage_FejdStartup_Start_Patch
    {
        public static void Postfix(FejdStartup __instance)
        {
            if (EpicLoot.AlwaysShowWelcomeMessage.Value || !WelcomeMessage.PlayerHasSeenMessage())
            {
                var welcomeMessage = Object.Instantiate(EpicLoot.Assets.WelcomMessagePrefab, __instance.transform, false);
                welcomeMessage.name = "WelcomeMessage";
                welcomeMessage.AddComponent<WelcomeMessage>();
            }
        }
    }
}
