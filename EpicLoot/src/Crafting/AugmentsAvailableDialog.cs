using System.Text;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    public class AugmentsAvailableDialog : MonoBehaviour
    {
        public Text NameText;
        public Text Description;
        public Image Icon;
        public Image MagicBG;

        [UsedImplicitly]
        public void Awake()
        {
        }

        [UsedImplicitly]
        public void Update()
        {
            if (!EpicLoot.HasAuga)
            {
                if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") ||
                    (ZInput.GetButtonDown("JoyButtonY") || ZInput.GetKeyDown(KeyCode.Escape)) ||
                    ZInput.GetButtonDown("Use"))
                {
                    OnClose();
                }
            }
        }

        public void Show(AugmentHelper.AugmentRecipe recipe)
        {
            gameObject.SetActive(true);

            var item = recipe.FromItem;
            var rarity = item.GetRarity();
            var magicItem = item.GetMagicItem();
            var rarityColor = item.GetRarityColor();

            if (MagicBG != null)
            {
                MagicBG.enabled = item.IsMagic();
                MagicBG.color = rarityColor;
            }

            if (NameText != null)
            {
                NameText.text = Localization.instance.Localize(item.GetDecoratedName());
            }

            if (Icon != null)
            {
                Icon.sprite = item.GetIcon();
            }

            if (Description != null)
            {
                var availableEffects = AugmentHelper.GetAvailableAugments(recipe, item, magicItem, rarity);
                var t = new StringBuilder();
                if (availableEffects.Count < 20)
                {
                    Description.resizeTextForBestFit = true;
                    Description.fontSize = 18;
                }
                else
                {
                    Description.resizeTextForBestFit = false;
                    Description.fontSize = 12;
                }

                foreach (var effectDef in availableEffects)
                {
                    var values = effectDef.GetValuesForRarity(item.GetRarity());
                    var valueDisplay = values != null ? Mathf.Approximately(values.MinValue, values.MaxValue) ? $"{values.MinValue}" : $"({values.MinValue}-{values.MaxValue})" : "";
                    t.AppendLine($"‣ {string.Format(Localization.instance.Localize(effectDef.DisplayText), valueDisplay)}");
                }
                
                Description.color = rarityColor;
                Description.text = t.ToString();
            }
        }

        public void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
