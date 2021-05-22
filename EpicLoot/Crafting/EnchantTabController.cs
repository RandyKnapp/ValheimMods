using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using ExtendedItemDataFramework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public class EnchantTabController : TabController
    {
        public class EnchantRecipe
        {
            public ItemDrop.ItemData FromItem;
        }
        
        public readonly List<EnchantRecipe> Recipes = new List<EnchantRecipe>();
        public ItemRarity SelectedRarity;
        public readonly List<Button> RarityButtons = new List<Button>();
        public CraftSuccessDialog SuccessDialog;

        public EnchantTabController() : base(CraftingTabType.Enchant, true)
        {
        }

        public override string GetTabButtonId() => "Enchant";
        public override string GetTabButtonText() => "ENCHANT";

        public override void TryInitialize(InventoryGui inventoryGui, int tabIndex, Action<TabController> onTabPressed)
        {
            base.TryInitialize(inventoryGui, tabIndex, onTabPressed);

            if (RarityButtons.Count == 0)
            {
                var index = 0;
                var startPos = new Vector2(60, -95);
                foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
                {
                    var rarityColor = EpicLoot.GetRarityColorARGB(rarity);

                    var rarityButton = Object.Instantiate(inventoryGui.m_variantButton, inventoryGui.m_variantButton.transform.parent, true);
                    rarityButton.gameObject.name = $"{rarity}EnchantButton";
                    rarityButton.gameObject.SetActive(false);
                    rarityButton.onClick = new Button.ButtonClickedEvent();
                    rarityButton.onClick.AddListener(() => OnSelectedRarity(rarity));
                    rarityButton.colors = new ColorBlock()
                    {
                        disabledColor = Color.white,
                        highlightedColor = Color.white,
                        pressedColor = Color.white,
                        normalColor = new Color(0.7f, 0.7f, 0.7f, 1)
                    };
                    rarityButton.spriteState = new SpriteState()
                    {
                        disabledSprite = rarityButton.spriteState.selectedSprite,
                        selectedSprite = rarityButton.spriteState.selectedSprite,
                        pressedSprite = rarityButton.spriteState.pressedSprite,
                        highlightedSprite = rarityButton.spriteState.highlightedSprite
                    };
                    var outlineGO = new GameObject("EnchantOutline", typeof(RectTransform), typeof(Image));
                    var outline = outlineGO.GetComponent<Image>();
                    outlineGO.transform.SetParent(rarityButton.transform, false);
                    outline.type = Image.Type.Sliced;
                    outline.sprite = EpicLoot.Assets.SmallButtonEnchantOverlay;
                    outline.rectTransform.anchorMin = new Vector2(0, 0);
                    outline.rectTransform.anchorMax = new Vector2(1, 1);
                    outline.rectTransform.anchoredPosition = new Vector2(0, 0);
                    outline.rectTransform.sizeDelta = new Vector2(0, 0);
                    outline.color = rarityColor;
                    outline.enabled = true;

                    var buttonTextColor = rarityButton.GetComponent<ButtonTextColor>();
                    buttonTextColor.m_defaultColor = rarityColor;
                    buttonTextColor.m_defaultColor.a = 0.7f;
                    buttonTextColor.m_disabledColor = rarityColor;
                    var text = rarityButton.GetComponentInChildren<Text>();
                    text.text = rarity.ToString();
                    text.color = rarityColor;
                    RarityButtons.Add(rarityButton);
                    var rt = rarityButton.gameObject.RectTransform();
                    rt.anchoredPosition = startPos + (index * new Vector2(rt.rect.width + 4, 0));
                    index++;
                }
            }

            if (SuccessDialog == null)
            {
                SuccessDialog = CraftSuccessDialog.Create(inventoryGui.m_variantDialog.transform.parent);
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var rarityButton in RarityButtons)
            {
                Object.Destroy(rarityButton);
            }
            RarityButtons.Clear();
            Object.Destroy(SuccessDialog);
            SuccessDialog = null;
        }

        public override void SetActive(bool active)
        {
            if (!active)
            {
                Recipes.Clear();
                OnSelectedRarity(ItemRarity.Magic);
                foreach (var rarityButton in RarityButtons)
                {
                    rarityButton.gameObject.SetActive(false);
                }
            }
            base.SetActive(active);
        }

        public override bool DisallowInventoryHiding()
        {
            if (SuccessDialog.gameObject.activeSelf)
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
            var isForgeWithEnchanter = station.m_name == "$piece_forge" && station.m_attachedExtensions.Find(x => x.name.StartsWith("piece_enchanter"));
            return isArtisan || isForgeWithEnchanter;
        }

        public void OnSelectedRarity(ItemRarity rarity)
        {
            SelectedRarity = rarity;
            var nextButtonIndex = (int) SelectedRarity + 1;
            if (nextButtonIndex >= RarityButtons.Count)
            {
                nextButtonIndex = 0;
            }

            for (var index = 0; index < RarityButtons.Count; index++)
            {
                var rarityButton = RarityButtons[index];
                var gp = rarityButton.GetComponent<UIGamePad>();
                gp.enabled = index == nextButtonIndex;
                gp.m_hint.SetActive(ZInput.IsGamepadActive() && index == RarityButtons.Count - 1);
            }
        }

        public override void UpdateRecipe(InventoryGui __instance, Player player, float dt, Image bgImage)
        {
            __instance.m_craftButton.GetComponentInChildren<Text>().text = "Enchant";

            if (SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
            {
                var recipe = Recipes[SelectedRecipe];
                var itemData = recipe.FromItem;
                var rarityColor = EpicLoot.GetRarityColor(SelectedRarity);
                var rarityColorARGB = EpicLoot.GetRarityColorARGB(SelectedRarity);

                for (var index = 0; index < RarityButtons.Count; index++)
                {
                    var rarityButton = RarityButtons[index];
                    var rarity = (ItemRarity) index;
                    var canEnchantRarity = CanEnchantRarity(player, rarity);
                    rarityButton.gameObject.SetActive(canEnchantRarity);
                    rarityButton.interactable = SelectedRarity != rarity;

                    var outline = rarityButton.transform.Find("EnchantOutline").GetComponent<Image>();
                    outline.enabled = !rarityButton.interactable;
                }

                __instance.m_recipeIcon.enabled = true;
                __instance.m_recipeIcon.sprite = itemData.GetIcon();

                __instance.m_recipeName.enabled = true;
                __instance.m_recipeName.text = Localization.instance.Localize(itemData.GetDecoratedName(rarityColor));

                __instance.m_recipeDecription.enabled = true;
                __instance.m_recipeDecription.text = Localization.instance.Localize(GenerateEnchantTooltip(recipe));

                bgImage.color = rarityColorARGB;
                bgImage.enabled = true;

                __instance.m_itemCraftType.gameObject.SetActive(false);
                __instance.m_variantButton.gameObject.SetActive(false);

                SetupRequirementList(__instance, player, recipe);

                __instance.m_minStationLevelIcon.gameObject.SetActive(false);

                var canCraft = Player.m_localPlayer.HaveRequirements(GetRecipeRequirementArray(recipe, SelectedRarity), false, 1);
                __instance.m_craftButton.interactable = canCraft;
                __instance.m_craftButton.GetComponent<UITooltip>().m_text = canCraft ? "" : Localization.instance.Localize("$msg_missingrequirement");
            }
            else
            {
                bgImage.enabled = false;
                __instance.m_itemCraftType.gameObject.SetActive(false);
                __instance.m_variantButton.gameObject.SetActive(false);
                __instance.m_minStationLevelIcon.gameObject.SetActive(false);
                __instance.m_recipeIcon.enabled = false;
                __instance.m_recipeName.enabled = false;
                __instance.m_recipeDecription.enabled = false;
                foreach (var rarityButton in RarityButtons)
                {
                    rarityButton.gameObject.SetActive(false);
                }
                foreach (var req in __instance.m_recipeRequirementList)
                {
                    InventoryGui.HideRequirement(req.transform);
                }

                __instance.m_craftButton.interactable = false;
            }
        }

        public static bool CanEnchantRarity(Player player, ItemRarity rarity)
        {
            return true;
        }

        private string GenerateEnchantTooltip(EnchantRecipe recipe)
        {
            var sb = new StringBuilder();
            var rarityColor = EpicLoot.GetRarityColor(SelectedRarity);
            var rarityDisplay = EpicLoot.GetRarityDisplayName(SelectedRarity);
            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine($"{recipe.FromItem.m_shared.m_name} \u2794 <color={rarityColor}>{rarityDisplay}</color> {recipe.FromItem.GetDecoratedName(rarityColor)}");
            sb.AppendLine($"<color={rarityColor}>");

            var effectCountWeights = LootRoller.GetEffectCountsPerRarity(SelectedRarity);
            float totalWeight = effectCountWeights.Sum(x => x.Value);
            foreach (var effectCountEntry in effectCountWeights)
            {
                var count = effectCountEntry.Key;
                var weight = effectCountEntry.Value;
                var percent = (int)(weight / totalWeight * 100.0f);
                var label = count == 1 ? $"{count} effect:" : $"{count} effects:";
                sb.AppendLine($"  ‣ {label} {percent}%");
            }
            sb.Append("</color>");

            return sb.ToString();
        }

        public void SetupRequirementList(InventoryGui __instance, Player player, EnchantRecipe recipe)
        {
            var index = 0;
            var cost = GetRecipeCost(recipe, SelectedRarity);
            foreach (var product in cost)
            {
                if (SetupRequirement(__instance, __instance.m_recipeRequirementList[index].transform, product.Key, product.Value, player, true))
                {
                    ++index;
                }
            }

            for (; index < __instance.m_recipeRequirementList.Length; ++index)
            {
                InventoryGui.HideRequirement(__instance.m_recipeRequirementList[index].transform);
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
                var recipe = Recipes[SelectedRecipe];

                if (!recipe.FromItem.IsExtended())
                {
                    var inventory = player.GetInventory();
                    inventory.RemoveItem(recipe.FromItem);
                    var extendedItemData = new ExtendedItemData(recipe.FromItem);
                    inventory.m_inventory.Add(extendedItemData);
                    inventory.Changed();
                    recipe.FromItem = extendedItemData;
                }

                float previousDurabilityPercent = 0;
                if (recipe.FromItem.m_shared.m_useDurability)
                {
                    previousDurabilityPercent = recipe.FromItem.m_durability / recipe.FromItem.GetMaxDurability();
                }

                var luckFactor = player.GetTotalActiveMagicEffectValue(MagicEffectType.Luck, 0.01f);
                var magicItemComponent = recipe.FromItem.Extended().AddComponent<MagicItemComponent>();
                var magicItem = LootRoller.RollMagicItem(SelectedRarity, recipe.FromItem.Extended(), luckFactor);
                magicItemComponent.SetMagicItem(magicItem);

                EquipmentEffectCache.Reset(player);

                // Spend Resources
                if (!player.NoCostCheat())
                {
                    player.ConsumeResources(GetRecipeRequirementArray(recipe, SelectedRarity), 1);
                }

                // Maintain durability
                if (recipe.FromItem.m_shared.m_useDurability)
                {
                    recipe.FromItem.m_durability = previousDurabilityPercent * recipe.FromItem.GetMaxDurability();
                }

                __instance.UpdateCraftingPanel();

                if (player.GetCurrentCraftingStation() != null)
                {
                    player.GetCurrentCraftingStation().m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }

                SuccessDialog.Show(recipe.FromItem.Extended());
                
                MagicItemEffects.Indestructible.MakeItemIndestructible(recipe.FromItem);

                Game.instance.GetPlayerProfile().m_playerStats.m_crafts++;
                Gogan.LogEvent("Game", "Enchanted", recipe.FromItem.m_shared.m_name, 1);
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
                var enchantRecipe = Recipes[index];
                AddRecipeToList(__instance, enchantRecipe, index);
            }

            __instance.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(__instance.m_recipeListBaseSize, __instance.m_recipeList.Count * __instance.m_recipeListSpace));
        }

        public void AddRecipeToList(InventoryGui __instance, EnchantRecipe recipe, int index)
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

            /*var bgImage = Object.Instantiate(image, image.transform.parent, true);
            bgImage.name = "MagicItemBG";
            bgImage.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
            bgImage.sprite = EpicLoot.Assets.GenericItemBgSprite;
            bgImage.color = EpicLoot.GetRarityColorARGB(recipe.ToRarity);
            if (!canCraft)
            {
                bgImage.color -= new Color(0, 0, 0, 0.66f);
            }*/

            var nameText = element.transform.Find("name").GetComponent<Text>();
            nameText.text = Localization.instance.Localize(item.m_shared.m_name);
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
                OnSelectedRarity(ItemRarity.Magic);
                SetRecipe(__instance, SelectedRecipe, false);
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
                    GenerateEnchantRecipesForItem(item);
                }
            }
        }

        private void GenerateEnchantRecipesForItem(ItemDrop.ItemData item)
        {
            if (!item.IsMagic() && EpicLoot.CanBeMagicItem(item))
            {
                var recipe = new EnchantRecipe { FromItem = item.Extended() };
                Recipes.Add(recipe);
            }
        }

        public static List<KeyValuePair<ItemDrop, int>> GetRecipeCost(EnchantRecipe recipe, ItemRarity rarity)
        {
            return GetEnchantCosts(recipe.FromItem, rarity);
        }

        public static Piece.Requirement[] GetRecipeRequirementArray(EnchantRecipe recipe, ItemRarity rarity)
        {
            var cost = GetRecipeCost(recipe, rarity);
            return cost.Select(x => new Piece.Requirement() { m_amount = x.Value, m_resItem = x.Key }).ToArray();
        }

        public static List<KeyValuePair<ItemDrop, int>> GetEnchantCosts(ItemDrop.ItemData item, ItemRarity rarity)
        {
            var enchantCostDef = EnchantCostsHelper.GetEnchantCost(item, rarity);
            if (enchantCostDef == null)
            {
                return null;
            }

            var costList = new List<KeyValuePair<ItemDrop, int>>();

            foreach (var itemAmountConfig in enchantCostDef)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item).GetComponent<ItemDrop>();
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Tried to add unknown item ({itemAmountConfig.Item}) to enchant cost for item ({item.m_shared.m_name})");
                    continue;
                }
                costList.Add(new KeyValuePair<ItemDrop, int>(prefab, itemAmountConfig.Amount));
            }

            return costList;
        }
    }
}
