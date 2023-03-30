using System.Linq;
using EpicLoot_UnityLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot
{
    public static class EpicLootAuga
    {
        public static Button ReplaceButton(Button button, bool icon = false, bool keepListeners = false)
        {
            var newButton = Auga.API.MediumButton_Create(button.transform.parent, button.name, string.Empty);
            return ReplaceButtonInternal(newButton, button, icon, keepListeners);
        }

        public static Button ReplaceButtonFancy(Button button, bool icon = false, bool keepListeners = false)
        {
            var newButton = Auga.API.FancyButton_Create(button.transform.parent, button.name, string.Empty);
            return ReplaceButtonInternal(newButton, button, icon, keepListeners);
        }

        private static Button ReplaceButtonInternal(Button newButton, Button button, bool icon, bool keepListeners)
        {
            if (icon)
            {
                Object.Destroy(newButton.GetComponentInChildren<Text>().gameObject);
                var newIcon = Object.Instantiate(button.transform.Find("Icon"), newButton.transform);
                newIcon.name = "Icon";
            }
            else
            {
                var newLabel = button.GetComponentInChildren<Text>().text;
                newButton.GetComponentInChildren<Text>().text = newLabel;
            }
            var oldRt = (RectTransform)button.transform;
            var rt = (RectTransform)newButton.transform;
            rt.anchorMin = oldRt.anchorMin;
            rt.anchorMax = oldRt.anchorMax;
            rt.pivot = oldRt.pivot;
            rt.anchoredPosition = oldRt.anchoredPosition;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, oldRt.rect.width);

            if (keepListeners)
            {
                newButton.onClick = button.onClick;
                button.onClick = new Button.ButtonClickedEvent();
            }

            var uiGamePad = button.GetComponent<UIGamePad>();
            if (uiGamePad != null && uiGamePad.m_hint != null)
            {
                uiGamePad.m_hint.transform.SetParent(newButton.transform);
                var newUiGamePad = newButton.gameObject.AddComponent<UIGamePad>();
                newUiGamePad.m_keyCode = uiGamePad.m_keyCode;
                newUiGamePad.m_zinputKey = uiGamePad.m_zinputKey;
                newUiGamePad.m_hint = uiGamePad.m_hint;
                newUiGamePad.m_blockingElements = uiGamePad.m_blockingElements.ToList();
            }

            Object.DestroyImmediate(button.gameObject);
            return newButton;
        }

        public static void MakeSimpleTooltip(GameObject obj)
        {
            Auga.API.Tooltip_MakeSimpleTooltip(obj);
        }

        public static void ReplaceBackground(GameObject obj, bool withCornerDecoration)
        {
            var image = obj.GetComponent<Image>();
            Object.Destroy(image);
            var backgroundPanel = Auga.API.Panel_Create(obj.transform, Vector2.one, "AugaBackground", withCornerDecoration);
            var rt = (RectTransform)backgroundPanel.transform;
            rt.SetSiblingIndex(0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(40, 40);
        }

        public static void FixItemBG(GameObject obj)
        {
            var magicBG = obj.transform.Find("MagicBG");
            if (magicBG != null)
            {
                var image = magicBG.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = EpicLoot.GetMagicItemBgSprite();
                }
            }

            var replacedBG = false;
            var itemBG = obj.transform.Find("ItemBG");
            var baseImage = itemBG != null ? itemBG.GetComponent<Image>() : null;
            if (baseImage == null)
                baseImage = obj.GetComponent<Image>();

            if (baseImage != null)
            {
                baseImage.sprite = Auga.API.GetItemBackgroundSprite();
                baseImage.color = new Color(0, 0, 0, 0.5f);
                replacedBG = true;
            }

            if (!replacedBG)
            {
                var icon = obj.transform.Find("Icon");
                if (icon != null)
                {
                    var iconBG = Object.Instantiate(icon, icon.parent);
                    iconBG.SetSiblingIndex(2);
                    var image = iconBG.GetComponent<Image>();
                    image.sprite = Auga.API.GetItemBackgroundSprite();
                    image.color = new Color(0, 0, 0, 0.5f);
                }
            }
        }

        public static void FixListElementColors(GameObject obj)
        {
            var selected = obj.transform.Find("Selected");
            if (selected != null)
            {
                var image = selected.GetComponent<Image>();
                if (image != null)
                {
                    ColorUtility.TryParseHtmlString(Auga.API.Blue, out var color);
                    image.color = color;
                }
            }

            var background = obj.transform.Find("Background");
            if (background != null)
            {
                var image = background.GetComponent<Image>();
                if (image != null)
                {
                    image.color = new Color(0, 0, 0, 0.5f);
                }
            }

            var button = obj.GetComponent<Button>();
            if (button != null)
            {
                button.colors = ColorBlock.defaultColorBlock;
            }
        }

        public static void FixFonts(GameObject obj)
        {
            var texts = obj.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                if (text.font.name == "Norsebold")
                {
                    text.font = Auga.API.GetBoldFont();
                    ColorUtility.TryParseHtmlString(Auga.API.Brown1, out var color);
                    text.color = color;
                    text.text = Localization.instance.Localize(text.text).ToUpperInvariant();
                }
                else
                {
                    text.font = Auga.API.GetSemiBoldFont();
                }

                if (text.name == "Count" || text.name == "RewardLabel" || text.name.EndsWith("Count"))
                {
                    ColorUtility.TryParseHtmlString(Auga.API.BrightGold, out var color);
                    text.color = color;
                }

                text.text = text.text.Replace("<color=yellow>", $"<color={Auga.API.BrightGold}>");
            }
        }

        public static Button ReplaceVerticalLargeTab(Button button)
        {
            var newButtonPrefab = EpicLoot.LoadAsset<GameObject>("EnchantingTabAuga");
            var newButton = Object.Instantiate(newButtonPrefab, button.transform.parent);
            FixFonts(newButton);
            var siblingIndex = button.transform.GetSiblingIndex();

            var oldRt = (RectTransform)button.transform;
            var rt = (RectTransform)newButton.transform;
            rt.anchorMin = oldRt.anchorMin;
            rt.anchorMax = oldRt.anchorMax;
            rt.pivot = oldRt.pivot;
            rt.anchoredPosition = oldRt.anchoredPosition;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, oldRt.rect.width);

            newButton.transform.Find("Text").GetComponent<Text>().text = button.transform.Find("Text").GetComponent<Text>().text;
            newButton.transform.Find("Image").GetComponent<Image>().sprite = button.transform.Find("Image").GetComponent<Image>().sprite;
            var featureStatus = newButton.GetComponent<FeatureStatus>();
            var otherFeatureStatus = button.GetComponent<FeatureStatus>();
            if (otherFeatureStatus == null && featureStatus != null)
            {
                Object.DestroyImmediate(featureStatus);
            }

            if (featureStatus != null && otherFeatureStatus != null)
            {
                featureStatus.Feature = otherFeatureStatus.Feature;
                featureStatus.Refresh();
            }

            Object.DestroyImmediate(button.gameObject);
            newButton.transform.SetSiblingIndex(siblingIndex);
            return newButton.GetComponent<Button>();
        }

        public static void FixupScrollbar(Scrollbar scrollbar)
        {
            Object.Destroy(scrollbar.GetComponent<Image>());
            scrollbar.colors = ColorBlock.defaultColorBlock;
            var scrollbarImage = scrollbar.handleRect.GetComponent<Image>();
            if (ColorUtility.TryParseHtmlString("#8B7C6A", out var brown))
                scrollbarImage.color = new Color(brown.r, brown.g, brown.b, 1.0f);
        }
    }
}
