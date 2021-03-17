using System;
using System.Collections;
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

        protected override string GetTabButtonId() => "Enchant";
        protected override string GetTabButtonText() => "ENCHANT";

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
                    var rt = rarityButton.transform as RectTransform;
                    rt.anchoredPosition = startPos + (index * new Vector2(rt.rect.width + 4, 0));
                    index++;
                }
            }

            if (SuccessDialog == null)
            {
                var newDialog = Object.Instantiate(inventoryGui.m_variantDialog, inventoryGui.m_variantDialog.transform.parent);
                SuccessDialog = newDialog.gameObject.AddComponent<CraftSuccessDialog>();
                Object.Destroy(newDialog);
                SuccessDialog.gameObject.name = "CraftingSuccessDialog";

                var background = SuccessDialog.gameObject.transform.Find("VariantFrame") as RectTransform;
                background.gameObject.name = "Frame";
                for (int i = 1; i < background.transform.childCount; ++i)
                {
                    var child = background.transform.GetChild(i);
                    Object.Destroy(child.gameObject);
                }
                background.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 380);
                background.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 550);
                background.anchoredPosition += new Vector2(20, -270);

                SuccessDialog.MagicBG = Object.Instantiate(inventoryGui.m_recipeIcon, background);
                SuccessDialog.MagicBG.name = "MagicItemBG";
                SuccessDialog.MagicBG.sprite = EpicLoot.GetMagicItemBgSprite();
                SuccessDialog.MagicBG.color = Color.white;

                SuccessDialog.NameText = Object.Instantiate(inventoryGui.m_recipeName, background);
                SuccessDialog.Description = Object.Instantiate(inventoryGui.m_recipeDecription, background);
                SuccessDialog.Description.rectTransform.anchoredPosition += new Vector2(0, -110);
                SuccessDialog.Description.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 460);
                SuccessDialog.Icon = Object.Instantiate(inventoryGui.m_recipeIcon, background);

                var closeButton = SuccessDialog.gameObject.GetComponentInChildren<Button>();
                closeButton.onClick = new Button.ButtonClickedEvent();
                closeButton.onClick.AddListener(SuccessDialog.OnClose);
                closeButton.transform.SetAsLastSibling();
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
                SelectedRarity = ItemRarity.Magic;
                foreach (var rarityButton in RarityButtons)
                {
                    rarityButton.gameObject.SetActive(false);
                }
            }
            base.SetActive(active);
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
        }

        public override void UpdateRecipe(InventoryGui __instance, Player player, float dt, Image bgImage)
        {
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
                __instance.m_craftButton.GetComponentInChildren<Text>().text = "Enchant";
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
            return player.m_knownMaterial.Contains($"{rarity} Runestone");
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
                if (SetupRequirement(__instance, __instance.m_recipeRequirementList[index].transform, product.Key, product.Value, player))
                {
                    ++index;
                }
            }

            for (; index < __instance.m_recipeRequirementList.Length; ++index)
            {
                InventoryGui.HideRequirement(__instance.m_recipeRequirementList[index].transform);
            }
        }

        public static bool SetupRequirement(
            InventoryGui __instance,
            Transform elementRoot,
            ItemDrop item,
            int amount,
            Player player)
        {
            var icon = elementRoot.transform.Find("res_icon").GetComponent<Image>();
            var nameText = elementRoot.transform.Find("res_name").GetComponent<Text>();
            var amountText = elementRoot.transform.Find("res_amount").GetComponent<Text>();
            var tooltip = elementRoot.GetComponent<UITooltip>();
            if (item != null)
            {
                icon.gameObject.SetActive(true);
                nameText.gameObject.SetActive(true);
                amountText.gameObject.SetActive(true);
                if (item.m_itemData.IsMagicCraftingMaterial())
                {
                    var rarity = item.m_itemData.GetCraftingMaterialRarity();
                    icon.sprite = item.m_itemData.m_shared.m_icons[EpicLoot.GetRarityIconIndex(rarity)];
                }
                else
                {
                    icon.sprite = item.m_itemData.GetIcon();
                }
                icon.color = Color.white;

                var bgIconTransform = icon.transform.parent.Find("bgIcon");
                if (item.m_itemData.UseMagicBackground())
                {
                    if (bgIconTransform == null)
                    {
                        bgIconTransform = GameObject.Instantiate(icon, icon.transform.parent, true).transform;
                        bgIconTransform.name = "bgIcon";
                        bgIconTransform.SetSiblingIndex(icon.transform.GetSiblingIndex());
                    }

                    bgIconTransform.gameObject.SetActive(true);
                    var bgIcon = bgIconTransform.GetComponent<Image>();
                    bgIcon.sprite = EpicLoot.GetMagicItemBgSprite();
                    bgIcon.color = item.m_itemData.GetRarityColor();
                }
                else if (bgIconTransform != null)
                {
                    bgIconTransform.gameObject.SetActive(false);
                }

                tooltip.m_text = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
                nameText.text = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
                if (amount <= 0)
                {
                    InventoryGui.HideRequirement(elementRoot);
                    return false;
                }
                amountText.text = amount.ToString();

                var currentAmount = player.GetInventory().CountItems(item.m_itemData.m_shared.m_name);
                if (currentAmount < amount)
                {
                    amountText.color = Mathf.Sin(Time.time * 10.0f) > 0.0f ? Color.red : Color.white;
                }
                else
                {
                    amountText.color = Color.white;
                }
            }
            else
            {
                var bgIconTransform = icon.transform.parent.Find("bgIcon");
                if (bgIconTransform != null)
                {
                    bgIconTransform.gameObject.SetActive(false);
                }
            }
            return true;
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
                
                var magicItemComponent = recipe.FromItem.Extended().AddComponent<MagicItemComponent>();
                var magicItem = LootRoller.RollMagicItem(SelectedRarity, recipe.FromItem.Extended());
                magicItemComponent.SetMagicItem(magicItem);

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

                Game.instance.GetPlayerProfile().m_playerStats.m_crafts++;
                Gogan.LogEvent("Game", "Disenchanted", recipe.FromItem.m_shared.m_name, 1);
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
                Mathf.Max(__instance.m_recipeListBaseSize, (float)__instance.m_recipeList.Count * __instance.m_recipeListSpace));
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
            SelectedRecipe = index;
            SelectedRarity = ItemRarity.Magic;
            SetRecipe(__instance, SelectedRecipe, false);
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
                //foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
                {
                    if (Player.m_localPlayer.m_knownMaterial.Contains($"Magic Runestone"))
                    {
                        var recipe = new EnchantRecipe { FromItem = item.Extended() }; // todo, no rarity in recipe
                        Recipes.Add(recipe);
                    }
                }
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
            var costList = new List<KeyValuePair<ItemDrop, int>>();
            const int runestoneCost = 1;
            const int dustCost = 5;
            const int otherCost = 5;

            var dustPrefab = ObjectDB.instance.GetItemPrefab($"Dust{rarity}").GetComponent<ItemDrop>();
            var essencePrefab = ObjectDB.instance.GetItemPrefab($"Essence{rarity}").GetComponent<ItemDrop>();
            var reagentPrefab = ObjectDB.instance.GetItemPrefab($"Reagent{rarity}").GetComponent<ItemDrop>();
            var runestonePrefab = ObjectDB.instance.GetItemPrefab($"Runestone{rarity}").GetComponent<ItemDrop>();

            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Tool:
                    costList.Add(new KeyValuePair<ItemDrop, int>(runestonePrefab, runestoneCost));
                    costList.Add(new KeyValuePair<ItemDrop, int>(dustPrefab, dustCost));
                    costList.Add(new KeyValuePair<ItemDrop, int>(essencePrefab, otherCost));
                    break;
                    
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Utility:
                    costList.Add(new KeyValuePair<ItemDrop, int>(runestonePrefab, runestoneCost));
                    costList.Add(new KeyValuePair<ItemDrop, int>(dustPrefab, dustCost));
                    costList.Add(new KeyValuePair<ItemDrop, int>(reagentPrefab, otherCost));
                    break;
            }

            return costList;
        }
    }
}
