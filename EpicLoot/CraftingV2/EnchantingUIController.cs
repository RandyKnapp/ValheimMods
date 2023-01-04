using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;
using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot_UnityLib;
using UnityEngine;
using UnityEngine.UI;

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
            SetRarityColor.GetRarityColor = GetRarityColor;
            EnchantUI.GetEnchantableItems = GetEnchantableItems;
            EnchantUI.GetEnchantInfo = GetEnchantInfo;
            EnchantUI.GetEnchantCost = GetEnchantCost;
            EnchantUI.EnchantItem = EnchantItemAndReturnSuccessDialog;
            AugmentUI.GetAugmentableItems = GetAugmentableItems;
            AugmentUI.GetAugmentableEffects = GetAugmentableEffects;
            AugmentUI.GetAvailableEffects = GetAvailableAugmentEffects;
            AugmentUI.GetAugmentCost = GetAugmentCost;
            AugmentUI.AugmentItem = AugmentItem;
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
            return items.OrderBy(x => x.GetItem().HasRarity() ? x.GetItem().GetRarity() : (ItemRarity)(-1))
                .ThenBy(x => Localization.instance.Localize(x.GetItem().GetDecoratedName()))
                .ToList();
        }

        private static List<IListElement> SortByName(List<IListElement> items)
        {
            var richTextRegex = new Regex(@"<[^>]*>");
            return items.OrderBy(x => richTextRegex.Replace(Localization.instance.Localize(x.GetItem().GetDecoratedName()), string.Empty))
                .ThenByDescending(x => x.GetItem().m_stack)
                .ToList();
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

        private static Color GetRarityColor(MagicRarityUnity rarity)
        {
            return EpicLoot.GetRarityColorARGB((ItemRarity)rarity);
        }

        private static List<InventoryItemListElement> GetEnchantableItems()
        {
            return Player.m_localPlayer.GetInventory().GetAllItems()
                .Where( item => !item.IsMagic() && EpicLoot.CanBeMagicItem(item))
                .Select( item => new InventoryItemListElement() { Item = item })
                .ToList();
        }

        private static string GetEnchantInfo(ItemDrop.ItemData item, MagicRarityUnity _rarity)
        {
            var rarity = (ItemRarity)_rarity;
            var sb = new StringBuilder();
            var rarityColor = EpicLoot.GetRarityColor(rarity);
            var rarityDisplay = EpicLoot.GetRarityDisplayName(rarity);
            sb.AppendLine($"{item.m_shared.m_name} \u2794 <color={rarityColor}>{rarityDisplay}</color> {item.GetDecoratedName(rarityColor)}");
            sb.AppendLine($"<color={rarityColor}>");

            var effectCountWeights = LootRoller.GetEffectCountsPerRarity(rarity);
            float totalWeight = effectCountWeights.Sum(x => x.Value);
            foreach (var effectCountEntry in effectCountWeights)
            {
                var count = effectCountEntry.Key;
                var weight = effectCountEntry.Value;
                var percent = (int)(weight / totalWeight * 100.0f);
                var label = count == 1 ? $"{count} $mod_epicloot_enchant_effect" : $"{count} $mod_epicloot_enchant_effects";
                sb.AppendLine($"‣ {label} {percent}%");
            }
            sb.Append("</color>");

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(Localization.instance.Localize("$mod_epicloot_augment_availableeffects"));
            sb.AppendLine($"<color={rarityColor}>");

            var tempMagicItem = new MagicItem() { Rarity = rarity };
            var availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(item.Extended(), tempMagicItem);
            
            foreach (var effectDef in availableEffects)
            {
                var values = effectDef.GetValuesForRarity(rarity);
                var valueDisplay = values != null ? Mathf.Approximately(values.MinValue, values.MaxValue) ? $"{values.MinValue}" : $"({values.MinValue}-{values.MaxValue})" : "";
                sb.AppendLine($"‣ {string.Format(Localization.instance.Localize(effectDef.DisplayText), valueDisplay)}");
            }

            sb.Append("</color>");

            return Localization.instance.Localize(sb.ToString());
        }

        private static List<InventoryItemListElement> GetEnchantCost(ItemDrop.ItemData item, MagicRarityUnity _rarity)
        {
            return EnchantTabController.GetEnchantCosts(item, (ItemRarity)_rarity).Select(entry =>
            {
                var i = entry.Key.m_itemData.Clone();
                item.m_stack = entry.Value;
                return new InventoryItemListElement() { Item = i };
            }).ToList();
        }

        private static GameObject EnchantItemAndReturnSuccessDialog(ItemDrop.ItemData item, MagicRarityUnity rarity)
        {
            var player = Player.m_localPlayer;
  
            float previousDurabilityPercent = 0;
            if (item.m_shared.m_useDurability)
                previousDurabilityPercent = item.m_durability / item.GetMaxDurability();

            var luckFactor = player.GetTotalActiveMagicEffectValue(MagicEffectType.Luck, 0.01f);
            var magicItemComponent = item.Data().Add<MagicItemComponent>();
            var magicItem = LootRoller.RollMagicItem((ItemRarity)rarity, item.Extended(), luckFactor);
            magicItemComponent?.SetMagicItem(magicItem);

            EquipmentEffectCache.Reset(player);

            // Maintain durability
            if (item.m_shared.m_useDurability)
                item.m_durability = previousDurabilityPercent * item.GetMaxDurability();

            CraftSuccessDialog successDialog;
            if (EpicLoot.HasAuga)
            {
                var resultsPanel = Auga.API.Workbench_CreateNewResultsPanel();
                resultsPanel.transform.SetParent(EnchantingTableUI.instance.transform);
                resultsPanel.SetActive(false);
                successDialog = resultsPanel.gameObject.AddComponent<CraftSuccessDialog>();
                successDialog.NameText = successDialog.transform.Find("Topic").GetComponent<Text>();
            }
            else
            {
                successDialog = CraftSuccessDialog.Create(EnchantingTableUI.instance.transform);
            }
            successDialog.Show(item.Extended());

            var rt = (RectTransform)successDialog.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 0);

            if (!EpicLoot.HasAuga)
            {
                var frame = successDialog.transform.Find("Frame");
                if (frame != null)
                {
                    var frameRT = (RectTransform)frame;
                    frameRT.pivot = new Vector2(0.5f, 0.5f);
                    frameRT.anchorMax = new Vector2(0.5f, 0.5f);
                    frameRT.anchorMin = new Vector2(0.5f, 0.5f);
                    frameRT.anchoredPosition = new Vector2(0, 0);
                }
            }

            MagicItemEffects.Indestructible.MakeItemIndestructible(item);

            Game.instance.GetPlayerProfile().m_playerStats.m_crafts++;
            Gogan.LogEvent("Game", "Enchanted", item.m_shared.m_name, 1);

            return successDialog.gameObject;
        }

        private static List<InventoryItemListElement> GetAugmentableItems()
        {
            return Player.m_localPlayer.GetInventory().GetAllItems()
                .Where(item => item.CanBeAugmented())
                .Select(item => new InventoryItemListElement() { Item = item })
                .ToList();
        }

        private static List<Tuple<string, bool>> GetAugmentableEffects(ItemDrop.ItemData item)
        {
            var result = new List<Tuple<string, bool>>();

            var magicItem = item?.GetMagicItem();
            if (magicItem != null)
            {
                var rarity = magicItem.Rarity;
                var augmentableEffects = magicItem.Effects;

                for (var index = 0; index < augmentableEffects.Count; index++)
                {
                    var augmentableEffect = augmentableEffects[index];
                    var effectDef = MagicItemEffectDefinitions.Get(augmentableEffect.EffectType);
                    var canAugment = effectDef != null && effectDef.CanBeAugmented;

                    var text = AugmentTabController.GetAugmentSelectorText(magicItem, index, augmentableEffects, rarity);
                    var color = EpicLoot.GetRarityColor(rarity);
                    var alpha = canAugment ? "FF" : "7F";
                    text = $"<color={color}{alpha}>{text}</color>";

                    result.Add(new Tuple<string, bool>(text, canAugment));
                }
            }

            return result;
        }

        private static string GetAvailableAugmentEffects(ItemDrop.ItemData item, int augmentindex)
        {
            var magicItem = item?.GetMagicItem();
            if (magicItem == null)
                return string.Empty;

            var rarity = magicItem.Rarity;
            var rarityColor = EpicLoot.GetRarityColor(rarity);

            var valuelessEffect = false;
            if (augmentindex >= 0 && augmentindex < magicItem.Effects.Count)
            {
                var currentEffectDef = MagicItemEffectDefinitions.Get(magicItem.Effects[augmentindex].EffectType);
                valuelessEffect = currentEffectDef.GetValuesForRarity(rarity) == null;
            }

            var availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(item.Extended(), item.GetMagicItem(), valuelessEffect ? -1 : augmentindex);
            
            var sb = new StringBuilder();
            sb.Append($"<color={rarityColor}>");
            foreach (var effectDef in availableEffects)
            {
                var values = effectDef.GetValuesForRarity(item.GetRarity());
                var valueDisplay = values != null ? Mathf.Approximately(values.MinValue, values.MaxValue) ? $"{values.MinValue}" : $"({values.MinValue}-{values.MaxValue})" : "";
                sb.AppendLine($"‣ {string.Format(Localization.instance.Localize(effectDef.DisplayText), valueDisplay)}");
            }
            sb.Append("</color>");

            return sb.ToString();
        }

        private static List<InventoryItemListElement> GetAugmentCost(ItemDrop.ItemData item, int augmentindex)
        {
            return AugmentTabController.GetAugmentCosts(item, augmentindex)
                .Select(x =>
                {
                    var itemData = x.Key.m_itemData.Clone();
                    itemData.m_dropPrefab = x.Key.gameObject;
                    itemData.m_stack = x.Value;
                    return new InventoryItemListElement() { Item = itemData };
                }).ToList();
        }

        private static GameObject AugmentItem(ItemDrop.ItemData item, int augmentindex)
        {
            // Set as augmented
            var magicItem = item?.GetMagicItem();
            if (magicItem == null)
                return null;

            magicItem.SetEffectAsAugmented(augmentindex);
            item.SaveMagicItem(magicItem);

            var choiceDialog = AugmentTabController.CreateAugmentChoiceDialog();
            choiceDialog.transform.SetParent(EnchantingTableUI.instance.transform);

            var rt = (RectTransform)choiceDialog.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 0);

            if (!EpicLoot.HasAuga)
            {
                var frame = choiceDialog.transform.Find("Frame");
                if (frame != null)
                {
                    var frameRT = (RectTransform)frame;
                    frameRT.pivot = new Vector2(0.5f, 0.5f);
                    frameRT.anchorMax = new Vector2(0.5f, 0.5f);
                    frameRT.anchorMin = new Vector2(0.5f, 0.5f);
                    frameRT.anchoredPosition = new Vector2(0, 0);
                }
            }

            choiceDialog.Show(item, augmentindex, OnAugmentComplete);
            return choiceDialog.gameObject;
        }

        private static void OnAugmentComplete(ItemDrop.ItemData item, int effectIndex, MagicItemEffect newEffect)
        {
            var magicItem = item?.GetMagicItem();
            if (magicItem == null)
                return;

            if (magicItem.HasEffect(MagicEffectType.Indestructible))
            {
                item.m_shared.m_useDurability = item.m_dropPrefab?.GetComponent<ItemDrop>().m_itemData.m_shared.m_useDurability ?? false;

                if (item.m_shared.m_useDurability)
                {
                    item.m_durability = item.GetMaxDurability();
                }
            }

            var oldEffects = magicItem.GetEffects();
            var oldEffect = (effectIndex >= 0 && effectIndex < oldEffects.Count) ? oldEffects[effectIndex] : null;
            EpicLoot.LogWarning($"oldEffect: ({effectIndex}) {oldEffect?.EffectType} {oldEffect?.EffectValue}");

            magicItem.ReplaceEffect(effectIndex, newEffect);

            // Don't count this free augment as locking in an augment
            if (oldEffect != null && EnchantCostsHelper.EffectIsDeprecated(oldEffect.EffectType))
            {
                EpicLoot.LogWarning("Unaugmenting effect");
                EpicLoot.LogWarning($"Augmented indices before: {(string.Join(",", magicItem.AugmentedEffectIndices))}");
                magicItem.AugmentedEffectIndices.Remove(effectIndex);
                EpicLoot.LogWarning($"Augmented indices after: {(string.Join(",", magicItem.AugmentedEffectIndices))}");
            }

            if (magicItem.Rarity == ItemRarity.Rare)
            {
                magicItem.DisplayName = MagicItemNames.GetNameForItem(item, magicItem);
            }

            item.SaveMagicItem(magicItem);

            MagicItemEffects.Indestructible.MakeItemIndestructible(item);

            Game.instance.GetPlayerProfile().m_playerStats.m_crafts++;
            Gogan.LogEvent("Game", "Augmented", item.m_shared.m_name, 1);

            EquipmentEffectCache.Reset(Player.m_localPlayer);
        }
    }
}
