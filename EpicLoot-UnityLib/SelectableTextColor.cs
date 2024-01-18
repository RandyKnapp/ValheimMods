using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    public class SelectableTextColor : MonoBehaviour
    {
        private Color _defaultColor = Color.white;
        public Color DisabledColor = Color.grey;
        private Selectable _selectable;
        private Text _text;

        public void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _text = GetComponentInChildren<Text>();
            _defaultColor = _text.color;
        }

        public void Update()
        {
            if (_selectable.IsInteractable())
                _text.color = _defaultColor;
            else
                _text.color = DisabledColor;
        }
    }
}
