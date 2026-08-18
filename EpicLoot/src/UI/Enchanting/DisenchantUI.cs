using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using EpicLoot.CraftingV2;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class DisenchantUI : EnchantingTableUIPanelBase
    {
        public Text CostLabel;
        public MultiSelectItemList CostList;
        public EnchantBonus BonusPanel;

        [UsedImplicitly]
        public void OnEnable()
        {
            List<InventoryItemListElement> items = EnchantingUIController.GetDisenchantItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
        }

        protected override void DoMainAction()
        {
            Cancel();

            System.Tuple<IListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<IListElement>();
            if (selectedItem?.Item1.GetItem() == null)
            {
                return;
            }

            ItemDrop.ItemData item = selectedItem.Item1.GetItem();
            List<InventoryItemListElement> cost = EnchantingUIController.GetDisenchantCost(item);
            if (!LocalPlayerCanAffordCost(cost))
            {
                return;
            }

            foreach (InventoryItemListElement costElement in cost)
            {
                InventoryManagement.Instance.RemoveItem(costElement.GetItem());
            }

            // Returned items are the bonus roll plus any shards/runestones freed from the item's sockets;
            // only the bonus roll gets the bonus fanfare.
            List<InventoryItemListElement> returnedItems = EnchantingUIController.DisenchantItem(item, out bool bonusRolled);

            if (bonusRolled)
            {
                EnchantingTableUI.instance.PlayEnchantBonusSFX();
                BonusPanel.Show();
            }

            if (returnedItems.Count > 0)
            {
                GiveItemsToPlayer(returnedItems);
            }

            RefreshAvailableItems();
        }

        public void RefreshAvailableItems()
        {
            List<InventoryItemListElement> items = EnchantingUIController.GetDisenchantItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            System.Tuple<InventoryItemListElement, int> selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();

            if (selectedItem != null)
            {
                CostLabel.enabled = true;
                List<InventoryItemListElement> cost = EnchantingUIController.GetDisenchantCost(selectedItem.Item1.GetItem());
                CostList.SetItems(cost.Cast<IListElement>().ToList());

                System.Tuple<float, float> featureValues = EnchantingTableUI.instance.SourceTable.GetFeatureCurrentValue(EnchantingFeature.Disenchant);
                int costReduction = float.IsNaN(featureValues.Item2) ? 0 : (int)featureValues.Item2;

                if (costReduction > 0 && cost.Count > 0)
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_disenchantcost <color=#EAA800>($mod_epicloot_disenchantcostreduction)</color>", costReduction.ToString());
                else
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_disenchantcost");

                bool canAfford = LocalPlayerCanAffordCost(cost);
                bool featureUnlocked = EnchantingTableUI.instance.SourceTable.IsFeatureUnlocked(EnchantingFeature.Disenchant);
                MainButton.interactable = featureUnlocked && canAfford;
            }
            else
            {
                CostList.SetItems(new List<IListElement>());
                CostLabel.enabled = false;
                MainButton.interactable = false;
            }
        }

        public override void DeselectAll()
        {
            AvailableItems.DeselectAll();
        }
    }
}
