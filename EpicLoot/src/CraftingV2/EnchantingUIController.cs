
using EpicLoot.Config;
using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.GatedItemType;
using EpicLoot.ShardStones;
using EpicLoot_UnityLib;
using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EpicLoot.CraftingV2
{
    [Flags]
    public enum EnchantingTabs : uint
    {
        None = 0,
        Sacrifice = 1 << 0,
        ConvertMaterials = 1 << 1,
        Enchant = 1 << 2,
        Augment = 1 << 3,
        Disenchant = 1 << 4,
        //Helheim = 1 << 5,
        Upgrade = 1 << 6,
        Rune = 1 << 7
    }

    public enum RuneActions
    {
        Extract,
        Etch
    }

    public class EnchantingUIController : MonoBehaviour
    {
        internal static float GetAudioLevel() {
            return AudioMan.GetSFXVolume() * ELConfig.UIAudioVolumeAdjustment.Value;
        }

        internal static bool UpgradesActive(EnchantingFeature feature, out bool featureActive)
        {
            EnchantingTabs tabEnum = EnchantingTabs.None;

            switch (feature)
            {
                case EnchantingFeature.Augment:
                    tabEnum = EnchantingTabs.Augment;
                    break;
                case EnchantingFeature.Enchant:
                    tabEnum = EnchantingTabs.Enchant;
                    break;
                case EnchantingFeature.Disenchant:
                    tabEnum = EnchantingTabs.Disenchant;
                    break;
                case EnchantingFeature.ConvertMaterials:
                    tabEnum = EnchantingTabs.ConvertMaterials;
                    break;
                case EnchantingFeature.Sacrifice:
                    tabEnum = EnchantingTabs.Sacrifice;
                    break;
                case EnchantingFeature.Rune:
                    tabEnum = EnchantingTabs.Rune;
                    break;
            }

            featureActive = (tabEnum & ELConfig.EnchantingTableActivatedTabs.Value) != 0;
            return ELConfig.EnchantingTableUpgradesActive.Value;
        }

        internal static void TabActivation(EnchantingTableUI ui)
        {
            if (ui == null || ui.TabHandler == null)
            {
                return;
            }

            for (int i = 0; i < ui.TabHandler.transform.childCount; i++)
            {
                GameObject tabGo = ui.TabHandler.transform.GetChild(i).gameObject;
                Enum.TryParse(tabGo.name, out EnchantingTabs selectTab);

                switch (selectTab)
                {
                    case EnchantingTabs.Upgrade:
                        tabGo.SetActive(ELConfig.EnchantingTableUpgradesActive.Value);
                        break;
                    case EnchantingTabs.None:
                        break;
                    default:
                        tabGo.SetActive((ELConfig.EnchantingTableActivatedTabs.Value & selectTab) != 0);
                        break;
                }
            }
        }

        internal static void MakeFeatureUnlockTooltip(GameObject obj)
        {
            // EpicLoot.Log($"Setting up tooltip for {obj.name}");
            if (EpicLoot.HasAuga)
            {
                //Auga.API.Tooltip_MakeSimpleTooltip(obj);
            }
            else
            {
                UITooltip uiTooltip = obj.GetComponent<UITooltip>();
                uiTooltip.m_tooltipPrefab = InventoryGui.instance.m_playerGrid.m_elementPrefab
                    .GetComponent<UITooltip>().m_tooltipPrefab;
            }
        }

        internal static void SetMagicItem(MultiSelectItemListElement element, ItemDrop.ItemData item, UITooltip tooltip)
        {
            if (element.ItemIcon != null)
            {
                element.ItemIcon.sprite = item.GetIcon();
            }

            if (element.ItemName != null)
            {
                element.ItemName.text = item.GetDecoratedName();
            }

            if (element.MagicBG != null)
            {
                bool useMagicBG = item.UseMagicBackground();
                element.MagicBG.enabled = useMagicBG;

                if (useMagicBG)
                {
                    element.MagicBG.color = item.GetRarityColor();
                }
            }

            if (tooltip)
            {
                if (EpicLoot.HasAuga)
                {
                    //Auga.API.Tooltip_MakeItemTooltip(element.gameObject, item);
                }
                else
                {
                    tooltip.m_topic = Localization.instance.Localize(item.GetDecoratedName());
                    tooltip.m_text = Localization.instance.Localize(item.GetTooltip());
                }
            }
        }

        internal static void SetItemTooltip(ItemDrop.ItemData item,
            UITooltip tooltip)
        {
            if (EpicLoot.IsAllowedMagicItemType(item))
            {
                tooltip.Set(item.GetDisplayName(), item.GetTooltip());
            }
            else
            {
                tooltip.Set("", "");
            }
        }

        internal static List<IListElement> SortByRarity(List<IListElement> items)
        {
            return items.OrderBy(x => x.GetItem().HasRarity() ? x.GetItem().GetRarity() : (ItemRarity)(-1))
                .ThenBy(x => Localization.instance.Localize(x.GetItem().GetDecoratedName()))
                .ToList();
        }

        internal static List<IListElement> SortByName(List<IListElement> items)
        {
            Regex richTextRegex = new Regex(@"<[^>]*>");
            return items.OrderBy(x => richTextRegex.Replace(Localization.instance.Localize(
                x.GetItem().GetDecoratedName()), string.Empty))
                .ThenByDescending(x => x.GetItem().m_stack)
                .ToList();
        }

        internal static List<InventoryItemListElement> GetSacrificeItems()
        {
            Player player = Player.m_localPlayer;
            List<InventoryItemListElement> result = new List<InventoryItemListElement>();

            List<ItemDrop.ItemData> boundItems = InventoryManagement.Instance.GetBoundItems();
            List<ItemDrop.ItemData> items = InventoryManagement.Instance.GetAllItems();
            if (items != null)
            {
                foreach (ItemDrop.ItemData item in items)
                {
                    if (!ELConfig.ShowEquippedAndHotbarItemsInSacrificeTab.Value &&
                        (item != null && item.m_equipped || boundItems.Contains(item)))
                    {
                        continue;
                    }

                    List<ItemAmountConfig> products = EnchantCostsHelper.GetSacrificeProducts(item);
                    if (products != null)
                    {
                        result.Add(new InventoryItemListElement() { Item = item });
                    }
                }
            }

            return result;
        }

        private static void AddItemToProductSet(Dictionary<string, ItemDrop.ItemData> productSet, string itemID, int amount)
        {
            if (amount <= 0)
            {
                EpicLoot.LogWarning($"Tried to add item ({itemID}) with zero quantity to sacrifice product");
                return;
            }

            GameObject prefab = ObjectDB.instance.GetItemPrefab(itemID);
            if (prefab == null)
            {
                EpicLoot.LogWarning($"Tried to add unknown item ({itemID}) to sacrifice product");
                return;
            }

            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                EpicLoot.LogWarning($"Tried to add object with no ItemDrop ({itemID}) to sacrifice product");
                return;
            }

            ItemDrop.ItemData itemData;
            if (productSet.TryGetValue(itemID, out itemData))
            {
                itemData.m_stack += amount;
            }
            else
            {
                itemData = itemDrop.m_itemData.Clone();
                itemData.m_dropPrefab = prefab;
                itemData.m_stack = amount;
                // Add MagicItemComponent or products will not stack until reloaded.
                MagicItemComponent magicItem = itemData.Data().GetOrCreate<MagicItemComponent>();
                itemData.SaveMagicItem(magicItem.MagicItem);
                productSet.Add(itemID, itemData);
            }
        }

        internal static List<InventoryItemListElement> GetSacrificeProducts(List<Tuple<ItemDrop.ItemData, int>> items)
        {
            Dictionary<string, ItemDrop.ItemData> productsSet = new Dictionary<string, ItemDrop.ItemData>();
            foreach (Tuple<ItemDrop.ItemData, int> entry in items)
            {
                ItemDrop.ItemData item = entry.Item1;
                int amount = entry.Item2;
                if (amount <= 0)
                {
                    continue;
                }

                List<ItemAmountConfig> products = EnchantCostsHelper.GetSacrificeProducts(item);
                if (products == null)
                {
                    continue;
                }

                foreach (ItemAmountConfig itemAmountConfig in products)
                {
                    AddItemToProductSet(productsSet, itemAmountConfig.Item, itemAmountConfig.Amount * amount);
                }
            }

            return productsSet.Values.OrderByDescending(x => x.HasRarity() ? x.GetRarity() : (ItemRarity)(-1))
                .ThenBy(x => Localization.instance.Localize(x.GetDecoratedName()))
                .Select(x => new InventoryItemListElement() { Item = x })
                .ToList();
        }

        internal static List<ConversionRecipeUnity> GetConversionRecipes(MaterialConversionType mode)
        {
            MaterialConversionType conversionType = mode;
            List<MaterialConversion> conversions = MaterialConversions.Conversions.GetValues(conversionType, true);

            Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(
                EnchantingFeature.ConvertMaterials);
            float materialConversionAmount = float.IsNaN(featureValues.Item1) ? -1 : featureValues.Item1;
            float runestoneConversionAmount = float.IsNaN(featureValues.Item2) ? -1 : featureValues.Item2;

            List<ConversionRecipeUnity> result = new List<ConversionRecipeUnity>();

            foreach (MaterialConversion conversion in conversions)
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(conversion.Product);
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Could not find conversion product ({conversion.Product})!");
                    continue;
                }

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    EpicLoot.LogWarning($"Conversion product ({conversion.Product}) is not an ItemDrop!");
                    continue;
                }

                itemDrop.m_itemData.m_dropPrefab = prefab;

                // The product is a full per-rarity prefab (e.g. {color}_{rarity}_ShardStone) whose baked
                // metadata already carries the correct rarity, so the clone needs no quality stamping.
                ConversionRecipeUnity recipe = new ConversionRecipeUnity()
                {
                    Product = itemDrop.m_itemData.Clone(),
                    Amount = conversion.Amount,
                    Cost = new List<ConversionRecipeCostUnity>()
                };

                bool hasSomeItems = false;
                foreach (MaterialConversionRequirement requirement in conversion.Resources)
                {
                    GameObject reqPrefab = ObjectDB.instance.GetItemPrefab(requirement.Item);
                    if (reqPrefab == null)
                    {
                        EpicLoot.LogWarning($"Could not find conversion requirement ({requirement.Item})!");
                        continue;
                    }

                    ItemDrop reqItemDrop = reqPrefab.GetComponent<ItemDrop>();
                    if (reqItemDrop == null)
                    {
                        EpicLoot.LogWarning($"Conversion requirement ({requirement.Item}) is not an ItemDrop!");
                        continue;
                    }

                    reqItemDrop.m_itemData.m_dropPrefab = reqPrefab;

                    int requiredAmount = requirement.Amount;
                    if (runestoneConversionAmount > 0 && conversion.Type == MaterialConversionType.Upgrade &&
                        recipe.Product.IsRunestone() && reqItemDrop.m_itemData.IsRunestone())
                    {
                        requiredAmount = Mathf.CeilToInt(runestoneConversionAmount * recipe.Amount);
                    }
                    else if (materialConversionAmount > 0 && conversion.Type == MaterialConversionType.Upgrade &&
                        recipe.Product.IsMagicCraftingMaterial() && reqItemDrop.m_itemData.IsMagicCraftingMaterial())
                    {
                        requiredAmount = Mathf.CeilToInt(materialConversionAmount * recipe.Amount);
                    }

                    // A rarity-specific requirement (e.g. the "From" shard) is its own full per-rarity
                    // prefab, so the clone already carries the right rarity/name -- no quality stamping.
                    ItemDrop.ItemData costItem = reqItemDrop.m_itemData.Clone();

                    recipe.Cost.Add(new ConversionRecipeCostUnity
                    {
                        Item = costItem,
                        Amount = requiredAmount
                    });

                    if (InventoryManagement.Instance.CountItem(costItem) > 0)
                    {
                        hasSomeItems = true;
                    }
                }

                if (hasSomeItems)
                {
                    result.Add(recipe);
                }
            }

            return result;
        }

        internal static Color GetRarityColor(ItemRarity rarity)
        {
            return EpicLoot.GetRarityColorARGB(rarity);
        }

        internal static List<InventoryItemListElement> GetEnchantableItems()
        {
            return InventoryManagement.Instance.GetAllItems()
                .Where(item => !item.IsMagic() && EpicLoot.CanBeMagicItem(item))
                .Select(item => new InventoryItemListElement() { Item = item })
                .ToList();
        }

        // Builds the "x% " prefix shown after the bullet for an available effect, representing the
        // weighted chance that effect is chosen on a single roll (SelectionWeight / total). Returns
        // empty when the display is disabled or the pool has no weight, leaving the line unchanged.
        private static string GetSelectionChancePrefix(float selectionWeight, float totalSelectionWeight)
        {
            if (!ELConfig.ShowEnchantSelectionChance.Value || totalSelectionWeight <= 0f)
            {
                return string.Empty;
            }

            float chance = selectionWeight / totalSelectionWeight * 100f;
            return $"{chance:0.#}% ";
        }

        internal static string GetEnchantInfo(ItemDrop.ItemData item, ItemRarity _rarity)
        {
            ItemRarity rarity = _rarity;
            StringBuilder sb = new StringBuilder();
            string rarityColor = EpicLoot.GetRarityColor(rarity);
            string rarityDisplay = EpicLoot.GetRarityDisplayName(rarity);
            sb.AppendLine($"{item.m_shared.m_name} \u2794 <color={rarityColor}>{rarityDisplay}</color> " +
                $"{item.GetDecoratedName(rarityColor)}");
            sb.AppendLine($"<color={rarityColor}>");

            Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Enchant);
            float highValueBonus = float.IsNaN(featureValues.Item1) ? 0 : featureValues.Item1;
            float midValueBonus = float.IsNaN(featureValues.Item2) ? 0 : featureValues.Item2;

            List<KeyValuePair<int, float>> effectCountWeights = LootRoller.GetEffectCountsPerRarity(rarity, true);
            float totalWeight = effectCountWeights.Sum(x => x.Value);
            for (int index = 0; index < effectCountWeights.Count; index++)
            {
                KeyValuePair<int, float> effectCountEntry = effectCountWeights[index];
                int count = effectCountEntry.Key;
                float weight = effectCountEntry.Value;
                int percent = (int)(weight / totalWeight * 100.0f);
                string label = count == 1 ? $"{count} $mod_epicloot_enchant_effect" : $"{count} $mod_epicloot_enchant_effects";

                if (index == effectCountWeights.Count - 1 && highValueBonus > 0)
                    sb.AppendLine($"‣ {label} {percent}% <color=#EAA800>(+{highValueBonus}% $mod_epicloot_bonus)</color>");
                else if (index == effectCountWeights.Count - 2 && midValueBonus > 0)
                    sb.AppendLine($"‣ {label} {percent}% <color=#EAA800>(+{midValueBonus}% $mod_epicloot_bonus)</color>");
                else
                    sb.AppendLine($"‣ {label} {percent}%");
            }

            // Socket odds come from the same roll, but enchanting upgrades never affect them, so there
            // are no bonus lines here. The 0-socket entry is implied by the others and left out.
            List<KeyValuePair<int, float>> socketCountWeights = LootRoller.GetSocketCountsPerRarity(rarity);
            float totalSocketWeight = socketCountWeights.Sum(x => x.Value);
            foreach (KeyValuePair<int, float> socketCountEntry in socketCountWeights)
            {
                int count = socketCountEntry.Key;
                if (count <= 0 || totalSocketWeight <= 0)
                    continue;

                int percent = (int)(socketCountEntry.Value / totalSocketWeight * 100.0f);
                string label = count == 1 ? $"{count} $mod_epicloot_enchant_socket" : $"{count} $mod_epicloot_enchant_sockets";
                sb.AppendLine($"‣ {label} {percent}%");
            }

            sb.Append("</color>");

            sb.AppendLine();
            sb.AppendLine();
            sb.AppendLine(Localization.instance.Localize("$mod_epicloot_augment_availableeffects"));
            sb.AppendLine($"<color={rarityColor}>");

            MagicItem tempMagicItem = new MagicItem() { Rarity = rarity };
            List<MagicItemEffectDefinition> availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(item, tempMagicItem);
            float totalSelectionWeight = availableEffects.Sum(x => x.SelectionWeight);

            foreach (MagicItemEffectDefinition effectDef in availableEffects)
            {
                MagicItemEffectDefinition.ValueDef values = effectDef.GetValuesForRarity(rarity);
                string chancePrefix = GetSelectionChancePrefix(effectDef.SelectionWeight, totalSelectionWeight);
                sb.AppendLine($"‣ {chancePrefix}{MagicItem.GetEffectTextRange(effectDef, values)}");
            }

            sb.Append("</color>");

            return Localization.instance.Localize(sb.ToString());
        }

        internal static List<InventoryItemListElement> GetEnchantCost(ItemDrop.ItemData item, ItemRarity _rarity)
        {
            return EnchantHelper.GetEnchantCosts(item, _rarity).Select(entry =>
            {
                ItemDrop.ItemData itemData = entry.Key.m_itemData.Clone();
                itemData.m_dropPrefab = entry.Key.gameObject;
                itemData.m_stack = entry.Value;
                return new InventoryItemListElement() { Item = itemData };
            }).ToList();
        }

        internal static GameObject EnchantItemAndReturnSuccessDialog(ItemDrop.ItemData item, ItemRarity rarity)
        {
            Player player = Player.m_localPlayer;

            float previousDurabilityPercent = 0;
            if (item.m_shared.m_useDurability)
            {
                previousDurabilityPercent = item.m_durability / item.GetMaxDurability();
            }

            float luckFactor = player.GetTotalActiveMagicEffectValue(MagicEffectType.Luck, 0.01f);
            MagicItem magicItem = LootRoller.RollMagicItem(rarity, item, luckFactor);

            MagicItemComponent magicItemComponent = item.Data().GetOrCreate<MagicItemComponent>();
            API.WithChangeReason(API.ChangeReason.Enchant, () => magicItemComponent.SetMagicItem(magicItem));

            EquipmentEffectCache.Reset(player);

            // Maintain durability
            if (item.m_shared.m_useDurability)
            {
                item.m_durability = previousDurabilityPercent * item.GetMaxDurability();
            }

            CraftSuccessDialog successDialog;
            //if (EpicLoot.HasAuga)
            //{
            //    //var resultsPanel = Auga.API.Workbench_CreateNewResultsPanel();
            //    //resultsPanel.transform.SetParent(EnchantingTableUI.instance.transform);
            //    //resultsPanel.SetActive(false);
            //    //successDialog = resultsPanel.gameObject.AddComponent<CraftSuccessDialog>();
            //    //successDialog.NameText = successDialog.transform.Find("Topic").GetComponent<TMP_Text>();
            //}
            //else
            //{

            //}
            successDialog = CraftSuccessDialog.Create(EnchantingTableUI.instance.transform);

            successDialog.Show(item.Extended());

            RectTransform rt = (RectTransform)successDialog.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 0);

            if (!EpicLoot.HasAuga)
            {
                Transform frame = successDialog.transform.Find("Frame");
                if (frame != null)
                {
                    RectTransform frameRT = (RectTransform)frame;
                    frameRT.pivot = new Vector2(0.5f, 0.5f);
                    frameRT.anchorMax = new Vector2(0.5f, 0.5f);
                    frameRT.anchorMin = new Vector2(0.5f, 0.5f);
                    frameRT.anchoredPosition = new Vector2(0, 0);
                }
            }

            Game.instance.GetPlayerProfile().m_playerStats.m_stats[PlayerStatType.Crafts]++;
            Gogan.LogEvent("Game", "Enchanted", item.m_shared.m_name, 1);

            return successDialog.gameObject;
        }

        private static IdentifyTypeConfig SelectLootIdentifyDetails(string filter)
        {
            if (EnchantCostsHelper.Config.IdentifyTypes.Count == 0)
            {
                EpicLoot.LogWarning("IdentifyTypes is not defined in the configurations! Cannot select loot.");
                return null;
            }

            foreach (KeyValuePair<string, IdentifyTypeConfig> identifyStyle in EnchantCostsHelper.Config.IdentifyTypes)
            {
                if (Localization.instance.Localize(identifyStyle.Value.Localization) == filter)
                {
                    return identifyStyle.Value;
                }
            }

            return EnchantCostsHelper.Config.IdentifyTypes.First().Value;
        }

        private static List<LootTable> GetLootTablesForIdentifyStyle(IdentifyTypeConfig cfg, Heightmap.Biome biome)
        {
            EpicLoot.Log($"Getting loot tables for identify style " +
                $"{Localization.instance.Localize(cfg.Localization)} in biome {biome} " +
                $"cfg keys: { string.Join(",", cfg.BiomeLootLists.Keys) }");
            Heightmap.Biome allowedBiome = GatedItemTypeHelper.GetCurrentOrLowerBiomeByDefeatedBossSettings(
                biome, EpicLoot.GetGatedItemTypeMode());

            List<LootTable> lootTables = new List<LootTable>() { };

            if (!cfg.BiomeLootLists.ContainsKey(allowedBiome))
            {
                // Fallback to the first defined biome loot list if the biome cannot be found.
                // This should be set to none in the user configurations for best results.
                allowedBiome = cfg.BiomeLootLists.First().Key;
            }

            foreach (string lootSetName in cfg.BiomeLootLists[allowedBiome])
            {
                EpicLoot.Log($" - Checking loot set {lootSetName}");
                List<LootTable> lootTable = LootRoller.GetFullyResolvedLootTable(lootSetName);
                if (lootTable != null)
                {
                    lootTables.AddRange(lootTable);
                }
            }

            EpicLoot.Log($"Loot tables for {Localization.instance.Localize(cfg.Localization)} {lootTables.Count}");
            return lootTables;
        }

        internal static List<InventoryItemListElement> LootRollSelectedItems(
            string filter, List<Tuple<ItemDrop.ItemData, int>> items, float powerModifier)
        {
            IdentifyTypeConfig category = SelectLootIdentifyDetails(filter);

            if (category == null)
            {
                return new List<InventoryItemListElement>();
            }

            Player player = Player.m_localPlayer;
            List<ItemDrop.ItemData> totalRolledItems = new List<ItemDrop.ItemData>();
            foreach (Tuple<ItemDrop.ItemData, int> itemstack in items)
            {
                Heightmap.Biome biome = EnchantHelper.GetBiomeFromUnidentifiedItem(itemstack.Item1);
                List<LootTable> selectedLootTables = GetLootTablesForIdentifyStyle(category, biome);
                List<ItemDrop.ItemData> rolledItems = LootRoller.RollLootNoTableWithSpecifics(
                    player.transform.position, selectedLootTables, itemstack.Item2, itemstack.Item1.GetRarity(), true, 2, powerModifier);
                InventoryManagement.Instance.RemoveExactItem(itemstack.Item1, itemstack.Item2);
                totalRolledItems.AddRange(rolledItems);

                foreach (ItemDrop.ItemData item in rolledItems)
                {
                    InventoryManagement.Instance.GiveItem(item);
                }
            }
            
            EquipmentEffectCache.Reset(player);
            return totalRolledItems.Select(item => new InventoryItemListElement() { Item = item }).ToList();
        }

        internal static List<InventoryItemListElement> GetPotentialItemRollsByCategory(string filter, List<ItemDrop.ItemData> itemsSelected)
        {
            IdentifyTypeConfig category = SelectLootIdentifyDetails(filter);
            List<string> resultItemNames = new List<string>();

            if (category == null || Player.m_localPlayer == null)
            {
                return new List<InventoryItemListElement>();
            }

            List<Heightmap.Biome> biomesCovered = new List<Heightmap.Biome> { };

            foreach (ItemDrop.ItemData item in itemsSelected)
            {
                if (item == null || item.m_dropPrefab == null)
                {
                    continue;
                }
                
                Heightmap.Biome biome = EnchantHelper.GetBiomeFromUnidentifiedItem(item);
                if (biomesCovered.Contains(biome))
                {
                    continue;
                }

                List<LootTable> selectedLootTables = GetLootTablesForIdentifyStyle(category, biome);
                biomesCovered.Add(biome);
                Dictionary<string, float> itemChances = LootRoller
                    .GetLootTableChances(Player.m_localPlayer.transform.position, selectedLootTables);
                foreach (KeyValuePair<string, float> entry in itemChances)
                {
                    if (!resultItemNames.Contains(entry.Key))
                    {
                        resultItemNames.Add(entry.Key);
                    }
                }
            }

            List<InventoryItemListElement> result = new List<InventoryItemListElement>();

            foreach (string item in resultItemNames)
            {
                ObjectDB.instance.TryGetItemPrefab(item, out GameObject founditem);
                if (founditem == null)
                {
                    continue;
                }

                result.Add(new InventoryItemListElement()
                {
                    Item = founditem.GetComponent<ItemDrop>().m_itemData
                });
            }

            return result;
        }

        internal static Dictionary<string, string> GetIdentifyStyles()
        {
            return EnchantCostsHelper.GetIdentificationCategories();
        }

        internal static List<InventoryItemListElement> GetIdentifyCostForCategory(
            string filter, List<Tuple<ItemDrop.ItemData, int>> items, float costModifier = 1.0f)
        {
            if (items == null || items.Count == 0)
            {
                return new List<InventoryItemListElement>();
            }

            // Find category key from localized filter
            string categoryKey = null;
            foreach (KeyValuePair<string, IdentifyTypeConfig> identifyStyle in EnchantCostsHelper.Config.IdentifyTypes)
            {
                if (Localization.instance.Localize(identifyStyle.Value.Localization) == filter)
                {
                    categoryKey = identifyStyle.Key;
                    break;
                }
            }

            if (categoryKey == null)
            {
                // Fall back to first category
                categoryKey = EnchantCostsHelper.Config.IdentifyTypes.FirstOrDefault().Key;
                if (categoryKey == null)
                {
                    EpicLoot.LogWarning("IdentifyTypes is not defined in the configurations!");
                    return new List<InventoryItemListElement>();
                }
            }

            // Aggregate costs from all items based on their biome and rarity
            Dictionary<string, int> aggregatedCosts = new Dictionary<string, int>();

            foreach (Tuple<ItemDrop.ItemData, int> itemTuple in items)
            {
                ItemDrop.ItemData item = itemTuple.Item1;
                int quantity = itemTuple.Item2;

                Heightmap.Biome biome = EnchantHelper.GetBiomeFromUnidentifiedItem(item);
                ItemRarity rarity = item.GetRarity();

                List<ItemAmountConfig> costs = EnchantCostsHelper.GetIdentifyCosts(categoryKey, rarity, biome);

                foreach (ItemAmountConfig costConfig in costs)
                {
                    if (aggregatedCosts.ContainsKey(costConfig.Item))
                    {
                        aggregatedCosts[costConfig.Item] += costConfig.Amount * quantity;
                    }
                    else
                    {
                        aggregatedCosts[costConfig.Item] = costConfig.Amount * quantity;
                    }
                }
            }

            // Convert to InventoryItemListElement
            List<InventoryItemListElement> results = new List<InventoryItemListElement>();

            foreach (KeyValuePair<string, int> costEntry in aggregatedCosts)
            {
                GameObject costGo = PrefabManager.Instance.GetPrefab(costEntry.Key);
                if (costGo == null)
                {
                    EpicLoot.LogWarning($"Could not find identify cost item {costEntry.Key} in ObjectDB");
                    continue;
                }

                ItemDrop id = costGo.GetComponent<ItemDrop>();
                ItemDrop.ItemData itemData = id.m_itemData.Clone();
                itemData.m_dropPrefab = costGo.gameObject;

                int cost = costEntry.Value;
                if (!float.IsNaN(costModifier))
                {
                    cost = Mathf.RoundToInt(cost * costModifier);
                }

                if (cost <= 0)
                {
                    continue;
                }

                itemData.m_stack = cost;
                results.Add(new InventoryItemListElement() { Item = itemData });
            }

            return results;
        }

        internal static List<InventoryItemListElement> GetUnidentifiedItems()
        {
            return InventoryManagement.Instance.GetAllItems()
                .Where(item => item.IsMagic() && item.IsUnidentified())
                .Select(item => new InventoryItemListElement() { Item = item })
                .ToList();
        }

        internal static List<InventoryItemListElement> GetAugmentableItems()
        {
            return InventoryManagement.Instance.GetAllItems()
                .Where(item => item.CanBeAugmented() && item.IsRunestone() == false && !item.IsUnidentified())
                .Select(item => new InventoryItemListElement() { Item = item })
                .ToList();
        }

        internal static ItemRarity GetItemRarity(ItemDrop.ItemData item)
        {
           ItemRarity rarity = item.GetRarity();
            return rarity;
        }

        internal static List<InventoryItemListElement> GetRuneExtractItems()
        {
            return GetRuneModifyableItems(false);
        }

        internal static List<InventoryItemListElement> GetRuneEtchItems()
        {
            return GetRuneModifyableItems(true);
        }

        private static List<InventoryItemListElement> GetRuneModifyableItems(bool allowBound)
        {
            List<InventoryItemListElement> result = new List<InventoryItemListElement>();

            if (Player.m_localPlayer == null)
            {
                return result;
            }

            List<ItemDrop.ItemData> boundItems = InventoryManagement.Instance.GetBoundItems();
            List<ItemDrop.ItemData> items = InventoryManagement.Instance.GetAllItems();

            if (items != null)
            {
                foreach (ItemDrop.ItemData item in items)
                {
                    if (!allowBound && !ELConfig.ShowEquippedAndHotbarItemsInSacrificeTab.Value &&
                        (item != null && item.m_equipped || boundItems.Contains(item)))
                    {
                        continue;
                    }

                    if (item.IsMagic() && item.CanBeRunified() && !item.IsRunestone() && !item.IsUnidentified())
                    {
                        result.Add(new InventoryItemListElement() { Item = item });
                    }
                }
            }

            return result;
        }

        internal static List<InventoryItemListElement> GetApplyableRunesforItem(ItemDrop.ItemData item, string selectedEffect)
        {
            MagicItem magicItem = item.GetMagicItem();
            ItemRarity rarity = magicItem.Rarity;
            List<MagicItemEffect> selectedEnchant = magicItem.GetEffects(selectedEffect);
            int selectedEnchantIndex = magicItem.Effects.FindIndex(x => x.EffectType == selectedEffect);

            // Determine if the effect has values
            EpicLoot.Log($"ME effects: {string.Join(",", magicItem.Effects.Select(e => e.EffectType).ToList())}, " +
                $"selected effect filter {selectedEffect}");

            // Guard clause against not having any target effects selected
            if (selectedEnchant.Count == 0)
            {
                return new List<InventoryItemListElement>() { };
            }

            bool valuelessEffect = false;
            if (magicItem.Effects.Count > 0 && selectedEffect != "")
            {
                MagicItemEffectDefinition currentEffectDef = MagicItemEffectDefinitions.Get(selectedEnchant.First().EffectType);
                valuelessEffect = currentEffectDef.GetValuesForRarity(rarity) == null;
            }

            List<MagicItemEffectDefinition> availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(
                item.Extended(), item.GetMagicItem(), valuelessEffect ? -1 : selectedEnchantIndex, checkruneroll: true);
            List<string> availableEffectNames = availableEffects.Select(x => x.Type).ToList();

            IEnumerable<ItemDrop.ItemData> selectedItems = InventoryManagement.Instance.GetAllItems()
                .Where(item => item.IsMagic() &&
                    item.IsRunestone() &&
                    item.GetMagicItem().Effects.Any(e => availableEffectNames.Contains(e.EffectType)));

            List<InventoryItemListElement> returnList = new List<InventoryItemListElement>();
            foreach(ItemDrop.ItemData entry in selectedItems)
            {
                returnList.Add(new InventoryItemListElement()
                {
                    Item = entry,
                    Effects = entry.GetMagicItem().Effects.Select(c => new Tuple<string, float>(c.EffectType, c.EffectValue)).ToList(),
                    EnchantName = entry.GetMagicItem().GetCompactTooltip()
                });
            }
            
            foreach (InventoryItemListElement entry in returnList)
            {
                EpicLoot.Log($"Rune item {entry.Item.GetDecoratedName()} has effects: {string.Join(",", entry.Effects.Select(e => e.Item1))}");
            }

            return returnList;
        }

        internal static List<InventoryItemListElement> GetRuneExtractCost(ItemDrop.ItemData item, ItemRarity rarity, float costModifier)
        {
            return EnchantHelper.GetRuneCost(item, rarity, RuneActions.Extract).Select(entry =>
            {
                ItemDrop.ItemData itemData = entry.Key.m_itemData.Clone();
                itemData.m_dropPrefab = entry.Key.gameObject;
                int cost = entry.Value;
                if (costModifier != float.NaN)
                {
                    cost = Mathf.RoundToInt(entry.Value * costModifier);
                }

                EpicLoot.Log($"Cost settings: E:{entry.Value} modifier:{costModifier} result:{cost}");
                itemData.m_stack = cost;
                if (itemData.m_stack <= 0)
                {
                    itemData.m_stack = 1;
                }

                return new InventoryItemListElement() { Item = itemData };
            }).ToList();
        }

        internal static List<InventoryItemListElement> GetRuneEtchCost(ItemDrop.ItemData item, ItemRarity rarity, float costModifier)
        {
            return EnchantHelper.GetRuneCost(item, rarity, RuneActions.Etch).Select(entry =>
            {
                ItemDrop.ItemData itemData = entry.Key.m_itemData.Clone();
                itemData.m_dropPrefab = entry.Key.gameObject;
                int cost = entry.Value;
                if (costModifier != float.NaN)
                {
                    cost = Mathf.RoundToInt(entry.Value * costModifier);
                }

                EpicLoot.Log($"Cost settings: E:{entry.Value} modifier:{costModifier} result:{cost}");
                itemData.m_stack = cost;
                if (itemData.m_stack <= 0)
                {
                    itemData.m_stack = 1;
                }

                return new InventoryItemListElement() { Item = itemData };
            }).ToList();
        }

        internal static ItemDrop.ItemData BuildEnchantedRune(ItemDrop.ItemData selectedItem, int targetEnchant, float powerModifier)
        {
            MagicItemEffect effect = selectedItem.GetMagicItem().Effects[targetEnchant];
            MagicItemEffect runeEffect = new MagicItemEffect(effect.EffectType);

            if (effect.EffectValue == float.NaN)
            {
                return null;
            }

            string prefabName = $"EtchedRunestone{selectedItem.GetRarity()}";
            EpicLoot.Log($"Checking for EtchedRune ({prefabName}) with power " +
                $"{effect.EffectValue} * {powerModifier} = {runeEffect.EffectValue} to return");

            GameObject item = PrefabManager.Instance.GetPrefab(prefabName);

            if (item == null)
            {
                return null;
            }

            ItemDrop baseData = item.GetComponent<ItemDrop>();

            if (baseData == null)
            {
                return null;
            }

            ItemDrop.ItemData newItem = baseData.m_itemData.Clone();
            MagicItemComponent magicItemComponent = newItem.Data().GetOrCreate<MagicItemComponent>();

            // We might need to rethink how power modifier is checked and applied here
            if (powerModifier != float.NaN && powerModifier < 999f && powerModifier > 0 && effect.EffectValue > 1)
            {
                runeEffect.EffectValue = effect.EffectValue * powerModifier;
                float maxDefaultValue = MagicItemEffectDefinitions.AllDefinitions[effect.EffectType].ValuesPerRarity
                    .GetValueDefForRarity(selectedItem.GetRarity()).MaxValue;
                // To clamp down on potentially infinite power looping by re-runing items
                if (runeEffect.EffectValue > (maxDefaultValue * powerModifier))
                {
                    runeEffect.EffectValue = (maxDefaultValue * powerModifier);
                }
            }
            else
            {
                // Tried to set the item to infinite power level
                runeEffect.EffectValue = effect.EffectValue;
            }

            MagicItem enchantmentsToRune = new MagicItem
            {
                Rarity = selectedItem.GetRarity(),
                Effects = new List<MagicItemEffect> { runeEffect }
            };
            API.WithChangeReason(API.ChangeReason.Rune, () => magicItemComponent.SetMagicItem(enchantmentsToRune));

            return newItem;
        }

        internal static string GetSelectedEnchantmentNameByIndex(ItemDrop.ItemData selectedItem, int targetEnchant)
        {
            if (targetEnchant > selectedItem.GetMagicItem().Effects.Count) {
                EpicLoot.LogWarning($"Tried to get enchantment {targetEnchant} from item with only {selectedItem.GetMagicItem().Effects.Count} effects");
                return "invalid";
            }

            return selectedItem.GetMagicItem().Effects[targetEnchant].EffectType;
        }

        internal static RuneExtractMode GetRuneExtractMode()
        {
            return ELConfig.RuneExtractItemMode.Value;
        }

        // The extract-tab warning localization key that matches the configured extract mode.
        internal static string GetRuneExtractWarningKey()
        {
            switch (GetRuneExtractMode())
            {
                case RuneExtractMode.KeepItem:
                    return "$mod_epicloot_rune_extract_warning_keep";
                case RuneExtractMode.ReduceEnchants:
                    return "$mod_epicloot_rune_extract_warning_reduce";
                case RuneExtractMode.ReduceEnchantsAndRarity:
                    return "$mod_epicloot_rune_extract_warning_reduce_rarity";
                case RuneExtractMode.DestroyItem:
                default:
                    return "$mod_epicloot_rune_extract_warning_destroy";
            }
        }

        // Applies the item-reduction side effect after a rune has been extracted from it.
        // reduceRarity: also drop the item one rarity tier and clamp remaining effect values down.
        // If no rolled effects remain, the item is reverted to a plain, non-magic item.
        internal static void ReduceItemAfterRuneExtract(ItemDrop.ItemData item, int targetEnchant, bool reduceRarity)
        {
            MagicItem magicItem = item.GetMagicItem();
            if (magicItem == null || targetEnchant < 0 || targetEnchant >= magicItem.Effects.Count)
            {
                return;
            }

            magicItem.Effects.RemoveAt(targetEnchant);

            // Drop stale augmented-effect bookkeeping so the augmented pip can't point at the wrong effect.
            magicItem.AugmentedEffectIndex = -1;
            magicItem.AugmentedEffectIndices?.Clear();

            // No rolled effects left -> revert to a plain, non-magic item. Dropping the component does
            // not write through SetMagicItem, so raise the change event by hand.
            if (magicItem.Effects.Count == 0)
            {
                item.Data().Remove<MagicItemComponent>();
                API.RaiseMagicItemChanged(item, API.ChangeReason.Rune);
                return;
            }

            if (reduceRarity && magicItem.Rarity > ItemRarity.Magic)
            {
                magicItem.Rarity = magicItem.Rarity - 1;

                // Clamp remaining effect values down to the new (lower) rarity's max.
                foreach (MagicItemEffect effect in magicItem.Effects)
                {
                    MagicItemEffectDefinition.ValueDef values =
                        MagicItemEffectDefinitions.Get(effect.EffectType)?.GetValuesForRarity(magicItem.Rarity);
                    if (values != null && effect.EffectValue > values.MaxValue)
                    {
                        effect.EffectValue = values.MaxValue;
                    }
                }
            }

            API.WithChangeReason(API.ChangeReason.Rune, () => item.SaveMagicItem(magicItem));
        }

        internal static GameObject RuneEnhanceItemAndReturnSuccess(ItemDrop.ItemData item, ItemDrop.ItemData rune, int enchantment)
        {
            List<MagicItemEffect> runeEffects = rune.GetMagicItem().Effects;

            if (runeEffects.Count > 1)
            {
                foreach (MagicItemEffect effect in runeEffects)
                {
                    // Replace the target enchantment
                    if (runeEffects.IndexOf(effect) == 0)
                    {
                        item.GetMagicItem().Effects[enchantment] = effect;
                        continue;
                    }

                    // Skip or replace existing effects with the same effect type
                    if (item.GetMagicItem().Effects.Any(x => x.EffectType == effect.EffectType))
                    {
                        // If the item already has this effect, but with a lower value, replace it
                        if (item.GetMagicItem().Effects.Any(x => x.EffectValue < effect.EffectValue))
                        {
                            int index = item.GetMagicItem().Effects.FindIndex(x => x.EffectType == effect.EffectType);
                            item.GetMagicItem().Effects[index] = effect;
                        }

                        // If the item already has this effect, skip it
                        continue;
                    }

                    // Add additional effects
                    item.GetMagicItem().Effects.Add(effect);
                }
            }
            else
            {
                item.GetMagicItem().Effects[enchantment] = rune.GetMagicItem().Effects[0];
            }

            MagicItem magicItem = item.GetMagicItem();
            API.WithChangeReason(API.ChangeReason.Rune, () => item.SaveMagicItem(magicItem));

            CraftSuccessDialog successDialog;
            //if (EpicLoot.HasAuga)
            //{
            //    var resultsPanel = Auga.API.Workbench_CreateNewResultsPanel();
            //    resultsPanel.transform.SetParent(EnchantingTableUI.instance.transform);
            //    resultsPanel.SetActive(false);
            //    successDialog = resultsPanel.gameObject.AddComponent<CraftSuccessDialog>();
            //    successDialog.NameText = successDialog.transform.Find("Topic").GetComponent<TMP_Text>();
            //}
            //else
            //{
                
            //}
            successDialog = CraftSuccessDialog.Create(EnchantingTableUI.instance.transform);

            successDialog.Show(item.Extended());

            RectTransform rt = (RectTransform)successDialog.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 0);

            if (!EpicLoot.HasAuga)
            {
                Transform frame = successDialog.transform.Find("Frame");
                if (frame != null)
                {
                    RectTransform frameRT = (RectTransform)frame;
                    frameRT.pivot = new Vector2(0.5f, 0.5f);
                    frameRT.anchorMax = new Vector2(0.5f, 0.5f);
                    frameRT.anchorMin = new Vector2(0.5f, 0.5f);
                    frameRT.anchoredPosition = new Vector2(0, 0);
                }
            }

            Game.instance.GetPlayerProfile().m_playerStats.m_stats[PlayerStatType.Crafts]++;
            Gogan.LogEvent("Game", "RuneEnhanced", item.m_shared.m_name, 1);

            return successDialog.gameObject;
        }

        internal static List<Tuple<string, bool>> GetEnchantmentEffects(ItemDrop.ItemData item, bool runecheck = false)
        {
            List<Tuple<string, bool>> result = new List<Tuple<string, bool>>();
            MagicItem magicItem = item?.GetMagicItem();
            if (magicItem != null)
            {
                ItemRarity rarity = magicItem.Rarity;
                List<MagicItemEffect> augmentableEffects = magicItem.Effects;

                for (int index = 0; index < augmentableEffects.Count; index++)
                {
                    MagicItemEffect augmentableEffect = augmentableEffects[index];
                    MagicItemEffectDefinition effectDef = MagicItemEffectDefinitions.Get(augmentableEffect.EffectType);
                    bool canAugment = (effectDef != null && effectDef.CanBeAugmented);
                    if (runecheck)
                    {
                        // Rune check if it is for the Rune UI, Augment if not
                        canAugment = (effectDef != null && effectDef.CanBeRunified);
                    }

                    string text = AugmentHelper.GetAugmentSelectorText(magicItem, index, augmentableEffects, rarity);
                    string color = EpicLoot.GetRarityColor(rarity);
                    string alpha = canAugment ? "FF" : "7F";
                    text = $"<color={color}{alpha}>{text}</color>";

                    result.Add(new Tuple<string, bool>(text, canAugment));
                }
            }

            return result;
        }

        internal static string GetAvailableAugmentEffects(ItemDrop.ItemData item, int augmentindex)
        {
            MagicItem magicItem = item?.GetMagicItem();
            if (magicItem == null)
            {
                return string.Empty;
            }

            ItemRarity rarity = magicItem.Rarity;
            string rarityColor = EpicLoot.GetRarityColor(rarity);

            bool valuelessEffect = false;
            if (augmentindex >= 0 && augmentindex < magicItem.Effects.Count)
            {
                MagicItemEffectDefinition currentEffectDef = MagicItemEffectDefinitions.Get(magicItem.Effects[augmentindex].EffectType);
                valuelessEffect = currentEffectDef.GetValuesForRarity(rarity) == null;
            }

            List<MagicItemEffectDefinition> availableEffects = MagicItemEffectDefinitions.GetAvailableEffects(
                item.Extended(), item.GetMagicItem(), valuelessEffect ? -1 : augmentindex);

            float totalSelectionWeight = availableEffects.Sum(x => x.SelectionWeight);

            StringBuilder sb = new StringBuilder();
            sb.Append($"<color={rarityColor}>");
            foreach (MagicItemEffectDefinition effectDef in availableEffects)
            {
                MagicItemEffectDefinition.ValueDef values = effectDef.GetValuesForRarity(item.GetRarity());
                string chancePrefix = GetSelectionChancePrefix(effectDef.SelectionWeight, totalSelectionWeight);
                sb.AppendLine($"‣ {chancePrefix}{MagicItem.GetEffectTextRange(effectDef, values)}");
            }
            sb.Append("</color>");

            return sb.ToString();
        }

        internal static List<InventoryItemListElement> GetAugmentCost(ItemDrop.ItemData item, int augmentindex)
        {
            return AugmentHelper.GetAugmentCosts(item, augmentindex)
                .Select(x =>
                {
                    ItemDrop.ItemData itemData = x.Key.m_itemData.Clone();
                    itemData.m_dropPrefab = x.Key.gameObject;
                    itemData.m_stack = x.Value;
                    return new InventoryItemListElement() { Item = itemData };
                }).ToList();
        }

        internal static GameObject AugmentItem(ItemDrop.ItemData item, int augmentindex)
        {
            // Set as augmented
            MagicItem magicItem = item?.GetMagicItem();
            if (magicItem == null)
            {
                return null;
            }

            magicItem.SetEffectAsAugmented(augmentindex);
            API.WithChangeReason(API.ChangeReason.Augment, () => item.SaveMagicItem(magicItem));

            AugmentChoiceDialog choiceDialog = AugmentHelper.CreateAugmentChoiceDialog(true);
            choiceDialog.transform.SetParent(EnchantingTableUI.instance.transform);

            // Fix audio sources
            foreach (AudioSource audioSource in choiceDialog.GetComponentsInChildren<AudioSource>())
            {
                audioSource.volume = GetAudioLevel();
            }

            RectTransform rt = (RectTransform)choiceDialog.transform;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, 0);

            if (!EpicLoot.HasAuga)
            {
                Transform frame = choiceDialog.transform.Find("Frame");
                if (frame != null)
                {
                    RectTransform frameRT = (RectTransform)frame;
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
            MagicItem magicItem = item?.GetMagicItem();
            if (magicItem == null)
            {
                return;
            }

            List<MagicItemEffect> oldEffects = magicItem.GetEffects();
            MagicItemEffect oldEffect = (effectIndex >= 0 && effectIndex < oldEffects.Count) ? oldEffects[effectIndex] : null;

            magicItem.ReplaceEffect(effectIndex, newEffect);

            // Don't count this free augment as locking in an augment
            if (oldEffect != null && EnchantCostsHelper.EffectIsDeprecated(oldEffect.EffectType))
            {
                magicItem.AugmentedEffectIndices.Remove(effectIndex);
            }

            if (magicItem.Rarity == ItemRarity.Rare)
            {
                magicItem.DisplayName = MagicItemNames.GetNameForItem(item, magicItem);
            }

            API.WithChangeReason(API.ChangeReason.Augment, () => item.SaveMagicItem(magicItem));

            Game.instance.GetPlayerProfile().m_playerStats.m_stats[PlayerStatType.Crafts]++;
            Gogan.LogEvent("Game", "Augmented", item.m_shared.m_name, 1);

            EquipmentEffectCache.Reset(Player.m_localPlayer);
        }

        internal static List<InventoryItemListElement> GetDisenchantItems()
        {
            List<ItemDrop.ItemData> boundItems = InventoryManagement.Instance.GetBoundItems();

            return InventoryManagement.Instance.GetAllItems()
                .Where(item => !item.m_equipped && !item.IsRunestone()  && (ELConfig.ShowEquippedAndHotbarItemsInSacrificeTab.Value ||
                    !boundItems.Contains(item)))
                .Where(item => item.IsMagic(out MagicItem magicItem) && magicItem.CanBeDisenchanted())
                .Select(item => new InventoryItemListElement() { Item = item })
                .ToList();
        }

        internal static List<InventoryItemListElement> GetDisenchantCost(ItemDrop.ItemData item)
        {
            List<InventoryItemListElement> result = new List<InventoryItemListElement>();
            if (item == null || !item.IsMagic() || item.IsUnidentified())
            {
                return result;
            }

            ItemRarity rarity = item.GetRarity();
            List<ItemAmountConfig> costList;
            switch (rarity)
            {
                case ItemRarity.Magic:
                    costList = EnchantCostsHelper.Config.DisenchantCosts.Magic;
                    break;

                case ItemRarity.Rare:
                    costList = EnchantCostsHelper.Config.DisenchantCosts.Rare;
                    break;

                case ItemRarity.Epic:
                    costList = EnchantCostsHelper.Config.DisenchantCosts.Epic;
                    break;

                case ItemRarity.Legendary:
                    costList = EnchantCostsHelper.Config.DisenchantCosts.Legendary;
                    break;

                case ItemRarity.Mythic:
                    costList = EnchantCostsHelper.Config.DisenchantCosts.Mythic;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Disenchant);
            int reducedCost = 0;
            if (!float.IsNaN(featureValues.Item2))
            {
                reducedCost = (int)featureValues.Item2;
            }

            foreach (ItemAmountConfig itemAmountConfig in costList)
            {
                GameObject prefab = ObjectDB.instance.GetItemPrefab(itemAmountConfig.Item);
                if (prefab == null)
                {
                    EpicLoot.LogWarning($"Tried to add unknown item ({itemAmountConfig.Item}) " +
                        $"to disenchant cost for item ({item.m_shared.m_name})");
                    continue;
                }

                ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                if (itemDrop == null)
                {
                    EpicLoot.LogWarning($"Tried to add item without ItemDrop ({itemAmountConfig.Item}) " +
                        $"to disenchant cost for item ({item.m_shared.m_name})");
                    continue;
                }

                ItemDrop.ItemData costItem = itemDrop.m_itemData.Clone();
                costItem.m_stack = itemAmountConfig.Amount - reducedCost;
                result.Add(new InventoryItemListElement() { Item = costItem });
            }

            return result;
        }

        // Strips the item's magic and returns everything owed back to the player: the shards/runestones
        // it was carrying in its sockets, plus the sacrifice products on a lucky bonus roll.
        // `bonusRolled` is true only for that lucky roll, so the caller can reserve the bonus
        // presentation for it rather than firing it for a plain socket return.
        internal static List<InventoryItemListElement> DisenchantItem(ItemDrop.ItemData item, out bool bonusRolled)
        {
            bonusRolled = false;
            List<InventoryItemListElement> returnedItems = new List<InventoryItemListElement>();
            if (item.IsMagic(out MagicItem magicItem) && magicItem.CanBeDisenchanted())
            {
                Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(
                    EnchantingFeature.Disenchant);
                int bonusItemChance = 0;

                if (!float.IsNaN(featureValues.Item1))
                {
                    bonusItemChance = (int)featureValues.Item1;
                }

                // Range(0, 100) rolls 0-99, so a chance of N is exactly N-in-100.
                if (Random.Range(0, 100) < bonusItemChance)
                {
                    List<InventoryItemListElement> bonusItems =
                        GetSacrificeProducts(new List<Tuple<ItemDrop.ItemData, int>>() { new(item, 1) });
                    bonusRolled = bonusItems.Count > 0;
                    returnedItems.AddRange(bonusItems);
                }

                // Disenchanting drops the MagicItem wholesale, sockets included, so anything socketed
                // has to be handed back before it goes.
                returnedItems.AddRange(ReclaimSockets(magicItem));

                item.Data().Remove<MagicItemComponent>();

                // Dropping the component does not write through SetMagicItem, so the change event has to
                // be raised by hand here.
                API.RaiseMagicItemChanged(item, API.ChangeReason.Disenchant);
            }

            return returnedItems;
        }

        // The socketed shards/runestones that survive disenchanting: everything whose removal policy is
        // not Locked. Break-only sockets come back intact -- disenchanting already costs materials and
        // every rolled effect on the item, which stands in for breaking them out one at a time.
        // Permanently bound sockets (Locked) are destroyed along with the enchantment.
        internal static List<InventoryItemListElement> ReclaimSockets(MagicItem magicItem)
        {
            List<InventoryItemListElement> result = new List<InventoryItemListElement>();
            foreach (SocketedEffect socketed in magicItem.Sockets)
            {
                if (ShardSocketManager.GetRemovalPolicy(socketed) == SocketRemoval.Locked)
                {
                    continue;
                }

                ItemDrop.ItemData socketItem = ShardSocketManager.ReconstructShardItem(socketed);
                if (socketItem != null)
                {
                    result.Add(new InventoryItemListElement() { Item = socketItem });
                }
            }

            return result;
        }
    }
}
