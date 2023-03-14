using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class FeatureStatus : MonoBehaviour
    {
        public EnchantingFeature Feature;
        public Transform UnlockedContainer;
        public Transform LockedContainer;
        public GameObject UnlockedLabel;
        public Image[] Stars;
        public Text ManyStarsLabel;

        public void Awake()
        {
            EnchantingTableUpgrades.OnFeatureLevelChanged += OnFeatureLevelChanged;
            Refresh();
        }

        public void SetFeature(EnchantingFeature feature)
        {
            if (Feature != feature)
            {
                Feature = feature;
                Refresh();
            }
        }

        private void Refresh()
        {
            if (!EnchantingTableUpgrades.IsFeatureAvailable(Feature))
            {
                UnlockedContainer.gameObject.SetActive(false);
                LockedContainer.gameObject.SetActive(false);
                return;
            }

            if (EnchantingTableUpgrades.IsFeatureLocked(Feature))
            {
                UnlockedContainer.gameObject.SetActive(false);
                LockedContainer.gameObject.SetActive(true);
            }
            else
            {
                UnlockedContainer.gameObject.SetActive(true);
                LockedContainer.gameObject.SetActive(false);
                var level = EnchantingTableUpgrades.GetFeatureLevel(Feature);
                if (level > Stars.Length)
                {
                    for (var index = 0; index < Stars.Length; index++)
                    {
                        var star = Stars[index];
                        star.enabled = index == 0;
                    }

                    ManyStarsLabel.enabled = true;
                    ManyStarsLabel.text = $"×{level}";
                }
                else
                {
                    for (var index = 0; index < Stars.Length; index++)
                    {
                        var star = Stars[index];
                        star.enabled = level > index;
                    }
                    ManyStarsLabel.enabled = false;
                }

                UnlockedLabel.SetActive(level == 0);
            }
        }

        private void OnFeatureLevelChanged(EnchantingFeature feature, int _)
        {
            if (feature == Feature)
                Refresh();
        }
    }
}
