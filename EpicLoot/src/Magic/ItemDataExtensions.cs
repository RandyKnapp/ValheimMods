using BepInEx;
using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.LegendarySystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EpicLoot;

public static class ItemDataExtensions
{
    public static bool IsMagic(this ItemDrop.ItemData itemData)
    {
        MagicItemComponent magicData = itemData.Data().Get<MagicItemComponent>();
        return magicData != null && magicData.MagicItem != null;
    }

    public static bool IsUnidentified(this ItemDrop.ItemData itemData)
    {
        MagicItemComponent mic = itemData.Data().Get<MagicItemComponent>();
        if (mic == null || mic.MagicItem == null)
        {
            return false;
        }

        return mic.MagicItem.IsUnidentified;
    }

    public static bool IsMagic(this ItemDrop.ItemData itemData, out MagicItem magicItem)
    {
        magicItem = itemData.GetMagicItem();
        return magicItem != null;
    }

    public static bool UseMagicBackground(this ItemDrop.ItemData itemData)
    {
        return itemData.IsMagic() || itemData.IsRunestone();
    }

    public static bool HasRarity(this ItemDrop.ItemData itemData)
    {
        return itemData.IsMagic() || itemData.IsMagicCraftingMaterial() || itemData.IsRunestone();
    }

    public static ItemRarity GetRarity(this ItemDrop.ItemData itemData)
    {
        if (itemData.IsMagic())
        {
            return itemData.GetMagicItem().Rarity;
        }
        else if (itemData.IsMagicCraftingMaterial())
        {
            return itemData.GetCraftingMaterialRarity();
        }
        else if (itemData.IsRunestone())
        {
            return itemData.GetRunestoneRarity();
        }

        throw new ArgumentException("itemData is not magic item, magic crafting material, or runestone");
    }

    public static Color GetRarityColor(this ItemDrop.ItemData itemData)
    {
        string colorString = "white";
        if (itemData.IsMagic())
        {
            colorString = itemData.GetMagicItem().GetColorString();
        }
        else if (itemData.IsMagicCraftingMaterial())
        {
            colorString = itemData.GetCraftingMaterialRarityColor();
        }
        else if (itemData.IsRunestone())
        {
            colorString = itemData.GetRunestoneRarityColor();
        }

        return ColorUtility.TryParseHtmlString(colorString, out Color color) ? color : Color.white;
    }

    public static bool HasMagicEffect(this ItemDrop.ItemData itemData, string effectType)
    {
        return itemData.GetMagicItem()?.HasEffect(effectType) ?? false;
    }

    public static void CreateMagicItem(this ItemDrop.ItemData itemData)
    {
        MagicItemComponent magicItem = itemData.Data().GetOrCreate<MagicItemComponent>();
        itemData.SaveMagicItem(magicItem.MagicItem);
    }

    public static void SaveMagicItem(this ItemDrop.ItemData itemData, MagicItem magicItem)
    {
        itemData.Data().GetOrCreate<MagicItemComponent>().SetMagicItem(magicItem);
    }

    public static bool IsExtended(this ItemDrop.ItemData itemData)
    {
        return itemData.Data().Get<MagicItemComponent>() != null;
    }

    public static ItemDrop.ItemData Extended(this ItemDrop.ItemData itemData)
    {
        MagicItemComponent value = itemData.Data().GetOrCreate<MagicItemComponent>();
        return value.Item;
    }

    public static MagicItem GetMagicItem(this ItemDrop.ItemData itemData)
    {
        return itemData.Data().Get<MagicItemComponent>()?.MagicItem;
    }

    public static string GetDisplayName(this ItemDrop.ItemData itemData)
    {
        // TODO: investigate
        string name = itemData.m_shared.m_name;

        if (itemData.IsMagic(out MagicItem magicItem) && !string.IsNullOrEmpty(magicItem.DisplayName))
        {
            const string pattern = @"\(.+?[+\-]\d+.+?\)";
            Match match = Regex.Match(itemData.m_shared.m_name, pattern);
            string appendedText = string.Empty;

            if (match.Success)
            {
                string matchedValue = match.Value;
                appendedText = $" {matchedValue}";
            }

            name = magicItem.DisplayName + appendedText;
        }

        return name;
    }

    public static string GetDecoratedName(this ItemDrop.ItemData itemData, string colorOverride = null)
    {
        string color = "white";
        string name = GetDisplayName(itemData);

        if (!string.IsNullOrEmpty(colorOverride))
        {
            color = colorOverride;
        }
        else if (itemData.IsMagic(out MagicItem magicItem))
        {
            color = magicItem.GetColorString();
        }
        else if (itemData.IsMagicCraftingMaterial() || itemData.IsRunestone())
        {
            color = itemData.GetCraftingMaterialRarityColor();
        }

        return $"<color={color}>{name}</color>";
    }

    public static string GetDescription(this ItemDrop.ItemData itemData)
    {
        if (itemData.IsMagic())
        {
            MagicItem magicItem = itemData.GetMagicItem();
            if (magicItem.IsUniqueLegendary() &&
                UniqueLegendaryHelper.TryGetLegendaryInfo(magicItem.LegendaryID, out LegendaryInfo itemInfo))
            {
                return itemInfo.Description;
            }
        }

        return itemData.m_shared.m_description;
    }

    public static bool IsPartOfSet(this ItemDrop.ItemData itemData, string setName)
    {
        return itemData.m_shared.m_setName == setName ||
            (itemData.IsMagic(out MagicItem magicItem) && magicItem.SetID == setName);
    }

    public static bool CanBeAugmented(this ItemDrop.ItemData itemData)
    {
        if (!itemData.IsMagic())
        {
            return false;
        }

        return itemData.GetMagicItem().Effects.Select(effect => MagicItemEffectDefinitions.Get(effect.EffectType))
            .Any(effectDef => effectDef.CanBeAugmented);
    }

    public static bool CanBeRunified(this ItemDrop.ItemData itemData)
    {
        if (!itemData.IsMagic())
        {
            return false;
        }

        return itemData.GetMagicItem().Effects.Select(effect => MagicItemEffectDefinitions.Get(effect.EffectType))
            .Any(effectDef => effectDef.CanBeRunified);
    }

    public static string GetSetID(this ItemDrop.ItemData itemData, out bool isMundane)
    {
        isMundane = true;
        if (itemData.IsMagic(out MagicItem magicItem) && !string.IsNullOrEmpty(magicItem.SetID))
        {
            isMundane = false;
            return magicItem.SetID;
        }

        if (!string.IsNullOrEmpty(itemData.m_shared.m_setName))
        {
            return itemData.m_shared.m_setName;
        }

        return null;
    }

    public static string GetSetID(this ItemDrop.ItemData itemData)
    {
        return GetSetID(itemData, out _);
    }

    public static LegendarySetInfo GetLegendarySetInfo(this ItemDrop.ItemData itemData)
    {
        UniqueLegendaryHelper.TryGetLegendarySetInfo(itemData.GetSetID(), out LegendarySetInfo setInfo, out ItemRarity rarity);
        return setInfo;
    }

    public static bool IsSetItem(this ItemDrop.ItemData itemData)
    {
        return !string.IsNullOrEmpty(itemData.GetSetID());
    }

    public static bool IsMagicSetItem(this ItemDrop.ItemData itemData)
    {
        return itemData.IsMagic(out MagicItem magicItem) && !string.IsNullOrEmpty(magicItem.SetID);
    }

    public static bool IsMundaneSetItem(this ItemDrop.ItemData itemData)
    {
        return !string.IsNullOrEmpty(itemData.m_shared.m_setName);
    }

    public static int GetSetSize(this ItemDrop.ItemData itemData, string setID = null, bool isMundane = false)
    {
        if (setID == null)
        {
            setID = itemData.GetSetID(out isMundane);
        }

        if (!string.IsNullOrEmpty(setID))
        {
            if (isMundane)
            {
                return itemData.m_shared.m_setSize;
            }
            else if (UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out LegendarySetInfo setInfo, out ItemRarity rarity))
            {
                return setInfo.LegendaryIDs.Count;
            }
        }

        return 0;
    }

    public static List<string> GetSetPieces(string setName, bool isMundane)
    {
        if (!isMundane && UniqueLegendaryHelper.TryGetLegendarySetInfo(setName, out LegendarySetInfo setInfo, out ItemRarity rarity))
        {
            return setInfo.LegendaryIDs;
        }

        return GetMundaneSetPieces(setName);
    }

    public static List<string> GetMundaneSetPieces(string setName)
    {
        // TODO: improve performace of this call
        List<string> results = new List<string>();
        foreach (GameObject itemPrefab in ObjectDB.instance.m_items)
        {
            if (itemPrefab == null)
            {
                EpicLoot.LogError("Null Item left in ObjectDB! (This means that a prefab was deleted and not an instance)");
                continue;
            }

            ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
            if (itemDrop == null)
            {
                EpicLoot.LogError($"Item in ObjectDB missing ItemDrop: ({itemPrefab.name})");
                continue;
            }

            if (itemDrop.m_itemData.m_shared.m_setName == setName)
            {
                results.Add(itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared.m_name);
            }
        }

        return results;
    }

    /// <summary>
    /// Copies the MagicItemComponent Magic Item from the drop prefab to set the magic data on this instance.
    /// </summary>
    public static void InitializeCustomData(this ItemDrop.ItemData itemData)
    {
        GameObject prefab = itemData.m_dropPrefab;
        if (prefab == null)
        {
            return;
        }

        ItemDrop itemDropPrefab = prefab.GetComponent<ItemDrop>();
        if (EpicLoot.CanBeMagicItem(itemDropPrefab.m_itemData) && !itemData.IsExtended())
        {
            MagicItemComponent instanceData = itemData.Data().Add<MagicItemComponent>();
            MagicItemComponent prefabData = itemDropPrefab.m_itemData.Data().Get<MagicItemComponent>();

            if (instanceData != null && prefabData != null)
            {
                instanceData.SetMagicItem(prefabData.MagicItem);
            }
        }
    }

    public static string GetSetTooltip(this ItemDrop.ItemData item)
    {
        if (item == null || Player.m_localPlayer == null)
        {
            return String.Empty;
        }

        StringBuilder text = new StringBuilder();

        try
        {
            // TODO: Clean up code associated with set data
            text.Append(GetMundaneSetTooltip(item));
            text.Append(GetMagicSetTooltip(item));
        }
        catch (Exception e)
        {
            EpicLoot.LogWarning($"[GetSetTooltip] Error on item {item.m_shared?.m_name} - {e.Message}");
        }

        return text.ToString();
    }

    public static string GetMundaneSetTooltip(ItemDrop.ItemData item)
    {
        if (string.IsNullOrEmpty(item.m_shared.m_setName))
        {
            return String.Empty;
        }

        string setID = item.m_shared.m_setName;
        int setSize = item.m_shared.m_setSize;

        return GetSetTooltip(item, setID, setSize, true);
    }

    public static string GetMagicSetTooltip(ItemDrop.ItemData item)
    {
        string setID = item.GetSetID(out bool isMundane);

        if (isMundane)
        {
            return String.Empty;
        }

        int setSize = item.GetSetSize(setID, isMundane);

        return GetSetTooltip(item, setID, setSize, false);
    }

    private static string GetSetTooltip(ItemDrop.ItemData item, string setID, int setSize, bool isMundane)
    {
        StringBuilder text = new StringBuilder();
        List<string> setPieces = GetSetPieces(setID, isMundane);
        List<ItemDrop.ItemData> currentSetEquipped = Player.m_localPlayer.GetEquippedSetPieces(setID);

        string setDisplayName = GetSetDisplayName(item, isMundane);
        text.Append($"\n\n<color={EpicLoot.GetSetItemColor()}> $mod_epicloot_set: " +
            $"{setDisplayName} ({currentSetEquipped.Count}/{setSize}):</color>");

        foreach (string setItemName in setPieces)
        {
            bool isEquipped = IsSetItemEquipped(currentSetEquipped, setItemName, isMundane);
            string color = isEquipped ? "white" : "#808080ff";
            string displayName = GetSetItemDisplayName(setItemName, isMundane);
            text.Append($"\n  <color={color}>{displayName}</color>");
        }

        if (isMundane)
        {
            string setEffectColor = currentSetEquipped.Count == setSize ? EpicLoot.GetSetItemColor() : "#808080ff";
            float skillLevel = Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType);
            text.Append($"\n<color={setEffectColor}>({setSize}) ‣ " +
                $"{item.GetSetStatusEffectTooltip(item.m_quality, skillLevel).Replace("\n", " ")}</color>");
        }
        else
        {
            LegendarySetInfo setInfo = item.GetLegendarySetInfo();

            if (setInfo != null)
            {
                foreach (SetBonusInfo setBonusInfo in setInfo.SetBonuses.OrderBy(x => x.Count))
                {
                    bool hasEquipped = currentSetEquipped.Count >= setBonusInfo.Count;
                    MagicItemEffectDefinition effectDef = MagicItemEffectDefinitions.Get(setBonusInfo.Effect.Type);

                    if (effectDef == null)
                    {
                        EpicLoot.LogError($"Set Tooltip: Could not find effect ({setBonusInfo.Effect.Type}) " +
                            $"for set ({setInfo.ID}) bonus ({setBonusInfo.Count})!");
                        continue;
                    }

                    string display = MagicItem.GetEffectText(effectDef, setBonusInfo.Effect.Values?.MinValue ?? 0);
                    text.Append($"\n<color={(hasEquipped ? EpicLoot.GetSetItemColor() : "#808080ff")}>" +
                        $"({setBonusInfo.Count}) ‣ {display}</color>");
                }
            }
        }

        return text.ToString();
    }

    private static string GetSetItemDisplayName(string setItemName, bool isMundane)
    {
        if (isMundane)
        {
            return setItemName;
        }
        else if (UniqueLegendaryHelper.TryGetLegendaryInfo(setItemName, out LegendaryInfo itemInfo))
        {
            return itemInfo.Name;
        }

        return setItemName;
    }

    public static string GetSetDisplayName(ItemDrop.ItemData item, bool isMundane)
    {
        if (!isMundane)
        {
            LegendarySetInfo setInfo = item.GetLegendarySetInfo();
            if (setInfo != null)
            {
                return setInfo.Name;
            }
            else
            {
                return $"{item.GetSetID()}";
            }
        }

        if (item.m_shared.m_setStatusEffect != null && !item.m_shared.m_setStatusEffect.m_name.IsNullOrWhiteSpace())
        {
            return item.m_shared.m_setStatusEffect.m_name;
        }

        if (!item.m_shared.m_setName.IsNullOrWhiteSpace())
        {
            return item.m_shared.m_setName;
        }

        return "<unknown set>";
    }

    public static bool IsSetItemEquipped(List<ItemDrop.ItemData> currentSetEquipped, string setItemName, bool isMundane)
    {
        if (isMundane)
        {
            return currentSetEquipped.Find(x => x.m_shared.m_name == setItemName) != null;
        }
        else
        {
            return currentSetEquipped.Find(x => x.IsMagic(out MagicItem magicItem) && magicItem.LegendaryID == setItemName) != null;
        }
    }
}
