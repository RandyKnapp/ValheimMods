using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class SacrificeUI : MonoBehaviour
    {
        public const float SacrificeCountdownTime = 0.8f;

        public MultiSelectItemList AvailableItems;
        public MultiSelectItemList SacrificeProducts;
        public Button PerformSacrificeButton;
        public GuiBar ProgressBar;
        public Text ProductsFitWarning;
        public AudioSource Audio;
        public AudioClip ProgressLoopSFX;
        public AudioClip CompleteSFX;

        public delegate List<ItemDrop.ItemData> GetSacrificeItemsDelegate();
        public delegate List<ItemDrop.ItemData> GetSacrificeProductsDelegate(List<Tuple<ItemDrop.ItemData, int>> items);

        public static GetSacrificeItemsDelegate GetSacrificeItems;
        public static GetSacrificeProductsDelegate GetSacrificeProducts;

        private bool _inProgress;
        private float _countdown;
        private Text _buttonLabel;

        public void Awake()
        {
            AvailableItems.OnSelectedItemsChanged += OnSelectedItemsChanged;
            PerformSacrificeButton.onClick.AddListener(OnSacrificeButtonClicked);
            _buttonLabel = PerformSacrificeButton.GetComponentInChildren<Text>();

            var uiSFX = GameObject.Find("sfx_gui_button");
            if (uiSFX)
                Audio.outputAudioMixerGroup = uiSFX.GetComponent<AudioSource>().outputAudioMixerGroup;

            if (ProductsFitWarning != null)
                ProductsFitWarning.gameObject.SetActive(false);
        }

        public void OnEnable()
        {
            var items = GetSacrificeItems();
            AvailableItems.SetItems(items);
        }

        public void Update()
        {
            ProgressBar.gameObject.SetActive(_inProgress);
            if (_inProgress)
            {
                if (ProductsFitWarning != null)
                    ProductsFitWarning.gameObject.SetActive(false);

                ProgressBar.SetValue(SacrificeCountdownTime - _countdown);

                _countdown -= Time.deltaTime;
                if (_countdown < 0)
                {
                    DoSacrifice();
                }
            }
        }

        private void DoSacrifice()
        {
            // Doesn't really cancel, just does all the same stuff
            Cancel();

            Audio.PlayOneShot(CompleteSFX);

            var selectedItems = AvailableItems.GetSelectedItems();
            var sacrificeProducts = GetSacrificeProducts(selectedItems);

            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            foreach (var selectedItem in selectedItems)
            {
                inventory.RemoveItem(selectedItem.Item1, selectedItem.Item2);
            }

            foreach (var sacrificeProduct in sacrificeProducts)
            {
                if (inventory.CanAddItem(sacrificeProduct))
                {
                    inventory.AddItem(sacrificeProduct);
                    player.Message(MessageHud.MessageType.TopLeft, $"$msg_added {sacrificeProduct.m_shared.m_name}", sacrificeProduct.m_stack, sacrificeProduct.GetIcon());
                }
                else
                {
                    var itemDrop = ItemDrop.DropItem(sacrificeProduct, sacrificeProduct.m_stack, player.transform.position + player.transform.forward + player.transform.up, player.transform.rotation);
                    itemDrop.GetComponent<Rigidbody>().velocity = Vector3.up * 5f;
                    player.Message(MessageHud.MessageType.TopLeft, $"$msg_dropped {itemDrop.m_itemData.m_shared.m_name} $mod_epicloot_sacrifice_inventoryfullexplanation", itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
                }
            }

            RefreshAvailableItems();
        }

        private void RefreshAvailableItems()
        {
            var items = GetSacrificeItems();
            AvailableItems.SetItems(items);
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        private void OnSelectedItemsChanged()
        {
            var selectedItems = AvailableItems.GetSelectedItems();
            var sacrificeProducts = GetSacrificeProducts(selectedItems);
            SacrificeProducts.SetItems(sacrificeProducts);

            Debug.LogWarning($"Selected Items: {selectedItems.Count}");
            PerformSacrificeButton.interactable = selectedItems.Count > 0;
        }

        public bool CanCancel()
        {
            return _inProgress;
        }

        public void Cancel()
        {
            _inProgress = false;
            _countdown = 0;
            _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_sacrifice");

            Audio.loop = false;
            Audio.Stop();

            UnlockSelector();
        }

        private void OnSacrificeButtonClicked()
        {
            if (_inProgress)
                Cancel();
            else
                StartCountdown();
        }

        private void StartCountdown()
        {
            _buttonLabel.text = Localization.instance.Localize("$menu_cancel");
            _inProgress = true;
            _countdown = SacrificeCountdownTime;
            ProgressBar.SetMaxValue(SacrificeCountdownTime);

            Audio.loop = true;
            Audio.clip = ProgressLoopSFX;
            Audio.Play();

            LockSelector();
        }

        private void LockSelector()
        {
            AvailableItems.Lock();
            SacrificeProducts.Lock();
            EnchantingUI.instance.LockTabs();
        }

        private void UnlockSelector()
        {
            AvailableItems.Unlock();
            SacrificeProducts.Unlock();
            EnchantingUI.instance.UnlockTabs();
        }

        public void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }
    }
}
