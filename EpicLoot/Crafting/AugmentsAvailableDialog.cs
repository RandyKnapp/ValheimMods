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
            //EpicLoot.Log("AugmentsAvailableDialog.Update");
            if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") || (ZInput.GetButtonDown("JoyButtonY") || Input.GetKeyDown(KeyCode.Escape)) || ZInput.GetButtonDown("Use"))
            {
                OnClose();
            }
        }

        public void Show(AugmentTabController.AugmentRecipe recipe)
        {
            gameObject.SetActive(true);

            var item = recipe.FromItem;
            var rarity = item.GetRarity();
            var magicItem = item.GetMagicItem();
            var rarityColor = item.GetRarityColor();

            MagicBG.enabled = item.IsMagic();
            MagicBG.color = rarityColor;

            NameText.text = Localization.instance.Localize(item.GetDecoratedName());
            Icon.sprite = item.GetIcon();

            var availableEffects = AugmentTabController.GetAvailableAugments(recipe, item, magicItem, rarity);
            var t = new StringBuilder();
            foreach (var effectDef in availableEffects)
            {
                var values = effectDef.GetValuesForRarity(item.GetRarity());
                var valueDisplay = values != null ? $"({values.MinValue}-{values.MaxValue})" : "";
                t.AppendLine($"‣ {string.Format(Localization.instance.Localize(effectDef.DisplayText), valueDisplay)}");
            }

            Description.color = rarityColor;
            Description.text = t.ToString();
        }

        public void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
