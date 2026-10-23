using System;
using System.Collections.Generic;
using System.Linq;
using EpicLoot;
using EpicLoot.CraftingV2;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public abstract class EnchantingTableUIPanelBase : MonoBehaviour
    {
        public const float CountdownTime = 0.8f;

        public const string MainActionButton = "JoyButtonX";

        protected virtual string MainActionInput => MainActionButton;

        public MultiSelectItemList AvailableItems;
        public Button MainButton;
        public GameObject LevelDisplay;
        public GuiBar ProgressBar;
        public AudioSource Audio;
        public AudioClip ProgressLoopSFX;
        public AudioClip CompleteSFX;
        public AudioClip MainActionSFX;

        protected bool _inProgress;
        protected float _countdown;
        protected TMP_Text _buttonLabel;
        protected string _defaultButtonLabelText;
        protected bool _locked;

        private GameObject _mainButtonGamepadHint;

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
                _buttonLabel = FindButtonLabel(MainButton);
                _defaultButtonLabelText = _buttonLabel.text;
                _mainButtonGamepadHint = FindGamepadHint(MainButton.transform);
            }

            EnchantingUIController.SetupUIAudioSource(Audio);
            EnchantingUIController.SetupUIAudioSources(gameObject);
        }

        /// <summary>
        /// Takes <paramref name="button"/> as the main button. The Auga fixup swaps the prefab's button for an
        /// Auga one in EnchantingTableUI.Start, after Awake has already cached the old button's label on the
        /// panel that is open; a panel that wakes later reads the new button in Awake itself.
        /// </summary>
        public void ReplaceMainButton(Button button)
        {
            MainButton = button;
            _buttonLabel = FindButtonLabel(button) ?? _buttonLabel;
            _mainButtonGamepadHint = FindGamepadHint(button.transform);
        }

        // The button's first text that is not its gamepad glyph.
        private static TMP_Text FindButtonLabel(Button button)
        {
            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.GetComponent<GamepadGlyph>() == null)
                {
                    return text;
                }
            }

            return null;
        }

        // Matches the prefab's glyph child by name: "Hint" in most panels, "Hint-1" in the ones with two.
        private static GameObject FindGamepadHint(Transform button)
        {
            for (int i = 0; i < button.childCount; ++i)
            {
                Transform child = button.GetChild(i);
                if (child.name.StartsWith("Hint", StringComparison.Ordinal))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        private void UpdateMainButtonGamepadInput()
        {
            if (MainButton == null)
            {
                return;
            }

            bool usable = ZInput.IsGamepadActive() && (_inProgress || !_locked) && MainButton.IsInteractable();

            if (_mainButtonGamepadHint != null && _mainButtonGamepadHint.activeSelf != usable)
            {
                _mainButtonGamepadHint.SetActive(usable);
            }

            if (usable && ZInput.GetButtonDown(MainActionInput))
            {
                ZInput.ResetButtonStatus(MainActionInput);
                OnMainButtonClicked();
            }
        }

        // The d-pad's horizontal axis still moves the player's hotbar selection behind this panel, so it
        // is eaten here whether or not the panel answers it.
        private void UpdateDPadHorizontalInput()
        {
            if (!ZInput.IsGamepadActive())
            {
                return;
            }

            int direction;
            if (ZInput.GetButtonDown("JoyDPadLeft"))
            {
                direction = -1;
                ZInput.ResetButtonStatus("JoyDPadLeft");
            }
            else if (ZInput.GetButtonDown("JoyDPadRight"))
            {
                direction = 1;
                ZInput.ResetButtonStatus("JoyDPadRight");
            }
            else
            {
                return;
            }

            OnDPadHorizontal(direction);
        }

        protected virtual void OnDPadHorizontal(int direction)
        {
        }

        protected virtual void OnMainButtonClicked()
        {
            if (MainActionSFX != null)
            {
                Audio.PlayOneShot(MainActionSFX);
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
            UpdateMainButtonGamepadInput();
            UpdateDPadHorizontalInput();

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
                Audio.PlayOneShot(clip);
            }
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
            _buttonLabel.text = Localization.instance.Localize(_defaultButtonLabelText);

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

        // Internal as well as protected: EnchantingUIController re-checks the identify cost with it after
        // rolling, right before charging.
        protected internal static bool LocalPlayerCanAffordCost(List<InventoryItemListElement> cost)
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

        protected internal static void NotifyItemsUnavailable(params ItemDrop.ItemData[] items)
        {
            Player player = Player.m_localPlayer;
            if (player == null || items == null)
            {
                return;
            }

            string names = string.Join(", ", items
                .Where(x => x?.m_shared != null)
                .Select(x => Localization.instance.Localize(x.GetDecoratedName()))
                .Distinct());
            if (names.Length == 0)
            {
                return;
            }

            player.Message(MessageHud.MessageType.Center,
                Localization.instance.Localize("$mod_epicloot_item_unavailable", names));
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
