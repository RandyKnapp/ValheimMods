using System;
using System.Collections.Generic;
using ExtendedItemDataFramework;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    public class AugmentChoiceDialog : MonoBehaviour
    {
        public Text NameText;
        public Text Description;
        public Image Icon;
        public Image MagicBG;
        public List<Button> EffectChoiceButtons = new List<Button>();

        private AudioSource _audioSource;

        [UsedImplicitly]
        public void Awake()
        {
            _audioSource = transform.parent.GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = transform.parent.gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
            }
        }

        [UsedImplicitly]
        public void Update()
        {
            // Not all of these inputs work and I do not know why, only Escape works
            if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") || (ZInput.GetButtonDown("JoyButtonY") || Input.GetKeyDown(KeyCode.Escape)) || ZInput.GetButtonDown("Use"))
            {
                EffectChoiceButtons[0].onClick.Invoke();
            }
        }

        public void Show(AugmentTabController.AugmentRecipe recipe, Action<MagicItemEffect> onCompleteCallback)
        {
            gameObject.SetActive(true);

            _audioSource.loop = true;
            _audioSource.clip = EpicLoot.Assets.ItemLoopSFX;
            _audioSource.volume = 0.5f;
            _audioSource.Play();

            var item = recipe.FromItem.Extended();
            var rarity = item.GetRarity();
            var magicItem = item.GetMagicItem();
            var rarityColor = item.GetRarityColor();
            
            MagicBG.enabled = item.IsMagic();
            MagicBG.color = rarityColor;

            NameText.text = Localization.instance.Localize(item.GetDecoratedName());
            Description.text = Localization.instance.Localize(item.GetTooltip());
            Icon.sprite = item.GetIcon();

            var newEffectOptions = LootRoller.RollAugmentEffects(item, magicItem, recipe.EffectIndex);
            for (var index = 0; index < newEffectOptions.Count; index++)
            {
                var effect = newEffectOptions[index];
                var button = EffectChoiceButtons[index];
                var text = button.GetComponentInChildren<Text>();
                text.text = (index == 0 ? "(keep) " : "") + MagicItem.GetEffectText(effect, rarity, true);
                text.color = rarityColor;
                var buttonColor = button.GetComponent<ButtonTextColor>();
                buttonColor.m_defaultColor = rarityColor;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => {
                    onCompleteCallback(effect);
                    OnClose();
                });
            }
        }

        public void OnClose()
        {
            _audioSource.loop = false;
            _audioSource.Stop();
            _audioSource.PlayOneShot(EpicLoot.Assets.AugmentItemSFX);
            gameObject.SetActive(false);
        }
    }
}
