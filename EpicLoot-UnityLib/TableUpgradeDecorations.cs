using System;
using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class TableUpgradeDecorations : MonoBehaviour
    {
        public EnchantingTable SourceTable;

        public GameObject BaseObjects;
        public GameObject TwoPlusObjects;
        public GameObject FourPlusObjects;

        public void OnEnable()
        {
            SourceTable.OnAnyFeatureLevelChanged += Refresh;
            Refresh();
        }

        public void OnDisable()
        {
            SourceTable.OnAnyFeatureLevelChanged -= Refresh;
        }

        public void Refresh()
        {
            var unlockedCount = 0;
            foreach (EnchantingFeature feature in Enum.GetValues(typeof(EnchantingFeature)))
            {
                if (SourceTable.IsFeatureAvailable(feature) && SourceTable.IsFeatureUnlocked(feature))
                    ++unlockedCount;
            }

            BaseObjects.SetActive(true);
            TwoPlusObjects.SetActive(unlockedCount >= 2);
            FourPlusObjects.SetActive(unlockedCount >= 4);
        }
    }
}
