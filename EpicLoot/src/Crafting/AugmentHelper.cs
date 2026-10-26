using Common;
using EpicLoot.Compendium;
using EpicLoot_UnityLib;
using Jotunn.Managers;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public class AugmentHelper
    {
        public class AugmentRecipe
        {
            public ItemDrop.ItemData FromItem;
            public int EffectIndex = -1;
        }

        public static AugmentChoiceDialog CreateAugmentChoiceDialog(bool useEnchantingUpgrades)
        {
            int augmentChoices = 3;
            if (useEnchantingUpgrades && EnchantingTableUI.instance && EnchantingTableUI.instance.SourceTable)
            {
                System.Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
                if (!float.IsNaN(featureValues.Item1))
                {
                    augmentChoices = (int)featureValues.Item1 + 1;
                }
            }

            InventoryGui inventoryGui = InventoryGui.instance;
            AugmentChoiceDialog choiceDialog = EpicLoot.HasAuga ? CreateAugaAugmentChoiceDialog(augmentChoices) : null;
            if (choiceDialog != null)
            {
                return choiceDialog;
            }

            float height = 550.0f;
            if (augmentChoices > 3)
            {
                int extra = augmentChoices - 3;
                height += extra * 45;
            }

            choiceDialog = CreateDialog<AugmentChoiceDialog>(inventoryGui, "AugmentChoiceDialog", height);

            RectTransform background = choiceDialog.gameObject.transform.Find("Frame").gameObject.RectTransform();
            choiceDialog.MagicBG = Object.Instantiate(inventoryGui.m_recipeIcon, background);
            choiceDialog.MagicBG.name = "MagicItemBG";
            choiceDialog.MagicBG.sprite = EpicLoot.GetMagicItemBgSprite();
            choiceDialog.MagicBG.color = Color.white;

            choiceDialog.NameText = Object.Instantiate(inventoryGui.m_recipeName, background);
            choiceDialog.Description = Object.Instantiate(inventoryGui.m_recipeDecription, background);
            choiceDialog.Description.rectTransform.anchoredPosition += new Vector2(0, -47);
            choiceDialog.Description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 340);
            choiceDialog.Icon = Object.Instantiate(inventoryGui.m_recipeIcon, background);

            ScrollRect scrollview = CraftSuccessDialog.ConvertToScrollingDescription(choiceDialog.Description, background);
            RectTransform svrt = (RectTransform)scrollview.transform;
            svrt.SetSiblingIndex(1);
            svrt.anchorMin = new Vector2(0, 0);
            svrt.anchorMax = new Vector2(1, 1);
            svrt.pivot = new Vector2(0.5f, 0.5f);
            svrt.anchoredPosition = new Vector2(0, 50);
            svrt.sizeDelta = new Vector2(-20, -300);
            svrt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 74, 300);

            Button closeButton = choiceDialog.gameObject.GetComponentInChildren<Button>();
            Object.Destroy(closeButton.gameObject);

            for (int i = 0; i < augmentChoices; i++)
            {
                Button button = GUIManager.Instance.CreateButton(
                    text: "Augment",
                    parent: background.transform,
                    anchorMin: new Vector2(0.5f, 0f),
                    anchorMax: new Vector2(0.5f, 0f),
                    position: new Vector2(0, 55 + ((augmentChoices - 1 - i) * 45)),
                    width: 300f,
                    height: 40f).GetComponent<Button>();
                button.interactable = true;
                ConvertLabelToTmp(button);
                GameObject focus = Object.Instantiate(EpicLoot.LoadAsset<GameObject>("ButtonFocus"), button.transform);
                focus.SetActive(false);
                focus.name = "ButtonFocus";
                RectTransform rt = button.gameObject.RectTransform();
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
                choiceDialog.EffectChoiceButtons.Add(button);
            }

            return choiceDialog;
        }

        /// <summary>
        /// The augment choice dialog on one of Auga's crafting results panels: the item as an Auga tooltip
        /// (AugmentChoiceDialog.Show fills it in, AugaTooltip adds the rarity background and effects) with the
        /// choices as Auga buttons below it. Null when Auga cannot make a results panel.
        /// </summary>
        private static AugmentChoiceDialog CreateAugaAugmentChoiceDialog(int augmentChoices)
        {
            GameObject resultsPanel = Auga.API.Workbench_CreateNewResultsPanel();
            if (resultsPanel == null)
            {
                return null;
            }

            resultsPanel.SetActive(false);
            resultsPanel.name = "AugmentChoiceDialog";
            if (EnchantingTableUI.instance != null)
            {
                resultsPanel.transform.SetParent(EnchantingTableUI.instance.transform, false);
            }

            AugmentChoiceDialog choiceDialog = resultsPanel.AddComponent<AugmentChoiceDialog>();
            choiceDialog.IsAugaPanel = true;
            choiceDialog.NameText = resultsPanel.transform.Find("Topic")?.GetComponent<TMPro.TMP_Text>();

            // The choices close the dialog; the panel's own close button would skip the choice callback.
            Transform closeButton = resultsPanel.transform.Find("CloseButton");
            if (closeButton != null)
            {
                Object.Destroy(closeButton.gameObject);
            }

            float tooltipHeight = 360;
            float buttonStart = -220;
            if (augmentChoices > 3)
            {
                int extra = augmentChoices - 3;
                tooltipHeight -= extra * 40;
                buttonStart += extra * 40;
            }

            if (resultsPanel.transform.Find("TooltipScrollContainer") is RectTransform tooltip)
            {
                tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight);
            }

            if (resultsPanel.transform.Find("ScrollBar") is RectTransform scrollbar)
            {
                scrollbar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight);
            }

            for (int i = 0; i < augmentChoices; i++)
            {
                Button button = Auga.API.MediumButton_Create(resultsPanel.transform, $"AugmentButton{i}", string.Empty);
                if (button == null)
                {
                    Object.Destroy(resultsPanel);
                    return null;
                }

                Auga.API.Button_SetTextColors(button, Color.white, Color.white, Color.white, Color.white, Color.white, Color.white);
                button.navigation = new Navigation { mode = Navigation.Mode.None };

                // AugmentChoiceDialog shows the gamepad focus through a "ButtonFocus" child; Auga's medium
                // button carries one of its own.
                if (button.transform.Find("ButtonFocus") == null)
                {
                    GameObject focus = Object.Instantiate(EpicLoot.LoadAsset<GameObject>("ButtonFocusAuga"), button.transform);
                    focus.SetActive(false);
                    focus.name = "ButtonFocus";
                }

                RectTransform rt = (RectTransform)button.transform;
                rt.anchoredPosition = new Vector2(0, buttonStart - (i * 40));
                rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 295);
                choiceDialog.EffectChoiceButtons.Add(button);
            }

            return choiceDialog;
        }
        
        private static void ConvertLabelToTmp(Button button)
        {
            GameObject labelObject = button.GetComponentInChildren<Text>(true).gameObject;
            foreach (BaseMeshEffect effect in labelObject.GetComponents<BaseMeshEffect>())
            {
                Object.DestroyImmediate(effect);
            }
            Object.DestroyImmediate(labelObject.GetComponent<Text>());

            TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
            MagicFontManager.Apply(label, MagicFontManager.TMP_FontOptions.AveriaSerifLibreOutline);
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            // A long effect wraps onto a second line and shrinks before its end is cut off.
            label.textWrappingMode = TextWrappingModes.Normal;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = 21;
            // The dialog is built hidden, so this label's Awake waits for the first Show, and TMP's Awake
            // loads its own defaults (18-72 pt auto-size, wrapping, raycast target) over every setting here
            // unless a font size has been set. Kept from those defaults: the font features (kerning).
            label.fontSize = 21;
            label.fontFeatures = new List<UnityEngine.TextCore.OTL_FeatureTag>(TMP_Settings.fontFeatures);
            label.margin = new Vector4(8, 2, 8, 2);
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            MagicTextShimmer.Ensure(label);
        }

        public static T CreateDialog<T>(InventoryGui inventoryGui, string name, float height = 550) where T : Component
        {
            VariantDialog newDialog = Object.Instantiate(inventoryGui.m_variantDialog, inventoryGui.m_variantDialog.transform.parent);
            T newDialogT = newDialog.gameObject.AddComponent<T>();
            Object.Destroy(newDialog);
            newDialogT.gameObject.name = name;

            RectTransform background = newDialogT.gameObject.transform.Find("VariantFrame").gameObject.RectTransform();
            background.gameObject.name = "Frame";
            for (int i = 1; i < background.transform.childCount; ++i)
            {
                Transform child = background.transform.GetChild(i);
                Object.Destroy(child.gameObject);
            }

            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 380);
            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            background.anchoredPosition += new Vector2(20, -270);
            
            return newDialogT;
        }

        public static List<MagicItemEffectDefinition> GetAvailableAugments(
            AugmentRecipe recipe, ItemDrop.ItemData item, MagicItem magicItem, ItemRarity rarity)
        {
            // The replaced effect is left out of the checks even when it has no value; GetAvailableEffects
            // only offers it back when it has a value to re-roll.
            return MagicItemEffectDefinitions.GetAvailableEffects(
                item.Extended(), item.GetMagicItem(), recipe.EffectIndex, checkaugment: true);
        }

        public static string GetAugmentSelectorText(MagicItem magicItem, int i,
            IReadOnlyList<MagicItemEffect> augmentableEffects, ItemRarity rarity)
        {
            string pip = EpicLoot.GetMagicEffectPip(magicItem.IsEffectAugmented(i));
            bool free = EnchantCostsHelper.EffectIsDeprecated(augmentableEffects[i].EffectType);

            // The item's own legendary ID: these are effects already on the item, so a unique shows the
            // range its legendary entry declares (if any) rather than the plain rarity table.
            string effectText = MagicEffectRarity.Decorate(augmentableEffects[i].EffectType,
                Localization.instance.Localize(MagicItem.GetEffectText(augmentableEffects[i], rarity, true, magicItem.LegendaryID)), true);

            return $"{pip} {effectText}" +
                $"{(free ? " [<color=yellow>*FREE</color>]" : "")}";
        }

        public static List<KeyValuePair<ItemDrop, int>> GetAugmentCosts(ItemDrop.ItemData item, int recipeEffectIndex)
        {
            List<KeyValuePair<ItemDrop, int>> costList = new List<KeyValuePair<ItemDrop, int>>();
            ItemRarity rarity = item.GetRarity();

            List<ItemAmountConfig> augmentCostDef = EnchantCostsHelper.GetAugmentCost(item, rarity, recipeEffectIndex);
            if (augmentCostDef == null)
            {
                return costList;
            }

            foreach (ItemAmountConfig itemAmountConfig in augmentCostDef)
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Tried to add unknown item ({itemAmountConfig.Item}) " +
                        $"to augment cost for item ({item.m_shared.m_name})");
                    continue;
                }

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    EpicLoot.LogWarning($"Tried to add item without ItemDrop ({itemAmountConfig.Item}) " +
                        $"to augment cost for item ({item.m_shared.m_name})");
                    continue;
                }

                costList.Add(new KeyValuePair<ItemDrop, int>(itemDrop, itemAmountConfig.Amount));
            }

            return costList;
        }
    }
}
