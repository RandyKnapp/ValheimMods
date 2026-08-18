using JetBrains.Annotations;
using UnityEngine;

namespace EpicLootAPI;

/// <summary>
/// For plugins that draw their own item slots. Epic Loot decorates the vanilla inventory grid and
/// hotkey bar with transpilers, so reimplementing those methods skips the decoration.
/// </summary>
public static partial class EpicLoot
{
    private static readonly Method API_ApplyMagicItemBackground = new(
        "ApplyMagicItemBackground",
        typeof(GameObject),
        typeof(GameObject),
        typeof(ItemDrop.ItemData),
        typeof(bool));

    /// <summary>
    /// Applies Epic Loot's rarity background to one item slot you drew yourself, creating the child
    /// images the first time one is needed. Safe to call every frame.
    /// </summary>
    /// <param name="slotRoot">The slot's root object -- the one holding "icon", "equiped" and friends.</param>
    /// <param name="equippedOverlay">The slot's "equiped" child, used as the image template.</param>
    /// <param name="item">The item in the slot. Null, or an item with no rarity, hides the background.</param>
    /// <param name="inventoryGrid">true for an inventory grid cell, which also gets the legendary set
    /// marker; false for a hotbar element.</param>
    /// <returns>true if the slot was handled</returns>
    [PublicAPI]
    public static bool ApplyMagicItemBackground(GameObject slotRoot, GameObject equippedOverlay,
        ItemDrop.ItemData item, bool inventoryGrid)
    {
        object[] result = API_ApplyMagicItemBackground.Invoke(slotRoot, equippedOverlay, item, inventoryGrid);
        return (bool)(result[0] ?? false);
    }

    private static readonly Method API_ApplyMagicItemBackgroundToIcon = new(
        "ApplyMagicItemBackgroundToIcon",
        typeof(GameObject),
        typeof(ItemDrop.ItemData));

    /// <summary>
    /// Applies the rarity background behind an item icon that is not a 64x64 inventory cell -- a recipe
    /// row, a detail panel, your own list. The background clones the icon, so it inherits its anchors,
    /// pivot, size and position and lines up whatever the layout. Safe to call every frame.
    /// </summary>
    /// <param name="iconObject">The object holding the icon <c>Image</c>. Must have a parent -- the
    /// background is created as its sibling.</param>
    /// <param name="item">The item the icon depicts. Null, or an item with no rarity, hides the
    /// background.</param>
    /// <returns>true if the icon was handled</returns>
    [PublicAPI]
    public static bool ApplyMagicItemBackgroundToIcon(GameObject iconObject, ItemDrop.ItemData item)
    {
        object[] result = API_ApplyMagicItemBackgroundToIcon.Invoke(iconObject, item);
        return (bool)(result[0] ?? false);
    }
}
