using BepInEx;
using EpicLoot.Crafting;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.ShardStones;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace EpicLoot;

public static class ItemDataExtensions
{
    private static readonly MethodInfo MemberwiseCloner = AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone");

    // A shallow copy, for an item that needs shared fields of its own without touching its prefab's.
    // SharedData has no copy method of its own and MemberwiseClone is protected.
    internal static ItemDrop.ItemData.SharedData Clone(this ItemDrop.ItemData.SharedData sharedData)
    {
        return (ItemDrop.ItemData.SharedData)MemberwiseCloner.Invoke(sharedData, null);
    }

    public static bool IsMagic(this ItemDrop.ItemData itemData)
    {
        return MagicItemData.Get(itemData) != null;
    }

    public static bool IsShardStone(this ItemDrop.ItemData itemData) {
        // A shard is identified by its shared data, not its magic data, so this answers correctly even
        // for an instance whose MagicItem has not been rebuilt yet.
        return Shards.IsShard(itemData);
    }

    public static bool IsUnidentified(this ItemDrop.ItemData itemData)
    {
        return itemData.GetMagicItem()?.IsUnidentified ?? false;
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

    // Keyed by the color string itself, so a config reload that changes a rarity's color simply
    // misses the cache — no invalidation needed. Callers run per item per UI refresh.
    private static readonly Dictionary<string, Color> ParsedColorCache = new Dictionary<string, Color>();

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

        if (ParsedColorCache.TryGetValue(colorString, out Color cached))
        {
            return cached;
        }

        Color parsed = ColorUtility.TryParseHtmlString(colorString, out Color color) ? color : Color.white;
        ParsedColorCache[colorString] = parsed;
        return parsed;
    }

    // Sockets count by default: effect patches gate on this. CheckRequirements passes false.
    public static bool HasMagicEffect(this ItemDrop.ItemData itemData, string effectType, bool includeSocketed = true)
    {
        return itemData.GetMagicItem()?.HasEffect(effectType, includeSocketed: includeSocketed) ?? false;
    }

    // The one write path for an item's magic: see MagicItemComponent.SetMagicItem for what a write entails.
    public static void SaveMagicItem(this ItemDrop.ItemData itemData, MagicItem magicItem)
    {
        MagicItemData.GetOrCreate(itemData).SetMagicItem(magicItem);
    }

    // Drops the item's magic data, leaving it mundane. Returns whether it had any.
    public static bool RemoveMagicItem(this ItemDrop.ItemData itemData)
    {
        return MagicItemData.Remove(itemData);
    }

    public static MagicItem GetMagicItem(this ItemDrop.ItemData itemData)
    {
        return MagicItemData.Get(itemData)?.MagicItem;
    }

    public static string GetDisplayName(this ItemDrop.ItemData itemData)
    {
        if (itemData.IsSetRune())
        {
            return GetSetRuneName(itemData);
        }

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

    // A set rune is named after its set when shown, never stored, so it follows a language change or a
    // renamed set, and reads as unknown once the set is gone from legendaries.json.
    public static string GetSetRuneName(this ItemDrop.ItemData itemData)
    {
        MagicItem magicItem = itemData.GetMagicItem();
        return UniqueLegendaryHelper.TryGetLegendarySetInfo(magicItem?.SetID, out LegendarySetInfo set)
            ? Localization.instance.Localize("$mod_epicloot_setrune_nameformat", Localization.instance.Localize(set.Name))
            : Localization.instance.Localize("$mod_epicloot_setrune_unknown");
    }

    public static string GetDescription(this ItemDrop.ItemData itemData)
    {
        // A set rune carries its source piece's LegendaryID only as an etch preference; the piece's lore
        // does not describe the rune.
        if (itemData.IsSetRune())
        {
            return "$mod_epicloot_setrune_desc";
        }

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

        // An effect with no registered definition is never augmentable.
        return itemData.GetMagicItem().Effects.Any(effect =>
            MagicItemEffectDefinitions.TryGet(effect.EffectType, out MagicItemEffectDefinition effectDef) &&
            effectDef.CanBeAugmented);
    }

    public static bool CanBeRunified(this ItemDrop.ItemData itemData)
    {
        if (!itemData.IsMagic())
        {
            return false;
        }

        // An effect with no registered definition is never extracted or overwritten by a rune.
        return itemData.GetMagicItem().Effects.Any(effect =>
            MagicItemEffectDefinitions.TryGet(effect.EffectType, out MagicItemEffectDefinition effectDef) &&
            effectDef.CanBeRunified);
    }

    // Shardstones and Brokkr's Gifts carry a cosmetic MagicItem -- a rarity and nothing else -- purely
    // so they render with a magic name and background. That makes IsMagic() true for them, and
    // MagicItem.CanBeDisenchanted() vacuously true as well, since it only vetoes on effects and they
    // have none. Disenchanting one would charge the player, hand back nothing and strip the metadata,
    // leaving a plain grey consumable. They are not enchanted gear; keep them out of the flow.
    public static bool CanBeDisenchanted(this ItemDrop.ItemData itemData)
    {
        if (itemData == null || itemData.IsShardStone() || itemData.IsShardSlotChisel())
        {
            return false;
        }

        return itemData.IsMagic(out MagicItem magicItem) && !magicItem.IsUnidentified && magicItem.CanBeDisenchanted();
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
        UniqueLegendaryHelper.TryGetLegendarySetInfo(itemData.GetSetID(), out LegendarySetInfo setInfo);
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

    /// <summary>A named unique (a legendaries.json item) that is not a set piece.</summary>
    public static bool IsUniqueItem(this ItemDrop.ItemData itemData)
    {
        return itemData.IsMagic(out MagicItem magicItem) && magicItem.IsUniqueLegendary() && !magicItem.IsLegendarySetItem();
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
            else if (UniqueLegendaryHelper.TryGetLegendarySetInfo(setID, out LegendarySetInfo setInfo))
            {
                // Pieces needed for every bonus, which is less than the piece list when the set offers
                // alternatives for one slot.
                return UniqueLegendaryHelper.GetFullSetCount(setInfo);
            }
        }

        return 0;
    }

    /// <summary>
    /// Restores a real item prefab to <see cref="ItemDrop.ItemData.m_dropPrefab"/> when it is missing or is
    /// <see cref="EpicAssets.DummyName"/>. <see cref="Humanoid_Patch"/> stamps that dummy (no mesh, no
    /// ItemDrop) onto equipped items with a null m_dropPrefab and it is saved with the item, which then
    /// renders as nothing and passes every null check. ObjectDB's <c>m_itemByData</c> map is tried first
    /// (ItemData.Clone shares <c>m_shared</c> with its prefab), then a scan by shared name for items whose
    /// shared data was deep-copied (Instantiate, shards). Returns true only when a prefab was restored.
    /// </summary>
    public static bool HealDropPrefab(this ItemDrop.ItemData itemData)
    {
        if (itemData?.m_shared == null || ObjectDB.instance == null)
        {
            return false;
        }

        // Unity's operator== is the only thing that reports a destroyed object, so compare against null
        // rather than pattern-matching. A prefab that is present and is not the dummy is already good.
        GameObject current = itemData.m_dropPrefab;
        if (current != null && current.name != EpicAssets.DummyName)
        {
            return false;
        }

        if (!ObjectDB.instance.TryGetItemPrefab(itemData.m_shared, out GameObject prefab) || prefab == null)
        {
            prefab = FindItemPrefabBySharedName(itemData.m_shared.m_name);
        }

        if (prefab == null)
        {
            // Nothing to restore it to. Leave whatever is there: the dummy at least keeps
            // Humanoid.SetupVisEquipment from throwing, which is why it exists.
            return false;
        }

        itemData.m_dropPrefab = prefab;
        EpicLoot.Log($"Healed m_dropPrefab on '{itemData.m_shared.m_name}' -> '{prefab.name}' " +
            $"(was {(current == null ? "null" : EpicAssets.DummyName)}).");
        return true;
    }

    /// <summary>
    /// Last-resort lookup for <see cref="HealDropPrefab"/>: the first ObjectDB item whose shared name matches.
    /// Ambiguous in principle (two prefabs may share a display token) but only reached when the exact
    /// reference lookup has already failed, and a same-named item prefab is a far better answer than the dummy.
    /// </summary>
    private static GameObject FindItemPrefabBySharedName(string sharedName)
    {
        if (sharedName.IsNullOrWhiteSpace())
        {
            return null;
        }

        foreach (GameObject itemPrefab in ObjectDB.instance.m_items)
        {
            if (itemPrefab == null)
            {
                continue;
            }

            // Vanilla registers some non-item prefabs in ObjectDB.m_items; its own lookups skip them too.
            if (itemPrefab.TryGetComponent(out ItemDrop itemDrop) &&
                itemDrop.m_itemData?.m_shared?.m_name == sharedName)
            {
                return itemPrefab;
            }
        }

        return null;
    }

    /// <summary>
    /// Gives a fresh instance the magic its prefab carries, and a vanilla item the magic it is born with.
    /// Runs on ItemDrop.Awake and on every Inventory and Container load.
    /// </summary>
    public static void InitializeCustomData(this ItemDrop.ItemData itemData)
    {
        // Opening a pre-1.0 world runs ZDOMan.ConvertContainers, which loads and re-saves every chest through
        // a temporary Inventory. Its items are bare ItemData (AddTempItem sets m_dropPrefab, never m_shared),
        // and the Inventory.Load postfix still sees them: reading m_shared.m_name here threw out of
        // ZNet.LoadOldWorld and aborted the world load. Skipping loses nothing -- vanilla writes
        // m_customData back verbatim, and the item is initialized for real when the chest is next loaded.
        if (itemData?.m_shared == null)
        {
            return;
        }

        // Shards rebuild their own magic data from m_shared.m_ammoType, so they need neither the prefab
        // reference nor its baked custom data. Done ahead of the m_dropPrefab check so a shard is healed
        // even when the prefab is unresolved. Cheap no-op for everything else.
        Shards.EnsureShardMetadata(itemData);

        // Repair a dummy-stamped prefab before anything reads it. This runs from the ItemDrop.Awake,
        // Inventory.Load and Container.Load postfixes, so it is the point every already-corrupted item
        // passes through on load.
        itemData.HealDropPrefab();

        GameObject prefab = itemData.m_dropPrefab;
        if (prefab == null)
        {
            return;
        }

        // The dummy prefab (see HealDropPrefab) stays when nothing better was found, and it has no ItemDrop.
        if (!prefab.TryGetComponent(out ItemDrop itemDropPrefab))
        {
            return;
        }

        // A prefab's own magic (a shardstone's rarity, an unidentified item, a Brokkr's Gift, a crafting
        // material) reaches every instance made from it, so instances stack with each other and with the
        // ones a save already holds.
        MagicItemData.CopyFromPrefab(itemData, itemDropPrefab.m_itemData);

        // A few vanilla items are magic by nature (the Dverger circlet, Megingjord, the Wishbone): an
        // instance with no magic of its own gets its preset, quietly, as on every load. Not when the item
        // is kept from being magic by config.
        if (!MagicItemData.HasMagicData(itemData) && EpicLoot.CanBeMagicItem(itemDropPrefab.m_itemData) &&
            MagicItemComponent.VanillaMagicFor(itemData.m_shared.m_name) is MagicItem preset)
        {
            MagicItemData.GetOrCreate(itemData).SetMagicItemQuietly(preset);
        }

        // A stackable item's magic is compared as text when stacks merge, so a saved stack is read once here,
        // which brings text from an older build's serializer up to date (MagicItemComponent.Reload) before
        // a fresh drop is held against it.
        if (itemData.m_shared.m_maxStackSize > 1 && MagicItemData.HasMagicData(itemData))
        {
            MagicItemData.Get(itemData);
        }
    }
}
