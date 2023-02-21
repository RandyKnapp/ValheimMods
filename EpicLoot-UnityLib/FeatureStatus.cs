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
        }

        public void OnDestroy()
        {
            EnchantingTableUpgrades.OnFeatureLevelChanged -= OnFeatureLevelChanged;
        }

        public void OnEnable()
        {
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

        public void Refresh()
        {
            if (!EnchantingTableUpgrades.IsFeatureAvailable(Feature))
            {
                if (UnlockedContainer != null)
                    UnlockedContainer.gameObject.SetActive(false);
                if (LockedContainer != null)
                    LockedContainer.gameObject.SetActive(false);
                return;
            }

            if (EnchantingTableUpgrades.IsFeatureLocked(Feature))
            {
                if (UnlockedContainer != null)
                    UnlockedContainer.gameObject.SetActive(false);
                if (LockedContainer != null)
                    LockedContainer.gameObject.SetActive(true);
            }
            else
            {
                if (UnlockedContainer != null)
                    UnlockedContainer.gameObject.SetActive(true);
                if (LockedContainer != null)
                    LockedContainer.gameObject.SetActive(false);

                var level = EnchantingTableUpgrades.GetFeatureLevel(Feature);
                if (level > Stars.Length)
                {
                    for (var index = 0; index < Stars.Length; index++)
                    {
                        var star = Stars[index];
                        star.enabled = index == 0;
                    }

                    if (ManyStarsLabel != null)
                    {
                        ManyStarsLabel.enabled = true;
                        ManyStarsLabel.text = $"×{level}";
                    }
                }
                else
                {
                    for (var index = 0; index < Stars.Length; index++)
                    {
                        var star = Stars[index];
                        star.gameObject.SetActive(level > index);
                    }

                    if (ManyStarsLabel != null)
                        ManyStarsLabel.enabled = false;
                }

                if (UnlockedLabel != null)
                    UnlockedLabel.SetActive(level == 0);
            }
        }

        private void OnFeatureLevelChanged(EnchantingFeature feature, int _)
        {
            if (isActiveAndEnabled && feature == Feature)
                Refresh();
        }
    }
}
