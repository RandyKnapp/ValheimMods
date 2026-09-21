using System;
using System.Collections.Generic;
using EpicLoot.CraftingV2;
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

        private int _hiddenFrames;
        private GameObject[] _gamepadHintContainers = Array.Empty<GameObject>();
        private bool _gamepadHintsShown = true;
        private bool _correctingTab;

        public void Awake()
        {
            instance = this;
        }

        public void Start()
        {
            Localization.instance.Localize(transform);

            EnchantingUIController.SetupUIAudioSource(Audio);

            instance.SetupTabs();
            instance.CollectGamepadHints();

            EnchantingUIAugaFixup.AugaFixup(this);
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

            // Overrides the prefab's bindings, which put the tab bar on the bumpers that
            // MultiSelectListFocusController also claims, and left Tab bound here while Update uses it to
            // close the window.
            TabHandler.m_gamepadInput = true;
            TabHandler.m_gamepadNavigateLeft = "JoyLTrigger";
            TabHandler.m_gamepadNavigateRight = "JoyRTrigger";
            TabHandler.m_tabKeyInput = false;

            if (TabScrim != null && !TabHandler.m_blockingElements.Contains(TabScrim))
            {
                TabHandler.m_blockingElements.Add(TabScrim);
            }

            TabHandler.ActiveTabChanged += OnActiveTabChanged;

            SortTabsIntoVisualOrder();
            RefreshTabActivation();
        }

        // The prefab lists Upgrade before Rune while the tab bar shows Rune before Upgrade, and TabHandler
        // cycles by list index, so the triggers would visit the last two tabs in the wrong order.
        private void SortTabsIntoVisualOrder()
        {
            TabHandler.Tab selected = TabHandler.m_selected >= 0 && TabHandler.m_selected < TabHandler.m_tabs.Count
                ? TabHandler.m_tabs[TabHandler.m_selected]
                : null;

            TabHandler.m_tabs.Sort((a, b) => GetTabSiblingIndex(a).CompareTo(GetTabSiblingIndex(b)));

            if (selected != null)
            {
                TabHandler.m_selected = TabHandler.m_tabs.IndexOf(selected);
            }
        }

        private static int GetTabSiblingIndex(TabHandler.Tab tab)
        {
            return tab.m_button != null ? tab.m_button.transform.GetSiblingIndex() : int.MaxValue;
        }

        // Also called directly, not just from the event: TabHandler picks its default tab in Start, which
        // may run before the subscription above.
        private void RefreshTabActivation()
        {
            EnchantingUIController.TabActivation(this);

            if (TabHandler != null)
            {
                OnActiveTabChanged(TabHandler.GetActiveTab());
            }
        }

        // TabHandler's own cycling skips tabs whose button is null, not ones config has deactivated, so it
        // can land on a hidden tab and show its empty page.
        private void OnActiveTabChanged(int index)
        {
            if (_correctingTab || TabHandler == null || index < 0 ||
                index >= TabHandler.m_tabs.Count || IsTabAvailable(index))
            {
                return;
            }

            // Still readable here: this fires in the same frame as the press that moved the tab.
            int direction = ZInput.GetButtonDown(TabHandler.m_gamepadNavigateLeft) ? -1 : 1;

            int tabCount = TabHandler.m_tabs.Count;
            for (int offset = 1; offset < tabCount; ++offset)
            {
                int candidate = ((index + offset * direction) % tabCount + tabCount) % tabCount;
                if (!IsTabAvailable(candidate))
                {
                    continue;
                }

                _correctingTab = true;
                try
                {
                    TabHandler.SetActiveTab(candidate);
                }
                finally
                {
                    _correctingTab = false;
                }

                return;
            }
        }

        private bool IsTabAvailable(int index)
        {
            TabHandler.Tab tab = TabHandler.m_tabs[index];
            return tab.m_button != null && tab.m_button.gameObject.activeSelf;
        }

        // Matches the prefab's hint containers by name; they ship always-on with nothing to toggle them.
        private void CollectGamepadHints()
        {
            List<GameObject> hints = new List<GameObject>();
            foreach (Transform child in Root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "GamepadHints" || child.name == "TabGamepadHints")
                {
                    hints.Add(child.gameObject);
                }
            }

            _gamepadHintContainers = hints.ToArray();
            RefreshGamepadHints();
        }

        private void RefreshGamepadHints()
        {
            bool show = ZInput.IsGamepadActive();
            if (show == _gamepadHintsShown)
            {
                return;
            }

            _gamepadHintsShown = show;
            foreach (GameObject hint in _gamepadHintContainers)
            {
                if (hint != null)
                {
                    hint.SetActive(show);
                }
            }
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

            // On first creation the prefab is instantiated already-active, so every panel and tab
            // FeatureStatus ran OnEnable before SourceTable was assigned and skipped subscribing to
            // the table's change events. Force an inactive->active transition so their OnEnable runs
            // again with SourceTable set (this is what the close/reopen path already does).
            if (instance.Root.activeSelf)
            {
                instance.Root.SetActive(false);
            }
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

            RefreshGamepadHints();

            // The player died (or logged out) with the table open: close it -- nothing else does,
            // and every dereference below would NRE each frame over the death screen.
            if (Player.m_localPlayer == null)
            {
                Hide();
                return;
            }

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
            instance?.RefreshTabActivation();
        }

        public static void UpdateUpgradeActivation()
        {
            UpdateTabActivation();
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
            Audio.PlayOneShot(TabClickSFX);
        }

        public void PlayEnchantBonusSFX()
        {
            Audio.PlayOneShot(EnchantBonusSFX);
        }
    }
}
