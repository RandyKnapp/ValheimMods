using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EpicLoot_UnityLib
{
    public class SacrificeUI : EnchantingTableUIPanelBase
    {
        public MultiSelectItemList SacrificeProducts;
        public EnchantBonus BonusPanel;

        public delegate List<InventoryItemListElement> GetSacrificeItemsDelegate();
        public delegate List<InventoryItemListElement> GetSacrificeProductsDelegate(List<Tuple<ItemDrop.ItemData, int>> items);

        public static GetSacrificeItemsDelegate GetSacrificeItems;
        public static GetSacrificeProductsDelegate GetSacrificeProducts;

        [UsedImplicitly]
        public void OnEnable()
        {
            var items = GetSacrificeItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            var random = new System.Random();
        }

        protected override void DoMainAction()
        {
            var selectedItems = AvailableItems.GetSelectedItems<IListElement>();
            var sacrificeProducts = GetSacrificeProducts(selectedItems.Select(x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList());

            Cancel();

            var chanceToDoubleEntry = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Sacrifice);
            var chanceToDouble = float.IsNaN(chanceToDoubleEntry.Item1) ? 0.0f : chanceToDoubleEntry.Item1 / 100.0f;

            if (Random.Range(0.0f, 1.0f) < chanceToDouble)
            {
                EnchantingTableUI.instance.PlayEnchantBonusSFX();
                BonusPanel.Show();

                foreach (var sacrificeProduct in sacrificeProducts)
                {
                    sacrificeProduct.Item.m_stack *= 2;
                }
            }

            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            foreach (var selectedItem in selectedItems)
            {
                inventory.RemoveItem(selectedItem.Item1.GetItem(), selectedItem.Item2);
            }

            GiveItemsToPlayer(sacrificeProducts);

            RefreshAvailableItems();
            AvailableItems.GiveFocus(true, 0);
        }

        private void RefreshAvailableItems()
        {
            var items = GetSacrificeItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            var selectedItems = AvailableItems.GetSelectedItems<IListElement>();
            var sacrificeProducts = GetSacrificeProducts(selectedItems.Select(x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList());
            SacrificeProducts.SetItems(sacrificeProducts.Cast<IListElement>().ToList());
            var featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Sacrifice);
            MainButton.interactable = featureUnlocked && selectedItems.Count > 0;
        }
        
        public override void Cancel()
        {
            _buttonLabel.text = Localization.instance.Localize("$mod_epicloot_sacrifice");
            base.Cancel();
        }
        
        public override void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }
    }
}
