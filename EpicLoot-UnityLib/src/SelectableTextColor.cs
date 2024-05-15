using TMPro;
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
        private TMP_Text _tmpText;
        private bool useTMP = false;

        public void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _text = GetComponentInChildren<Text>();
            if (_text != null)
            {
                _defaultColor = _text.color;
                return;
            }
            _tmpText = GetComponentInChildren<TMP_Text>();
            _defaultColor = _tmpText.color;
            useTMP = true;
        }

        public void Update()
        {
            if (_selectable.IsInteractable())
            {
                if (!useTMP)
                    _text.color = _defaultColor;
                else
                    _tmpText.color = _defaultColor;
            }
            else
            {
                if (!useTMP)
                    _text.color = DisabledColor;
                else
                    _tmpText.color = DisabledColor;
            }
        }
    }
}
