using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class SacrificeUI : EnchantingTableUIPanelBase
    {
        public MultiSelectItemList SacrificeProducts;

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
        }

        protected override void DoMainAction()
        {
            var selectedItems = AvailableItems.GetSelectedItems<IListElement>();
            var sacrificeProducts = GetSacrificeProducts(selectedItems.Select(x => new Tuple<ItemDrop.ItemData, int>(x.Item1.GetItem(), x.Item2)).ToList());

            Cancel();

            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            foreach (var selectedItem in selectedItems)
            {
                inventory.RemoveItem(selectedItem.Item1.GetItem(), selectedItem.Item2);
            }

            foreach (var sacrificeProduct in sacrificeProducts)
            {
                var item = sacrificeProduct.GetItem();
                if (inventory.CanAddItem(item))
                {
                    inventory.AddItem(item);
                    player.Message(MessageHud.MessageType.TopLeft, $"$msg_added {item.m_shared.m_name}", item.m_stack, item.GetIcon());
                }
                else
                {
                    var itemDrop = ItemDrop.DropItem(item, item.m_stack, player.transform.position + player.transform.forward + player.transform.up, player.transform.rotation);
                    itemDrop.GetComponent<Rigidbody>().velocity = Vector3.up * 5f;
                    player.Message(MessageHud.MessageType.TopLeft, $"$msg_dropped {itemDrop.m_itemData.m_shared.m_name} $mod_epicloot_sacrifice_inventoryfullexplanation", itemDrop.m_itemData.m_stack, itemDrop.m_itemData.GetIcon());
                }
            }

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
            MainButton.interactable = selectedItems.Count > 0;
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
