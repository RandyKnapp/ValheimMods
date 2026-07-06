using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class DisenchantUI : EnchantingTableUIPanelBase
    {
        public Text CostLabel;
        public MultiSelectItemList CostList;
        public EnchantBonus BonusPanel;

        public delegate List<InventoryItemListElement> GetDisenchantItemsDelegate();
        public delegate List<InventoryItemListElement> GetDisenchantCostDelegate(ItemDrop.ItemData item);
        public delegate List<InventoryItemListElement> DisenchantItemDelegate(ItemDrop.ItemData item);

        public static GetDisenchantItemsDelegate GetDisenchantItems;
        public static GetDisenchantCostDelegate GetDisenchantCost;
        public static DisenchantItemDelegate DisenchantItem;

        [UsedImplicitly]
        public void OnEnable()
        {
            List<InventoryItemListElement> items = GetDisenchantItems();
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
            List<InventoryItemListElement> cost = GetDisenchantCost(item);
            if (!LocalPlayerCanAffordCost(cost))
            {
                return;
            }

            foreach (InventoryItemListElement costElement in cost)
            {
                InventoryManagement.Instance.RemoveItem(costElement.GetItem());
            }

            List<InventoryItemListElement> bonusItems = DisenchantItem(item);

            if (bonusItems.Count > 0)
            {
                EnchantingTableUI.instance.PlayEnchantBonusSFX();
                BonusPanel.Show();

                GiveItemsToPlayer(bonusItems);
            }

            RefreshAvailableItems();
        }

        public void RefreshAvailableItems()
        {
            List<InventoryItemListElement> items = GetDisenchantItems();
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
                List<InventoryItemListElement> cost = GetDisenchantCost(selectedItem.Item1.GetItem());
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
