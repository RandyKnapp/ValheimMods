using System.Collections.Generic;
using Common;
using ExtendedItemDataFramework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public class DisenchantTabController : TabController
    {
        public static readonly Dictionary<ItemRarity, List<string>> TrophyShards = new Dictionary<ItemRarity, List<string>>()
        {
            {ItemRarity.Magic, new List<string> {
                "$item_trophy_boar",
                "$item_trophy_deer",
                "$item_trophy_greydwarf",
                "$item_trophy_neck",
                "$item_trophy_skeleton",
                "$item_trophy_skeletonpoison",
            }},
            {ItemRarity.Rare, new List<string> {
                "$item_trophy_troll",
                "$item_trophy_greydwarfbrute",
                "$item_trophy_greydwarfshaman",
                "$item_trophy_blob",
                "$item_trophy_draugr",
                "$item_trophy_draugrelite",
                "$item_trophy_draugrfem",
                "$item_trophy_foresttroll",
                "$item_trophy_leech",
                "$item_trophy_surtling",
                "$item_trophy_wolf",
                "$item_trophy_wraith",
            }},
            {ItemRarity.Epic, new List<string> {
                "$item_trophy_fenring",
                "$item_trophy_goblin",
                "$item_trophy_hatchling",
                "$item_trophy_lox",
                "$item_trophy_sgolem",
            }},
            {ItemRarity.Legendary, new List<string> {
                "$item_trophy_deathsquito",
                "$item_trophy_goblinbrute",
                "$item_trophy_goblinshaman",
                "$item_trophy_serpent"
            }},
        };

        public static readonly Dictionary<string, KeyValuePair<string, int>> BossTrophies = new Dictionary<string, KeyValuePair<string, int>>()
        {
            { "$item_trophy_eikthyr", new KeyValuePair<string, int>("RunestoneMagic", 1) },
            { "$item_trophy_elder", new KeyValuePair<string, int>("RunestoneRare", 1) },
            { "$item_trophy_bonemass", new KeyValuePair<string, int>("RunestoneEpic", 1) },
            { "$item_trophy_dragonqueen", new KeyValuePair<string, int>("RunestoneLegendary", 1) },
            { "$item_trophy_goblinking", new KeyValuePair<string, int>("RunestoneLegendary", 3) },
        };

        public static readonly Dictionary<string, List<KeyValuePair<string, int>>> SpecialDisenchants = new Dictionary<string, List<KeyValuePair<string, int>>>()
        {
            { "$item_cryptkey", new List<KeyValuePair<string, int>>() {
                new KeyValuePair<string, int>("DustRare", 1),
                new KeyValuePair<string, int>("EssenceRare", 1),
                new KeyValuePair<string, int>("ReagentRare", 1),
            }},
        };

        public class DisenchantRecipe
        {
            public ItemDrop.ItemData FromItem;
            public List<KeyValuePair<ItemDrop, int>> Products = new List<KeyValuePair<ItemDrop, int>>();
        }

        public readonly List<DisenchantRecipe> Recipes = new List<DisenchantRecipe>();

        public DisenchantTabController() : base(CraftingTabType.Disenchant, true)
        {
        }

        protected override string GetTabButtonId() => "Disenchant";
        protected override string GetTabButtonText() => "SACRIFICE";

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
                var canPutProductsInInventory = true;
                foreach (var product in recipe.Products)
                {
                    canPutProductsInInventory = canPutProductsInInventory && player.GetInventory().CanAddItem(product.Key.m_itemData, product.Value);
                }
                __instance.m_craftButton.interactable = canPutProductsInInventory && !isEquipped;
                __instance.m_craftButton.GetComponentInChildren<Text>().text = "Sacrifice";
                __instance.m_craftButton.GetComponent<UITooltip>().m_text =
                    canPutProductsInInventory ? (isEquipped ? "Item is currently equipped" : "") : Localization.instance.Localize("$inventory_full");
            }
        }

        public static void SetupRequirementList(InventoryGui __instance, Player player, DisenchantRecipe recipe)
        {
            var index = 0;
            foreach (var product in recipe.Products)
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
                        bgIconTransform = Object.Instantiate(icon, icon.transform.parent, true).transform;
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
                amountText.color = Color.white;
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
                player.GetInventory().RemoveOneItem(recipe.FromItem);
                foreach (var product in recipe.Products)
                {
                    var itemData = player.GetInventory().AddItem(product.Key.name, product.Value, 1, 0, 0, "");
                    if (itemData.IsMagicCraftingMaterial())
                    {
                        itemData.m_variant = EpicLoot.GetRarityIconIndex(itemData.GetRarity());
                    }
                }
                __instance.UpdateCraftingPanel();

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

        private void OnSelectedRecipe(InventoryGui __instance, int index)
        {
            __instance.OnCraftCancelPressed();
            SelectedRecipe = index;
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
                var trophyRecipes = new List<DisenchantRecipe>();
                var bossRecipes = new List<DisenchantRecipe>();
                var specialRecipes = new List<DisenchantRecipe>();
                var standardRecipes = new List<DisenchantRecipe>();

                foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    var recipe = GenerateDisenchantRecipe(item);
                    if (recipe != null)
                    {
                        standardRecipes.Add(recipe);
                        continue;
                    }

                    recipe = GenerateTrophyRecipe(item);
                    if (recipe != null)
                    {
                        trophyRecipes.Add(recipe);
                        continue;
                    }

                    recipe = GenerateBossTrophyRecipe(item);
                    if (recipe != null)
                    {
                        bossRecipes.Add(recipe);
                        continue;
                    }

                    recipe = GenerateSpecialRecipe(item);
                    if (recipe != null)
                    {
                        specialRecipes.Add(recipe);
                    }
                }

                Recipes.AddRange(trophyRecipes);
                Recipes.AddRange(bossRecipes);
                Recipes.AddRange(specialRecipes);
                Recipes.AddRange(standardRecipes);
            }
        }

        private static DisenchantRecipe GenerateTrophyRecipe(ItemDrop.ItemData item)
        {
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
            {
                foreach (var entry in TrophyShards)
                {
                    var rarity = entry.Key;
                    if (entry.Value.Contains(item.m_shared.m_name))
                    {
                        var recipe = new DisenchantRecipe { FromItem = item.Extended() };
                        AddDisenchantProducts(item, recipe, rarity);
                        return recipe;
                    }
                }
            }

            return null;
        }

        private static DisenchantRecipe GenerateBossTrophyRecipe(ItemDrop.ItemData item)
        {
            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophie)
            {
                if (BossTrophies.TryGetValue(item.m_shared.m_name, out var entry))
                {
                    var recipe = new DisenchantRecipe {FromItem = item};
                    var prefab = ObjectDB.instance.GetItemPrefab(entry.Key).GetComponent<ItemDrop>();
                    recipe.Products.Add(new KeyValuePair<ItemDrop, int>(prefab, entry.Value));
                    return recipe;
                }
            }
            return null;
        }

        private static DisenchantRecipe GenerateSpecialRecipe(ItemDrop.ItemData item)
        {
            if (SpecialDisenchants.TryGetValue(item.m_shared.m_name, out var entry))
            {
                var recipe = new DisenchantRecipe { FromItem = item };
                foreach (var productEntry in entry)
                {
                    var prefab = ObjectDB.instance.GetItemPrefab(productEntry.Key).GetComponent<ItemDrop>();
                    recipe.Products.Add(new KeyValuePair<ItemDrop, int>(prefab, productEntry.Value));
                }
                return recipe;
            }
            return null;
        }

        private static DisenchantRecipe GenerateDisenchantRecipe(ItemDrop.ItemData item)
        {
            if (item.IsMagic())
            {
                var recipe = new DisenchantRecipe { FromItem = item.Extended() };
                AddDisenchantProducts(item, recipe, item.GetMagicItem().Rarity);
                return recipe;
            }

            return null;
        }

        private static void AddDisenchantProducts(ItemDrop.ItemData item, DisenchantRecipe recipe, ItemRarity rarity)
        {
            var dustPrefab = ObjectDB.instance.GetItemPrefab($"Dust{rarity}").GetComponent<ItemDrop>();
            var essencePrefab = ObjectDB.instance.GetItemPrefab($"Essence{rarity}").GetComponent<ItemDrop>();
            var reagentPrefab = ObjectDB.instance.GetItemPrefab($"Reagent{rarity}").GetComponent<ItemDrop>();
            var shardPrefab = ObjectDB.instance.GetItemPrefab($"Shard{rarity}").GetComponent<ItemDrop>();

            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.Trophie:
                    recipe.Products.Add(new KeyValuePair<ItemDrop, int>(shardPrefab, 1));
                    break;

                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Tool:
                    recipe.Products.Add(new KeyValuePair<ItemDrop, int>(dustPrefab, 1));
                    recipe.Products.Add(new KeyValuePair<ItemDrop, int>(essencePrefab, 1));
                    break;

                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Utility:
                    recipe.Products.Add(new KeyValuePair<ItemDrop, int>(dustPrefab, 1));
                    recipe.Products.Add(new KeyValuePair<ItemDrop, int>(reagentPrefab, 1));
                    break;
            }
        }
    }
}
