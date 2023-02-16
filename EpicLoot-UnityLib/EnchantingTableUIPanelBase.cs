using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public abstract class EnchantingTableUIPanelBase : MonoBehaviour
    {
        public const float CountdownTime = 0.8f;

        public MultiSelectItemList AvailableItems;
        public Button MainButton;
        public GuiBar ProgressBar;
        public AudioSource Audio;
        public AudioClip ProgressLoopSFX;
        public AudioClip CompleteSFX;

        protected bool _inProgress;
        protected float _countdown;
        protected Text _buttonLabel;
        protected string _defaultButtonLabelText;
        protected bool _locked;

        protected abstract void DoMainAction();
        protected abstract void OnSelectedItemsChanged();

        public virtual void Awake()
        {
            AvailableItems.OnSelectedItemsChanged += OnSelectedItemsChanged;
            MainButton.onClick.AddListener(OnMainButtonClicked);
            _buttonLabel = MainButton.GetComponentInChildren<Text>();
            _defaultButtonLabelText = _buttonLabel.text;

            var uiSFX = GameObject.Find("sfx_gui_button");
            if (uiSFX)
                Audio.outputAudioMixerGroup = uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;

            AvailableItems.GiveFocus(true, 0);
        }

        protected virtual void OnMainButtonClicked()
        {
            if (_inProgress)
                Cancel();
            else
                StartProgress();
        }

        public virtual void DeselectAll()
        {
        }

        public virtual void Update()
        {
            ProgressBar.gameObject.SetActive(_inProgress);
            if (_inProgress)
            {
                ProgressBar.SetValue(CountdownTime - _countdown);

                _countdown -= Time.deltaTime;
                if (_countdown < 0)
                {
                    _inProgress = false;
                    _countdown = 0;

                    Audio.loop = false;
                    Audio.Stop();

                    DoMainAction();
                    PlayCompleteSFX();
                }
            }
        }

        private void PlayCompleteSFX()
        {
            var clip = GetCompleteAudioClip();
            if (clip)
                Audio.PlayOneShot(clip);
        }

        protected virtual AudioClip GetCompleteAudioClip()
        {
            return CompleteSFX;
        }

        public virtual void StartProgress()
        {
            _buttonLabel.text = Localization.instance.Localize("$menu_cancel");
            _inProgress = true;
            _countdown = CountdownTime;
            ProgressBar.SetMaxValue(CountdownTime);

            Audio.loop = true;
            Audio.clip = ProgressLoopSFX;
            Audio.Play();

            Lock();
        }

        public virtual bool CanCancel()
        {
            return _inProgress;
        }

        public virtual void Cancel()
        {
            _buttonLabel.text = Localization.instance.Localize(_defaultButtonLabelText);
            _inProgress = false;
            _countdown = 0;

            Audio.loop = false;
            Audio.Stop();

            Unlock();
        }

        public virtual void Lock()
        {
            _locked = true;
            var lists = GetComponentsInChildren<MultiSelectItemList>();
            foreach (var list in lists)
            {
                list.Lock();
            }

            EnchantingTableUI.instance.LockTabs();
        }

        public virtual void Unlock()
        {
            _locked = false;
            var lists = GetComponentsInChildren<MultiSelectItemList>();
            foreach (var list in lists)
            {
                list.Unlock();
            }

            EnchantingTableUI.instance.UnlockTabs();
        }

        protected static bool LocalPlayerCanAffordCost(List<InventoryItemListElement> cost)
        {
            var player = Player.m_localPlayer;
            if (player.NoCostCheat())
                return true;

            var inventory = player.GetInventory();
            foreach (var element in cost)
            {
                var item = element.GetItem();
                if (inventory.CountItems(item.m_shared.m_name) < item.m_stack)
                    return false;
            }

            return true;
        }
    }
}
