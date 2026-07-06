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
            magicItemObject.transform.SetSiblingIndex(EpicLoot.HasAuga ? equipped.transform.GetSiblingIndex() :
                equipped.transform.GetSiblingIndex() + 1);
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

        // Also change equipped image
        Image equippedImage = equipped.GetComponent<Image>();
        if (equippedImage != null && (!isInventoryGrid || !EpicLoot.HasAuga))
        {
            equippedImage.sprite = EpicLoot.GetEquippedSprite();
            equippedImage.color = Color.white;
            equippedImage.raycastTarget = false;
            RectTransform rectTransform = equipped.RectTransform();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, equippedImage.sprite.texture.width);
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, equippedImage.sprite.texture.height);
        }

        return magicItemTransform.GetComponent<Image>();
    }
}