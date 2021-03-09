using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public static class Disenchant
    {
        public static readonly Dictionary<ItemRarity, List<string>> TrophyShards = new Dictionary<ItemRarity, List<string>>()
        {
            {ItemRarity.Magic, new List<string> {
                "$item_trophy_boar",
                "$item_trophy_deer",
                "$item_trophy_greydwarf",
                "$item_trophy_greydwarfbrute",
                "$item_trophy_greydwarfshaman",
                "$item_trophy_neck",
                "$item_trophy_skeleton",
                "$item_trophy_skeletonpoison",
            }},
            {ItemRarity.Rare, new List<string> {
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
                "$item_trophy_serpent",
                "$item_trophy_sgolem",
            }},
            {ItemRarity.Legendary, new List<string> {
                "$item_trophy_deathsquito",
                "$item_trophy_goblinbrute",
                "$item_trophy_goblinshaman",
            }},
        };

        public static readonly Dictionary<string, KeyValuePair<string, int>> BossTrophies = new Dictionary<string, KeyValuePair<string, int>>()
        {
            { "$item_trophy_eikthyr", new KeyValuePair<string, int>("RunestoneMagic", 1) },
            { "$item_trophy_elder", new KeyValuePair<string, int>("RunestoneEpic", 1) },
            { "$item_trophy_bonemass", new KeyValuePair<string, int>("RunestoneRare", 1) },
            { "$item_trophy_dragonqueen", new KeyValuePair<string, int>("RunestoneLegendary", 1) },
            { "$item_trophy_goblinking", new KeyValuePair<string, int>("RunestoneLegendary", 3) },
        };

        public class DisenchantRecipe
        {
            public ItemDrop.ItemData FromItem;
            public List<KeyValuePair<ItemDrop, int>> Products = new List<KeyValuePair<ItemDrop, int>>();
        }

        public static Button TabDisenchant;
        public static readonly List<DisenchantRecipe> Recipes = new List<DisenchantRecipe>();
        public static int SelectedRecipe;

        [HarmonyPatch(typeof(InventoryGui), "OnDestroy")]
        public static class InventoryGui_OnDestroy_Patch
        {
            public static void Postfix()
            {
                TabDisenchant = null;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Hide")]
        public static class InventoryGui_Hide_Patch
        {
            public static void Postfix()
            {
                if (TabDisenchant != null)
                {
                    TabDisenchant.interactable = true;
                }
                Recipes.Clear();
                SelectedRecipe = -1;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateCraftingPanel")]
        public static class InventoryGui_UpdateCraftingPanel_Patch
        {
            public static bool Prefix(InventoryGui __instance, bool focusView)
            {
                if (TabDisenchant == null)
                {
                    var go = Object.Instantiate(__instance.m_tabUpgrade, __instance.m_tabUpgrade.transform.parent, true).gameObject;
                    go.name = "Disenchant";
                    go.GetComponentInChildren<Text>().text = "SACRIFICE";
                    go.transform.SetSiblingIndex(__instance.m_tabUpgrade.transform.GetSiblingIndex() + 1);
                    go.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100);
                    TabDisenchant = go.GetComponent<Button>();
                    TabDisenchant.gameObject.RectTransform().anchoredPosition = TabDisenchant.gameObject.RectTransform().anchoredPosition + new Vector2(107, 0);
                    TabDisenchant.onClick.RemoveAllListeners();
                    TabDisenchant.onClick.AddListener(TabDisenchant.GetComponent<ButtonSfx>().OnClick);
                    TabDisenchant.onClick.AddListener(OnDisenchatTabPressed);
                }

                var player = Player.m_localPlayer;
                var station = player.GetCurrentCraftingStation();
                var canDisenchantAtThisStation = CanDisenchantHere(station);
                TabDisenchant.gameObject.SetActive(canDisenchantAtThisStation);
                if (!canDisenchantAtThisStation)
                {
                    return true;
                }

                if (!InDisenchantTab())
                {
                    return true;
                }

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

                return false;
            }

            private static bool CanDisenchantHere(CraftingStation station)
            {
                if (station == null)
                {
                    return false;
                }

                var isArtisan = station.m_name == "$piece_artisanstation";
                var isForgeWithEnchanter = station.m_name == "$piece_forge" && station.m_attachedExtensions.Find(x => x.name.StartsWith("piece_enchanter"));
                return isArtisan || isForgeWithEnchanter;
            }
        }

        //public void UpdateRecipe(Player player, float dt)
        [HarmonyPatch(typeof(InventoryGui), "UpdateRecipe")]
        public static class InventoryGui_UpdateRecipe_Patch
        {
            public static void Postfix(InventoryGui __instance, Player player, float dt)
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

                    var magicItemBG = __instance.m_recipeIcon.transform.parent.Find("MagicItemBG");
                    Image bgImage = null;
                    if (magicItemBG == null)
                    {
                        bgImage = Object.Instantiate(__instance.m_recipeIcon, __instance.m_recipeIcon.transform.parent, true);
                        bgImage.name = "MagicItemBG";
                        bgImage.transform.SetSiblingIndex(__instance.m_recipeIcon.transform.GetSiblingIndex());
                        bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                        bgImage.color = recipe.FromItem.GetRarityColor();
                    }
                    else
                    {
                        bgImage = magicItemBG.GetComponent<Image>();
                        bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                        bgImage.color = recipe.FromItem.GetRarityColor();
                    }

                    bgImage.enabled = recipe.FromItem.UseMagicBackground();

                    __instance.m_itemCraftType.gameObject.SetActive(false);
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
                else
                {
                    var magicItemBG = __instance.m_recipeIcon.transform.parent.Find("MagicItemBG");
                    if (magicItemBG != null)
                    {
                        magicItemBG.GetComponent<Image>().enabled = false;
                    }
                }
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
                icon.sprite = item.m_itemData.GetIcon();
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
                amountText.color = Color.white;
            }
            return true;
        }

        //public void OnTabCraftPressed()
        [HarmonyPatch(typeof(InventoryGui), "OnTabCraftPressed")]
        public static class InventoryGui_OnTabCraftPressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                TabDisenchant.interactable = true;
                SelectedRecipe = -1;
                return true;
            }
        }

        //public void OnTabUpgradePressed()
        [HarmonyPatch(typeof(InventoryGui), "OnTabUpgradePressed")]
        public static class InventoryGui_OnTabUpgradePressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                TabDisenchant.interactable = true;
                SelectedRecipe = -1;
                return true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
        public static class InventoryGui_OnCraftPressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                if (InDisenchantTab() && SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
                {
                    __instance.m_craftTimer = 0.0f;
                    CraftingStation currentCraftingStation = Player.m_localPlayer.GetCurrentCraftingStation();
                    if (currentCraftingStation != null)
                    {
                        currentCraftingStation.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
                    }
                    return false;
                }
                return true;
            }
        }

        //public void DoCrafting(Player player)
        [HarmonyPatch(typeof(InventoryGui), "DoCrafting")]
        public static class InventoryGui_DoCrafting_Patch
        {
            public static bool Prefix(InventoryGui __instance, Player player)
            {
                if (InDisenchantTab() && SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
                {
                    var recipe = Recipes[SelectedRecipe];
                    player.GetInventory().RemoveItem(recipe.FromItem);
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
                    return false;
                }
                return true;
            }
        }

        public static void UpdateRecipeList(InventoryGui __instance)
        { 
            __instance.m_availableRecipes.Clear();
            foreach (var recipe in __instance.m_recipeList)
            {
                Object.Destroy(recipe);
            }
            __instance.m_recipeList.Clear();

            GenerateDisenchantRecipes();
            for (var index = 0; index < Recipes.Count; index++)
            {
                var disenchantRecipe = Recipes[index];
                AddRecipeToList(__instance, disenchantRecipe, index);
            }

            __instance.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 
                Mathf.Max(__instance.m_recipeListBaseSize, (float)__instance.m_recipeList.Count * __instance.m_recipeListSpace));
        }

        public static void AddRecipeToList(InventoryGui __instance, DisenchantRecipe recipe, int index)
        {
            var count = __instance.m_recipeList.Count;
            var element = Object.Instantiate(__instance.m_recipeElementPrefab, __instance.m_recipeListRoot);
            element.SetActive(true);
            element.RectTransform().anchoredPosition = new Vector2(0.0f, count * -__instance.m_recipeListSpace);

            var image = element.transform.Find("icon").GetComponent<Image>();
            image.sprite = recipe.FromItem.GetIcon();
            image.color = Color.white;

            if (recipe.FromItem.UseMagicBackground())
            {
                var bgImage = Object.Instantiate(image, image.transform.parent, true);
                bgImage.name = "MagicItemBG";
                bgImage.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
                bgImage.sprite = EpicLoot.Assets.GenericItemBgSprite;
                bgImage.color = recipe.FromItem.GetRarityColor();
            }

            var nameText = element.transform.Find("name").GetComponent<Text>();
            nameText.text = Localization.instance.Localize(recipe.FromItem.GetDecoratedName());

            var durability = element.transform.Find("Durability").GetComponent<GuiBar>();
            durability.gameObject.SetActive(false);

            var quality = element.transform.Find("QualityLevel").GetComponent<Text>();
            quality.gameObject.SetActive(false);

            element.GetComponent<Button>().onClick.AddListener(() => OnSelectedRecipe(__instance, index));
            __instance.m_recipeList.Add(element);
        }

        private static void OnSelectedRecipe(InventoryGui __instance, int index)
        {
            SelectedRecipe = index;
            SetRecipe(__instance, SelectedRecipe, false);
        }

        public static void SetRecipe(InventoryGui __instance, int index, bool center)
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

        private static bool InDisenchantTab()
        {
            return TabDisenchant != null && !TabDisenchant.interactable;
        }

        public static void OnDisenchatTabPressed()
        {
            var instance = InventoryGui.instance;
            instance.m_tabCraft.interactable = true;
            instance.m_tabUpgrade.interactable = true;
            TabDisenchant.interactable = false;

            GenerateDisenchantRecipes();
            instance.UpdateCraftingPanel();
        }

        public static void GenerateDisenchantRecipes()
        {
            Recipes.Clear();
            if (Player.m_localPlayer != null)
            {
                foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    var recipe = GenerateTrophyRecipe(item);
                    if (recipe != null)
                    {
                        Recipes.Add(recipe);
                    }
                }

                foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    var recipe = GenerateBossTrophyRecipe(item);
                    if (recipe != null)
                    {
                        Recipes.Add(recipe);
                    }
                }

                foreach (var item in Player.m_localPlayer.GetInventory().GetAllItems())
                {
                    var recipe = GenerateDisenchantRecipe(item);
                    if (recipe != null)
                    {
                        Recipes.Add(recipe);
                    }
                }
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
