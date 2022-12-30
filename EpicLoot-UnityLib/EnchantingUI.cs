using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class EnchantingUI : MonoBehaviour
    {
        public GameObject Root;
        public GameObject Scrim;
        public SacrificeUI Sacrifice;

        public static EnchantingUI instance { get; set; }

        private int _hiddenFrames;

        public void Awake()
        {
            instance = this;
            Localization.instance.Localize(transform);
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

            var gotCloseInput = ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape);
            if (gotCloseInput)
            {
                ZInput.ResetButtonStatus("JoyButtonB");

                if (Sacrifice.isActiveAndEnabled && Sacrifice.CanCancel())
                {
                    Sacrifice.Cancel();
                }
                else
                {
                    Hide();
                }
            }
        }
    }
}
