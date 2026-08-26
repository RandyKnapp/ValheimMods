using Common;
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
            GameObject magicItemObject = UnityEngine.Object.Instantiate(equipped, equipped.transform.parent);
            // Directly below "equiped", never above it: the rarity background fills the whole cell,
            // so drawing it on top of the equipped marker hides the marker completely and a worn
            // magic item becomes indistinguishable from one sitting in the bag.
            magicItemObject.transform.SetSiblingIndex(equipped.transform.GetSiblingIndex());
            magicItemObject.name = "magicItem";
            magicItemObject.SetActive(true);
            magicItemTransform = (RectTransform)magicItemObject.transform;
            magicItemTransform.anchorMin = magicItemTransform.anchorMax = new Vector2(0.5f, 0.5f);
            magicItemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
            magicItemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);
            magicItemTransform.pivot = new Vector2(0.5f, 0.5f);
            magicItemTransform.anchoredPosition = Vector2.zero;
            Image magicItemInit = magicItemTransform.GetComponent<Image>();
            magicItemInit.color = Color.white;
            magicItemInit.raycastTarget = false;
        }

        // Also add set item marker
        if (isInventoryGrid)
        {
            RectTransform setItemTransform = (RectTransform)elementGo.transform.Find("setItem");
            if (setItemTransform == null)
            {
                GameObject setItemObject = UnityEngine.Object.Instantiate(equipped, equipped.transform.parent);
                setItemObject.transform.SetAsLastSibling();
                setItemObject.name = "setItem";
                setItemObject.SetActive(true);
                setItemTransform = (RectTransform)setItemObject.transform;
                setItemTransform.anchorMin = setItemTransform.anchorMax = new Vector2(0.5f, 0.5f);
                setItemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 64);
                setItemTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 64);
                setItemTransform.pivot = new Vector2(0.5f, 0.5f);
                setItemTransform.anchoredPosition = Vector2.zero;
                Image setItemInit = setItemTransform.GetComponent<Image>();
                setItemInit.raycastTarget = false;
                setItemInit.sprite = EpicLoot.GetSetItemSprite();
                setItemInit.color = ColorUtility.TryParseHtmlString(EpicLoot.GetSetItemColor(), out Color color) ? color : Color.white;
            }
        }

        return magicItemTransform.GetComponent<Image>();
    }

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