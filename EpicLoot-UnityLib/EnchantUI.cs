using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class EnchantUI : MonoBehaviour
    {
        public const float CountdownTime = 0.8f;

        public MultiSelectItemList AvailableItems;
        public Text EnchantInfo;
        public Scrollbar EnchantInfoScrollbar;
        public Button MainButton;
        public GuiBar ProgressBar;
        public List<Toggle> RarityButtons;

        [Header("Cost")]
        public Text CostLabel;
        public MultiSelectItemList CostList;

        [Header("Audio")]
        public AudioSource Audio;
        public AudioClip ProgressLoopSFX;
        public AudioClip[] CompleteSFX;

        public delegate List<InventoryItemListElement> GetEnchantableItemsDelegate();
        public delegate string GetEnchantInfoDelegate(ItemDrop.ItemData item, MagicRarityUnity rarity);
        public delegate List<InventoryItemListElement> GetEnchantCostDelegate(ItemDrop.ItemData item, MagicRarityUnity rarity);
        // Returns the success dialog
        public delegate GameObject EnchantItemDelegate(ItemDrop.ItemData item, MagicRarityUnity rarity);

        public static GetEnchantableItemsDelegate GetEnchantableItems;
        public static GetEnchantInfoDelegate GetEnchantInfo;
        public static GetEnchantCostDelegate GetEnchantCost;
        public static EnchantItemDelegate EnchantItem;

        private bool _inProgress;
        private float _countdown;
        private Text _buttonLabel;
        private Text _progressLabel;
        private ToggleGroup _toggleGroup;
        private MagicRarityUnity _rarity;
        private GameObject _successDialog;

        public void Awake()
        {
            AvailableItems.OnSelectedItemsChanged += OnSelectedItemChanged;
            MainButton.onClick.AddListener(OnMainButtonClicked);
            _buttonLabel = MainButton.GetComponentInChildren<Text>();
            _progressLabel = ProgressBar.gameObject.GetComponentInChildren<Text>();

            var uiSFX = GameObject.Find("sfx_gui_button");
            if (uiSFX)
                Audio.outputAudioMixerGroup = uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;

            if (RarityButtons.Count > 0)
            {
                _toggleGroup = RarityButtons[0].group;
                _toggleGroup.EnsureValidState();
            }

            for (var index = 0; index < RarityButtons.Count; index++)
            {
                var rarityButton = RarityButtons[index];
                rarityButton.onValueChanged.AddListener((isOn) => {
                    if (isOn)
                        RefreshRarity();
                });
            }
        }

        public void OnEnable()
        {
            _rarity = MagicRarityUnity.Magic;
            OnRarityChanged();
            RarityButtons[0].isOn = true;
            var items = GetEnchantableItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
        }

        public void Update()
        {
            ProgressBar.gameObject.SetActive(_inProgress);
            if (_inProgress)
            {
                ProgressBar.SetValue(CountdownTime - _countdown);

                _countdown -= Time.deltaTime;
                if (_countdown < 0)
                {
                    DoEnchant();
                }
            }

            if (_successDialog != null && !_successDialog.activeSelf)
            {
                UnlockSelector();
                Destroy(_successDialog);
                _successDialog = null;
            }
        }

        public void RefreshRarity()
        {
            var prevRarity = _rarity;
            for (var index = 0; index < RarityButtons.Count; index++)
            {
                var button = RarityButtons[index];
                if (button.isOn)
                {
                    _rarity = (MagicRarityUnity)index;
                }
            }

            if (prevRarity != _rarity)
                OnRarityChanged();
        }

        public void OnRarityChanged()
        {
            var selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();
            if (selectedItem?.Item1.GetItem() == null)
            {
                MainButton.interactable = false;
                EnchantInfo.text = "";
                CostLabel.enabled = false;
                CostList.SetItems(new List<IListElement>());
                return;
            }

            var item = selectedItem.Item1.GetItem();
            var info = GetEnchantInfo(item, _rarity);

            EnchantInfo.text = info;
            ScrollEnchantInfoToTop();

            CostLabel.enabled = true;
            var cost = GetEnchantCost(item, _rarity);
            CostList.SetItems(cost.Cast<IListElement>().ToList());

            var canAfford = LocalPlayerCanAffordCost(cost);
            MainButton.interactable = canAfford;
        }

        private void ScrollEnchantInfoToTop()
        {
            EnchantInfoScrollbar.value = 1;
        }

        public void DoEnchant()
        {
            var selectedItem = AvailableItems.GetSelectedItems<InventoryItemListElement>().FirstOrDefault();
            if (selectedItem?.Item1.GetItem() == null)
                return;

            var item = selectedItem.Item1.GetItem();
            var cost = GetEnchantCost(item, _rarity);

            // Doesn't really cancel, just does all the same stuff
            Cancel();

            var completeSFX = CompleteSFX[(int)_rarity];
            if (Audio != null && completeSFX != null)
                Audio.PlayOneShot(completeSFX);

            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            foreach (var costElement in cost)
            {
                var costItem = costElement.GetItem();
                inventory.RemoveItem(costItem.m_shared.m_name, costItem.m_stack);
            }

            if (_successDialog != null)
                Destroy(_successDialog);

            DeselectAll();
            LockSelector();

            _successDialog = EnchantItem(item, _rarity);

            RefreshAvailableItems();
        }

        public void RefreshAvailableItems()
        {
            var items = GetEnchantableItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemChanged();
        }

        public void OnSelectedItemChanged()
        {
            OnRarityChanged();
        }

        private bool LocalPlayerCanAffordCost(List<InventoryItemListElement> cost)
        {
            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            foreach (var element in cost)
            {
                var item = element.GetItem();
                if (inventory.CountItems(item.m_shared.m_name) < item.m_stack)
                    return false;
            }

            return true;
        }

        public bool CanCancel()
        {
            return _inProgress || (_successDialog != null && _successDialog.activeSelf);
        }

        public void Cancel()
        {
            if (_successDialog != null && _successDialog.activeSelf)
            {
                Destroy(_successDialog);
                _successDialog = null;
            }

            _inProgress = false;
            _countdown = 0;

            Audio.loop = false;
            Audio.Stop();

            UnlockSelector();
            OnRarityChanged();
        }

        public void OnMainButtonClicked()
        {
            if (_inProgress)
                Cancel();
            else
                StartCountdown();
        }

        public void StartCountdown()
        {
            _buttonLabel.text = Localization.instance.Localize("$menu_cancel");
            _inProgress = true;
            _countdown = CountdownTime;
            ProgressBar.SetMaxValue(CountdownTime);

            Audio.loop = true;
            Audio.clip = ProgressLoopSFX;
            Audio.Play();

            LockSelector();
        }

        public void LockSelector()
        {
            AvailableItems.Lock();
            EnchantingTableUI.instance.LockTabs();
            foreach (var modeButton in RarityButtons)
            {
                modeButton.interactable = false;
            }
        }

        public void UnlockSelector()
        {
            AvailableItems.Unlock();
            EnchantingTableUI.instance.UnlockTabs();
            foreach (var modeButton in RarityButtons)
            {
                modeButton.interactable = true;
            }
        }

        public void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }
    }
}
