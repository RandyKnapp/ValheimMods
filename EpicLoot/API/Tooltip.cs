using JetBrains.Annotations;
using Newtonsoft.Json;
using System;

namespace EpicLoot;

/// <summary>
/// Epic Loot's item tooltip for other plugins: read what Epic Loot would show for an item, change it before
/// Epic Loot draws it, or take over drawing the inventory tooltip entirely and draw it from that data. This
/// replaces patching Epic Loot's tooltip builders, which are internal and change shape between releases.
/// </summary>
/// <remarks>
/// The data is Epic Loot's part of the tooltip only: rarity and title, the stat lines magic can change (with
/// a flag saying whether it did), effects, sockets, sets, shard and set-rune previews and the key hints.
/// Plain vanilla lines (crafter, teleport, food, ...) are not in it. The JSON shape is documented in
/// docs/API.md and mirrored by the EpicLootAPI shim's ItemTooltipData.
/// </remarks>
public static partial class API
{
    /// <summary>
    /// Everything Epic Loot contributes to an item's tooltip, after the registered tooltip data editors have
    /// run, as JSON.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <param name="qualityLevel">The quality to work the values out at; below 1 means the item's own.</param>
    /// <param name="includeDetails">Also fill the detail fields Epic Loot shows while Shift is held: effect
    /// descriptions, roll ranges and config values, set bonus descriptions and activation keys.</param>
    /// <param name="localize">Run every text field through the game's localization. Ids, kinds and colours are
    /// never localized.</param>
    /// <returns>The JSON, or "" for a null item, before a local player exists, or on an error</returns>
    [PublicAPI]
    public static string GetItemTooltipData(ItemDrop.ItemData item, int qualityLevel, bool includeDetails, bool localize)
    {
        if (item == null)
        {
            return "";
        }

        try
        {
            ItemTooltipData data = ItemTooltip.GetData(item, qualityLevel < 1 ? item.m_quality : qualityLevel, includeDetails);
            if (data == null)
            {
                return "";
            }

            string json = JsonConvert.SerializeObject(data, ItemTooltip.JsonSettings);
            if (!localize)
            {
                return json;
            }

            // A copy: the data is cached for the frame and drawn by Epic Loot unlocalized.
            ItemTooltipData copy = JsonConvert.DeserializeObject<ItemTooltipData>(json, ItemTooltip.JsonSettings);
            copy.Localize(text => Localization.instance.Localize(text));
            return JsonConvert.SerializeObject(copy, ItemTooltip.JsonSettings);
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"GetItemTooltipData failed for {item.m_shared?.m_name}: {ex}");
            return "";
        }
    }

    /// <summary>
    /// Tells Epic Loot that your plugin draws the inventory item tooltip. While any plugin holds the override,
    /// InventoryGrid.CreateItemTooltip runs as vanilla, the ItemData.GetTooltip calls made inside it return
    /// vanilla's text, and Epic Loot leaves the inventory tooltip's box alone (sizing, scrolling, the Ctrl
    /// comparison). Nothing else changes: GetTooltip anywhere else -- the crafting panel, other mods' windows,
    /// Epic Loot's own panels -- still carries Epic Loot's text. Draw Epic Loot's part from
    /// <see cref="GetItemTooltipData"/>, and get the rest of an item's text from
    /// <see cref="GetItemTooltipWithoutEpicLoot"/>.
    /// </summary>
    /// <remarks>Every change is logged at info level, whatever the logging settings, so a player's log shows
    /// which plugin took the tooltip over. Calling it again with the same state does nothing.</remarks>
    /// <param name="ownerId">Your plugin GUID; each plugin holds or releases its own claim.</param>
    /// <param name="overriding">true to take over, false to give it back</param>
    /// <returns>false if <paramref name="ownerId"/> is empty</returns>
    [PublicAPI]
    public static bool SetTooltipOverride(string ownerId, bool overriding)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            OnError?.Invoke("Failed to set tooltip override: owner id is empty");
            return false;
        }

        ItemTooltip.SetOverride(ownerId, overriding);
        return true;
    }

    /// <returns>true while some plugin overrides the inventory item tooltip</returns>
    [PublicAPI]
    public static bool IsTooltipOverridden()
    {
        return ItemTooltip.IsOverridden;
    }

    /// <summary>
    /// <c>ItemDrop.ItemData.GetTooltip</c> with Epic Loot's part switched off for this one call: vanilla's text,
    /// plus whatever other plugins add to it. For a plugin that draws Epic Loot's part itself from
    /// <see cref="GetItemTooltipData"/>; every other GetTooltip call keeps Epic Loot's text.
    /// </summary>
    /// <param name="item">may be null</param>
    /// <param name="qualityLevel">below 1 means the item's own quality</param>
    /// <param name="crafting">true for a crafting preview, as vanilla's crafting panel passes it</param>
    /// <param name="worldLevel">below 0 means the item's own world level</param>
    /// <param name="stackOverride">the stack size to show, or -1 for the item's own</param>
    /// <returns>The text (unlocalized, as GetTooltip returns it), or "" for a null item</returns>
    [PublicAPI]
    public static string GetItemTooltipWithoutEpicLoot(ItemDrop.ItemData item, int qualityLevel, bool crafting,
        float worldLevel, int stackOverride)
    {
        if (item == null)
        {
            return "";
        }

        return ItemTooltip.GetTextWithoutEpicLoot(item, qualityLevel < 1 ? item.m_quality : qualityLevel, crafting,
            worldLevel < 0f ? item.m_worldLevel : worldLevel, stackOverride);
    }

    /// <summary>
    /// Lets your plugin change Epic Loot's tooltip data before anything reads it: Epic Loot's own tooltip, Auga's
    /// and <see cref="GetItemTooltipData"/> all see the edited data. Change a value, remove an effect or a hint,
    /// or add a line to <c>Extra</c> with an anchor saying where it goes.
    /// </summary>
    /// <remarks>
    /// Editors run in registration order, each getting the JSON the one before returned. They run every frame
    /// a tooltip is shown, so keep them cheap. One that throws or returns something that is not tooltip data
    /// is skipped and logged once. The data holds Epic Loot's parts only; plain vanilla lines cannot be edited
    /// here.
    /// </remarks>
    /// <param name="id">Unique id, normally your plugin GUID</param>
    /// <param name="editor">Given the item and the data as JSON, returns the edited JSON, or null to leave it
    /// as it is.</param>
    /// <returns>true if registered; false if <paramref name="id"/> is empty or taken, or the editor is null</returns>
    [PublicAPI]
    public static bool AddTooltipDataEditor(string id, Func<ItemDrop.ItemData, string, string> editor)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            OnError?.Invoke("Failed to register tooltip data editor: id is empty");
            return false;
        }

        if (editor == null)
        {
            OnError?.Invoke($"Failed to register tooltip data editor '{id}': callback is null");
            return false;
        }

        if (ItemTooltip.HasEditor(id))
        {
            OnError?.Invoke($"Duplicate tooltip data editor: {id}");
            return false;
        }

        ItemTooltip.AddEditor(id, editor);
        OnReload?.Invoke($"Registered tooltip data editor: {id}");
        return true;
    }

    /// <param name="id">The id passed to <see cref="AddTooltipDataEditor"/></param>
    /// <returns>true if an editor was removed</returns>
    [PublicAPI]
    public static bool RemoveTooltipDataEditor(string id)
    {
        return id != null && ItemTooltip.RemoveEditor(id);
    }
}
