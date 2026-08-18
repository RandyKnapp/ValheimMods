using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

/// <summary>
/// Hooks for plugins that draw their own item slots -- custom hotbars, extra equipment rows, quick
/// slots. Epic Loot decorates the vanilla inventory grid and hotkey bar with transpilers, so a plugin
/// that reimplements <c>InventoryGrid.UpdateGui</c> or <c>HotkeyBar.UpdateIcons</c> (a prefix returning
/// false, say) necessarily skips that decoration. This is the supported way to put it back.
/// </summary>
public static partial class API
{
    /// <summary>
    /// Applies Epic Loot's rarity background to a single item slot, creating the child images the first
    /// time one is actually needed. Safe to call every frame: after the first call only the sprite,
    /// colour and enabled state are touched.
    /// </summary>
    /// <param name="slotRoot">The slot's root object -- the one holding "icon", "equiped" and friends.</param>
    /// <param name="equippedOverlay">The slot's "equiped" child. Used as the template for the images
    /// this creates, and restyled to Epic Loot's equipped sprite.</param>
    /// <param name="item">The item in the slot. Null, or an item with no rarity, hides the background.</param>
    /// <param name="inventoryGrid">true for an inventory grid cell, which also gets the legendary set
    /// marker; false for a hotbar element, which does not.</param>
    /// <returns>true if the slot was handled</returns>
    [PublicAPI]
    public static bool ApplyMagicItemBackground(GameObject slotRoot, GameObject equippedOverlay,
        ItemDrop.ItemData item, bool inventoryGrid)
    {
        if (slotRoot == null || equippedOverlay == null)
        {
            return false;
        }

        bool showBackground = item != null && item.UseMagicBackground();
        bool showSetMarker = inventoryGrid && item != null && item.IsSetItem();

        // An empty slot has nothing to show and, until something has been shown in it, nothing to hide
        // either -- so do not build images for every empty cell of every grid.
        if (!showBackground && !showSetMarker && slotRoot.transform.Find("magicItem") == null)
        {
            return true;
        }

        Image magicItem = ItemBackgroundHelper.CreateAndGetMagicItemBackgroundImage(slotRoot, equippedOverlay, inventoryGrid);
        if (magicItem == null)
        {
            return false;
        }

        magicItem.enabled = showBackground;
        if (showBackground)
        {
            magicItem.sprite = EpicLoot.GetMagicItemBgSprite();
            magicItem.color = item.GetRarityColor();
        }

        if (!inventoryGrid)
        {
            return true;
        }

        Transform setItemTransform = slotRoot.transform.Find("setItem");
        if (setItemTransform != null)
        {
            Image setItem = setItemTransform.GetComponent<Image>();
            if (setItem != null)
            {
                setItem.enabled = showSetMarker;
            }
        }

        return true;
    }

    /// <summary>
    /// Applies Epic Loot's rarity background behind an arbitrary item icon, for UI that is not a 64x64
    /// inventory cell -- crafting recipe rows, the crafting detail panel, custom lists.
    /// <see cref="ApplyMagicItemBackground"/> assumes cell geometry and will not fit those.
    /// </summary>
    /// <remarks>
    /// The background is a clone of the icon, so it inherits the icon's anchors, pivot, size and
    /// position and lines up whatever the layout. Safe to call every frame: after the first call only
    /// the sprite, colour and enabled state are touched.
    /// </remarks>
    /// <param name="iconObject">The object holding the item's icon <c>Image</c>. Must have a parent --
    /// the background is created as its sibling.</param>
    /// <param name="item">The item the icon depicts. Null, or an item with no rarity, hides the
    /// background.</param>
    /// <returns>true if the icon was handled</returns>
    [PublicAPI]
    public static bool ApplyMagicItemBackgroundToIcon(GameObject iconObject, ItemDrop.ItemData item)
    {
        if (iconObject == null || iconObject.transform.parent == null)
        {
            return false;
        }

        Transform parent = iconObject.transform.parent;
        Transform existing = parent.Find(IconBackgroundName);
        bool showBackground = item != null && item.UseMagicBackground();

        // Nothing to show and nothing built yet -- do not create an image per row of every list.
        if (!showBackground && existing == null)
        {
            return true;
        }

        Image background;
        if (existing == null)
        {
            GameObject backgroundObject = UnityEngine.Object.Instantiate(iconObject, parent);
            backgroundObject.name = IconBackgroundName;
            backgroundObject.SetActive(true);
            // Taking the icon's own index leaves the background drawn behind it.
            backgroundObject.transform.SetSiblingIndex(iconObject.transform.GetSiblingIndex());

            background = backgroundObject.GetComponent<Image>();
            if (background == null)
            {
                UnityEngine.Object.Destroy(backgroundObject);
                return false;
            }

            background.raycastTarget = false;
        }
        else
        {
            background = existing.GetComponent<Image>();
            if (background == null)
            {
                return false;
            }
        }

        background.enabled = showBackground;
        if (showBackground)
        {
            background.sprite = EpicLoot.GetMagicItemBgSprite();
            background.color = item.GetRarityColor();
        }

        return true;
    }

    private const string IconBackgroundName = "magicItemIconBG";
}
