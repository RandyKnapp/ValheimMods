using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    public class AbandonBountyDialog : MonoBehaviour
    {
        public MerchantPanel MerchantPanel;
        public BountyListElement BountyDisplay;
        public Button YesButton;
        public Button NoButton;

        private BountyInfo _bountyInfo;

        public void Awake()
        {
            MerchantPanel = transform.parent.GetComponent<MerchantPanel>();
            BountyDisplay = transform.Find("BountyInfo").gameObject.AddComponent<BountyListElement>();
            YesButton = transform.Find("YesButton").GetComponent<Button>();
            NoButton = transform.Find("NoButton").GetComponent<Button>();

            YesButton.onClick.AddListener(OnYesButtonClicked);
            NoButton.onClick.AddListener(OnNoButtonClicked);
        }

        private void OnYesButtonClicked()
        {
            AdventureDataManager.Bounties.AbandonBounty(_bountyInfo);
            MerchantPanel.OnAbandonBounty();
            Close();
        }

        private void OnNoButtonClicked()
        {
            Close();
        }

        public void Show(BountyInfo bountyInfo)
        {
            gameObject.SetActive(true);

            _bountyInfo = bountyInfo;

            BountyDisplay.SetItem(_bountyInfo);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
