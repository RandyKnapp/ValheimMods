using System.Text;
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
        public UITooltip Tooltip;

        public delegate void MakeFeatureUnlockTooltipDelegate(GameObject obj);
        public static MakeFeatureUnlockTooltipDelegate MakeFeatureUnlockTooltip;

        public delegate bool UpgradesActiveDelegate(EnchantingFeature feature, out bool featureActive);
        public static UpgradesActiveDelegate UpgradesActive;

        public void Awake()
        {
            if (Tooltip != null)
            {
                MakeFeatureUnlockTooltip(Tooltip.gameObject);
            }
        }

        public void OnEnable()
        {
            if (EnchantingTableUI.instance != null && EnchantingTableUI.instance.SourceTable != null)
            {
                EnchantingTableUI.instance.SourceTable.OnFeatureLevelChanged += OnFeatureLevelChanged;
            }

            Refresh();
        }

        public void OnDisable()
        {
            if (EnchantingTableUI.instance != null && EnchantingTableUI.instance.SourceTable != null)
            {
                EnchantingTableUI.instance.SourceTable.OnFeatureLevelChanged -= OnFeatureLevelChanged;
            }
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
            if (EnchantingTableUI.instance == null || EnchantingTableUI.instance.SourceTable == null)
            {
                return;
            }

            if (EnchantingTableUI.instance.SourceTable.IsFeatureAvailable(Feature) == false)
            {
                if (UnlockedContainer != null)
                {
                    UnlockedContainer.gameObject.SetActive(false);
                }

                if (LockedContainer != null)
                {
                    LockedContainer.gameObject.SetActive(false);
                }

                return;
            }

            if (EnchantingTableUI.instance.SourceTable.IsFeatureLocked(Feature) == true)
            {
                if (UnlockedContainer != null)
                {
                    UnlockedContainer.gameObject.SetActive(false);
                }
                if (LockedContainer != null)
                {
                    LockedContainer.gameObject.SetActive(true);
                }
            }
            else
            {
                if (UnlockedContainer != null)
                {
                    UnlockedContainer.gameObject.SetActive(true);
                }
                if (LockedContainer != null)
                {
                    LockedContainer.gameObject.SetActive(false);
                }

                int level = EnchantingTableUI.instance.SourceTable.GetFeatureLevel(Feature);
                if (level > Stars.Length)
                {
                    for (int index = 0; index < Stars.Length; index++)
                    {
                        Stars[index].gameObject.SetActive(index == 0);
                    }

                    if (ManyStarsLabel != null)
                    {
                        ManyStarsLabel.gameObject.SetActive(true);
                        ManyStarsLabel.text = $"×{level}";
                    }
                }
                else
                {
                    for (int index = 0; index < Stars.Length; index++)
                    {
                        bool active = level > index && UpgradesActive(Feature, out _);
                        Stars[index].gameObject.SetActive(active);
                    }

                    ManyStarsLabel?.gameObject.SetActive(false);
                }

                UnlockedLabel?.SetActive(level == 0);
            }

            if (Tooltip != null && UpgradesActive(Feature, out _))
            {
                Tooltip.m_topic = Localization.instance.Localize(EnchantingTableUpgrades.GetFeatureName(Feature));

                StringBuilder sb = new StringBuilder();
                bool locked = EnchantingTableUI.instance.SourceTable.IsFeatureLocked(Feature);
                int currentLevel = EnchantingTableUI.instance.SourceTable.GetFeatureLevel(Feature);
                int maxLevel = EnchantingTableUpgrades.GetFeatureMaxLevel(Feature);

                if (locked)
                {
                    sb.AppendLine(Localization.instance.Localize("$mod_epicloot_currentlevel: " +
                        "<color=#AD1616><b>$mod_epicloot_featurelocked</b></color>"));
                }
                else if (currentLevel == 0)
                {
                    sb.AppendLine(Localization.instance.Localize($"$mod_epicloot_currentlevel: " +
                        $"<color=#1AACEF><b>$mod_epicloot_featureunlocked</b></color> / {maxLevel}"));
                }
                else
                {
                    sb.AppendLine(Localization.instance.Localize($"$mod_epicloot_currentlevel: " +
                        $"<color=#EAA800><b>{currentLevel}</b></color> / {maxLevel}"));
                }

                if (!locked && currentLevel > 0)
                {
                    string text = EnchantingTableUpgrades.GetFeatureUpgradeLevelDescription(
                        EnchantingTableUI.instance.SourceTable, Feature, currentLevel);
                    sb.AppendLine($"<color=#EAA800>{text}</color>");
                }

                sb.AppendLine();
                sb.AppendLine(Localization.instance.Localize(EnchantingTableUpgrades.GetFeatureDescription(Feature)));

                Tooltip.m_text = Localization.instance.Localize(sb.ToString());
            }
        }

        private void OnFeatureLevelChanged(EnchantingFeature feature, int _)
        {
            if (isActiveAndEnabled && feature == Feature)
            {
                Refresh();
            }
        }
    }
}
