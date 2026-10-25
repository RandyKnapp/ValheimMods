using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EpicLootAPI;

/// <summary>
/// Epic Loot's item tooltip: read what Epic Loot shows for an item, change it before Epic Loot draws it, or
/// take over drawing the inventory tooltip and draw it from the data. Requires API 3.
/// </summary>
public static partial class EpicLoot
{
    private static readonly Method API_GetItemTooltipData = new("GetItemTooltipData");
    private static readonly Method API_SetTooltipOverride = new("SetTooltipOverride");
    private static readonly Method API_IsTooltipOverridden = new("IsTooltipOverridden");
    private static readonly Method API_GetItemTooltipWithoutEpicLoot = new("GetItemTooltipWithoutEpicLoot");

    private static readonly Method API_AddTooltipDataEditor = new(
        "AddTooltipDataEditor",
        typeof(string),
        typeof(Func<ItemDrop.ItemData, string, string>));

    private static readonly Method API_RemoveTooltipDataEditor = new("RemoveTooltipDataEditor");

    /// <summary>
    /// Everything Epic Loot contributes to this item's tooltip, after every registered tooltip editor ran.
    /// Plain vanilla lines (crafter, teleport, food, ...) are not part of it.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <param name="quality">The quality to work the values out at; -1 for the item's own.</param>
    /// <param name="includeDetails">Also fill what Epic Loot shows while Shift is held: effect descriptions,
    /// ranges and config values, set bonus descriptions and activation keys.</param>
    /// <param name="localize">Localize the text fields (ids, kinds and colours never are).</param>
    /// <returns>null for a null item, before a local player exists, or without Epic Loot (or before API 3)</returns>
    [PublicAPI]
    public static ItemTooltipData? GetTooltipData(this ItemDrop.ItemData item, int quality = -1,
        bool includeDetails = false, bool localize = true)
    {
        string json = (string)(API_GetItemTooltipData.Invoke(item, quality, includeDetails, localize)[0] ?? "");
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<ItemTooltipData>(json);
        }
        catch
        {
            logger.LogWarning("Failed to parse item tooltip data");
            return null;
        }
    }

    /// <summary>
    /// Tells Epic Loot your plugin draws the inventory item tooltip. While any plugin holds the override, the
    /// inventory tooltip is vanilla's (InventoryGrid.CreateItemTooltip runs as vanilla, and GetTooltip inside it
    /// returns vanilla's text) and Epic Loot leaves its box alone (no resizing, scrolling or Ctrl comparison).
    /// GetTooltip anywhere else keeps Epic Loot's text. Every change is written to the player's log, naming
    /// <paramref name="ownerId"/>.
    /// </summary>
    /// <param name="ownerId">Your plugin GUID</param>
    /// <param name="overriding">true to take over, false to give it back</param>
    /// <returns>true if Epic Loot accepted the call</returns>
    [PublicAPI]
    public static bool SetTooltipOverride(string ownerId, bool overriding)
    {
        object[] result = API_SetTooltipOverride.Invoke(ownerId, overriding);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Set tooltip override: {ownerId} {overriding}, {output}");
        return output;
    }

    /// <returns>true while some plugin overrides the inventory item tooltip</returns>
    [PublicAPI]
    public static bool IsTooltipOverridden()
    {
        return (bool)(API_IsTooltipOverridden.Invoke()[0] ?? false);
    }

    /// <summary>
    /// The item's tooltip text with Epic Loot's part switched off for this one call: vanilla's text plus what
    /// other plugins add. Draw Epic Loot's part from <see cref="GetTooltipData"/>. Unlocalized, as GetTooltip
    /// returns it.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <param name="quality">-1 for the item's own</param>
    /// <param name="crafting">true for a crafting preview</param>
    /// <param name="worldLevel">-1 for the item's own</param>
    /// <param name="stackOverride">-1 for the item's own stack</param>
    /// <returns>The text; "" for a null item, without Epic Loot, or before API 3</returns>
    [PublicAPI]
    public static string GetTooltipWithoutEpicLoot(this ItemDrop.ItemData item, int quality = -1,
        bool crafting = false, float worldLevel = -1f, int stackOverride = -1)
    {
        return (string)(API_GetItemTooltipWithoutEpicLoot.Invoke(item, quality, crafting, worldLevel, stackOverride)[0] ?? "");
    }

    /// <summary>
    /// Changes Epic Loot's tooltip data before anything reads it: Epic Loot's own tooltip, Auga's and
    /// <see cref="GetTooltipData"/> all see your edit. Change a value, remove an effect or a hint, or add a line
    /// to <see cref="ItemTooltipData.Extra"/>.
    /// </summary>
    /// <remarks>
    /// Runs every frame a tooltip is shown, in registration order, so keep it cheap. The data it gets is
    /// unlocalized, as Epic Loot draws it. An editor that throws is skipped, and Epic Loot logs that once.
    /// </remarks>
    /// <param name="id">Unique id, normally your plugin GUID</param>
    /// <param name="editor">Given the item and the data, returns the data to use; null leaves it unchanged.</param>
    /// <returns>true if registered</returns>
    [PublicAPI]
    public static bool AddTooltipEditor(string id, Func<ItemDrop.ItemData, ItemTooltipData, ItemTooltipData?> editor)
    {
        if (editor == null)
        {
            return false;
        }

        Func<ItemDrop.ItemData, string, string> jsonEditor = (item, json) =>
        {
            ItemTooltipData? data = JsonConvert.DeserializeObject<ItemTooltipData>(json);
            ItemTooltipData? edited = data == null ? null : editor(item, data);
            return edited == null ? null! : JsonConvert.SerializeObject(edited);
        };

        object[] result = API_AddTooltipDataEditor.Invoke(id, jsonEditor);
        bool output = (bool)(result[0] ?? false);
        logger.LogDebug($"Registered tooltip editor: {id}, {output}");
        return output;
    }

    /// <param name="id">The id passed to <see cref="AddTooltipEditor"/></param>
    /// <returns>true if an editor was removed</returns>
    [PublicAPI]
    public static bool RemoveTooltipEditor(string id)
    {
        return (bool)(API_RemoveTooltipDataEditor.Invoke(id)[0] ?? false);
    }
}

/// <summary>
/// Everything Epic Loot contributes to one item's tooltip. Kept field-for-field in sync with
/// EpicLoot/src/Tooltip/TooltipData.cs -- a field missing here is dropped when an editor's result goes back.
/// </summary>
/// <remarks>
/// Text fields hold rich text, with $tokens unless the data was fetched localized. Every field is what Epic
/// Loot draws from, so an editor's change always shows.
/// </remarks>
[Serializable]
[PublicAPI]
public class ItemTooltipData
{
    /// <summary>Bumped when the meaning of the data changes.</summary>
    public int Schema;
    /// <summary>The quality level the values were worked out at.</summary>
    public int Quality;
    /// <summary>Whether the Shift details were gathered.</summary>
    public bool IncludeDetails;

    /// <summary>One of <see cref="TooltipKind"/>.</summary>
    public string Kind = TooltipKind.Mundane;
    /// <summary>True when Epic Loot draws the whole tooltip (the item carries magic data).</summary>
    public bool IsMagic;
    /// <summary>The tooltip's topic: the item name in its rarity colour.</summary>
    public string Title = "";
    public string DisplayName = "";
    /// <summary>Rarity index of a magic item (cast to <see cref="ItemRarity"/>), or -1.</summary>
    public int Rarity = -1;
    public string RarityName = "";
    public string RarityColor = "";
    public string ItemTypeName = "";
    /// <summary>The "set item" line heading a magic set piece, or null.</summary>
    public string? SetLabel;
    /// <summary>The colour of the set label, the set sections and a set rune's lines.</summary>
    public string SetColor = "";
    /// <summary>Rarity index of a magic crafting material or runestone, or -1.</summary>
    public int MaterialRarity = -1;
    public string Description = "";

    /// <summary>The stat lines magic can change, in drawing order. A line is drawn exactly when it is here.</summary>
    public List<TooltipStat> Stats = new();
    public List<TooltipEffect> Effects = new();
    /// <summary>Null when the item has no sockets.</summary>
    public TooltipSockets? Sockets;
    /// <summary>A vanilla set and an Epic Loot set can both apply; vanilla's comes first.</summary>
    public List<TooltipSet> Sets = new();
    /// <summary>What a loose shardstone grants once socketed; null for anything else.</summary>
    public TooltipShardPreview? ShardPreview;
    /// <summary>Null unless the item is a set rune.</summary>
    public TooltipSetRune? SetRune;
    /// <summary>Key hints, drawn under the inventory grid's tooltip only.</summary>
    public List<TooltipHint> Hints = new();
    /// <summary>Lines added by tooltip editors.</summary>
    public List<TooltipLine> Extra = new();
}

/// <summary>Values of <see cref="ItemTooltipData.Kind"/>.</summary>
[PublicAPI]
public static class TooltipKind
{
    public const string Magic = "Magic";
    public const string Shard = "Shard";
    public const string SetRune = "SetRune";
    /// <summary>Brokkr's Gift.</summary>
    public const string Chisel = "Chisel";
    public const string Runestone = "Runestone";
    public const string MagicMaterial = "MagicMaterial";
    public const string Mundane = "Mundane";
}

/// <summary>
/// One stat line magic can change, keyed by the vanilla label token it is drawn with ($item_weight,
/// $inventory_slash, $se_runstamina, ...).
/// </summary>
[Serializable]
[PublicAPI]
public class TooltipStat
{
    /// <summary>The stamina cost replaced by Bloodlust's health-scaled one.</summary>
    public const string Bloodlust = "Bloodlust";
    /// <summary>Durability replaced by the Indestructible effect; <see cref="Value"/> is unused.</summary>
    public const string Indestructible = "Indestructible";
    /// <summary>Health use as a percentage of current health, the second $item_healthuse line.</summary>
    public const string Percent = "Percent";

    public string Id = "";
    /// <summary>Null for the plain line, else one of the constants above.</summary>
    public string? Variant;
    public float Value;
    /// <summary>The figure in brackets: the stack's weight, the block power at the player's skill, or the
    /// player's total on an equipment modifier line.</summary>
    public float? Secondary;
    /// <summary>A damage line's roll range at the player's skill, or durability as current / maximum.</summary>
    public float? Min;
    public float? Max;
    /// <summary>Magic changes this line (drawn in the rarity colour).</summary>
    public bool Modified;
}

/// <summary>Values of <see cref="TooltipEffect.Marker"/>.</summary>
[PublicAPI]
public static class TooltipEffectMarker
{
    public const string Plain = "Plain";
    public const string Augmented = "Augmented";
    public const string Tempered = "Tempered";
}

[Serializable]
[PublicAPI]
public class TooltipEffect
{
    public string EffectType = "";
    public float Value;
    /// <summary>The effect's display text with its values filled in.</summary>
    public string Text = "";
    /// <summary>One of <see cref="TooltipEffectMarker"/>.</summary>
    public string Marker = TooltipEffectMarker.Plain;
    /// <summary>The range it rolls in at the item's rarity; null for a valueless effect.</summary>
    public float? RollMin;
    public float? RollMax;
    /// <summary>The Shift block (description, range, config), indented as drawn. Null without details.</summary>
    public string? Detail;
}

/// <summary>Values of <see cref="TooltipSocket.State"/>.</summary>
[PublicAPI]
public static class TooltipSocketState
{
    public const string Active = "Active";
    /// <summary>A runestone held inert because rune socketing is turned off.</summary>
    public const string InactiveRune = "InactiveRune";
    /// <summary>A shard with no effect for this item type.</summary>
    public const string InertShard = "InertShard";
}

/// <summary>Values of the removal fields: how a socketed stone may leave its socket.</summary>
[PublicAPI]
public static class TooltipRemoval
{
    public const string Free = "Free";
    public const string BreakOnly = "BreakOnly";
    public const string Locked = "Locked";
}

[Serializable]
[PublicAPI]
public class TooltipSockets
{
    public int Count;
    public int Used;
    public int Open;
    public List<TooltipSocket> Entries = new();
}

[Serializable]
[PublicAPI]
public class TooltipSocket
{
    /// <summary>The prefab of the stone in the socket.</summary>
    public string SourcePrefab = "";
    public int Rarity;
    /// <summary>Empty for an inert shard.</summary>
    public string EffectType = "";
    public float Value;
    public string Text = "";
    /// <summary>One of <see cref="TooltipSocketState"/>.</summary>
    public string State = TooltipSocketState.Active;
    /// <summary>The share of its full value the socket grants, below 1 when stacking rules cut it.</summary>
    public float StackMultiplier = 1f;
    /// <summary>One of <see cref="TooltipRemoval"/>.</summary>
    public string Removal = TooltipRemoval.Free;
    public string? Detail;
}

[Serializable]
[PublicAPI]
public class TooltipSet
{
    public string SetId = "";
    public string Name = "";
    /// <summary>A vanilla set rather than an Epic Loot legendary set.</summary>
    public bool IsMundane;
    /// <summary>False for an Epic Loot set id that is no longer configured.</summary>
    public bool Known = true;
    public int Equipped;
    public int Full;
    public List<TooltipSetPiece> Pieces = new();
    public List<TooltipSetBonus> Bonuses = new();
}

[Serializable]
[PublicAPI]
public class TooltipSetPiece
{
    public string Id = "";
    public string Name = "";
    public bool Equipped;
    /// <summary>The rarity an equipped Epic Loot piece is worn at, or -1.</summary>
    public int Rarity = -1;
}

[Serializable]
[PublicAPI]
public class TooltipSetBonus
{
    /// <summary>Pieces needed.</summary>
    public int Count;
    public string EffectType = "";
    public float Value;
    public string Text = "";
    public bool Active;
    /// <summary>The rarity an active bonus applies at, or -1.</summary>
    public int Tier = -1;
    /// <summary>The bonus has per-rarity values.</summary>
    public bool TierVaries;
    /// <summary>Its description, with details only.</summary>
    public string? Detail;
    /// <summary>The key that activates it, with details only.</summary>
    public string? ActivationKey;
}

[Serializable]
[PublicAPI]
public class TooltipShardPreview
{
    /// <summary>The shard has no effect on any item type.</summary>
    public bool NoEffect;
    /// <summary>One effect on every slot (boss shards).</summary>
    public bool Uniform;
    public List<TooltipShardSlot> Slots = new();
    /// <summary>The "one per item" line of an exclusive shard, or null.</summary>
    public string? Exclusive;
    /// <summary>A removal rule for every slot (one of <see cref="TooltipRemoval"/>).</summary>
    public string BlanketRemoval = TooltipRemoval.Free;
}

[Serializable]
[PublicAPI]
public class TooltipShardSlot
{
    /// <summary>The item type, or "all slots" for a uniform shard.</summary>
    public string Name = "";
    public string EffectType = "";
    public float Value;
    public string Text = "";
    public string Removal = TooltipRemoval.Free;
    public string? Detail;
}

[Serializable]
[PublicAPI]
public class TooltipSetRune
{
    public string SetId = "";
    /// <summary>False when the set is no longer configured; only the id is known then.</summary>
    public bool Known;
    public string Name = "";
    /// <summary>The rarities the set drops at, as one line.</summary>
    public string Rarities = "";
    public List<TooltipSetRunePiece> Pieces = new();
    public List<TooltipSetBonus> Bonuses = new();
}

[Serializable]
[PublicAPI]
public class TooltipSetRunePiece
{
    public string Name = "";
    /// <summary>What an item needs to take this piece.</summary>
    public string Requirements = "";
    public bool FurtherRestricted;
}

/// <summary>Values of <see cref="TooltipHint.Id"/>.</summary>
[PublicAPI]
public static class TooltipHintId
{
    public const string SocketOpen = "socket_open";
    public const string Chisel = "chisel";
    public const string EffectDetails = "effect_details";
}

[Serializable]
[PublicAPI]
public class TooltipHint
{
    public string Id = "";
    public string Text = "";
}

/// <summary>Values of <see cref="TooltipLine.Anchor"/>: where an added line is drawn.</summary>
[PublicAPI]
public static class TooltipAnchor
{
    /// <summary>After the description.</summary>
    public const string AfterHeader = "AfterHeader";
    /// <summary>After the stat lines, before the effects.</summary>
    public const string AfterStats = "AfterStats";
    /// <summary>After the effects and sockets (or a shard's preview).</summary>
    public const string AfterEffects = "AfterEffects";
    /// <summary>After the set section.</summary>
    public const string AfterSet = "AfterSet";
    /// <summary>At the end; also where a line with an unknown anchor goes.</summary>
    public const string End = "End";
}

[Serializable]
[PublicAPI]
public class TooltipLine
{
    /// <summary>One of <see cref="TooltipAnchor"/>. On an item Epic Loot does not draw, every line goes at the end.</summary>
    public string Anchor = TooltipAnchor.End;
    public string Text = "";
}
