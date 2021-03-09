using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using ExtendedItemDataFramework;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EpicLoot.Crafting
{
    public static class Enchant
    {
        public class EnchantRecipe
        {
            public ItemDrop.ItemData FromItem;
            public ItemRarity ToRarity;
            public List<KeyValuePair<ItemDrop, int>> Cost = new List<KeyValuePair<ItemDrop, int>>();

            public Piece.Requirement[] GetRequirementArray()
            {
                return Cost.Select(x => new Piece.Requirement() {m_amount = x.Value, m_resItem = x.Key}).ToArray();
            }
        }

        public static Button TabEnchant;
        public static readonly List<EnchantRecipe> Recipes = new List<EnchantRecipe>();
        public static int SelectedRecipe;

        [HarmonyPatch(typeof(InventoryGui), "OnDestroy")]
        public static class InventoryGui_OnDestroy_Patch
        {
            public static void Postfix()
            {
                TabEnchant = null;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "Hide")]
        public static class InventoryGui_Hide_Patch
        {
            public static void Postfix()
            {
                if (TabEnchant != null)
                {
                    TabEnchant.interactable = true;
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
                if (TabEnchant == null)
                {
                    var go = Object.Instantiate(__instance.m_tabUpgrade, __instance.m_tabUpgrade.transform.parent, true).gameObject;
                    go.name = "Enchant";
                    go.GetComponentInChildren<Text>().text = "ENCHANT";
                    go.transform.SetSiblingIndex(__instance.m_tabUpgrade.transform.GetSiblingIndex() + 1);
                    go.RectTransform().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100);
                    TabEnchant = go.GetComponent<Button>();
                    TabEnchant.gameObject.RectTransform().anchoredPosition = TabEnchant.gameObject.RectTransform().anchoredPosition + new Vector2(107 * 2, 0);
                    TabEnchant.onClick.RemoveAllListeners();
                    TabEnchant.onClick.AddListener(TabEnchant.GetComponent<ButtonSfx>().OnClick);
                    TabEnchant.onClick.AddListener(OnEnchatTabPressed);
                }

                var player = Player.m_localPlayer;
                var station = player.GetCurrentCraftingStation();
                var canEnchantHere = CanEnchantHere(station);
                TabEnchant.gameObject.SetActive(canEnchantHere);
                if (!canEnchantHere)
                {
                    return true;
                }

                if (!InEnchantTab())
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
        }

        private static bool CanEnchantHere(CraftingStation station)
        {
            if (station == null)
            {
                return false;
            }

            var isArtisan = station.m_name == "$piece_artisanstation";
            var isForgeWithEnchanter = station.m_name == "$piece_forge" && station.m_attachedExtensions.Find(x => x.name.StartsWith("piece_enchanter"));
            return isArtisan || isForgeWithEnchanter;
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
                    var rarityColor = EpicLoot.GetRarityColor(recipe.ToRarity);
                    var rarityColorARGB = EpicLoot.GetRarityColorARGB(recipe.ToRarity);

                    __instance.m_recipeIcon.enabled = true;
                    __instance.m_recipeIcon.sprite = itemData.GetIcon();

                    __instance.m_recipeName.enabled = true;
                    __instance.m_recipeName.text = Localization.instance.Localize(itemData.GetDecoratedName(rarityColor));

                    __instance.m_recipeDecription.enabled = true;
                    __instance.m_recipeDecription.text = Localization.instance.Localize(GenerateEnchantTooltip(recipe));

                    var magicItemBG = __instance.m_recipeIcon.transform.parent.Find("MagicItemBG");
                    Image bgImage = null;
                    if (magicItemBG == null)
                    {
                        bgImage = Object.Instantiate(__instance.m_recipeIcon, __instance.m_recipeIcon.transform.parent, true);
                        bgImage.name = "MagicItemBG";
                        bgImage.transform.SetSiblingIndex(__instance.m_recipeIcon.transform.GetSiblingIndex());
                        bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                        bgImage.color = rarityColorARGB;
                    }
                    else
                    {
                        bgImage = magicItemBG.GetComponent<Image>();
                        bgImage.sprite = EpicLoot.GetMagicItemBgSprite();
                        bgImage.color = rarityColorARGB;
                    }

                    bgImage.enabled = true;

                    __instance.m_itemCraftType.gameObject.SetActive(false);
                    __instance.m_variantButton.gameObject.SetActive(false);

                    SetupRequirementList(__instance, player, recipe);

                    __instance.m_minStationLevelIcon.gameObject.SetActive(false);

                    __instance.m_craftButton.interactable = true;
                    __instance.m_craftButton.GetComponentInChildren<Text>().text = "Enchant";
                    __instance.m_craftButton.GetComponent<UITooltip>().m_text = "";
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

            private static string GenerateEnchantTooltip(EnchantRecipe recipe)
            {
                var sb = new StringBuilder();
                var rarityColor = EpicLoot.GetRarityColor(recipe.ToRarity);
                sb.AppendLine($"{recipe.FromItem.m_shared.m_name} -> {recipe.FromItem.GetDecoratedName(rarityColor)}");
                sb.AppendLine($"<color={rarityColor}>");

                var effectCountWeights = EpicLoot.MagicEffectCountWeightsPerRarity[recipe.ToRarity];
                var totalWeight = effectCountWeights.Sum(x => x.Value);
                foreach (var effectCountEntry in effectCountWeights)
                {
                    var count = effectCountEntry.Key;
                    var weight = effectCountEntry.Value;
                    var percent = (int)(weight / totalWeight * 100);
                    var label = count == 1 ? $"{count} effect:" : $"{count} effects:";
                    sb.AppendLine($"  ‣ {label} {percent}%");
                }
                sb.Append("</color>");

                return sb.ToString();
            }
        }

        public static void SetupRequirementList(InventoryGui __instance, Player player, EnchantRecipe recipe)
        {
            var index = 0;
            foreach (var product in recipe.Cost)
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

        //public void OnTabCraftPressed()
        [HarmonyPatch(typeof(InventoryGui), "OnTabCraftPressed")]
        public static class InventoryGui_OnTabCraftPressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                TabEnchant.interactable = true;
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
                TabEnchant.interactable = true;
                SelectedRecipe = -1;
                return true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), "OnCraftPressed")]
        public static class InventoryGui_OnCraftPressed_Patch
        {
            public static bool Prefix(InventoryGui __instance)
            {
                if (InEnchantTab() && SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
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
                if (InEnchantTab() && SelectedRecipe >= 0 && SelectedRecipe < Recipes.Count)
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
                    
                    var magicItemComponent = recipe.FromItem.Extended().AddComponent<MagicItemComponent>();
                    var magicItem = EpicLoot.RollMagicItem(recipe.ToRarity, recipe.FromItem.Extended());
                    magicItemComponent.SetMagicItem(magicItem);

                    // Spend Resources
                    if (!player.NoCostCheat())
                    {
                        player.ConsumeResources(recipe.GetRequirementArray(), 1);
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

            GenerateEnchantRecipes();
            for (var index = 0; index < Recipes.Count; index++)
            {
                var enchantRecipe = Recipes[index];
                AddRecipeToList(__instance, enchantRecipe, index);
            }

            __instance.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(__instance.m_recipeListBaseSize, (float)__instance.m_recipeList.Count * __instance.m_recipeListSpace));
        }

        public static void AddRecipeToList(InventoryGui __instance, EnchantRecipe recipe, int index)
        {
            var count = __instance.m_recipeList.Count;
            var element = Object.Instantiate(__instance.m_recipeElementPrefab, __instance.m_recipeListRoot);
            element.SetActive(true);
            element.RectTransform().anchoredPosition = new Vector2(0.0f, count * -__instance.m_recipeListSpace);

            var canCraft = Player.m_localPlayer.HaveRequirements(recipe.GetRequirementArray(), false, 1);

            var image = element.transform.Find("icon").GetComponent<Image>();
            image.sprite = recipe.FromItem.GetIcon();
            image.color = canCraft ? Color.white : new Color(0.66f, 0.66f, 0.66f, 1f);

            var bgImage = Object.Instantiate(image, image.transform.parent, true);
            bgImage.name = "MagicItemBG";
            bgImage.transform.SetSiblingIndex(image.transform.GetSiblingIndex());
            bgImage.sprite = EpicLoot.Assets.GenericItemBgSprite;
            bgImage.color = EpicLoot.GetRarityColorARGB(recipe.ToRarity);
            if (!canCraft)
            {
                bgImage.color -= new Color(0, 0, 0, 0.66f);
            }

            var nameText = element.transform.Find("name").GetComponent<Text>();
            nameText.text = Localization.instance.Localize(recipe.FromItem.m_shared.m_name);
            nameText.color = canCraft ? EpicLoot.GetRarityColorARGB(recipe.ToRarity) : new Color(0.66f, 0.66f, 0.66f, 1f);

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

        private static bool InEnchantTab()
        {
            return TabEnchant != null && !TabEnchant.interactable;
        }

        public static void OnEnchatTabPressed()
        {
            var instance = InventoryGui.instance;
            instance.m_tabCraft.interactable = true;
            instance.m_tabUpgrade.interactable = true;
            TabEnchant.interactable = false;
            if (Disenchant.TabDisenchant != null)
            {
                Disenchant.TabDisenchant.interactable = true;
            }

            GenerateEnchantRecipes();
            instance.UpdateCraftingPanel();
        }

        public static void GenerateEnchantRecipes()
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

        private static void GenerateEnchantRecipesForItem(ItemDrop.ItemData item)
        {
            if (!item.IsMagic() && EpicLoot.CanBeMagicItem(item))
            {
                foreach (ItemRarity rarity in Enum.GetValues(typeof(ItemRarity)))
                {
                    if (Player.m_localPlayer.m_knownMaterial.Contains($"{rarity} Runestone"))
                    {
                        var recipe = new EnchantRecipe { FromItem = item.Extended(), ToRarity = rarity };
                        AddEnchantCosts(recipe);
                        Recipes.Add(recipe);
                    }
                }
            }
        }

        private static void AddEnchantCosts(EnchantRecipe recipe)
        {
            const int runestoneCost = 1;
            const int dustCost = 5;
            const int otherCost = 5;

            var rarity = recipe.ToRarity;
            var dustPrefab = ObjectDB.instance.GetItemPrefab($"Dust{rarity}").GetComponent<ItemDrop>();
            var essencePrefab = ObjectDB.instance.GetItemPrefab($"Essence{rarity}").GetComponent<ItemDrop>();
            var reagentPrefab = ObjectDB.instance.GetItemPrefab($"Reagent{rarity}").GetComponent<ItemDrop>();
            var runestonePrefab = ObjectDB.instance.GetItemPrefab($"Runestone{rarity}").GetComponent<ItemDrop>();

            var item = recipe.FromItem;
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Torch:
                case ItemDrop.ItemData.ItemType.Tool:
                    recipe.Cost.Add(new KeyValuePair<ItemDrop, int>(runestonePrefab, runestoneCost));
                    recipe.Cost.Add(new KeyValuePair<ItemDrop, int>(dustPrefab, dustCost));
                    recipe.Cost.Add(new KeyValuePair<ItemDrop, int>(essencePrefab, otherCost));
                    break;
                    
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Helmet:
                case ItemDrop.ItemData.ItemType.Chest:
                case ItemDrop.ItemData.ItemType.Legs:
                case ItemDrop.ItemData.ItemType.Shoulder:
                case ItemDrop.ItemData.ItemType.Utility:
                    recipe.Cost.Add(new KeyValuePair<ItemDrop, int>(runestonePrefab, runestoneCost));
                    recipe.Cost.Add(new KeyValuePair<ItemDrop, int>(dustPrefab, dustCost));
                    recipe.Cost.Add(new KeyValuePair<ItemDrop, int>(reagentPrefab, otherCost));
                    break;
            }
        }
    }
}
