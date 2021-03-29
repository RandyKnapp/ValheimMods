using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    public class BountyListElement : MonoBehaviour
    {
        public GameObject SelectedBackground;
        public Image Icon;
        public Text NameText;
        public Text CostText;
        public Button Button;
        public UITooltip Tooltip;

        public void Awake()
        {
            Button = GetComponent<Button>();
            Tooltip = GetComponent<UITooltip>();
            SelectedBackground = transform.Find("Selected").gameObject;
            SelectedBackground.SetActive(false);
            Icon = transform.Find("Icon").GetComponent<Image>();
            NameText = transform.Find("Name").GetComponent<Text>();
            CostText = transform.Find("Price/PriceElement/Amount").GetComponent<Text>();

            var iconMaterial = StoreGui.instance.m_listElement.transform.Find("icon").GetComponent<Image>().material;
            if (iconMaterial != null)
            {
                Icon.material = iconMaterial;
            }
        }
    }
}
