using System;
using System.Collections.Generic;
using EpicLoot.MagicItemEffects;
using Newtonsoft.Json;
using UnityEngine;

namespace EpicLoot;

// One item's magic: the MagicItem parsed from the item's m_customData entry (see MagicItemData), and the one
// place it is written back.
public sealed class MagicItemComponent
{
    public ItemDrop.ItemData Item { get; }

    public MagicItem MagicItem;

    // The m_customData string MagicItem was parsed from. MagicItemData.Get compares it by reference with the
    // entry now under the key, which is how a replaced entry is noticed.
    internal string Raw;

    internal MagicItemComponent(ItemDrop.ItemData item)
    {
        Item = item;
    }

    // The stored JSON, or "" when there is none.
    public string Value => MagicItemData.RawOf(Item) ?? "";

    public void SetMagicItem(MagicItem magicItem)
    {
        if (magicItem == null)
        {
            return;
        }

        MagicItem = magicItem;
        Raw = Serialize();
        MagicItemData.Store(Item, Raw);

        // Every magic-data write funnels through here (enchant, augment, socket, rune, temper,
        // transfer, loot roll), so this is the one place Indestructible needs to be re-derived and the
        // one place the API's change event can be raised with guaranteed coverage. Callers that know
        // *why* they are writing wrap themselves in API.WithChangeReason, which only labels the event
        // raised here -- it still fires exactly once per write.
        Indestructible.Sync(Item);
        API.RaiseMagicItemChanged(Item);
        // A panel that rewrites an item shows its new tooltip in the same frame (tempering does).
        ItemTooltip.InvalidateCache();

        if (Player.m_localPlayer == null)
        {
            return;
        }

        if (Item.m_equipped && Player.m_localPlayer.IsItemEquiped(Item))
        {
            Multiplayer_Player_Patch.UpdatePlayerZDOForEquipment(Player.m_localPlayer, Item, MagicItem != null);
            // The worn item's effects just changed; drop the memoized per-player totals. This is
            // the single funnel for magic-data writes, so paths that never re-equip (tempering,
            // rune etching) get correct totals immediately instead of on the next equip change.
            EquipmentEffectCache.Reset(Player.m_localPlayer);
            // The loot effects (Luck, Riches, Lucky Loot...) are read off the player ZDO by whichever machine
            // rolls the loot, so republish them too: socketing, unsocketing and augmenting a worn item
            // otherwise kept the old values until the next equipment change.
            Multiplayer_Player_Patch.WatchMultiplayerMagicEffects_Player_Patch.UpdateRichesAndLuck(Player.m_localPlayer);
        }
    }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(MagicItem, Formatting.None);
    }

    // Re-reads the item's entry. MagicItemData.Get already follows a replaced entry on its own; this is for a
    // caller that has just written the entry itself.
    public void Load()
    {
        Reload(MagicItemData.RawOf(Item));
    }

    // Parses `raw` (null for no magic) and brings the item in line with it. A correction to the stored data
    // is written back; otherwise equipment is parsed once and never re-serialized.
    internal void Reload(string raw)
    {
        Raw = raw;
        MagicItem = Parse(raw);

        if (FixupValuelessEffects())
        {
            SetMagicItemQuietly(MagicItem);
            return;
        }

        // A stackable item's magic is compared as text when stacks merge (MagicItemData.StacksWith), so text
        // an older build's serializer wrote is brought up to date here, or a saved stack would never again
        // merge with a fresh drop. Stackables carry a rarity and at most an effect, so this is cheap;
        // equipment never stacks and keeps its text as saved.
        if (MagicItem != null && Item.m_shared != null && Item.m_shared.m_maxStackSize > 1)
        {
            string current = Serialize();
            if (!string.Equals(current, raw, StringComparison.Ordinal))
            {
                Raw = current;
                MagicItemData.Store(Item, current);
            }
        }

        // A component with no magic item still needs the flag reverted if this instance was previously
        // made indestructible.
        Indestructible.Sync(Item);
    }

    private MagicItem Parse(string raw)
    {
        if (raw == null)
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<MagicItem>(raw);
        }
        catch (Exception e)
        {
            // Corrupt data must not escape into the vanilla caller that happened to touch this item first
            // (damage patches, tooltips, the inventory grid all reach here). The item reads as mundane.
            EpicLoot.LogError($"[{nameof(MagicItemComponent)}] Could not deserialize MagicItem json data! " +
                $"({Item?.m_shared?.m_name}): {e.Message}");
            return null;
        }
    }

    // Called by MagicItemData.Remove before the entry is dropped, while HasMagicEffect would still report
    // the effect -- Sync would refuse to revert, so force it.
    internal void Unload()
    {
        Indestructible.Revert(Item);
    }

    /// <summary>
    /// A write that is not a change: data normalized while an item loads, or a vanilla item receiving the
    /// magic it is born with. Every item entering the world may pass through here, so the API change event
    /// is not raised, or listeners would be flooded at world load.
    /// </summary>
    internal void SetMagicItemQuietly(MagicItem magicItem)
    {
        bool previous = API.SuppressChangeEvents;
        API.SuppressChangeEvents = true;
        try
        {
            SetMagicItem(magicItem);
        }
        finally
        {
            API.SuppressChangeEvents = previous;
        }
    }

    // A few vanilla items are magic by nature. An instance with no magic of its own gets its preset when it
    // is initialized (ItemDataExtensions.InitializeCustomData). Null for every other item.
    public static MagicItem VanillaMagicFor(string sharedName)
    {
        switch (sharedName)
        {
            case "$item_helmet_dverger":
                return new MagicItem
                {
                    Rarity = ItemRarity.Rare,
                    Effects = { new MagicItemEffect(MagicEffectType.DvergerCirclet) },
                    TypeNameOverride = "$mod_epicloot_circlet"
                };
            case "$item_beltstrength":
                return new MagicItem
                {
                    Rarity = ItemRarity.Rare,
                    Effects = { new MagicItemEffect(MagicEffectType.Megingjord) },
                    TypeNameOverride = "$mod_epicloot_belt"
                };
            case "$item_wishbone":
                return new MagicItem
                {
                    Rarity = ItemRarity.Epic,
                    Effects = { new MagicItemEffect(MagicEffectType.Wishbone) },
                    TypeNameOverride = "$mod_epicloot_remains"
                };
            default:
                return null;
        }
    }

    // A binary effect (Indestructible, Weightless, Throwable...) must sit at the default value of 1:
    // older saves and hand-edited data can carry a stale number that would otherwise show up in the
    // tooltip. "Binary" is deliberately judged across every rarity rather than at this item's own
    // rarity -- the result is written straight back to m_customData, so a per-rarity test would
    // permanently flatten a legitimately rolled value whenever the loaded definitions simply lack a
    // block for that rarity. That happens for real: a dedicated server replaces the client's
    // definitions wholesale on connect (ELConfig.ApplyClientConfig), so a server config that predates
    // a newly added rarity would otherwise reset every effect on every item of that rarity to 1 --
    // once, silently, and irreversibly. Report the gap instead, and leave the value alone.
    // Returns whether anything was corrected.
    private bool FixupValuelessEffects()
    {
        if (MagicItem == null)
        {
            return false;
        }

        bool changed = false;
        foreach (MagicItemEffect effect in MagicItem.Effects)
        {
            if (MagicItemEffectDefinitions.IsValuelessEffect(effect.EffectType))
            {
                if (!Mathf.Approximately(effect.EffectValue, 1))
                {
                    EpicLoot.Log($"Fixing up effect on {MagicItem.DisplayName}: effect={effect.EffectType}");
                    effect.EffectValue = 1;
                    changed = true;
                }

                continue;
            }

            WarnOnceAboutMissingRarityValues(effect.EffectType, MagicItem.Rarity);
        }

        return changed;
    }

    // Types already reported by WarnOnceAboutMissingRarityValues. Every item entering the world passes
    // through Reload(), so an unguarded warning would repeat per item per load; the interesting fact is
    // which effect/rarity pairs have no values, and that is a property of the config, not of the item.
    private static readonly HashSet<string> ReportedRarityGaps = new HashSet<string>();

    private static void WarnOnceAboutMissingRarityValues(string effectType, ItemRarity rarity)
    {
        // An unknown effect is not a rarity gap; Get() reports the missing definition itself.
        if (!MagicItemEffectDefinitions.TryGet(effectType, out var effectDef) ||
            effectDef.ValuesPerRarity?.GetValueDefForRarity(rarity) != null)
        {
            return;
        }

        if (!ReportedRarityGaps.Add($"{effectType}/{rarity}"))
        {
            return;
        }

        // Forced: this is a silent data problem otherwise -- the effect renders with a blank value
        // everywhere (tooltip, enchant preview) and cannot be rolled at this rarity.
        EpicLoot.LogWarningForce($"Enchantment '{effectType}' has no values for rarity {rarity} in the " +
            "loaded magiceffects.json. Items carrying it at that rarity will show a blank value. If you " +
            "are on a dedicated server, the server's copy of magiceffects.json is the one in use.");
    }
}
