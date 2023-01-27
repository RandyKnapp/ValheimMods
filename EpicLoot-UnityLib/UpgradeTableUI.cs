using System;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class UpgradeTableUI : EnchantingTableUIPanelBase
    {
        public Text SelectedFeatureText;
        public Image SelectedFeatureImage;
        public Text CostLabel;
        public MultiSelectItemList CostList;

        protected override void DoMainAction()
        {
            throw new NotImplementedException();
        }

        protected override void OnSelectedItemsChanged()
        {
            throw new NotImplementedException();
        }
    }
}
