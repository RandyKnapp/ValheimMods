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
            if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") || (ZInput.GetButtonDown("JoyButtonY") || Input.GetKeyDown(KeyCode.Escape)) || ZInput.GetButtonDown("Use"))
            {
                OnClose();
            }
        }

        public void Show(ItemDrop.ItemData item)
        {
            gameObject.SetActive(true);

            var rarityColor = item.GetRarityColor();
            
            MagicBG.enabled = item.IsMagic();
            MagicBG.color = rarityColor;

            NameText.text = Localization.instance.Localize(item.GetDecoratedName());
            Description.text = Localization.instance.Localize(item.GetTooltip());
            Icon.sprite = item.GetIcon();

            _audioSource.PlayOneShot(EpicLoot.GetMagicItemDropSFX(item.GetRarity()));
        }

        public void OnClose()
        {
            gameObject.SetActive(false);
        }

        public static CraftSuccessDialog Create(Transform parent)
        {
            var inventoryGui = InventoryGui.instance;
            var newDialog = Instantiate(inventoryGui.m_variantDialog, parent);
            var dialog = newDialog.gameObject.AddComponent<CraftSuccessDialog>();
            Destroy(newDialog);
            dialog.gameObject.name = "CraftingSuccessDialog";

            dialog.Frame = dialog.gameObject.transform.Find("VariantFrame").gameObject.RectTransform();
            dialog.Frame.gameObject.name = "Frame";
            for (var i = 1; i < dialog.Frame.childCount; ++i)
            {
                var child = dialog.Frame.GetChild(i);
                Destroy(child.gameObject);
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

            var closeButton = dialog.gameObject.GetComponentInChildren<Button>();
            closeButton.onClick = new Button.ButtonClickedEvent();
            closeButton.onClick.AddListener(dialog.OnClose);
            closeButton.transform.SetAsLastSibling();

            return dialog;
        }
    }
}
