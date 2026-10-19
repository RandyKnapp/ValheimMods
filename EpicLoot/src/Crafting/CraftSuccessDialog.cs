using Common;
using EpicLoot.CraftingV2;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    public class CraftSuccessDialog : MonoBehaviour
    {
        public RectTransform Frame;
        public TMP_Text NameText;
        public TMP_Text Description;
        public Image Icon;
        public Image MagicBG;

        private AudioSource _audioSource;
        private bool _isAugaPanel;

        [UsedImplicitly]
        public void Awake()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }

            EnchantingUIController.SetupUIAudioSource(_audioSource);
        }

        [UsedImplicitly]
        public void Update()
        {
            if (ZInput.IsGamepadActive())
            {
                GamepadScroll.ApplyRightStickY(GetComponentInChildren<Scrollbar>());
            }

            if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") ||
                (ZInput.GetButtonDown("JoyButtonY") || ZInput.GetKeyDown(KeyCode.Escape)) ||
                ZInput.GetButtonDown("JoyButtonA"))
            {
                Close();
            }
        }

        public void Show(ItemDrop.ItemData item)
        {
            gameObject.SetActive(true);

            var rarityColor = item.IsMagic() ? item.GetRarityColor() : Color.white;

            if (MagicBG != null)
            {
                MagicBG.enabled = item.IsMagic();
                MagicBG.color = rarityColor;
            }

            // An Auga results panel (CreateAuga) is an Auga tooltip: this fills in its stats and, through
            // AugaTooltip, the magic effects. The fields below are then only the ones it left set.
            if (EpicLoot.HasAuga && _isAugaPanel)
            {
                Auga.API.ComplexTooltip_SetItem(gameObject, item);
            }

            if (NameText != null)
            {
                NameText.enabled = true;
                NameText.text = Localization.instance.Localize(item.GetDecoratedName());
            }

            if (Description != null)
            {
                Description.enabled = true;
                Description.text = Localization.instance.Localize(item.GetTooltip());
                MagicTextShimmer.Ensure(Description);
            }

            if (Icon != null)
            {
                Icon.enabled = true;
                Icon.sprite = item.GetIcon();
            }

            if (item.IsMagic())
            {
                _audioSource.PlayOneShot(EpicLoot.GetMagicItemDropSFX(item.GetRarity()));
            }

            RarityRevealFlare.Play(Frame, MagicEffectRarity.GetBestTier(item.GetMagicItem()));
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
        public static ScrollRect ConvertToScrollingDescription(TMP_Text recipeDesc, Transform parent)
        {
            var contentSizeFitter = recipeDesc.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            recipeDesc.fontSize = 18;
            recipeDesc.rectTransform.anchorMin = new Vector2(0, 1);
            recipeDesc.rectTransform.anchorMax = new Vector2(1, 1); // pin top, stretch horiz
            recipeDesc.rectTransform.pivot = new Vector2(0, 1);
            recipeDesc.textWrappingMode = TextWrappingModes.Normal;
            recipeDesc.overflowMode = TextOverflowModes.Overflow;
            recipeDesc.rectTransform.anchoredPosition = new Vector2(4, 4);
            recipeDesc.raycastTarget = false;

            var scrollRectGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollRectGO.transform.SetParent(parent, false);
            scrollRectGO.transform.SetSiblingIndex(0);
            var rt = (RectTransform)scrollRectGO.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(11, -74);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 330);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300);
            scrollRectGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.2f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollRectGO.transform, false);
            var vrt = (RectTransform)viewport.transform;
            vrt.anchorMin = new Vector2(0, 0);
            vrt.anchorMax = new Vector2(1, 1);
            vrt.sizeDelta = new Vector2(0, 0);
            recipeDesc.transform.SetParent(vrt, false);

            var scrollRect = scrollRectGO.GetComponent<ScrollRect>();
            scrollRect.viewport = vrt;
            scrollRect.content = recipeDesc.rectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            scrollRect.scrollSensitivity = 100;
            scrollRect.inertia = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.onValueChanged.RemoveAllListeners();

            var newScrollbar = Object.Instantiate(InventoryGui.instance.m_recipeListScroll, scrollRectGO.transform);
            newScrollbar.size = 0.4f;
            scrollRect.onValueChanged.AddListener((_) => newScrollbar.size = 0.4f);
            scrollRect.verticalScrollbar = newScrollbar;

            return scrollRect;
        }
        /// <summary>
        /// The dialog on one of Auga's crafting results panels, centred on <paramref name="parent"/>. Null when
        /// Auga cannot make one (its crafting panel does not exist yet); callers fall back to <see cref="Create"/>.
        /// </summary>
        public static CraftSuccessDialog CreateAuga(Transform parent)
        {
            GameObject resultsPanel = Auga.API.Workbench_CreateNewResultsPanel();
            if (resultsPanel == null)
            {
                return null;
            }

            resultsPanel.SetActive(false);
            resultsPanel.transform.SetParent(parent, false);
            resultsPanel.name = "CraftingSuccessDialog";

            var dialog = resultsPanel.AddComponent<CraftSuccessDialog>();
            dialog._isAugaPanel = true;
            dialog.NameText = resultsPanel.transform.Find("Topic")?.GetComponent<TMP_Text>();
            dialog.Frame = (RectTransform)resultsPanel.transform;
            dialog.Frame.pivot = dialog.Frame.anchorMin = dialog.Frame.anchorMax = new Vector2(0.5f, 0.5f);
            dialog.Frame.anchoredPosition = Vector2.zero;

            var closeButton = resultsPanel.transform.Find("CloseButton")?.GetComponent<Button>();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(dialog.Close);
            }

            return dialog;
        }

        /// <summary>Auga's results panel when Auga is installed, the vanilla-built dialog otherwise.</summary>
        public static CraftSuccessDialog CreateForCurrentUI(Transform parent)
        {
            return (EpicLoot.HasAuga ? CreateAuga(parent) : null) ?? Create(parent);
        }

        public static CraftSuccessDialog Create(Transform parent)
        {
            var inventoryGui = InventoryGui.instance;
            var newDialog = Instantiate(inventoryGui.m_variantDialog, parent);
            var dialog = newDialog.gameObject.AddComponent<CraftSuccessDialog>();
            //Destroy(newDialog);
            dialog.gameObject.name = "CraftingSuccessDialog";

            dialog.Frame = dialog.gameObject.transform.Find("VariantFrame").gameObject.RectTransform();
            dialog.Frame.gameObject.name = "Frame";
            for (var i = 1; i < dialog.Frame.childCount; ++i)
            {
                var child = dialog.Frame.GetChild(i);
                //Destroy(child.gameObject);
            }
            dialog.Frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 380);
            dialog.Frame.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 550);
            dialog.Frame.anchoredPosition += new Vector2(20, -270);

            dialog.MagicBG = Instantiate(inventoryGui.m_recipeIcon, dialog.Frame);
            dialog.MagicBG.name = "MagicItemBG";
            dialog.MagicBG.sprite = EpicLoot.GetMagicItemBgSprite();
            dialog.MagicBG.color = Color.white;

            dialog.NameText = Instantiate(inventoryGui.m_recipeName, dialog.Frame);
            dialog.Description = Instantiate(inventoryGui.m_recipeDecription, dialog.Frame);
            dialog.Description.rectTransform.anchoredPosition += new Vector2(0, -110);
            dialog.Description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 460);
            dialog.Icon = Instantiate(inventoryGui.m_recipeIcon, dialog.Frame);

            var scrollview = ConvertToScrollingDescription(dialog.Description, dialog.Frame);
            var svrt = (RectTransform)scrollview.transform;
            svrt.SetSiblingIndex(1);
            svrt.anchorMin = new Vector2(0, 0);
            svrt.anchorMax = new Vector2(1, 1);
            svrt.pivot = new Vector2(0.5f, 0.5f);
            svrt.offsetMin = new Vector2(10, 20);
            svrt.offsetMax = new Vector2(-10, -80);

            var closeButton = dialog.gameObject.GetComponentInChildren<Button>();
            closeButton.onClick = new Button.ButtonClickedEvent();
            closeButton.onClick.AddListener(dialog.Close);
            Transform transform1;
            (transform1 = closeButton.transform).SetAsLastSibling();
            var cbrt = (RectTransform)transform1;
            cbrt.anchoredPosition += new Vector2(0, -110);
            
            closeButton.gameObject.SetActive(true);

            return dialog;
        }
    }
}
