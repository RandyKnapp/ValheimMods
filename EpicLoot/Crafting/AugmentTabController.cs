using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using ExtendedItemDataFramework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public class AugmentTabController : TabController
    {
        public class AugmentRecipe
        {
            public ItemDrop.ItemData FromItem;
            public int EffectIndex = -1;
        }
        
        public readonly List<AugmentRecipe> Recipes = new List<AugmentRecipe>();
        public readonly List<Toggle> EffectSelectors = new List<Toggle>();
        public AugmentChoiceDialog ChoiceDialog;
        public AugmentsAvailableDialog AvailableAugmentsDialog;
        public Button AvailableAugmentsButton;

        public AugmentTabController() : base(CraftingTabType.Augment, true)
        {
        }

        public override string GetTabButtonId() => "Augment";
        public override string GetTabButtonText() => Localization.instance.Localize("$mod_epicloot_augment").ToUpperInvariant();

        public override void TryInitialize(InventoryGui inventoryGui, int tabIndex, Action<TabController> onTabPressed)
        {
            base.TryInitialize(inventoryGui, tabIndex, onTabPressed);

            if (ChoiceDialog == null)
            {
                if (EpicLoot.HasAuga)
                {
                    var resultDialog = Auga.API.Workbench_CreateNewResultsPanel();
                    resultDialog.SetActive(false);

                    ChoiceDialog = resultDialog.AddComponent<AugmentChoiceDialog>();

                    var icon = ChoiceDialog.transform.Find("InventoryElement/icon").GetComponent<Image>();
                    ChoiceDialog.MagicBG = Object.Instantiate(icon, icon.transform.parent);
                    ChoiceDialog.MagicBG.name = "MagicItemBG";
                    ChoiceDialog.MagicBG.sprite = EpicLoot.GetMagicItemBgSprite();
                    ChoiceDialog.MagicBG.color = Color.white;
                    ChoiceDialog.MagicBG.rectTransform.anchorMin = new Vector2(0, 0);
                    ChoiceDialog.MagicBG.rectTransform.anchorMax = new Vector2(1, 1);
                    ChoiceDialog.MagicBG.rectTransform.sizeDelta = new Vector2(0, 0);
                    ChoiceDialog.MagicBG.rectTransform.anchoredPosition = new Vector2(0, 0);

                    ChoiceDialog.NameText = ChoiceDialog.transform.Find("Topic").GetComponent<Text>();

                    var closeButton = ChoiceDialog.gameObject.GetComponentInChildren<Button>();
                    Object.Destroy(closeButton.gameObject);

                    var tooltip = (RectTransform)ChoiceDialog.transform.Find("TooltipScrollContainer");
                    tooltip.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 360);
                    var scrollbar = (RectTransform)ChoiceDialog.transform.Find("ScrollBar");
                    scrollbar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 360);

                    for (var i = 0; i < 3; i++)
                    {
                        var button = Auga.API.MediumButton_Create(resultDialog.transform, $"AugmentButton{i}", string.Empty);
                        Auga.API.Button_SetTextColors(button, Color.white, Color.white, Color.white, Color.white, Color.white, Color.white);
                        button.navigation = new Navigation { mode = Navigation.Mode.None };
                        var rt = (RectTransform)button.transform;
                        rt.anchoredPosition = new Vector2(0, -220 - (i * 40));
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 295);
                        ChoiceDialog.EffectChoiceButtons.Add(button);
                    }
                }
                else
                {
                    ChoiceDialog = CreateDialog<AugmentChoiceDialog>(inventoryGui, "AugmentChoiceDialog");

                    var background = ChoiceDialog.gameObject.transform.Find("Frame").gameObject.RectTransform();
                    ChoiceDialog.MagicBG = Object.Instantiate(inventoryGui.m_recipeIcon, background);
                    ChoiceDialog.MagicBG.name = "MagicItemBG";
                    ChoiceDialog.MagicBG.sprite = EpicLoot.GetMagicItemBgSprite();
                    ChoiceDialog.MagicBG.color = Color.white;

                    ChoiceDialog.NameText = Object.Instantiate(inventoryGui.m_recipeName, background);
                    ChoiceDialog.Description = Object.Instantiate(inventoryGui.m_recipeDecription, background);
                    ChoiceDialog.Description.rectTransform.anchoredPosition += new Vector2(0, -47);
                    ChoiceDialog.Description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 340);
                    ChoiceDialog.Icon = Object.Instantiate(inventoryGui.m_recipeIcon, background);

                    var closeButton = ChoiceDialog.gameObject.GetComponentInChildren<Button>();
                    Object.Destroy(closeButton.gameObject);

                    for (var i = 0; i < 3; i++)
                    {
                        var button = Object.Instantiate(inventoryGui.m_craftButton, background);
                        var rt = button.gameObject.RectTransform();
                        rt.anchoredPosition = new Vector2(0, -155 - (i * 45));
                        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 40);
                        ChoiceDialog.EffectChoiceButtons.Add(button);
                    }
                }
            }

            if (!EpicLoot.HasAuga)
            {
                if (AvailableAugmentsDialog == null)
                {
                    AvailableAugmentsDialog = CreateDialog<AugmentsAvailableDialog>(inventoryGui, "AvailableAugmentsDialog");

                    var background = AvailableAugmentsDialog.gameObject.transform.Find("Frame").gameObject.RectTransform();
                    AvailableAugmentsDialog.MagicBG = Object.Instantiate(inventoryGui.m_recipeIcon, background);
                    AvailableAugmentsDialog.MagicBG.name = "MagicItemBG";
                    AvailableAugmentsDialog.MagicBG.sprite = EpicLoot.GetMagicItemBgSprite();
                    AvailableAugmentsDialog.MagicBG.color = Color.white;

                    AvailableAugmentsDialog.NameText = Object.Instantiate(inventoryGui.m_recipeName, background);
                    AvailableAugmentsDialog.Description = Object.Instantiate(inventoryGui.m_recipeDecription, background);
                    AvailableAugmentsDialog.Description.rectTransform.anchoredPosition += new Vector2(0, -110);
                    AvailableAugmentsDialog.Description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 460);
                    AvailableAugmentsDialog.Description.resizeTextForBestFit = true;
                    AvailableAugmentsDialog.Icon = Object.Instantiate(inventoryGui.m_recipeIcon, background);

                    var closeButton = AvailableAugmentsDialog.gameObject.GetComponentInChildren<Button>();
                    closeButton.onClick = new Button.ButtonClickedEvent();
                    closeButton.onClick.AddListener(AvailableAugmentsDialog.OnClose);
                    closeButton.transform.SetAsLastSibling();
                }

                if (AvailableAugmentsButton == null)
                {
                    AvailableAugmentsButton = Object.Instantiate(inventoryGui.m_variantButton, inventoryGui.m_variantButton.transform.parent, true);
                    AvailableAugmentsButton.gameObject.name = "AvailableAugmentsButton";
                    AvailableAugmentsButton.gameObject.SetActive(false);
                    AvailableAugmentsButton.onClick = new Button.ButtonClickedEvent();
                    AvailableAugmentsButton.onClick.AddListener(ShowAvailableAugmentsDialog);

                    var text = AvailableAugmentsButton.GetComponentInChildren<Text>();
                    text.text = Localization.instance.Localize("$mod_epicloot_augment_availableeffects");

                    var rt = AvailableAugmentsButton.gameObject.RectTransform();
                    rt.anchoredPosition += new Vector2(50, 0);
                    rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 150);
                }
            }
        }

        public static T CreateDialog<T>(InventoryGui inventoryGui, string name) where T : Component
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
            background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 550);
            background.anchoredPosition += new Vector2(20, -270);
            
            return newDialogT;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if (!EpicLoot.HasAuga)
            {
                foreach (var toggle in EffectSelectors)
                {
                    Object.Destroy(toggle);
                }
            }
            EffectSelectors.Clear();
            Object.Destroy(ChoiceDialog);
            ChoiceDialog = null;
        }

        public override void SetActive(bool active)
        {
            if (!active)
            {
                Recipes.Clear();
                if (EpicLoot.HasAuga)
                {
                    EffectSelectors.Clear();
                }
                else
                {
                    foreach (var toggle in EffectSelectors)
                    {
                        toggle.gameObject.SetActive(false);
                    }
                }
            }

            if (EpicLoot.HasAuga)
            {
                if (active)
                {
                    var augmentsAvailableText = Auga.API.CustomVariantPanel_Enable("$mod_epicloot_augment_availableeffects", (showing) =>
                    {
                        if (showing)
                        {
                            ShowAvailableAugmentsDialog();
                        }
                    });

                    AvailableAugmentsDialog = augmentsAvailableText.gameObject.GetComponent<AugmentsAvailableDialog>();
                    if (AvailableAugmentsDialog == null)
                    {
                        AvailableAugmentsDialog = augmentsAvailableText.gameObject.AddComponent<AugmentsAvailableDialog>();
                    }
                    AvailableAugmentsDialog.Description = augmentsAvailableText;
                }
                else
                {
                    Auga.API.CustomVariantPanel_Disable();
                }
            }
            else
            {
                AvailableAugmentsButton.gameObject.SetActive(active);
                if (AvailableAugmentsDialog.gameObject.activeSelf && !active)
                {
                    AvailableAugmentsDialog.OnClose();
                }
                if (ChoiceDialog.gameObject.activeSelf && !active)
                {
                    ChoiceDialog.OnClose();
                }
            }

            base.SetActive(active);
        }

        public override bool DisallowInventoryHiding()
        {
            if ((ChoiceDialog != null && ChoiceDialog.gameObject.activeInHierarchy) || (AvailableAugmentsDialog != null && AvailableAugmentsDialog.gameObject.activeInHierarchy))
            {
                return true;
            }
            return base.DisallowInventoryHiding();
        }

        public override void UpdateCraftingPanel(InventoryGui __instance, bool focusView)
        {
            UpdateRecipeList(__instance);
            if (Recipes.Count > 0)
            {
                if (SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
                {
                    SetRecipe(__instance, SelectedRecipe, focusView);
                }
                else
                {
                    SelectedRecipe = 0;
                    SetRecipe(__instance, 0, focusView);
                }
            }
            else
            {
                SetRecipe(__instance, -1, focusView);
            }
        }

        public override bool IsAllowedAtThisStation(CraftingStation station)
        {
            if (station == null)
            {
                return false;
            }

            var isArtisan = station.m_name == "$piece_artisanstation";
            var isForgeWithEnchanter = station.m_name == "$piece_forge" && station.m_attachedExtensions.Find(x => x.name.StartsWith("piece_augmenter"));
            return isArtisan || isForgeWithEnchanter;
        }

        public override void UpdateRecipe(InventoryGui __instance, Player player, float dt, Image bgImage)
        {
            __instance.m_craftButton.GetComponentInChildren<Text>().text = Localization.instance.Localize("$mod_epicloot_augment");

            if (SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
            {
                var recipe = Recipes[SelectedRecipe];
                var itemData = recipe.FromItem;
                var rarity = itemData.GetRarity();
                var rarityColorARGB = EpicLoot.GetRarityColorARGB(rarity);

                __instance.m_itemCraftType.gameObject.SetActive(false);
                __instance.m_variantButton.gameObject.SetActive(false);

                __instance.m_minStationLevelIcon.gameObject.SetActive(false);

                var canCraft = CraftingTabs.HaveRequirementsHelper(player, GetRecipeRequirementArray(recipe), 1) || player.NoCostCheat();
                var hasSelectedEffect = recipe.EffectIndex >= 0;
                var hasAnyAvailableEnchants = GetAvailableAugments(recipe, recipe.FromItem, recipe.FromItem.GetMagicItem(), recipe.FromItem.GetRarity()).Count > 0;
                __instance.m_craftButton.interactable = canCraft && hasSelectedEffect && hasAnyAvailableEnchants;
                __instance.m_craftButton.GetComponent<UITooltip>().m_text = 
                    hasSelectedEffect ? 
                        (canCraft ? 
                            (hasAnyAvailableEnchants ?
                                ""
                                : Localization.instance.Localize("$mod_epicloot_augment_noeffects")) 
                            : Localization.instance.Localize("$msg_missingrequirement")) 
                        : Localization.instance.Localize("$mod_epicloot_augment_selecteffect");

                SetupRequirementList(__instance, player, recipe, canCraft);

                if (EpicLoot.HasAuga)
                {
                    AugaTabData.ItemInfoGO.SetActive(true);
                    AugaTabData.RequirementsPanelGO.SetActive(true);

                    EpicLoot.AugaTooltipNoTextBoxes = true;
                    Auga.API.ComplexTooltip_SetItemNoTextBoxes(AugaTabData.ItemInfoGO, itemData, itemData.m_quality, itemData.m_variant);
                    Auga.API.ComplexTooltip_SetTopic(AugaTabData.ItemInfoGO, Localization.instance.Localize(itemData.GetDecoratedName()));
                    Auga.API.ComplexTooltip_SetDescription(AugaTabData.ItemInfoGO, Localization.instance.Localize("$mod_epicloot_augment_explain"));
                    EpicLoot.AugaTooltipNoTextBoxes = false;
                    __instance.m_itemCraftType.text = "";
                }
                else
                {
                    __instance.m_recipeIcon.enabled = true;
                    __instance.m_recipeIcon.sprite = itemData.GetIcon();

                    __instance.m_recipeName.enabled = true;
                    __instance.m_recipeName.text = Localization.instance.Localize(itemData.GetDecoratedName());

                    __instance.m_recipeDecription.enabled = true;
                    __instance.m_recipeDecription.text = Localization.instance.Localize("$mod_epicloot_augment_explain");
                }

                bgImage.color = rarityColorARGB;
                bgImage.enabled = true;

                GenerateAugmentSelectors(recipe, __instance);
            }
            else
            {
                if (EpicLoot.HasAuga)
                {
                    AugaTabData.ItemInfoGO.SetActive(false);
                    AugaTabData.RequirementsPanelGO.SetActive(false);
                }

                bgImage.enabled = false;
                __instance.m_itemCraftType.gameObject.SetActive(false);
                __instance.m_variantButton.gameObject.SetActive(false);
                __instance.m_minStationLevelIcon.gameObject.SetActive(false);
                __instance.m_recipeIcon.enabled = false;
                __instance.m_recipeName.enabled = false;
                __instance.m_recipeDecription.enabled = false;

                if (!EpicLoot.HasAuga)
                {
                    foreach (var toggle in EffectSelectors)
                    {
                        toggle.gameObject.SetActive(false);
                    }
                }

                foreach (var req in __instance.m_recipeRequirementList)
                {
                    InventoryGui.HideRequirement(req.transform);
                }

                __instance.m_craftButton.interactable = false;
            }
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

        private void GenerateAugmentSelectors(AugmentRecipe recipe, InventoryGui inventoryGui)
        {
            const float spacing = 34;
            var checkboxPrefab = Menu.instance.m_settingsPrefab.GetComponent<Settings>().m_invertMouse;
            var magicItem = recipe.FromItem.GetMagicItem();
            var rarity = recipe.FromItem.GetRarity();
            var startOffset = new Vector2(-330, -165);

            var augmentableEffects = magicItem.Effects;
            var effectCount = augmentableEffects.Count;

            if (EpicLoot.HasAuga)
            {
                EffectSelectors.Clear();
                Auga.API.ComplexTooltip_ClearTextBoxes(AugaTabData.ItemInfoGO);
            }

            for (var i = 0; i < Mathf.Max(effectCount, EffectSelectors.Count); ++i)
            {
                if (i < effectCount && i >= EffectSelectors.Count)
                {
                    Toggle selector;
                    if (EpicLoot.HasAuga)
                    {
                        var checkboxTextBox = Auga.API.ComplexTooltip_AddCheckBoxTextBox(AugaTabData.ItemInfoGO);
                        selector = checkboxTextBox.GetComponentInChildren<Toggle>(true);
                    }
                    else
                    {
                        selector = Object.Instantiate(checkboxPrefab, inventoryGui.m_variantButton.transform.parent, false);
                        selector.gameObject.name = $"EffectSelector{i}";
                        var rt = selector.gameObject.RectTransform();
                        rt.anchoredPosition = startOffset + new Vector2(0, i * -spacing);
                        var t = selector.GetComponentInChildren<Text>();
                        t.font = inventoryGui.m_recipeDecription.font;
                        t.resizeTextMaxSize = 18;
                        t.resizeTextMinSize = 10;
                        t.rectTransform.anchoredPosition += new Vector2(300, 0);
                        t.alignment = TextAnchor.MiddleLeft;
                        t.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
                        t.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, spacing);
                    }

                    var index = i;
                    selector.onValueChanged.AddListener((selected) => OnSelectorValueChanged(index, selected));

                    EffectSelectors.Add(selector);

                    SetSelectorValues(recipe, i, magicItem, rarity);
                }
                else if (i >= effectCount && i < EffectSelectors.Count)
                {
                    if (EpicLoot.HasAuga)
                    {
                        EffectSelectors[i].transform.parent.gameObject.SetActive(false);
                    }
                    else
                    {
                        EffectSelectors[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    SetSelectorValues(recipe, i, magicItem, rarity);
                }
            }
        }

        private void SetSelectorValues(AugmentRecipe recipe, int i, MagicItem magicItem, ItemRarity rarity)
        {
            var selector = EffectSelectors[i];
            if (selector == null)
                return;

            var augmentableEffects = magicItem.Effects;

            if (EpicLoot.HasAuga)
            {
                selector.transform.parent.gameObject.SetActive(true);
            }
            else
            {
                selector.gameObject.SetActive(true);
            }

            selector.isOn = recipe.EffectIndex == i;
            var t = selector.GetComponentInChildren<Text>();
            t.text = GetAugmentSelectorText(magicItem, i, augmentableEffects, rarity);
            t.color = EpicLoot.GetRarityColorARGB(rarity);
        }

        private static string GetAugmentSelectorText(MagicItem magicItem, int i, IReadOnlyList<MagicItemEffect> augmentableEffects, ItemRarity rarity)
        {
            var pip = EpicLoot.GetMagicEffectPip(magicItem.IsEffectAugmented(i));
            bool free = EnchantCostsHelper.EffectIsDeprecated(augmentableEffects[i].EffectType);
            return $"{pip} {Localization.instance.Localize(MagicItem.GetEffectText(augmentableEffects[i], rarity, true))}{(free ? " [FREE]" : "")}";
        }

        private void OnSelectorValueChanged(int index, bool selected)
        {
            var recipe = SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count ? Recipes[SelectedRecipe] : null;

            if (recipe != null && selected)
            {
                recipe.EffectIndex = index;
            }

            for (var i = 0; i < EffectSelectors.Count; i++)
            {
                var toggle = EffectSelectors[i];
                toggle.isOn = recipe != null && i == recipe.EffectIndex;
            }
        }

        private void ShowAvailableAugmentsDialog()
        {
            var recipe = SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count ? Recipes[SelectedRecipe] : null;
            if (recipe != null && AvailableAugmentsDialog != null)
            {
                AvailableAugmentsDialog.Show(recipe);
            }
        }

        public void SetupRequirementList(InventoryGui __instance, Player player, AugmentRecipe recipe, bool canCraft)
        {
            var requirementStates = new[] { Auga.RequirementWireState.Absent, Auga.RequirementWireState.Absent, Auga.RequirementWireState.Absent, Auga.RequirementWireState.Absent };
            
            var index = 0;
            var cost = GetRecipeCost(recipe);
            foreach (var product in cost)
            {
                if (SetupRequirement(__instance, __instance.m_recipeRequirementList[index].transform, product.Key, product.Value, player, true, out var haveMaterials))
                {
                    requirementStates[index] = haveMaterials ? Auga.RequirementWireState.Have : Auga.RequirementWireState.DontHave;
                    ++index;
                }
            }

            for (; index < __instance.m_recipeRequirementList.Length; ++index)
            {
                InventoryGui.HideRequirement(__instance.m_recipeRequirementList[index].transform);
            }

            if (EpicLoot.HasAuga)
            {
                Auga.API.RequirementsPanel_SetWires(AugaTabData.RequirementsPanelGO, requirementStates, canCraft);
            }
        }

        public override void OnCraftPressed(InventoryGui __instance)
        {
            if (SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
            {
                __instance.m_craftTimer = 0.0f;
                var currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
                if (currentCraftingStation != null)
                {
                    currentCraftingStation.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
                }
            }
        }

        public override void DoCrafting(InventoryGui __instance, Player player)
        {
            if (SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
            {
                var previouslySelectedRecipe = SelectedRecipe;
                var recipe = Recipes[SelectedRecipe];

                // Spend Resources
                if (!player.NoCostCheat())
                {
                    player.ConsumeResources(GetRecipeRequirementArray(recipe), 1);
                }

                UpdateRecipeList(__instance);
                OnSelectedRecipe(__instance, previouslySelectedRecipe);

                // Set as augmented
                var magicItem = recipe.FromItem.GetMagicItem();
                magicItem.SetEffectAsAugmented(recipe.EffectIndex);
                // Note: I do not know why I have to do this, but this is the only thing that causes this item to save correctly
                recipe.FromItem.Extended().RemoveComponent<MagicItemComponent>();
                recipe.FromItem.Extended().AddComponent<MagicItemComponent>().SetMagicItem(magicItem);

                ChoiceDialog.Show(recipe, OnAugmentComplete);
            }
        }

        private void OnAugmentComplete(AugmentRecipe recipe, MagicItemEffect newEffect)
        {
            if (recipe != null)
            {
                var magicItem = recipe.FromItem.GetMagicItem();

                if (magicItem.HasEffect(MagicEffectType.Indestructible))
                {
                    recipe.FromItem.m_shared.m_useDurability = recipe.FromItem.m_dropPrefab?.GetComponent<ItemDrop>().m_itemData.m_shared.m_useDurability ?? false;

                    if (recipe.FromItem.m_shared.m_useDurability)
                    {
                        recipe.FromItem.m_durability = recipe.FromItem.GetMaxDurability();
                    }
                }
                
                magicItem.ReplaceEffect(recipe.EffectIndex, newEffect);

                if (magicItem.Rarity == ItemRarity.Rare)
                {
                    magicItem.DisplayName = MagicItemNames.GetNameForItem(recipe.FromItem, magicItem);
                }

                // Note: I do not know why I have to do this, but this is the only thing that causes this item to save correctly
                recipe.FromItem.Extended().RemoveComponent<MagicItemComponent>();
                recipe.FromItem.Extended().AddComponent<MagicItemComponent>().SetMagicItem(magicItem);

                InventoryGui.instance?.UpdateCraftingPanel();

                var player = Player.m_localPlayer;
                if (player.GetCurrentCraftingStation() != null)
                {
                    player.GetCurrentCraftingStation().m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }

                OnSelectorValueChanged(recipe.EffectIndex, true);

                MagicItemEffects.Indestructible.MakeItemIndestructible(recipe.FromItem);

                Game.instance.GetPlayerProfile().m_playerStats.m_crafts++;
                Gogan.LogEvent("Game", "Augmented", recipe.FromItem.m_shared.m_name, 1);
                
                EquipmentEffectCache.Reset(player);
            }
        }

        public void UpdateRecipeList(InventoryGui __instance)
        {
            __instance.m_availableRecipes.Clear();
            foreach (var recipe in __instance.m_recipeList)
            {
                Object.Destroy(recipe);
            }
            __instance.m_recipeList.Clear();

            GenerateRecipes();
            for (var index = 0; index < Recipes.Count; index++)
            {
                var recipe = Recipes[index];
                AddRecipeToList(__instance, recipe, index);
            }

            __instance.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(__instance.m_recipeListBaseSize, __instance.m_recipeList.Count * __instance.m_recipeListSpace));
        }

        public void AddRecipeToList(InventoryGui __instance, AugmentRecipe recipe, int index)
        {
            var count = __instance.m_recipeList.Count;
            var element = Object.Instantiate(__instance.m_recipeElementPrefab, __instance.m_recipeListRoot);
            element.SetActive(true);
            element.RectTransform().anchoredPosition = new Vector2(0.0f, count * -__instance.m_recipeListSpace);

           //var canCraft = Player.m_localPlayer.HaveRequirements(recipe.GetRequirementArray(), false, 1);
            var item = recipe.FromItem;

            var image = element.transform.Find("icon").GetComponent<Image>();
            image.sprite = item.GetIcon();
            image.color = Color.white;

            var bgImage = Object.Instantiate(image, image.transform.parent, true);
            bgImage.name = "MagicItemBG";
            bgImage.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
            bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
            bgImage.color = EpicLoot.GetRarityColorARGB(item.GetRarity());

            var nameText = element.transform.Find("name").GetComponent<Text>();
            nameText.text = Localization.instance.Localize(item.GetDecoratedName());
            if (item.GetMagicItem() != null && item.GetMagicItem().HasBeenAugmented())
            {
                nameText.text += $" {EpicLoot.GetMagicEffectPip(true)}";
            }
            nameText.color = Color.white;

            var durability = element.transform.Find("Durability").GetComponent<GuiBar>();
            if (item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability())
            {
                durability.gameObject.SetActive(true);
                durability.SetValue(item.GetDurabilityPercentage());
            }
            else
            {
                durability.gameObject.SetActive(false);
            }

            var quality = element.transform.Find("QualityLevel").GetComponent<Text>();
            quality.gameObject.SetActive(true);
            quality.text = item.m_quality.ToString();

            element.GetComponent<Button>().onClick.AddListener(() => OnSelectedRecipe(__instance, index));
            __instance.m_recipeList.Add(element);
        }

        private void OnSelectedRecipe(InventoryGui __instance, int index)
        {
            if (SelectedRecipe != index)
            {
                __instance.OnCraftCancelPressed();
                SelectedRecipe = index;
                SetRecipe(__instance, SelectedRecipe, false);
                OnSelectorValueChanged(-1, false);
            }
        }

        public void SetRecipe(InventoryGui __instance, int index, bool center)
        {
            for (var i = 0; i < __instance.m_recipeList.Count; ++i)
            {
                var selected = i == index;
                __instance.m_recipeList[i].transform.Find("selected").gameObject.SetActive(selected);
            }

            if (center && index >= 0)
            {
                __instance.m_recipeEnsureVisible.CenterOnItem(__instance.m_recipeList[index].transform as RectTransform);
            }
        }

        public override void GenerateRecipes()
        {
            Recipes.Clear();
            if (Player.m_localPlayer != null)
            {
                foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    GenerateAugmentRecipeForItem(item);
                }
            }
        }

        private void GenerateAugmentRecipeForItem(ItemDrop.ItemData item)
        {
            if (item.CanBeAugmented())
            {
                var recipe = new AugmentRecipe { FromItem = item };
                Recipes.Add(recipe);
            }
        }

        public static List<KeyValuePair<ItemDrop, int>> GetRecipeCost(AugmentRecipe recipe)
        {
            return GetAugmentCosts(recipe.FromItem, recipe.EffectIndex);
        }

        public static Piece.Requirement[] GetRecipeRequirementArray(AugmentRecipe recipe)
        {
            var cost = GetRecipeCost(recipe);
            return cost.Select(x => new Piece.Requirement() { m_amount = x.Value, m_resItem = x.Key }).ToArray();
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
