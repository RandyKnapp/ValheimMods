using EpicLoot_UnityLib;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot
{
    // Restyling helpers for EpicLoot's own panels when Project Auga is installed (EpicLoot.HasAuga), built on
    // the Auga API (src/integrations/AugaAPI.cs). Auga 1.x is TextMeshPro only, while EpicLoot's bundle prefabs
    // still mix legacy Text (merchant, enchanting contents, message panels) with TMP (tabs, temper panel), so
    // every helper here copes with either kind of label.
    //
    // Every Replace* call destroys the original widget: use the widget it returns, never a reference taken
    // before the call. When Auga cannot build a replacement the original is left in place and returned.
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
            if (newButton == null)
            {
                return button;
            }

            var newLabel = newButton.GetComponentInChildren<TMP_Text>(true);
            if (icon)
            {
                if (newLabel != null)
                {
                    Object.Destroy(newLabel.gameObject);
                }

                var oldIcon = button.transform.Find("Icon");
                if (oldIcon != null)
                {
                    var newIcon = Object.Instantiate(oldIcon, newButton.transform);
                    newIcon.name = "Icon";
                }
            }
            else if (newLabel != null)
            {
                newLabel.text = GetLabel(button);
            }

            var oldRt = (RectTransform)button.transform;
            var rt = (RectTransform)newButton.transform;
            CopyPlacement(oldRt, rt);
            rt.SetSiblingIndex(oldRt.GetSiblingIndex());

            newButton.interactable = button.interactable;
            newButton.navigation = button.navigation;
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

            // Gamepad glyphs the panel code toggles itself ("Hint", "Hint-1") come along too.
            for (int i = button.transform.childCount - 1; i >= 0; --i)
            {
                var child = button.transform.GetChild(i);
                if (child.name.StartsWith("Hint", System.StringComparison.Ordinal))
                {
                    child.SetParent(newButton.transform);
                }
            }

            newButton.gameObject.SetActive(button.gameObject.activeSelf);
            Object.DestroyImmediate(button.gameObject);
            return newButton;
        }

        private static void CopyPlacement(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, from.rect.width);
        }

        /// <summary>The label of a button, whichever kind of text component carries it.</summary>
        public static string GetLabel(Component obj)
        {
            var tmp = obj.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                return tmp.text;
            }

            var text = obj.GetComponentInChildren<Text>(true);
            return text != null ? text.text : string.Empty;
        }

        /// <summary>Sets the label of a button, whichever kind of text component carries it.</summary>
        public static void SetLabel(Component obj, string label)
        {
            var tmp = obj.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = label;
                return;
            }

            var text = obj.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }

        public static void MakeSimpleTooltip(GameObject obj)
        {
            Auga.API.Tooltip_MakeSimpleTooltip(obj);
        }

        public static void ReplaceBackground(GameObject obj, bool withCornerDecoration)
        {
            var backgroundPanel = Auga.API.Panel_Create(obj.transform, Vector2.one, "AugaBackground", withCornerDecoration);
            if (backgroundPanel == null)
            {
                return;
            }

            // DestroyImmediate, not Destroy: callers look up the root Graphic afterwards (MerchantPanel makes
            // it the drag target) and must not find the one that is going away at the end of the frame.
            var image = obj.GetComponent<Image>();
            if (image != null)
            {
                Object.DestroyImmediate(image);
            }

            var rt = (RectTransform)backgroundPanel.transform;
            rt.SetSiblingIndex(0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
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

            var itemBackground = Auga.API.GetItemBackgroundSprite();
            if (itemBackground == null)
            {
                return;
            }

            var itemBG = obj.transform.Find("ItemBG");
            var baseImage = itemBG != null ? itemBG.GetComponent<Image>() : null;
            if (baseImage == null)
            {
                baseImage = obj.GetComponent<Image>();
            }

            if (baseImage != null)
            {
                baseImage.sprite = itemBackground;
                baseImage.color = new Color(0, 0, 0, 0.5f);
                return;
            }

            var icon = obj.transform.Find("Icon");
            if (icon != null)
            {
                var iconBG = Object.Instantiate(icon, icon.parent);
                iconBG.name = "AugaItemBG";
                iconBG.SetSiblingIndex(icon.GetSiblingIndex());
                var image = iconBG.GetComponent<Image>();
                image.sprite = itemBackground;
                image.color = new Color(0, 0, 0, 0.5f);
            }
        }

        public static void FixListElementColors(GameObject obj)
        {
            var selected = obj.transform.Find("Selected");
            if (selected != null)
            {
                var image = selected.GetComponent<Image>();
                if (image != null && ColorUtility.TryParseHtmlString(Auga.API.Blue, out var color))
                {
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

        /// <summary>
        /// Auga's fonts and text colours. Headers (the game's Norsebold) become Auga's bold face in all caps,
        /// body text its semi-bold face, counts gold. Legacy Text takes the fonts the Auga API hands out; TMP
        /// text takes the bold TMP asset of Auga's own buttons, since the API has no TMP fonts, and keeps its
        /// body font.
        /// </summary>
        public static void FixFonts(GameObject obj)
        {
            ColorUtility.TryParseHtmlString(Auga.API.Brown1, out var headerColor);
            ColorUtility.TryParseHtmlString(Auga.API.BrightGold, out var countColor);
            var goldTag = $"<color={Auga.API.BrightGold}>";

            foreach (var text in obj.GetComponentsInChildren<Text>(true))
            {
                if (IsHeaderFont(text.font != null ? text.font.name : null))
                {
                    var bold = Auga.API.GetBoldFont();
                    if (bold != null)
                    {
                        text.font = bold;
                    }

                    text.color = headerColor;
                    text.text = Localization.instance.Localize(text.text).ToUpperInvariant();
                }
                else
                {
                    var semiBold = Auga.API.GetSemiBoldFont();
                    if (semiBold != null)
                    {
                        text.font = semiBold;
                    }
                }

                if (IsCountLabel(text.name))
                {
                    text.color = countColor;
                }

                text.text = text.text.Replace("<color=yellow>", goldTag);
            }

            foreach (var text in obj.GetComponentsInChildren<TMP_Text>(true))
            {
                // A VanillaFont picks its text's font when its page first opens, so the enchanting table's page
                // titles keep the game's face. Only a page already open here would keep a swap: that is the Upgrade
                // page, and the bold face's taller line truncates its title to nothing.
                if (IsHeaderFont(text.font != null ? text.font.name : null) && text.GetComponent<VanillaFont>() == null)
                {
                    var bold = GetBoldTmpFont();
                    if (bold != null)
                    {
                        text.font = bold;
                    }

                    text.color = headerColor;
                    text.text = Localization.instance.Localize(text.text).ToUpperInvariant();
                }

                if (IsCountLabel(text.name))
                {
                    text.color = countColor;
                }

                text.text = text.text.Replace("<color=yellow>", goldTag);
            }
        }

        // Legacy "Norsebold", TMP "Valheim-Norsebold" and the like.
        private static bool IsHeaderFont(string fontName)
        {
            return fontName != null && fontName.IndexOf("Norsebold", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsCountLabel(string name)
        {
            return name == "Count" || name == "RewardLabel" || name.EndsWith("Count");
        }

        private static TMP_FontAsset _boldTmpFont;

        /// <summary>Source Sans Pro Bold as Auga's buttons carry it, which includes the game fonts Auga links in as fallbacks.</summary>
        private static TMP_FontAsset GetBoldTmpFont()
        {
            if (_boldTmpFont != null)
            {
                return _boldTmpFont;
            }

            var holder = new GameObject("EpicLootAugaFontProbe", typeof(RectTransform));
            holder.SetActive(false);
            var probe = Auga.API.SmallButton_Create(holder.transform, "FontProbe", string.Empty);
            var label = probe != null ? probe.GetComponentInChildren<TMP_Text>(true) : null;
            _boldTmpFont = label != null ? label.font : null;
            Object.Destroy(holder);
            return _boldTmpFont;
        }

        public static Button ReplaceVerticalLargeTab(Button button)
        {
            var newButtonPrefab = EpicLoot.LoadAsset<GameObject>("EnchantingTabAuga");
            if (newButtonPrefab == null)
            {
                return button;
            }

            var newButton = Object.Instantiate(newButtonPrefab, button.transform.parent);
            newButton.name = button.name;
            var siblingIndex = button.transform.GetSiblingIndex();

            var oldRt = (RectTransform)button.transform;
            var rt = (RectTransform)newButton.transform;
            CopyPlacement(oldRt, rt);

            newButton.transform.Find("Text").GetComponent<TMP_Text>().text = button.transform.Find("Text").GetComponent<TMP_Text>().text;
            newButton.transform.Find("Image").GetComponent<Image>().sprite = button.transform.Find("Image").GetComponent<Image>().sprite;
            FixFonts(newButton);

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

            // TabHandler hooks its buttons in its own Start, which may already have run: the event carries its
            // listener over. Config can have switched the tab off, and TabActivation finds tabs by name.
            var newTabButton = newButton.GetComponent<Button>();
            newTabButton.onClick = button.onClick;
            button.onClick = new Button.ButtonClickedEvent();
            newButton.SetActive(button.gameObject.activeSelf);

            Object.DestroyImmediate(button.gameObject);
            newButton.transform.SetSiblingIndex(siblingIndex);
            return newTabButton;
        }

        public static void FixupScrollbar(Scrollbar scrollbar)
        {
            var background = scrollbar.GetComponent<Image>();
            if (background != null)
            {
                Object.Destroy(background);
            }

            scrollbar.colors = ColorBlock.defaultColorBlock;
            var scrollbarImage = scrollbar.handleRect != null ? scrollbar.handleRect.GetComponent<Image>() : null;
            if (scrollbarImage != null && ColorUtility.TryParseHtmlString("#8B7C6A", out var brown))
            {
                scrollbarImage.color = new Color(brown.r, brown.g, brown.b, 1.0f);
            }
        }
    }
}
