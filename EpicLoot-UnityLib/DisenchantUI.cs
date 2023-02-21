using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
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
            var items = GetDisenchantItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
        }

        protected override void DoMainAction()
        {
            Cancel();

            var selectedItem = AvailableItems.GetSingleSelectedItem<IListElement>();
            if (selectedItem?.Item1.GetItem() == null)
                return;

            var item = selectedItem.Item1.GetItem();
            var cost = GetDisenchantCost(item);
            if (!LocalPlayerCanAffordCost(cost))
                return;

            var player = Player.m_localPlayer;
            var inventory = player.GetInventory();
            foreach (var costElement in cost)
            {
                var costItem = costElement.GetItem();
                inventory.RemoveItem(costItem.m_shared.m_name, costItem.m_stack);
            }

            var bonusItems = DisenchantItem(item);

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
            var items = GetDisenchantItems();
            AvailableItems.SetItems(items.Cast<IListElement>().ToList());
            AvailableItems.DeselectAll();
            OnSelectedItemsChanged();
        }

        protected override void OnSelectedItemsChanged()
        {
            var selectedItem = AvailableItems.GetSingleSelectedItem<InventoryItemListElement>();

            if (selectedItem != null)
            {
                CostLabel.enabled = true;
                var cost = GetDisenchantCost(selectedItem.Item1.GetItem());
                CostList.SetItems(cost.Cast<IListElement>().ToList());

                var featureValues = EnchantingTableUpgrades.GetFeatureCurrentValue(EnchantingFeature.Disenchant);
                var costReduction = float.IsNaN(featureValues.Item2) ? 0 : (int)featureValues.Item2;

                if (costReduction > 0 && cost.Count > 0)
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_disenchantcost <color=#EAA800>($mod_epicloot_disenchantcostreduction)</color>", costReduction.ToString());
                else
                    CostLabel.text = Localization.instance.Localize("$mod_epicloot_disenchantcost");

                var canAfford = LocalPlayerCanAffordCost(cost);
                var featureUnlocked = EnchantingTableUpgrades.IsFeatureUnlocked(EnchantingFeature.Disenchant);
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
