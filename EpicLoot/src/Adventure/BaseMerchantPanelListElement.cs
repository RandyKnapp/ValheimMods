using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.Adventure
{
    public interface IMerchantPanelListElement
    {
        void SetSelected(bool selected);
        Button GetButton();
    }

    public class BaseMerchantPanelListElement<T> : MonoBehaviour, IMerchantPanelListElement where T : class
    {
        public GameObject SelectedBackground;

        private Button _button;

        public void SetSelected(bool selected)
        {
            SelectedBackground.SetActive(selected);
        }

        public Button GetButton()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            return _button;
        }
    }
}
