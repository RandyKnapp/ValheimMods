using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class SacrificeUI : MonoBehaviour
    {
        public const float SacrificeCountdownTime = 2.0f;

        public MultiSelectItemList AvailableItems;
        public MultiSelectItemList SacrificeProducts;
        public Button PerformSacrificeButton;

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
        }

        public void OnEnable()
        {
            var items = GetSacrificeItems();
            AvailableItems.SetItems(items);
        }

        public void Update()
        {
            if (_inProgress)
            {
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
            // TODO: Do the shit
        }

        private void OnSelectedItemsChanged()
        {
            var selectedItems = AvailableItems.GetSelectedItems();
            var sacrificeProducts = GetSacrificeProducts(selectedItems);
            SacrificeProducts.SetItems(sacrificeProducts);
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
            LockSelector();
        }

        private void LockSelector()
        {
            AvailableItems.Lock();
            SacrificeProducts.Lock();
        }

        private void UnlockSelector()
        {
            AvailableItems.Unlock();
            SacrificeProducts.Unlock();
        }

        public void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }
    }
}
