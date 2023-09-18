using System.Collections.Generic;
using Common;
using EpicLoot_UnityLib;
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
            var augmentChoices = 3;
            if (useEnchantingUpgrades && EnchantingTableUI.instance && EnchantingTableUI.instance.SourceTable)
            {
                var featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Augment);
                if (!float.IsNaN(featureValues.Item1))
                    augmentChoices = (int)featureValues.Item1 + 1;
            }

            var inventoryGui = InventoryGui.instance;
            AugmentChoiceDialog choiceDialog;
            if (EpicLoot.HasAuga)
            {
                var resultDialog = Auga.API.Workbench_CreateNewResultsPanel();
                resultDialog.SetActive(false);

                choiceDialog = resultDialog.AddComponent<AugmentChoiceDialog>();

                var icon = choiceDialog.transform.Find("InventoryElement/icon").GetComponent<Image>();
                choiceDialog.MagicBG = Object.Instantiate(icon, icon.transform.parent);
                choiceDialog.MagicBG.name = "MagicItemBG";
                choiceDialog.MagicBG.sprite = EpicLoot.GetMagicItemBgSprite();
                choiceDialog.MagicBG.color = Color.white;
                choiceDialog.MagicBG.rectTransform.anchorMin = new Vector2(0, 0);
                choiceDialog.MagicBG.rectTransform.anchorMax = new Vector2(1, 1);
                choiceDialog.MagicBG.rectTransform.sizeDelta = new Vector2(0, 0);
                choiceDialog.MagicBG.rectTransform.anchoredPosition = new Vector2(0, 0);

                choiceDialog.NameText = choiceDialog.transform.Find("Topic").GetComponent<TMP_Text>();

                var closeButton = choiceDialog.gameObject.GetComponentInChildren<Button>();
                Object.Destroy(closeButton.gameObject);

                var tooltipHeight = 360;
                var buttonStart = -220;
                if (augmentChoices > 3)
                {
                    var extra = augmentChoices - 3;
                    tooltipHeight -= extra * 40;
                    buttonStart += extra * 40;
                }

                var tooltip = (RectTransform)choiceDialog.transform.Find("TooltipScrollContainer");
                tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight);
                var scrollbar = (RectTransform)choiceDialog.transform.Find("ScrollBar");
                scrollbar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, tooltipHeight);

                for (var i = 0; i < augmentChoices; i++)
                {
                    var button = Auga.API.MediumButton_Create(resultDialog.transform, $"AugmentButton{i}", string.Empty);
                    Auga.API.Button_SetTextColors(button, Color.white, Color.white, Color.white, Color.white, Color.white, Color.white);
                    button.navigation = new Navigation { mode = Navigation.Mode.None };

                    var focus = Object.Instantiate(EpicLoot.LoadAsset<GameObject>("ButtonFocusAuga"), button.transform);
                    focus.SetActive(false);
                    focus.name = "ButtonFocus";

                    var rt = (RectTransform)button.transform;
                    rt.anchoredPosition = new Vector2(0, buttonStart - (i * 40));
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 295);
                    choiceDialog.EffectChoiceButtons.Add(button);
                }
            }
            else
            {
                var height = 550.0f;
                if (augmentChoices > 3)
                {
                    var extra = augmentChoices - 3;
                    height += extra * 45;
                }
                choiceDialog = CreateDialog<AugmentChoiceDialog>(inventoryGui, "AugmentChoiceDialog", height);

                var background = choiceDialog.gameObject.transform.Find("Frame").gameObject.RectTransform();
                choiceDialog.MagicBG = Object.Instantiate(inventoryGui.m_recipeIcon, background);
                choiceDialog.MagicBG.name = "MagicItemBG";
                choiceDialog.MagicBG.sprite = EpicLoot.GetMagicItemBgSprite();
                choiceDialog.MagicBG.color = Color.white;

                choiceDialog.NameText = Object.Instantiate(inventoryGui.m_recipeName, background);
                choiceDialog.Description = Object.Instantiate(inventoryGui.m_recipeDecription, background);
                choiceDialog.Description.rectTransform.anchoredPosition += new Vector2(0, -47);
                choiceDialog.Description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 340);
                choiceDialog.Icon = Object.Instantiate(inventoryGui.m_recipeIcon, background);

                var scrollview = CraftSuccessDialog.ConvertToScrollingDescription(choiceDialog.Description, background);
                var svrt = (RectTransform)scrollview.transform;
                svrt.SetSiblingIndex(1);
                svrt.anchorMin = new Vector2(0, 0);
                svrt.anchorMax = new Vector2(1, 1);
                svrt.pivot = new Vector2(0.5f, 0.5f);
                svrt.anchoredPosition = new Vector2(0, 50);
                svrt.sizeDelta = new Vector2(-20, -300);
                svrt.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 74, 300);

                var closeButton = choiceDialog.gameObject.GetComponentInChildren<Button>();
                Object.Destroy(closeButton.gameObject);

                for (var i = 0; i < augmentChoices; i++)
                {
                    var button = Object.Instantiate(inventoryGui.m_craftButton, background);
                    button.interactable = true;
                    Object.Destroy(button.GetComponent<UITooltip>());
                    var uiGamePad = button.GetComponent<UIGamePad>();
                    if (uiGamePad)
                    {
                        Object.Destroy(uiGamePad.m_hint);
                        Object.Destroy(uiGamePad);
                    }
                    var focus = Object.Instantiate(EpicLoot.LoadAsset<GameObject>("ButtonFocus"), button.transform);
                    focus.SetActive(false);
                    focus.name = "ButtonFocus";
                    var rt = button.gameObject.RectTransform();
                    rt.anchorMin = new Vector2(0.5f, 0);
                    rt.anchorMax = new Vector2(0.5f, 0);
                    rt.anchoredPosition = new Vector2(0, 55 + ((augmentChoices - 1 - i) * 45));
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
                    choiceDialog.EffectChoiceButtons.Add(button);
                }
            }

            return choiceDialog;
        }

        public static T CreateDialog<T>(InventoryGui inventoryGui, string name, float height = 550) where T : Component
        {
            var newDialog = Object.Instantiate(inventoryGui.m_variantDialog, inventoryGui.m_variantDialog.transform.parent);
            T newDialogT = newDialog.gameObject.AddComponent<T>();
            Object.Destroy(newDialog);
            newDialogT.gameObject.name = name;

            var background = newDialogT.gameObject.transform.Find("VariantFrame").gameObject.RectTransform();
            background.gameObject.name = "Frame";
            for (int i = 1; i < background.transform.childCount; ++i)
            {
                var child = background.transform.GetChild(i);
                Object.Destroy(child.gameObject);
            }
            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 380);
            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            background.anchoredPosition += new Vector2(20, -270);
            
            return newDialogT;
        }

        public static List<MagicItemEffectDefinition> GetAvailableAugments(AugmentRecipe recipe, ItemDrop.ItemData item, MagicItem magicItem, ItemRarity rarity)
        {
            var valuelessEffect = false;
            if (recipe.EffectIndex >= 0 && recipe.EffectIndex < magicItem.Effects.Count)
            {
                var currentEffectDef = MagicItemEffectDefinitions.Get(magicItem.Effects[recipe.EffectIndex].EffectType);
                valuelessEffect = currentEffectDef.GetValuesForRarity(rarity) == null;
            }

            return MagicItemEffectDefinitions.GetAvailableEffects(item.Extended(), item.GetMagicItem(), valuelessEffect ? -1 : recipe.EffectIndex);
        }

        public static string GetAugmentSelectorText(MagicItem magicItem, int i, IReadOnlyList<MagicItemEffect> augmentableEffects, ItemRarity rarity)
        {
            var pip = EpicLoot.GetMagicEffectPip(magicItem.IsEffectAugmented(i));
            bool free = EnchantCostsHelper.EffectIsDeprecated(augmentableEffects[i].EffectType);
            return $"{pip} {Localization.instance.Localize(MagicItem.GetEffectText(augmentableEffects[i], rarity, true))}{(free ? " [<color=yellow>*FREE</color>]" : "")}";
        }

        public static List<KeyValuePair<ItemDrop, int>> GetAugmentCosts(ItemDrop.ItemData item, int recipeEffectIndex)
        {
            var rarity = item.GetRarity();

            var augmentCostDef = EnchantCostsHelper.GetAugmentCost(item, rarity, recipeEffectIndex);
            if (augmentCostDef == null)
            {
                return null;
            }

            var costList = new List<KeyValuePair<ItemDrop, int>>();

            foreach (var itemAmountConfig in augmentCostDef)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Tried to add unknown item ({itemAmountConfig.Item}) to augment cost for item ({item.m_shared.m_name})");
                    continue;
                }

                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    EpicLoot.LogWarning($"Tried to add item without ItemDrop ({itemAmountConfig.Item}) to augment cost for item ({item.m_shared.m_name})");
                    continue;
                }

                costList.Add(new KeyValuePair<ItemDrop, int>(itemDrop, itemAmountConfig.Amount));
            }

            return costList;
        }
    }
}
