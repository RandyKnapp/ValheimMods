using System.Collections.Generic;
using Common;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public class DisenchantTabController : TabController
    {
        public class DisenchantRecipe
        {
            public ItemDrop.ItemData FromItem;
            public List<KeyValuePair<ItemDrop, int>> Products = new List<KeyValuePair<ItemDrop, int>>();
        }

        public readonly List<DisenchantRecipe> Recipes = new List<DisenchantRecipe>();

        public DisenchantTabController() : base(CraftingTabType.Disenchant, true)
        {
        }

        public override string GetTabButtonId() => "Disenchant";
        public override string GetTabButtonText() => "SACRIFICE";

        public override void SetActive(bool active)
        {
            if (!active)
            {
                Recipes.Clear();
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

        public override void UpdateRecipe(InventoryGui __instance, Player player, float dt, Image bgImage)
        {
            if (SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
            {
                var recipe = Recipes[SelectedRecipe];
                var itemData = recipe.FromItem;

                __instance.m_recipeIcon.enabled = true;
                __instance.m_recipeIcon.sprite = itemData.GetIcon();

                __instance.m_recipeName.enabled = true;
                __instance.m_recipeName.text = Localization.instance.Localize(itemData.GetDecoratedName());

                __instance.m_recipeDecription.enabled = true;
                __instance.m_recipeDecription.text = Localization.instance.Localize(ItemDrop.ItemData.GetTooltip(itemData, itemData.m_quality, true));
                __instance.m_recipeDecription.text += "\n\n<color=red>This item will be DESTROYED as a SACRIFICE to the gods.</color>";

                bgImage.color = recipe.FromItem.GetRarityColor();
                bgImage.enabled = recipe.FromItem.UseMagicBackground();

                __instance.m_itemCraftType.gameObject.SetActive(true);
                __instance.m_itemCraftType.text = "Destroy this item and get:";

                __instance.m_variantButton.gameObject.SetActive(false);

                SetupRequirementList(__instance, player, recipe);

                __instance.m_minStationLevelIcon.gameObject.SetActive(false);

                var isEquipped = recipe.FromItem.m_equiped;
                var canPutProductsInInventory = CanAddItems(player.GetInventory(), recipe.Products, 1);
                __instance.m_craftButton.interactable = canPutProductsInInventory && !isEquipped;
                __instance.m_craftButton.GetComponentInChildren<Text>().text = "Sacrifice";
                __instance.m_craftButton.GetComponent<UITooltip>().m_text =
                    canPutProductsInInventory ? (isEquipped ? "Item is currently equipped" : "") : Localization.instance.Localize("$inventory_full");
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
                foreach (var req in __instance.m_recipeRequirementList)
                {
                    InventoryGui.HideRequirement(req.transform);
                }

                __instance.m_craftButton.interactable = false;
            }
        }

        public static bool CanAddItems(Inventory inventory, List<KeyValuePair<ItemDrop, int>> items, int extraSpaces = 0)
        {
            var emptySlots = inventory.GetEmptySlots() + extraSpaces;
            var freeStackSpaceTable = new Dictionary<ItemDrop.ItemData, int>();
            foreach (var itemEntry in items)
            {
                var item = itemEntry.Key.m_itemData;
                var amount = itemEntry.Value;
                if (!freeStackSpaceTable.ContainsKey(item))
                {
                    freeStackSpaceTable.Add(item, inventory.FindFreeStackSpace(item.m_shared.m_name));
                }

                var freeStackSpace = freeStackSpaceTable[item];
                if (freeStackSpace < amount && emptySlots == 0)
                {
                    return false;
                }
                else
                {
                    if (freeStackSpace > 0 && freeStackSpace >= amount)
                    {
                        freeStackSpaceTable[item] -= amount;
                    }
                    else if (freeStackSpace > 0 && freeStackSpace < amount)
                    {
                        freeStackSpaceTable[item] = 0;
                        if (emptySlots == 0)
                        {
                            return false;
                        }
                        else
                        {
                            emptySlots--;
                        }
                    }
                    else if (freeStackSpace <= 0 && emptySlots > 0)
                    {
                        emptySlots--;
                    }
                }
            }

            return true;
        }

        public static void SetupRequirementList(InventoryGui __instance, Player player, DisenchantRecipe recipe)
        {
            var index = 0;
            foreach (var product in recipe.Products)
            {
                if (SetupRequirement(__instance, __instance.m_recipeRequirementList[index].transform, product.Key, product.Value, player, false))
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
                player.GetInventory().RemoveOneItem(recipe.FromItem);
                var didntAdd = new List<KeyValuePair<ItemDrop.ItemData, int>>();
                foreach (var product in recipe.Products)
                {
                    var canAdd = player.GetInventory().CanAddItem(product.Key.m_itemData, product.Value);
                    if (canAdd)
                    {
                        var itemData = player.GetInventory().AddItem(product.Key.name, product.Value, 1, 0, 0, "");
                        if (itemData.IsMagicCraftingMaterial())
                        {
                            itemData.m_variant = EpicLoot.GetRarityIconIndex(itemData.GetRarity());
                        }
                    }
                    else
                    {
                        var newItem = product.Key.m_itemData.Clone();
                        newItem.m_dropPrefab = ObjectDB.instance.GetItemPrefab(product.Key.GetPrefabName(product.Key.gameObject.name));
                        didntAdd.Add(new KeyValuePair<ItemDrop.ItemData, int>(newItem, product.Value));
                    }
                }
                __instance.UpdateCraftingPanel();

                foreach (var itemNotAdded in didntAdd)
                {
                    var itemDrop = ItemDrop.DropItem(itemNotAdded.Key, itemNotAdded.Value, player.transform.position + player.transform.forward + player.transform.up, player.transform.rotation);
                    itemDrop.GetComponent<Rigidbody>().velocity = (player.transform.forward + Vector3.up) * 5f;
                    player.Message(MessageHud.MessageType.TopLeft, $"$msg_dropped {itemDrop.m_itemData.m_shared.m_name} (Inventory was full when crafting)", itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
                }

                if (player.GetCurrentCraftingStation() != null)
                {
                    player.GetCurrentCraftingStation().m_craftItemDoneEffects.Create(player.transform.position, Quaternion.identity);
                }

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
                var disenchantRecipe = Recipes[index];
                AddRecipeToList(__instance, disenchantRecipe, index);
            }

            __instance.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 
                Mathf.Max(__instance.m_recipeListBaseSize, __instance.m_recipeList.Count * __instance.m_recipeListSpace));
        }

        public void AddRecipeToList(InventoryGui __instance, DisenchantRecipe recipe, int index)
        {
            var count = __instance.m_recipeList.Count;
            var element = Object.Instantiate(__instance.m_recipeElementPrefab, __instance.m_recipeListRoot);
            element.SetActive(true);
            element.RectTransform().anchoredPosition = new Vector2(0.0f, count * -__instance.m_recipeListSpace);

            var item = recipe.FromItem;

            var image = element.transform.Find("icon").GetComponent<Image>();
            image.sprite = item.GetIcon();
            image.color = Color.white;

            if (item.UseMagicBackground())
            {
                var bgImage = Object.Instantiate(image, image.transform.parent, true);
                bgImage.name = "MagicItemBG";
                bgImage.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
                bgImage.sprite = EpicLoot.Assets.GenericItemBgSprite;
                bgImage.color = item.GetRarityColor();
            }

            var nameText = element.transform.Find("name").GetComponent<Text>();
            nameText.text = Localization.instance.Localize(item.GetDecoratedName());
            if (item.m_shared.m_maxStackSize > 1 && item.m_stack > 1)
            {
                nameText.text += $" x{item.m_stack}";
            }

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
            quality.gameObject.SetActive(item.m_shared.m_maxQuality > 1);
            quality.text = item.m_quality.ToString();

            element.GetComponent<Button>().onClick.AddListener(() => OnSelectedRecipe(__instance, index));
            __instance.m_recipeList.Add(element);
        }

        public void OnSelectedRecipe(InventoryGui __instance, int index)
        {
            if (SelectedRecipe != index)
            {
                __instance.OnCraftCancelPressed();
                SelectedRecipe = index;
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
                    var recipe = GenerateDisenchantRecipe(item);
                    if (recipe != null)
                    {
                        Recipes.Add(recipe);
                    }
                }

                Recipes.Sort((a, b) => GetRecipeSortOrderValue(a).CompareTo(GetRecipeSortOrderValue(b)));
            }
        }

        public static int GetRecipeSortOrderValue(DisenchantRecipe recipe)
        {
            if (recipe.FromItem.IsMagic())
            {
                return 3;
            }

            if (recipe.FromItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
            {
                return recipe.Products.Exists(x => x.Key.m_itemData.IsRunestone()) ? 1 : 0;
            }

            return 2;
        }
        
        public static DisenchantRecipe GenerateDisenchantRecipe(ItemDrop.ItemData item)
        {
            var products = GetDisenchantProducts(item);
            if (products == null)
            {
                return null;
            }

            var recipe = new DisenchantRecipe {
                    FromItem = item,
                    Products = products
            };
            return recipe;
        }

        public static List<KeyValuePair<ItemDrop, int>> GetDisenchantProducts(ItemDrop.ItemData item)
        {
            var productsDef = EnchantCostsHelper.GetDisenchantProducts(item);
            if (productsDef == null)
            {
                return null;
            }

            var products = new List<KeyValuePair<ItemDrop, int>>();

            foreach (var itemAmountConfig in productsDef)
            {
                var prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Tried to add unknown item ({itemAmountConfig.Item}) to disenchant product for item ({item.m_shared.m_name})");
                    continue;
                }

                var itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    EpicLoot.LogWarning($"Tried to add object with no ItemDrop ({itemAmountConfig.Item}) to disenchant product for item ({item.m_shared.m_name})");
                    continue;
                }

                products.Add(new KeyValuePair<ItemDrop, int>(itemDrop, itemAmountConfig.Amount));
            }

            return products;
        }
    }
}
