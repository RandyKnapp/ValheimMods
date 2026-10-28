using EpicLoot.Compatibility;
using EpicLoot.Crafting;
using EpicLoot.LegendarySystem;
using EpicLoot.ShardStones;

namespace EpicLoot;

/// <summary>
/// The one place Epic Loot works out what an item's tooltip shows. <see cref="Gather"/> fills an
/// <see cref="ItemTooltipData"/>; <see cref="TooltipRenderer"/> draws it, and the API hands it to other mods.
/// Nothing here writes text layout: what is gathered is the values, the already-formatted effect lines and the
/// tokens, so the same data serves Epic Loot's own tooltip, Auga's, and a mod drawing its own.
/// </summary>
/// <remarks>
/// Call it through <see cref="ItemTooltip.GetData"/>, which caches it for the frame, runs it inside the
/// Elemental Archery skill scope and applies the registered tooltip editors.
/// </remarks>
internal partial class TooltipGatherer
{
    private readonly ItemDrop.ItemData item;
    // Null for an item without magic data, which only gets the header, set and hint parts.
    private readonly MagicItem magicItem;
    private readonly int qualityLevel;
    private readonly bool includeDetails;
    private readonly Player player;
    private readonly ItemTooltipData data;

    private TooltipGatherer(ItemDrop.ItemData item, int qualityLevel, bool includeDetails, Player player)
    {
        this.item = item;
        magicItem = item.GetMagicItem();
        this.qualityLevel = qualityLevel;
        this.includeDetails = includeDetails;
        this.player = player;
        data = new ItemTooltipData
        {
            Quality = qualityLevel,
            IncludeDetails = includeDetails
        };
    }

    /// <returns>The tooltip data, or null without an item or a local player (the stat and set values are the
    /// local player's).</returns>
    public static ItemTooltipData Gather(ItemDrop.ItemData item, int qualityLevel, bool includeDetails)
    {
        if (item == null || Player.m_localPlayer == null)
        {
            return null;
        }

        TooltipGatherer gatherer = new TooltipGatherer(item, qualityLevel, includeDetails, Player.m_localPlayer);
        gatherer.GatherAll();
        return gatherer.data;
    }

    private void GatherAll()
    {
        GatherHeader();

        if (magicItem == null)
        {
            GatherSets();
        }
        else if (item.IsSetRune())
        {
            // A set rune is drawn as a rune, not as a piece: no stats beyond its weight, no effects, and the set
            // it carries described rather than its progress.
            GatherWeight();
            GatherSetRune();
        }
        else
        {
            GatherStats();
            if (Shards.IsShard(item))
            {
                GatherShardPreview();
            }
            else
            {
                data.Effects = GatherEffects(magicItem, includeDetails);
                data.Sockets = GatherSockets(magicItem, includeDetails);
            }

            GatherSets();
        }

        GatherHints();
    }

    private void GatherHeader()
    {
        data.Kind = GetKind();
        data.IsMagic = magicItem != null;
        data.Title = item.GetDecoratedName();
        data.DisplayName = item.GetDisplayName();
        data.SetColor = EpicLoot.GetSetItemColor();
        data.Description = item.GetDescription();

        if (item.IsMagicCraftingMaterial() || item.IsRunestone())
        {
            data.MaterialRarity = (int)item.GetCraftingMaterialRarity();
        }

        if (magicItem == null)
        {
            return;
        }

        data.Rarity = (int)magicItem.Rarity;
        data.RarityName = EpicLoot.GetRarityDisplayName(magicItem.Rarity);
        data.RarityColor = magicItem.GetColorString();
        data.ItemTypeName = magicItem.GetItemTypeName(item);

        // A set rune carries a SetID too, but it is not a piece of the set.
        if (item.IsSetRune())
        {
            data.SetLabel = "$mod_epicloot_setrune_label";
        }
        else if (item.IsMagicSetItem())
        {
            data.SetLabel = UniqueLegendaryHelper.GetSetLabelToken(item.GetRarity());
        }
    }

    private string GetKind()
    {
        if (item.IsSetRune())
        {
            return TooltipKind.SetRune;
        }

        if (Shards.IsShard(item))
        {
            return TooltipKind.Shard;
        }

        if (item.IsShardSlotChisel())
        {
            return TooltipKind.Chisel;
        }

        if (item.IsRunestone())
        {
            return TooltipKind.Runestone;
        }

        if (item.IsMagicCraftingMaterial())
        {
            return TooltipKind.MagicMaterial;
        }

        return magicItem != null ? TooltipKind.Magic : TooltipKind.Mundane;
    }

    private void GatherHints()
    {
        // Ahead of the shard slot line: it is how the item opens on its own, which the shard slot gesture
        // is the exception to.
        string containerHint = BowsBeforeHoesCompat.GetOpenPrompt(item);
        if (!string.IsNullOrEmpty(containerHint))
        {
            data.Hints.Add(new TooltipHint { Id = TooltipHintId.ContainerOpen, Text = containerHint });
        }

        string socketHint = GetSocketOpenHint();
        if (socketHint != null)
        {
            data.Hints.Add(new TooltipHint
            {
                Id = item.IsShardSlotChisel() ? TooltipHintId.Chisel : TooltipHintId.SocketOpen,
                Text = socketHint
            });
        }

        if (HasEffectDetailsHint())
        {
            data.Hints.Add(new TooltipHint
            {
                Id = TooltipHintId.EffectDetails,
                Text = "[<color=yellow><b>$KEY_LTrigger</b></color>] $mod_epicloot_hold_details"
            });
        }
    }

    // Tells the player how to open an item's shard slots. Only the inventory grid draws it: the enchanting
    // panels, tempering panel, craft dialogs and trader lists show the same tooltip, and the hint means
    // nothing there.
    private string GetSocketOpenHint()
    {
        // Brokkr's Gift is used by dragging it, which is not discoverable on its own -- say so here.
        // Checked first: the gift carries a cosmetic MagicItem, so the socket branch below would
        // otherwise have to know to skip it.
        if (item.IsShardSlotChisel())
        {
            return "<color=yellow><b>$mod_epicloot_slotchisel_hint</b></color>";
        }

        if (magicItem == null || !magicItem.HasSockets())
        {
            return null;
        }

        // The gamepad gesture is written as $KEY_ glyph tokens, which only resolve to pad glyphs while a
        // gamepad is active (Localization looks up "Joy" + the binding name) -- exactly when that branch is
        // taken. The keyboard shortcut has no $KEY_ binding of its own, so its key names go in as they are
        // ("Left Control + E"). No hint where the gesture is turned off, or where it would not open these
        // slots: a shortcut that is only the Use key leaves another mod's container to that mod.
        string keys = ZInput.IsGamepadActive()
            ? SocketOverlayInput.DescribeGamepad()
            : SocketOverlayInput.IsBlockedFor(item, gamepad: false) ? null : SocketOverlayInput.DescribeShortcut();
        if (keys == null)
        {
            return null;
        }

        return $"[<color=yellow><b>{keys}</b></color>] $mod_epicloot_press_socket";
    }

    // The gamepad has no Shift, so the left trigger that stands in for it is named under anything with details.
    private bool HasEffectDetailsHint()
    {
        if (!ZInput.IsGamepadActive())
        {
            return false;
        }

        // A set rune has no effects of its own, but its set bonuses have details.
        return Shards.IsShard(item) || item.IsSetRune() || (magicItem != null && magicItem.Effects.Count > 0);
    }

    private static string ToRemoval(SocketRemoval removal)
    {
        switch (removal)
        {
            case SocketRemoval.BreakOnly:
                return TooltipRemoval.BreakOnly;
            case SocketRemoval.Locked:
                return TooltipRemoval.Locked;
            default:
                return TooltipRemoval.Free;
        }
    }
}
