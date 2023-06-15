using UnityEngine;

namespace EpicLoot_UnityLib
{
    public class FeatureStatus3D : MonoBehaviour
    {
        public EnchantingTable SourceTable;
        public EnchantingFeature Feature;
        public GameObject UnlockedObject;
        public GameObject[] LevelObjects;

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
            var featureIsUnlocked = SourceTable.IsFeatureAvailable(Feature) && SourceTable.IsFeatureUnlocked(Feature);
            if (UnlockedObject != null)
                UnlockedObject.SetActive(featureIsUnlocked);

            var currentLevel = SourceTable.GetFeatureLevel(Feature);
            for (var index = 0; index < LevelObjects.Length; index++)
            {
                var levelObject = LevelObjects[index];
                if (levelObject == null)
                    continue;
                levelObject.SetActive(featureIsUnlocked && currentLevel == index);
            }

            if (featureIsUnlocked && currentLevel >= LevelObjects.Length && LevelObjects[LevelObjects.Length - 1] != null)
                LevelObjects[LevelObjects.Length - 1].SetActive(true);
        }
    }
}
