using Common;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    public class CraftSuccessDialog : MonoBehaviour
    {
        public RectTransform Frame;
        public Text NameText;
        public Text Description;
        public Image Icon;
        public Image MagicBG;

        private AudioSource _audioSource;

        [UsedImplicitly]
        public void Awake()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        [UsedImplicitly]
        public void Update()
        {
            var scrollBar = GetComponentInChildren<Scrollbar>();
            if (scrollBar != null && ZInput.IsGamepadActive())
            {
                var rightStickAxis = ZInput.GetJoyRightStickY();
                if (Mathf.Abs(rightStickAxis) > 0.5f)
                    scrollBar.value = Mathf.Clamp01(scrollBar.value + rightStickAxis * -0.1f);
            }

            if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") || (ZInput.GetButtonDown("JoyButtonY") || Input.GetKeyDown(KeyCode.Escape)) || ZInput.GetButtonDown("JoyButtonA"))
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

            if (EpicLoot.HasAuga)
            {
                Auga.API.ComplexTooltip_SetItem(gameObject, item);
            }

            if (NameText != null)
            {
                NameText.text = Localization.instance.Localize(item.GetDecoratedName());
            }

            if (Description != null)
            {
                Description.text = Localization.instance.Localize(item.GetTooltip());
            }

            if (Icon != null)
            {
                Icon.sprite = item.GetIcon();
            }

            if (item.IsMagic())
            {
                _audioSource.PlayOneShot(EpicLoot.GetMagicItemDropSFX(item.GetRarity()));
            }
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
        public static ScrollRect ConvertToScrollingDescription(Text recipeDesc, Transform parent)
        {
            var contentSizeFitter = recipeDesc.gameObject.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            recipeDesc.resizeTextForBestFit = false;
            recipeDesc.fontSize = 18;
            recipeDesc.rectTransform.anchorMin = new Vector2(0, 1);
            recipeDesc.rectTransform.anchorMax = new Vector2(1, 1); // pin top, stretch horiz
            recipeDesc.rectTransform.pivot = new Vector2(0, 1);
            recipeDesc.horizontalOverflow = HorizontalWrapMode.Wrap;
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
            scrollRect.scrollSensitivity = 30;
            scrollRect.inertia = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.onValueChanged.RemoveAllListeners();

            var newScrollbar = Object.Instantiate(InventoryGui.instance.m_recipeListScroll, scrollRectGO.transform);
            newScrollbar.size = 0.4f;
            scrollRect.onValueChanged.AddListener((_) => newScrollbar.size = 0.4f);
            scrollRect.verticalScrollbar = newScrollbar;

            return scrollRect;
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
