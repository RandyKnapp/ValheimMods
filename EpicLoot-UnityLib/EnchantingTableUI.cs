using System.Linq;
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
        public SacrificeUI Sacrifice;
        public ConvertUI Conversion;
        public EnchantUI Enchant;

        [Header("Audio")]
        public AudioSource Audio;
        public AudioClip TabClickSFX;

        public static EnchantingTableUI instance { get; set; }

        private int _hiddenFrames;

        public void Awake()
        {
            instance = this;
            Localization.instance.Localize(transform);

            var uiSFX = GameObject.Find("sfx_gui_button");
            if (uiSFX)
                Audio.outputAudioMixerGroup = uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;

            foreach (var button in TabHandler.m_tabs.Select(x => x.m_button))
            {
                button.onClick.AddListener(PlayTabSelectSFX);
            }
        }

        public static void Show(GameObject enchantingUiPrefab)
        {
            if (instance == null)
            {
                if (StoreGui.instance != null)
                {
                    var inGameGui = StoreGui.instance.transform.parent;
                    var siblingIndex = StoreGui.instance.transform.GetSiblingIndex() + 1;
                    var enchantingUI = Instantiate(enchantingUiPrefab, inGameGui);
                    enchantingUI.transform.SetSiblingIndex(siblingIndex);
                }
            }

            if (instance == null)
                return;

            instance.Root.SetActive(true);
            instance.Scrim.SetActive(true);

            instance.Sacrifice.DeselectAll();
            instance.Conversion.DeselectAll();
        }

        public static void Hide()
        {
            if (instance == null)
                return;

            instance.Root.SetActive(false);
            instance.Scrim.SetActive(false);
        }

        public static bool IsVisible()
        {
            return instance != null && instance.Root != null && instance.Root.activeSelf && instance._hiddenFrames <= 2;
        }

        public static bool IsInTextInput()
        {
            if (!IsVisible())
                return false;

            var textFields = instance.Root.GetComponentsInChildren<InputField>(false);
            foreach (var inputField in textFields)
            {
                if (inputField.isFocused)
                    return true;
            }

            return false;
        }

        public void Update()
        {
            if (Root == null)
                return;

            if (!Root.activeSelf)
            {
                _hiddenFrames++;
                return;
            }

            _hiddenFrames = 0;

            var disallowClose = (Chat.instance != null && Chat.instance.HasFocus()) || Console.IsVisible() || Menu.IsVisible() || (TextViewer.instance != null && TextViewer.instance.IsVisible()) || Player.m_localPlayer.InCutscene();
            if (disallowClose)
                return;

            var gotCloseInput = ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab);
            if (gotCloseInput)
            {
                ZInput.ResetButtonStatus("JoyButtonB");

                if (Sacrifice.isActiveAndEnabled && Sacrifice.CanCancel())
                {
                    Sacrifice.Cancel();
                }
                else if (Conversion.isActiveAndEnabled && Conversion.CanCancel())
                {
                    Conversion.Cancel();
                }
                else if (Enchant.isActiveAndEnabled && Enchant.CanCancel())
                {
                    Enchant.Cancel();
                }
                else
                {
                    Hide();
                }
            }
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
    }
}
