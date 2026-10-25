using BepInEx;
using EpicLoot.Crafting;
using EpicLoot.Data;
using EpicLoot.LegendarySystem;
using EpicLoot.MagicItemEffects;
using EpicLoot.ShardStones;
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

    public static bool IsShardStone(this ItemDrop.ItemData itemData) {
        // A shard is identified by its shared data, not its magic data, so this answers correctly even
        // for an instance whose MagicItem has not been rebuilt yet.
        return Shards.IsShard(itemData);
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

    // includeSocketed defaults to true: this extension is used by effect-application patches to gate
    // behavior, so socketed effects should count. The only crafting caller (CheckRequirements) passes false.
    public static bool HasMagicEffect(this ItemDrop.ItemData itemData, string effectType, bool includeSocketed = true)
    {
        return itemData.GetMagicItem()?.HasEffect(effectType, includeSocketed: includeSocketed) ?? false;
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
        if (itemData.IsSetRune())
        {
            return GetSetRuneName(itemData);
        }

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
    /// Restores a real item prefab to <see cref="ItemDrop.ItemData.m_dropPrefab"/> when it is missing or has
    /// been replaced by <see cref="EpicAssets.DummyName"/>.
    ///
    /// <para><see cref="Humanoid_Patch"/> stamps the dummy -- an empty prefab with no mesh and no ItemDrop --
    /// onto any equipped ItemData whose m_dropPrefab is null, and that write is serialized with the item. The
    /// item then renders as nothing ("transparent weapon") and, because the dummy is non-null but carries no
    /// ItemDrop, it walks straight past every `m_dropPrefab == null` guard in the mod. Healing here repairs
    /// items already saved that way; the sources that produced them are fixed separately.</para>
    ///
    /// <para>Resolution order:
    /// <list type="number">
    /// <item>ObjectDB's reference-keyed <c>m_itemByData</c> map (<c>TryGetItemPrefab(SharedData)</c>).
    /// ItemData.Clone is a MemberwiseClone, so an instance shares its <c>m_shared</c> reference with the
    /// prefab it came from -- this is exact and O(1).</item>
    /// <item>A scan of ObjectDB matching <c>m_shared.m_name</c>, for items whose shared data was deep-copied
    /// rather than shared (Instantiate does this, and shards rebuild theirs) and so miss the map.</item>
    /// </list>
    /// Returns true only when a prefab was actually restored.</para>
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
    /// Copies the MagicItemComponent Magic Item from the drop prefab to set the magic data on this instance.
    /// </summary>
    public static void InitializeCustomData(this ItemDrop.ItemData itemData)
    {
        // Opening a pre-1.0 world runs ZDOMan.ConvertContainers, which loads and re-saves every chest through
        // a temporary Inventory. Its items are bare ItemData (AddTempItem sets m_dropPrefab, never m_shared),
        // and the Inventory.Load postfix still sees them: MagicItemComponent.FirstLoad reading m_shared.m_name
        // threw out of ZNet.LoadOldWorld and aborted the world load. Skipping loses nothing -- vanilla writes
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

        // m_dropPrefab is not necessarily an ITEM prefab: Humanoid_Patch.AssignEmptyToNull stamps
        // EpicAssets.DummyPrefab (an empty CreateEmptyPrefab stand-in carrying no ItemDrop) onto any
        // ItemData whose m_dropPrefab is null, which turns the safe null this method guards against into a
        // non-null that sails past that guard. Without this check GetComponent returns null and the
        // m_itemData dereference below throws -- and because the dummy is written back into the ItemData,
        // that NRE then repeats on every later Inventory.Load / Container.Load / ItemDrop.Awake for the item.
        if (!prefab.TryGetComponent(out ItemDrop itemDropPrefab))
        {
            return;
        }

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
}
