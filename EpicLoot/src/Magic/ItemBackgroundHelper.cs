using Common;
using EpicLoot.Config;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot;

public static class ItemBackgroundHelper
{
    public static Image CreateAndGetMagicItemBackgroundImage(GameObject elementGo, GameObject equipped, bool isInventoryGrid)
    {
        RectTransform magicItemTransform = (RectTransform)elementGo.transform.Find("magicItem");
        if (magicItemTransform == null)
        {
            Image magicItemInit = CloneEquippedOverlay(equipped, "magicItem");
            // Directly below "equiped", never above it: the rarity background fills the whole cell,
            // so drawing it on top of the equipped marker hides the marker completely and a worn
            // magic item becomes indistinguishable from one sitting in the bag.
            magicItemInit.transform.SetSiblingIndex(equipped.transform.GetSiblingIndex());
            magicItemInit.color = Color.white;
            magicItemTransform = (RectTransform)magicItemInit.transform;
        }

        if (isInventoryGrid && elementGo.transform.Find("setItem") == null)
        {
            Image setItemInit = CloneEquippedOverlay(equipped, "setItem");
            setItemInit.transform.SetAsLastSibling();
            setItemInit.sprite = EpicLoot.GetSetItemSprite();
        }

        return magicItemTransform.GetComponent<Image>();
    }

    // A 64x64 copy of the slot's "equiped" overlay, centred and ignoring raycasts.
    private static Image CloneEquippedOverlay(GameObject equipped, string name)
    {
        GameObject overlay = UnityEngine.Object.Instantiate(equipped, equipped.transform.parent);
        overlay.name = name;
        overlay.SetActive(true);
        RectTransform rectTransform = (RectTransform)overlay.transform;
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        Image image = rectTransform.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    public static void UpdateIconSetMarker(Image icon, ItemDrop.ItemData item)
    {
        if (icon == null)
        {
            return;
        }

        Transform existing = icon.transform.Find(IconSetMarkerName);
        string markerColor = GetMarkerColor(item);
        if (markerColor == null)
        {
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        Image marker = existing != null ? existing.GetComponent<Image>() : CreateIconSetMarker(icon);
        StyleMarker(marker, markerColor, item.IsUniqueItem());
        marker.gameObject.SetActive(true);
    }

    /// <summary>Colours a rune marker, and sets it breathing when it marks a unique and effects are on.</summary>
    public static void StyleMarker(Image marker, string color, bool isUnique)
    {
        UniqueRunePulse.Apply(marker, ParseColor(color), isUnique && EmbersEnabled);
    }

    /// <summary>Shows or hides the border and embers in a unique item's inventory cell, building them the first time.</summary>
    public static void UpdateUniqueEffects(GameObject slotRoot, bool isUnique)
    {
        bool embers = isUnique && EmbersEnabled;
        Toggle<UniqueItemEmbers>(slotRoot.transform, UniqueEmbersName, embers);
        Toggle<UniqueItemBorder>(slotRoot.transform, UniqueBorderName, embers);

        if (embers)
        {
            Transform setItem = slotRoot.transform.Find("setItem");
            if (setItem != null && setItem.GetSiblingIndex() < slotRoot.transform.childCount - 1)
            {
                setItem.SetAsLastSibling();
            }
        }
    }

    public static void HideUniqueEffects(Transform slotRoot)
    {
        Toggle<UniqueItemEmbers>(slotRoot, UniqueEmbersName, false);
        Toggle<UniqueItemBorder>(slotRoot, UniqueBorderName, false);
    }

    private static void Toggle<T>(Transform slotRoot, string name, bool show) where T : UniqueCellEffect
    {
        Transform existing = slotRoot.Find(name);
        T effect = existing == null ? null : existing.GetComponent<T>();
        if (!show)
        {
            if (effect != null)
            {
                effect.enabled = false;
            }

            return;
        }

        if (effect == null)
        {
            effect = UniqueCellEffect.Create<T>(slotRoot, name, 64);
        }

        effect.Tint = ParseColor(EpicLoot.GetUniqueItemColor());
        effect.enabled = true;
    }

    private static bool EmbersEnabled => ELConfig.UniqueItemEmbers?.Value ?? false;
    private const string UniqueEmbersName = "uniqueEmbers";
    private const string UniqueBorderName = "uniqueBorder";

    /// <summary>The colour of the rune marker on a set piece or a unique, or null for an item that gets none.</summary>
    public static string GetMarkerColor(ItemDrop.ItemData item)
    {
        if (item == null)
        {
            return null;
        }

        if (item.IsSetItem())
        {
            return EpicLoot.GetSetItemColor();
        }

        return item.IsUniqueItem() ? EpicLoot.GetUniqueItemColor() : null;
    }

    public static Color ParseColor(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color color) ? color : Color.white;
    }

    private static Image CreateIconSetMarker(Image icon)
    {
        GameObject go = new GameObject(IconSetMarkerName, typeof(RectTransform), typeof(Image));
        RectTransform rect = (RectTransform)go.transform;
        rect.SetParent(icon.transform, false);
        rect.anchorMin = new Vector2(0f, -IconSetMarkerDrop);
        rect.anchorMax = new Vector2(1f, 1f - IconSetMarkerDrop);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image marker = go.GetComponent<Image>();
        marker.sprite = EpicLoot.GetSetItemSprite();
        marker.raycastTarget = false;
        return marker;
    }

    private const string IconSetMarkerName = "EL_SetItemMarker";
    private const float IconSetMarkerDrop = 0.15f;

    /// <summary>
    /// Swaps the slot's vanilla "equiped" overlay (a translucent blue fill) for Epic Loot's frame.
    /// Unconditional per slot, and deliberately separate from the rarity background above: the
    /// equipped marker has to look the same on every worn item, magic or not, and must not depend
    /// on whether this particular cell ever happened to hold a magic item. Cheap to call every
    /// frame -- once the sprite is in place there is nothing left to do.
    /// </summary>
    public static void ApplyEquippedSprite(GameObject equipped, bool isInventoryGrid)
    {
        // Auga restyles the inventory grid's equipped marker itself; only the hotbar is ours there.
        if (equipped == null || (isInventoryGrid && EpicLoot.HasAuga))
        {
            return;
        }

        Image equippedImage = equipped.GetComponent<Image>();
        Sprite sprite = EpicLoot.GetEquippedSprite();
        if (equippedImage == null || sprite == null || equippedImage.sprite == sprite)
        {
            return;
        }

        equippedImage.sprite = sprite;
        equippedImage.color = Color.white;
        equippedImage.raycastTarget = false;
        RectTransform rectTransform = equipped.RectTransform();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sprite.texture.width);
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, sprite.texture.height);
    }
}