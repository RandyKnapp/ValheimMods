using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class EnchantingTableUI : MonoBehaviour
    {
        public GameObject Root;
        public GameObject Scrim;
        public TabHandler TabHandler;
        public GameObject TabScrim;

        [Header("Content")]
        public EnchantingTableUIPanelBase[] Panels;

        [Header("Audio")]
        public AudioSource Audio;
        public AudioClip TabClickSFX;
        public AudioClip EnchantBonusSFX;

        public EnchantingTable SourceTable { get; private set; }

        public static EnchantingTableUI instance { get; set; }

        public delegate void AugaFixupDelegate(EnchantingTableUI ui);
        public static AugaFixupDelegate AugaFixup;
        public delegate void TabActivationDelegate(EnchantingTableUI ui);
        public static TabActivationDelegate TabActivation;
        public delegate float AudioVolumeLevelDelegate();
        public static AudioVolumeLevelDelegate AudioVolumeLevel;

        private int _hiddenFrames;

        public void Awake()
        {
            instance = this;
        }

        public void Start()
        {
            Localization.instance.Localize(transform);

            GameObject uiSFX = GameObject.Find("sfx_gui_button");
            if (uiSFX)
            {
                Audio.outputAudioMixerGroup =
                    uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;
                Audio.volume = AudioVolumeLevel();
            }

            instance.SetupTabs();

            AugaFixup(this);
        }

        private static void CreateUI(EnchantingTable source)
        {
            if (StoreGui.instance == null)
            {
                return;
            }

            Transform inGameGui = StoreGui.instance.transform.parent;
            int siblingIndex = StoreGui.instance.transform.GetSiblingIndex() + 1;

            // Call to arms compatibility: increase scroll sensitivity
            foreach (ScrollRect scrollRect in source.EnchantingUIPrefab.GetComponentsInChildren<ScrollRect>(true))
            {
                scrollRect.scrollSensitivity = 800f;
            }

            GameObject enchantingUI = Instantiate(source.EnchantingUIPrefab, inGameGui);
            enchantingUI.transform.SetSiblingIndex(siblingIndex);

            // TODO: Reduce duplicate code, mock this inside unity in the future
            Transform existingBackground = StoreGui.instance.m_rootPanel.transform.Find("border (1)");
            Transform panel = enchantingUI.transform.Find("Panel");
            if (existingBackground != null & panel != null)
            {
                Image image = existingBackground.GetComponent<Image>();
                panel.GetComponent<Image>().material = image.material;
            }
        }

        private void SetupTabs()
        {
            foreach(TabHandler.Tab tab in TabHandler.m_tabs)
            {
                tab.m_onClick.AddListener(PlayTabSelectSFX);

                FeatureStatus fs = tab.m_button.gameObject.GetComponent<FeatureStatus>();
                if (fs != null)
                {
                    fs.Refresh();
                }
            }

            TabActivation(this);
        }

        public static void Show(EnchantingTable source)
        {
            if (instance == null)
            {
                CreateUI(source);
            }

            if (instance == null)
            {
                Debug.LogError("Enchanting Table UI not setup properly!");
                return;
            }

            instance.SourceTable = source;
            instance.Root.SetActive(true);
            instance.Scrim.SetActive(true);
            instance.SourceTable.Refresh();

            foreach (EnchantingTableUIPanelBase panel in instance.Panels)
            {
                panel.DeselectAll();
            }
        }

        public static void Hide()
        {
            if (instance == null)
            {
                return;
            }

            instance.Root.SetActive(false);
            instance.Scrim.SetActive(false);
            instance.SourceTable = null;
        }

        public static bool IsVisible()
        {
            return instance != null && ((instance._hiddenFrames <= 2) ||
                (instance.Root != null && instance.Root.activeSelf));
        }

        public static bool IsInTextInput()
        {
            if (!IsVisible())
            {
                return false;
            }

            InputField[] textFields = instance.Root.GetComponentsInChildren<InputField>(false);
            foreach (InputField inputField in textFields)
            {
                if (inputField.isFocused)
                {
                    return true;
                }
            }

            return false;
        }

        public void Update()
        {
            if (Root == null)
            {
                return;
            }

            if (!Root.activeSelf)
            {
                _hiddenFrames++;
                return;
            }

            _hiddenFrames = 0;

            bool disallowClose = (Chat.instance != null && Chat.instance.HasFocus()) ||
                Console.IsVisible() || Menu.IsVisible() || (TextViewer.instance != null &&
                TextViewer.instance.IsVisible()) || Player.m_localPlayer.InCutscene();

            if (disallowClose)
            {
                return;
            }

            bool gotCloseInput = ZInput.GetButtonDown("JoyButtonB") ||
                ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetKeyDown(KeyCode.Tab);

            if (gotCloseInput)
            {
                ZInput.ResetButtonStatus("JoyButtonB");
                ZInput.ResetButtonStatus("JoyJump");

                bool panelCapturedInput = false;
                foreach (EnchantingTableUIPanelBase panel in Panels)
                {
                    if (panel.isActiveAndEnabled && panel.CanCancel())
                    {
                        panel.Cancel();
                        panelCapturedInput = true;
                        break;
                    }
                }

                if (!panelCapturedInput)
                {
                    Hide();
                }
            }
        }

        public static void UpdateTabActivation()
        {
            TabActivation(instance);
        }

        public static void UpdateUpgradeActivation()
        {
            TabActivation(instance);
        }

        public void LockTabs()
        {
            TabScrim.SetActive(true);
        }

        public void UnlockTabs()
        {
            TabScrim.SetActive(false);
        }

        public void PlayTabSelectSFX()
        {
            Audio.PlayOneShot(TabClickSFX, Audio.volume);
        }

        public void PlayEnchantBonusSFX()
        {
            Audio.PlayOneShot(EnchantBonusSFX, Audio.volume);
        }
    }
}
