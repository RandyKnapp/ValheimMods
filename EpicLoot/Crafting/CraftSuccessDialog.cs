using ExtendedItemDataFramework;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    public class CraftSuccessDialog : MonoBehaviour
    {
        public Text NameText;
        public Text Description;
        public Image Icon;
        public Image MagicBG;

        private AudioSource _audioSource;

        [UsedImplicitly]
        public void Awake()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        public void Show(ExtendedItemData item)
        {
            gameObject.SetActive(true);

            var rarityColor = item.GetRarityColor();
            
            MagicBG.enabled = item.IsMagic();
            MagicBG.color = rarityColor;

            NameText.text = Localization.instance.Localize(item.GetDecoratedName());
            Description.text = Localization.instance.Localize(item.GetTooltip());
            Icon.sprite = item.GetIcon();

            _audioSource.PlayOneShot(EpicLoot.GetMagicItemDropSFX(item.GetRarity()));
        }

        public void OnClose()
        {
            gameObject.SetActive(false);
        }
    }
}
