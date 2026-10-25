using System;
using System.Collections.Generic;

namespace EpicLoot;

/// <summary>
/// Everything Epic Loot contributes to one item's tooltip, gathered by <see cref="TooltipGatherer"/> and drawn
/// by <see cref="TooltipRenderer"/>. It is also the JSON the API hands to other mods (API.GetItemTooltipData)
/// and the shape tooltip editors receive and return, so field names are a public contract: add fields,
/// never rename or repurpose one, and bump <see cref="CurrentSchema"/> when the meaning of the data changes.
/// </summary>
/// <remarks>
/// Every field is an input to the renderer; nothing is worked out again after the editors have run, so an
/// edit always shows. Text fields hold what Epic Loot draws: rich text, mostly unlocalized $tokens (effect
/// lines arrive already localized). Plain vanilla lines (crafter, teleport, food, ...) are not in here.
/// </remarks>
[Serializable]
public class ItemTooltipData
{
    public const int CurrentSchema = 1;

    public int Schema = CurrentSchema;
    /// <summary>The quality level the values were worked out at.</summary>
    public int Quality;
    /// <summary>Whether the Shift details (<see cref="TooltipEffect.Detail"/> and the like) were gathered.</summary>
    public bool IncludeDetails;

    /// <summary>One of <see cref="TooltipKind"/>.</summary>
    public string Kind = TooltipKind.Mundane;
    /// <summary>True when Epic Loot draws the whole tooltip (the item carries magic data).</summary>
    public bool IsMagic;
    /// <summary>The tooltip's topic: the item name in its rarity colour.</summary>
    public string Title = "";
    public string DisplayName = "";
    /// <summary>Rarity index of a magic item (0 = Magic), or -1.</summary>
    public int Rarity = -1;
    public string RarityName = "";
    public string RarityColor = "";
    public string ItemTypeName = "";
    /// <summary>The "set item" line heading a magic set piece, or null.</summary>
    public string SetLabel;
    /// <summary>The colour of the set label, the set sections and a set rune's lines (the Set Item Color setting).</summary>
    public string SetColor = "";
    /// <summary>Rarity index of a magic crafting material or runestone (its "crafting material" line), or -1.</summary>
    public int MaterialRarity = -1;
    /// <summary>The description Epic Loot shows: a unique's lore, a set rune's text, else the item's own.</summary>
    public string Description = "";

    /// <summary>The stat lines magic can change, in drawing order. A line is drawn exactly when it is here.</summary>
    public List<TooltipStat> Stats = new List<TooltipStat>();
    public List<TooltipEffect> Effects = new List<TooltipEffect>();
    /// <summary>Null when the item has no sockets.</summary>
    public TooltipSockets Sockets;
    /// <summary>A vanilla set and an Epic Loot set can both apply to one item; vanilla's comes first.</summary>
    public List<TooltipSet> Sets = new List<TooltipSet>();
    /// <summary>What a loose shardstone grants once socketed. Null for anything else.</summary>
    public TooltipShardPreview ShardPreview;
    /// <summary>Null unless the item is a set rune.</summary>
    public TooltipSetRune SetRune;
    /// <summary>Key hints, drawn under the inventory grid's tooltip only.</summary>
    public List<TooltipHint> Hints = new List<TooltipHint>();
    /// <summary>Lines another mod added through a tooltip editor. Epic Loot leaves this empty.</summary>
    public List<TooltipLine> Extra = new List<TooltipLine>();

    /// <returns>The stat with this id and variant, or null.</returns>
    public TooltipStat GetStat(string id, string variant = null)
    {
        foreach (TooltipStat stat in Stats)
        {
            if (stat != null && stat.Id == id && stat.Variant == variant)
            {
                return stat;
            }
        }

        return null;
    }

    /// <summary>Drops null entries an editor may have left in the lists, so the renderer need not check.</summary>
    internal void Normalize()
    {
        Stats.RemoveAll(x => x == null);
        Effects.RemoveAll(x => x == null);
        Sockets?.Entries.RemoveAll(x => x == null);
        Sets.RemoveAll(x => x == null);
        foreach (TooltipSet set in Sets)
        {
            set.Pieces.RemoveAll(x => x == null);
            set.Bonuses.RemoveAll(x => x == null);
        }

        ShardPreview?.Slots.RemoveAll(x => x == null);
        SetRune?.Pieces.RemoveAll(x => x == null);
        SetRune?.Bonuses.RemoveAll(x => x == null);
        Hints.RemoveAll(x => x == null);
        Extra.RemoveAll(x => x == null);
    }

    /// <summary>Runs every text field through <paramref name="localize"/>. Ids, kinds and colours are left alone.</summary>
    internal void Localize(Func<string, string> localize)
    {
        string L(string text) => string.IsNullOrEmpty(text) ? text : localize(text);

        Title = L(Title);
        DisplayName = L(DisplayName);
        RarityName = L(RarityName);
        ItemTypeName = L(ItemTypeName);
        SetLabel = L(SetLabel);
        Description = L(Description);

        foreach (TooltipEffect effect in Effects)
        {
            effect.Text = L(effect.Text);
            effect.Detail = L(effect.Detail);
        }

        if (Sockets != null)
        {
            foreach (TooltipSocket socket in Sockets.Entries)
            {
                socket.Text = L(socket.Text);
                socket.Detail = L(socket.Detail);
            }
        }

        foreach (TooltipSet set in Sets)
        {
            set.Name = L(set.Name);
            foreach (TooltipSetPiece piece in set.Pieces)
            {
                piece.Name = L(piece.Name);
            }

            LocalizeBonuses(set.Bonuses, L);
        }

        if (ShardPreview != null)
        {
            ShardPreview.Exclusive = L(ShardPreview.Exclusive);
            foreach (TooltipShardSlot slot in ShardPreview.Slots)
            {
                slot.Name = L(slot.Name);
                slot.Text = L(slot.Text);
                slot.Detail = L(slot.Detail);
            }
        }

        if (SetRune != null)
        {
            SetRune.Name = L(SetRune.Name);
            SetRune.Rarities = L(SetRune.Rarities);
            foreach (TooltipSetRunePiece piece in SetRune.Pieces)
            {
                piece.Name = L(piece.Name);
                piece.Requirements = L(piece.Requirements);
            }

            LocalizeBonuses(SetRune.Bonuses, L);
        }

        foreach (TooltipHint hint in Hints)
        {
            hint.Text = L(hint.Text);
        }

        foreach (TooltipLine line in Extra)
        {
            line.Text = L(line.Text);
        }
    }

    private static void LocalizeBonuses(List<TooltipSetBonus> bonuses, Func<string, string> localize)
    {
        foreach (TooltipSetBonus bonus in bonuses)
        {
            bonus.Text = localize(bonus.Text);
            bonus.Detail = localize(bonus.Detail);
            bonus.ActivationKey = localize(bonus.ActivationKey);
        }
    }
}

/// <summary>Values of <see cref="ItemTooltipData.Kind"/>.</summary>
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
public class TooltipStat
{
    /// <summary>The weapon's stamina cost replaced by Bloodlust's health-scaled one (drawn in red).</summary>
    public const string Bloodlust = "Bloodlust";
    /// <summary>Durability replaced by the Indestructible effect; <see cref="Value"/> is unused.</summary>
    public const string Indestructible = "Indestructible";
    /// <summary>Health use as a percentage of current health, the second $item_healthuse line.</summary>
    public const string Percent = "Percent";

    public string Id = "";
    /// <summary>Null for the plain line, else one of the constants above.</summary>
    public string Variant;
    public float Value;
    /// <summary>
    /// The figure in brackets: the whole stack's weight, the block power at the player's skill, or the
    /// player's total on an equipment modifier line. Null when the line has none.
    /// </summary>
    public float? Secondary;
    /// <summary>A damage line's roll range at the player's skill, or durability as current / maximum.</summary>
    public float? Min;
    public float? Max;
    /// <summary>Magic changes this line, so it is drawn in the rarity colour.</summary>
    public bool Modified;
}

/// <summary>Values of <see cref="TooltipEffect.Marker"/>.</summary>
public static class TooltipEffectMarker
{
    public const string Plain = "Plain";
    public const string Augmented = "Augmented";
    public const string Tempered = "Tempered";
}

[Serializable]
public class TooltipEffect
{
    public string EffectType = "";
    public float Value;
    /// <summary>The effect's display text with its values filled in, without the rarity flare.</summary>
    public string Text = "";
    /// <summary>One of <see cref="TooltipEffectMarker"/>; the renderer picks the glyph.</summary>
    public string Marker = TooltipEffectMarker.Plain;
    /// <summary>The range this effect rolls in at the item's rarity, or null for a valueless effect.</summary>
    public float? RollMin;
    public float? RollMax;
    /// <summary>The Shift block (description, range, config), indented as drawn. Null without details.</summary>
    public string Detail;
}

/// <summary>Values of <see cref="TooltipSocket.State"/>.</summary>
public static class TooltipSocketState
{
    public const string Active = "Active";
    /// <summary>A runestone held inert because rune socketing is turned off.</summary>
    public const string InactiveRune = "InactiveRune";
    /// <summary>A shard with no effect for this item type.</summary>
    public const string InertShard = "InertShard";
}

/// <summary>Values of the removal fields: how a socketed stone may leave its socket.</summary>
public static class TooltipRemoval
{
    public const string Free = "Free";
    public const string BreakOnly = "BreakOnly";
    public const string Locked = "Locked";
}

[Serializable]
public class TooltipSockets
{
    public int Count;
    public int Used;
    public int Open;
    public List<TooltipSocket> Entries = new List<TooltipSocket>();
}

[Serializable]
public class TooltipSocket
{
    /// <summary>The prefab of the stone in the socket; the renderer draws its icon from this.</summary>
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
    public string Detail;
}

[Serializable]
public class TooltipSet
{
    public string SetId = "";
    public string Name = "";
    /// <summary>A vanilla set (troll armor), rather than an Epic Loot legendary set.</summary>
    public bool IsMundane;
    /// <summary>False for an Epic Loot set id that is no longer in legendaries.json.</summary>
    public bool Known = true;
    public int Equipped;
    public int Full;
    public List<TooltipSetPiece> Pieces = new List<TooltipSetPiece>();
    public List<TooltipSetBonus> Bonuses = new List<TooltipSetBonus>();
}

[Serializable]
public class TooltipSetPiece
{
    public string Id = "";
    public string Name = "";
    public bool Equipped;
    /// <summary>The rarity an equipped Epic Loot piece is worn at, or -1.</summary>
    public int Rarity = -1;
}

[Serializable]
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
    /// <summary>The bonus has per-rarity values, so its tier is worth showing.</summary>
    public bool TierVaries;
    /// <summary>Its description, with details only.</summary>
    public string Detail;
    /// <summary>The key that activates it, with details only.</summary>
    public string ActivationKey;
}

[Serializable]
public class TooltipShardPreview
{
    /// <summary>The shard has no effect on any item type.</summary>
    public bool NoEffect;
    /// <summary>One effect on every slot (boss shards); <see cref="Slots"/> then holds at most one entry.</summary>
    public bool Uniform;
    public List<TooltipShardSlot> Slots = new List<TooltipShardSlot>();
    /// <summary>The "one per item" line of an exclusive shard, or null.</summary>
    public string Exclusive;
    /// <summary>A removal rule that applies to every slot (one of <see cref="TooltipRemoval"/>).</summary>
    public string BlanketRemoval = TooltipRemoval.Free;
}

[Serializable]
public class TooltipShardSlot
{
    /// <summary>The item type, or "all slots" for a uniform shard.</summary>
    public string Name = "";
    public string EffectType = "";
    public float Value;
    public string Text = "";
    /// <summary>Drawn per slot only while <see cref="TooltipShardPreview.BlanketRemoval"/> is Free.</summary>
    public string Removal = TooltipRemoval.Free;
    public string Detail;
}

[Serializable]
public class TooltipSetRune
{
    public string SetId = "";
    /// <summary>False when the set is no longer in legendaries.json; only the id is known then.</summary>
    public bool Known;
    public string Name = "";
    /// <summary>The rarities the set drops at, as one line.</summary>
    public string Rarities = "";
    public List<TooltipSetRunePiece> Pieces = new List<TooltipSetRunePiece>();
    public List<TooltipSetBonus> Bonuses = new List<TooltipSetBonus>();
}

[Serializable]
public class TooltipSetRunePiece
{
    public string Name = "";
    /// <summary>What an item needs to take this piece.</summary>
    public string Requirements = "";
    /// <summary>The piece has requirements beyond its item type, marked with an asterisk.</summary>
    public bool FurtherRestricted;
}

/// <summary>Values of <see cref="TooltipHint.Id"/>.</summary>
public static class TooltipHintId
{
    public const string SocketOpen = "socket_open";
    public const string Chisel = "chisel";
    public const string EffectDetails = "effect_details";
}

[Serializable]
public class TooltipHint
{
    public string Id = "";
    public string Text = "";
}

/// <summary>Values of <see cref="TooltipLine.Anchor"/>: where an added line is drawn.</summary>
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
    /// <summary>At the end. A line with an anchor not listed here is drawn here too.</summary>
    public const string End = "End";

    internal static bool IsKnown(string anchor)
    {
        return anchor == AfterHeader || anchor == AfterStats || anchor == AfterEffects || anchor == AfterSet ||
            anchor == End;
    }
}

[Serializable]
public class TooltipLine
{
    /// <summary>One of <see cref="TooltipAnchor"/>. On an item Epic Loot does not draw, every line goes at the end.</summary>
    public string Anchor = TooltipAnchor.End;
    public string Text = "";
}
