using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using EpicLoot.Crafting;
using EpicLoot_UnityLib;
using UnityEngine;

namespace EpicLoot.CraftingV2
{
    public class EnchantingUIController : MonoBehaviour
    {
        public static void Initialize()
        {
            MultiSelectItemList.SortByRarity = SortByRarity;
            MultiSelectItemList.SortByName = SortByName;
            MultiSelectItemListElement.SetMagicItem = SetMagicItem;
            SacrificeUI.GetSacrificeItems = GetSacrificeItems;
            SacrificeUI.GetSacrificeProducts = GetSacrificeProducts;
            ConvertUI.GetConversionRecipes = GetConversionRecipes;
        }

        private static void SetMagicItem(MultiSelectItemListElement element, ItemDrop.ItemData item, UITooltip tooltip)
        {
            if (element.ItemIcon != null)
                element.ItemIcon.sprite = item.GetIcon();
            if (element.ItemName != null)
                element.ItemName.text = item.GetDecoratedName();

            if (element.MagicBG != null)
            {
                var useMagicBG = item.UseMagicBackground();
                element.MagicBG.enabled = useMagicBG;

                if (useMagicBG)
                    element.MagicBG.color = item.GetRarityColor();
            }

            if (tooltip)
            {
                if (EpicLoot.HasAuga)
                {
                    Auga.API.Tooltip_MakeItemTooltip(element.gameObject, item);
                }
                else
                {
                    tooltip.m_topic = Localization.instance.Localize(item.GetDecoratedName());
                    tooltip.m_text = Localization.instance.Localize(item.GetTooltip());
                }
            }
        }

        private static List<IListElement> SortByRarity(List<IListElement> items)
        {
            return items.OrderBy(x => x.GetItem().HasRarity() ? x.GetItem().GetRarity() : (ItemRarity)(-1)).ThenBy(x => Localization.instance.Localize(x.GetItem().GetDecoratedName())).ToList();
        }

        private static List<IListElement> SortByName(List<IListElement> items)
        {
            return items.OrderBy(x => Localization.instance.Localize(x.GetItem().GetDecoratedName())).ThenByDescending(x => x.GetItem().m_stack).ToList();
        }

        private static List<InventoryItemListElement> GetSacrificeItems()
        {
            var player = Player.m_localPlayer;
            var result = new List<InventoryItemListElement>();

            var inventory = player.GetInventory();
            var boundItems = new List<ItemDrop.ItemData>();
            inventory.GetBoundItems(boundItems);
            foreach (var item in inventory.GetAllItems())
            {
                if (!EpicLoot.ShowEquippedAndHotbarItemsInSacrificeTab.Value)
                {
                    if (item != null && item.m_equiped || boundItems.Contains(item))
                        continue;
                }

                var products = EnchantCostsHelper.GetDisenchantProducts(item);
                if (products != null)
                    result.Add(new InventoryItemListElement() { Item = item });
            }

            return result;
        }

        private static void AddItemToProductSet(MultiValueDictionary<string, ItemDrop.ItemData> productSet, string itemID, int amount)
        {
            if (amount <= 0)
            {
                EpicLoot.LogWarning($"Tried to add item ({itemID}) with zero quantity to sacrifice product");
                return;
            }

            var prefab = ObjectDB.instance.GetItemPrefab(itemID);
            if (prefab == null)
            {
                EpicLoot.LogWarning($"Tried to add unknown item ({itemID}) to sacrifice product");
                return;
            }

            var itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                EpicLoot.LogWarning($"Tried to add object with no ItemDrop ({itemID}) to sacrifice product");
                return;
            }

            if (!productSet.ContainsKey(itemID))
                productSet.Add(itemID, new List<ItemDrop.ItemData>());

            var itemList = productSet.GetValues(itemID);
            var last = itemList.LastOrDefault();
            while (amount > 0)
            {
                var availableSpace = last == null ? 0 : last.m_shared.m_maxStackSize - last.m_stack;
                if (availableSpace == 0)
                {
                    var itemData = itemDrop.m_itemData.Clone();
                    itemData.m_dropPrefab = prefab;
                    itemData.m_stack = 0;
                    availableSpace = itemData.m_shared.m_maxStackSize;
                    itemList.Add(itemData);
                    last = itemData;
                }

                var toAdd = Mathf.Min(amount, availableSpace);
                last.m_stack += toAdd;
                amount -= last.m_stack;
            }
        }

        private static List<InventoryItemListElement> GetSacrificeProducts(List<Tuple<ItemDrop.ItemData, int>> items)
        {
            var productsSet = new MultiValueDictionary<string, ItemDrop.ItemData>();
            foreach (var entry in items)
            {
                var item = entry.Item1;
                var amount = entry.Item2;
                if (amount <= 0)
                    continue;

                var products = EnchantCostsHelper.GetDisenchantProducts(item);
                if (products == null)
                    continue;

                foreach (var itemAmountConfig in products)
                {
                    AddItemToProductSet(productsSet, itemAmountConfig.Item, itemAmountConfig.Amount * amount);
                }
            }

            var productsList = productsSet.Values.SelectMany(x => x).ToList();
            return productsList.OrderByDescending(x => x.HasRarity() ? x.GetRarity() : (ItemRarity)(-1))
                .ThenBy(x => Localization.instance.Localize(x.GetDecoratedName()))
                .Select(x => new InventoryItemListElement() { Item = x })
                .ToList();
        }

        private static List<ConversionRecipeUnity> GetConversionRecipes(int mode)
        {
            var conversionType = (MaterialConversionType)mode;
            var conversions = MaterialConversions.Conversions.GetValues(conversionType, true);

            var player = Player.m_localPlayer;
            var result = new List<ConversionRecipeUnity>();

            var inventory = player.GetInventory();
            var boundItems = new List<ItemDrop.ItemData>();
            inventory.GetBoundItems(boundItems);
            foreach (var item in inventory.GetAllItems())
            {
                if (item == null)
                    continue;

                if (!EpicLoot.ShowEquippedAndHotbarItemsInSacrificeTab.Value)
                {
                    if (item.m_equiped || boundItems.Contains(item))
                        continue;
                }

                var itemName = item.m_dropPrefab.name;
                if (itemName == "Coins")
                    continue;

                var itemIsUsedInConversion = conversions.Where(x => x.Resources.Any(r => r.Item == itemName));
                foreach (var conversion in itemIsUsedInConversion)
                {
                    var prefab = ObjectDB.instance.GetItemPrefab(conversion.Product);
                    if (prefab == null)
                    {
                        EpicLoot.LogWarning($"Could not find conversion product ({conversion.Product})!");
                        continue;
                    }

                    var itemDrop = prefab.GetComponent<ItemDrop>();
                    if (itemDrop == null)
                    {
                        EpicLoot.LogWarning($"Conversion product ({conversion.Product}) is not an ItemDrop!");
                        continue;
                    }

                    var recipe = new ConversionRecipeUnity()
                    {
                        Product = itemDrop.m_itemData.Clone(),
                        Amount = conversion.Amount,
                        Cost = new List<ConversionRecipeCostUnity>()
                    };

                    foreach (var requirement in conversion.Resources)
                    {
                        var reqPrefab = ObjectDB.instance.GetItemPrefab(requirement.Item);
                        if (reqPrefab == null)
                        {
                            EpicLoot.LogWarning($"Could not find conversion requirement ({requirement.Item})!");
                            continue;
                        }

                        var reqItemDrop = reqPrefab.GetComponent<ItemDrop>();
                        if (reqItemDrop == null)
                        {
                            EpicLoot.LogWarning($"Conversion requirement ({requirement.Item}) is not an ItemDrop!");
                            continue;
                        }

                        recipe.Cost.Add(new ConversionRecipeCostUnity
                        {
                            Item = reqItemDrop.m_itemData.Clone(),
                            Amount = requirement.Amount
                        });
                    }
                    
                    result.Add(recipe);
                }  
            }

            return result;
        }
    }
}
