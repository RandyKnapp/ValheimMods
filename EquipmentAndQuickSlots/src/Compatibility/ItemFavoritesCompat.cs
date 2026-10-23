using System;
using UnityEngine;
using UnityEngine.UI;
using Iff = ItemFavoriteFrameworkAPI.ItemFavorites;

namespace EquipmentAndQuickSlots {
    // Item Favorite Framework integration: the quick slot bar shows the framework's favorite star and
    // trash mark on its items, the way the framework marks them in the inventory grid.
    //
    // Optional in every direction: no assembly reference, only a soft BepInDependency for load order.
    // Every call goes through the framework's own shim (ItemFavoriteFrameworkAPI/ItemFavorites.cs,
    // copied unchanged from its repository; replace it whole to update it), which answers "no marks"
    // when the framework is not installed.
    //
    // The quick slot cells are real cells of the player's inventory, so the framework already draws
    // its borders on them in the equipment panel, and a favorited quick slot cell counts as a
    // favorited slot here too.
    internal static class ItemFavoritesCompat {
        private const string MarkName = "EAQS_FavoriteMark";

        private static bool _reportedFailure;

        internal static void Initialize() {
            if (!Iff.IsLoaded())
                return;

            // Marks change in the inventory, not through anything the bar's change gate watches
            Iff.AddChangedListener(HotkeyBarRefreshGate.Invalidate);
            EquipmentAndQuickSlots.Log($"Item Favorite Framework {Iff.GetPluginVersion()} found; the quick slot bar shows its marks");
        }

        // Called after the quick bar refreshed its icons. Every element is visited, empty ones too:
        // elements are reused for whichever item lands in them next, and would otherwise keep a stale
        // mark when the icon shows again.
        internal static void UpdateQuickBarMarks(HotkeyBar bar) {
            if (!Iff.IsLoaded())
                return;

            for (int i = 0; i < bar.m_elements.Count; i++)
                UpdateMark(bar.m_elements[i].m_icon, FindItemForElement(bar, i));
        }

        // Vanilla places each item in the element at its grid x (the quick slot index here)
        private static ItemDrop.ItemData FindItemForElement(HotkeyBar bar, int index) {
            foreach (ItemDrop.ItemData item in bar.m_items) {
                if (item.m_gridPos.x == index)
                    return item;
            }

            return null;
        }

        private static void UpdateMark(Image icon, ItemDrop.ItemData item) {
            if (icon == null)
                return;

            Transform existing = icon.transform.Find(MarkName);
            if (!TryGetMark(item, out Sprite sprite, out Color color)) {
                if (existing != null)
                    existing.gameObject.SetActive(false);
                return;
            }

            Image mark = existing != null ? existing.GetComponent<Image>() : CreateMark(icon);
            mark.sprite = sprite;
            mark.color = color;
            mark.gameObject.SetActive(true);
        }

        // The item's own mark first, as the framework's grid weaves it on the inside: a favorite in
        // its colour, then trash, then the slot colour for an item only kept by its favorited cell.
        private static bool TryGetMark(ItemDrop.ItemData item, out Sprite sprite, out Color color) {
            sprite = null;
            color = default;
            if (item == null || !ValConfig.ShowFavoriteMarks.Value)
                return false;

            try {
                int marks = Iff.GetMarks(item);
                if ((marks & Iff.MarkItemFavorite) != 0) {
                    sprite = Iff.GetFavoriteIcon();
                    color = Iff.GetFavoriteColor();
                } else if ((marks & Iff.MarkTrash) != 0) {
                    sprite = Iff.GetTrashIcon();
                    color = Iff.GetTrashColor();
                } else if ((marks & Iff.MarkSlotFavorite) != 0) {
                    sprite = Iff.GetFavoriteIcon();
                    color = Iff.GetFavoritedSlotColor();
                }
            } catch (Exception ex) {
                if (!_reportedFailure) {
                    _reportedFailure = true;
                    EquipmentAndQuickSlots.LogWarning($"Item Favorite Framework failed while reading an item's marks; the quick slot bar shows none: {ex}");
                }

                return false;
            }

            return sprite != null;
        }

        // In the icon's top-right corner: the top-left holds the hotkey label. Sized by anchors so it
        // follows the icon, and a child of the icon so it draws above it and hides with it.
        private static Image CreateMark(Image icon) {
            GameObject go = new GameObject(MarkName, typeof(RectTransform), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(icon.transform, false);
            rect.anchorMin = new Vector2(0.55f, 0.55f);
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image mark = go.GetComponent<Image>();
            mark.preserveAspect = true;
            mark.raycastTarget = false;
            return mark;
        }
    }
}
