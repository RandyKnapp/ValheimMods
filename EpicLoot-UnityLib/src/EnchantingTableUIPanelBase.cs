using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public abstract class EnchantingTableUIPanelBase : MonoBehaviour
    {
        public const float CountdownTime = 0.8f;

        public MultiSelectItemList AvailableItems;
        public Button MainButton;
        public GameObject LevelDisplay;
        public GuiBar ProgressBar;
        public AudioSource Audio;
        public AudioClip ProgressLoopSFX;
        public AudioClip CompleteSFX;
        public AudioClip MainActionSFX;

        public delegate float AudioVolumeLevelDelegate();
        public static AudioVolumeLevelDelegate AudioVolumeLevel;

        protected bool _inProgress;
        protected float _countdown;
        protected Text _buttonLabel;
        protected TMP_Text _tmpButtonLabel;
        protected bool _useTMP = false;
        protected string _defaultButtonLabelText;
        protected bool _locked;

        protected abstract void DoMainAction();
        protected abstract void OnSelectedItemsChanged();

        public virtual void Awake()
        {
            if (AvailableItems != null)
            {
                AvailableItems.OnSelectedItemsChanged += OnSelectedItemsChanged;
                AvailableItems.GiveFocus(true, 0);
            }

            if (MainButton != null)
            {
                MainButton.onClick.AddListener(OnMainButtonClicked);
                _buttonLabel = MainButton.GetComponentInChildren<Text>();
                if (_buttonLabel == null)
                {
                    _tmpButtonLabel = MainButton.GetComponentInChildren<TMP_Text>();
                    _useTMP = true;
                }
                
                _defaultButtonLabelText = _useTMP ? _tmpButtonLabel.text : _buttonLabel.text;
            }

            GameObject uiSFX = GameObject.Find("sfx_gui_button");
            if (uiSFX && Audio != null)
            {
                Audio.outputAudioMixerGroup = uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;
                Audio.volume = AudioVolumeLevel();
            }

            foreach (AudioSource audioSource in this.GetComponentsInChildren<AudioSource>())
            {
                audioSource.volume = AudioVolumeLevel();
            }
        }

        protected virtual void OnMainButtonClicked()
        {
            if (MainActionSFX != null)
            {
                Audio.PlayOneShot(MainActionSFX, Audio.volume);
            }

            if (_inProgress)
            {
                Cancel();
            }
            else
            {
                StartProgress();
            }
        }

        public virtual void DeselectAll()
        {
        }

        public virtual void Update()
        {
            if (ProgressBar != null)
            {
                ProgressBar.gameObject.SetActive(_inProgress);
            }
            if (LevelDisplay != null)
            {
                LevelDisplay.gameObject.SetActive(!_inProgress);
            }

            if (_inProgress)
            {
                if (ProgressBar != null)
                {
                    ProgressBar.SetValue(CountdownTime - _countdown);
                }

                _countdown -= Time.deltaTime;
                if (_countdown < 0)
                {
                    _inProgress = false;
                    _countdown = 0;

                    if (Audio != null)
                    {
                        Audio.loop = false;
                        Audio.Stop();
                    }

                    DoMainAction();
                    PlayCompleteSFX();
                }
            }
        }

        private void PlayCompleteSFX()
        {
            AudioClip clip = GetCompleteAudioClip();
            if (Audio != null && clip != null)
            {
                Audio.PlayOneShot(clip, Audio.volume);
            }
        }

        protected virtual AudioClip GetCompleteAudioClip()
        {
            return CompleteSFX;
        }

        public virtual void StartProgress()
        {
            if (_useTMP)
            {
                _tmpButtonLabel.text = Localization.instance.Localize("$menu_cancel");
            }
            else
            {
                _buttonLabel.text = Localization.instance.Localize("$menu_cancel");
            }

            _inProgress = true;
            _countdown = CountdownTime;

            if (ProgressBar != null)
            {
                ProgressBar.SetMaxValue(CountdownTime);
            }

            if (Audio != null)
            {
                Audio.loop = true;
                Audio.clip = ProgressLoopSFX;
                Audio.Play();
            }

            Lock();
        }

        public virtual bool CanCancel()
        {
            return _inProgress;
        }

        public virtual void Cancel()
        {
            if (_useTMP)
            {
                _tmpButtonLabel.text = Localization.instance.Localize(_defaultButtonLabelText);
            }
            else
            {
                _buttonLabel.text = Localization.instance.Localize(_defaultButtonLabelText);
            }

            _inProgress = false;
            _countdown = 0;

            if (Audio != null)
            {
                Audio.loop = false;
                Audio.Stop();
            }

            Unlock();
        }

        public virtual void Lock()
        {
            _locked = true;
            MultiSelectItemList[] lists = GetComponentsInChildren<MultiSelectItemList>();
            foreach (MultiSelectItemList list in lists)
            {
                list.Lock();
            }

            EnchantingTableUI.instance.LockTabs();
        }

        public virtual void Unlock()
        {
            _locked = false;
            MultiSelectItemList[] lists = GetComponentsInChildren<MultiSelectItemList>();
            foreach (MultiSelectItemList list in lists)
            {
                list.Unlock();
            }

            EnchantingTableUI.instance.UnlockTabs();
        }

        protected static bool LocalPlayerCanAffordCost(List<InventoryItemListElement> cost)
        {
            if (Player.m_localPlayer.NoCostCheat())
            {
                return true;
            }

            foreach (InventoryItemListElement element in cost)
            {
                ItemDrop.ItemData item = element.GetItem();

                if (!InventoryManagement.Instance.HasItem(item))
                {
                    return false;
                }
            }

            return true;
        }

        protected static void GiveItemsToPlayer(List<InventoryItemListElement> sacrificeProducts)
        {
            foreach (InventoryItemListElement sacrificeProduct in sacrificeProducts)
            {
                ItemDrop.ItemData item = sacrificeProduct.GetItem();
                InventoryManagement.Instance.GiveItem(item);
            }
        }
    }
}
