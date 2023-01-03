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

        public void Show(ItemDrop.ItemData fromItem, int effectIndex, Action<ItemDrop.ItemData, int, MagicItemEffect> onCompleteCallback)
        {
            gameObject.SetActive(true);

            _audioSource.loop = true;
            _audioSource.clip = EpicLoot.Assets.ItemLoopSFX;
            _audioSource.volume = 0.5f;
            _audioSource.Play();

            var item = fromItem.Extended();
            var rarity = item.GetRarity();
            var magicItem = item.GetMagicItem();
            var rarityColor = item.GetRarityColor();
            
            MagicBG.enabled = item.IsMagic();
            MagicBG.color = rarityColor;

            if (EpicLoot.HasAuga)
            {
                Auga.API.ComplexTooltip_SetItem(gameObject, item);
            }

            if (NameText != null)
            {
                NameText.text = Localization.instance.Localize(item.GetDecoratedName());
            }

            if (Description != null)
            {
                Description.text = Localization.instance.Localize(item.GetTooltip());
            }

            if (Icon != null)
            {
                Icon.sprite = item.GetIcon();
            }

            foreach (var button in EffectChoiceButtons)
            {
                button.gameObject.SetActive(false);
            }

            var newEffectOptions = LootRoller.RollAugmentEffects(item, magicItem, effectIndex);
            for (var index = 0; index < newEffectOptions.Count; index++)
            {
                var effect = newEffectOptions[index];
                var button = EffectChoiceButtons[index];
                button.gameObject.SetActive(true);
                var text = button.GetComponentInChildren<Text>();
                text.text = Localization.instance.Localize((index == 0 ? "<color=white>($mod_epicloot_augment_keep)</color> " : "") + MagicItem.GetEffectText(effect, rarity, true));
                text.color = rarityColor;

                if (EpicLoot.HasAuga)
                {
                    Auga.API.Button_SetTextColors(button, Color.white, Color.white, Color.white, Color.white, Color.white, rarityColor);
                }
                else
                {
                    var buttonColor = button.GetComponent<ButtonTextColor>();
                    if (buttonColor != null)
                    {
                        buttonColor.m_defaultColor = rarityColor;
                    }
                }

                button.onClick.RemoveAllListeners();
                if (EnchantCostsHelper.EffectIsDeprecated(effect.EffectType))
                {
                    text.color = new Color(text.color.r, text.color.g, text.color.b, 0.5f);
                    button.interactable = false;
                }
                else
                {
                    button.onClick.AddListener(() => {
                        onCompleteCallback(fromItem, effectIndex, effect);
                        OnClose();
                    });
                }
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
