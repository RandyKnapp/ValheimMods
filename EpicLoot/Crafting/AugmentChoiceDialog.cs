using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Crafting
{
    public class AugmentChoiceDialog : MonoBehaviour
    {
        public TMP_Text NameText;
        public TMP_Text Description;
        public Image Icon;
        public Image MagicBG;
        public List<Button> EffectChoiceButtons = new List<Button>();

        private AudioSource _audioSource;
        private int _choiceIndex = 0;

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
            if (ZInput.IsGamepadActive())
            {
                if (_choiceIndex > 0 && ZInput.GetButtonDown("JoyLStickUp"))
                {
                    _choiceIndex--;
                    ZInput.ResetButtonStatus("JoyLStickUp");
                }
                else if (_choiceIndex < EffectChoiceButtons.Count - 1 && ZInput.GetButtonDown("JoyLStickDown"))
                {
                    _choiceIndex++;
                    ZInput.ResetButtonStatus("JoyLStickDown");
                }
                else if (ZInput.GetButtonDown("JoyLStickLeft"))
                {
                    ZInput.ResetButtonStatus("JoyLStickLeft");
                }
                else if (ZInput.GetButtonDown("JoyLStickRight"))
                {
                    ZInput.ResetButtonStatus("JoyLStickRight");
                }

                if (_choiceIndex >= 0 && _choiceIndex < EffectChoiceButtons.Count && ZInput.GetButton("JoyButtonA"))
                {
                    var button = EffectChoiceButtons[_choiceIndex];
                    button.OnSubmit(null);
                    ZInput.ResetButtonStatus("JoyButtonA");
                }

                var scrollBar = GetComponentInChildren<Scrollbar>();
                if (scrollBar != null)
                {
                    var rightStickAxis = ZInput.GetJoyRightStickY();
                    if (Mathf.Abs(rightStickAxis) > 0.5f)
                        scrollBar.value = Mathf.Clamp01(scrollBar.value + rightStickAxis * -0.1f);
                }
            }

            for (var index = 0; index < EffectChoiceButtons.Count; index++)
            {
                var button = EffectChoiceButtons[index];
                var focus = button.transform.Find("ButtonFocus");
                if (focus != null)
                    focus.gameObject.SetActive(ZInput.IsGamepadActive() && index == _choiceIndex);
            }

            if (ZInput.GetButtonDown("Inventory") || ZInput.GetButtonDown("JoyButtonB") || Input.GetKeyDown(KeyCode.Escape))
            {
                EffectChoiceButtons[0].onClick.Invoke();
            }

            if (ZInput.IsGamepadActive() && ZInput.GetButtonDown("JoyButtonA"))
            {
                EffectChoiceButtons[_choiceIndex].onClick.Invoke();
            }
        }

        public void Show(ItemDrop.ItemData fromItem, int effectIndex, Action<ItemDrop.ItemData, int, MagicItemEffect> onCompleteCallback)
        {
            gameObject.SetActive(true);

            _audioSource.loop = true;
            _audioSource.clip = EpicLoot.Assets.ItemLoopSFX;
            _audioSource.volume = 0.5f;
            _audioSource.Play();

            var rarity = fromItem.GetRarity();
            var magicItem = fromItem.GetMagicItem();
            var rarityColor = fromItem.GetRarityColor();
            
            MagicBG.enabled = fromItem.IsMagic();
            MagicBG.color = rarityColor;

            if (EpicLoot.HasAuga)
            {
                Auga.API.ComplexTooltip_SetItem(gameObject, fromItem);
            }

            if (NameText != null)
            {
                NameText.text = Localization.instance.Localize(fromItem.GetDecoratedName());
            }

            if (Description != null)
            {
                Description.text = Localization.instance.Localize(fromItem.GetTooltip());
            }

            if (Icon != null)
            {
                Icon.sprite = fromItem.GetIcon();
            }

            foreach (var button in EffectChoiceButtons)
            {
                button.gameObject.SetActive(false);
            }

            var newEffectOptions = LootRoller.RollAugmentEffects(fromItem, magicItem, effectIndex);
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
