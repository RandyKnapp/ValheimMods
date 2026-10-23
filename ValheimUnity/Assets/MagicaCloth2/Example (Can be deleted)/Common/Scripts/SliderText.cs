// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEngine;
using UnityEngine.UI;

namespace MagicaCloth2
{
    public class SliderText : MonoBehaviour
    {
        [SerializeField]
        private Text text = null;

        [SerializeField]
        private string lable = "";

        [SerializeField]
        private string format = "0.00";

        private string formatString;

        protected void Start()
        {
            formatString = "{0} ({1:" + format + "})";

            var slider = GetComponent<Slider>();
            if (slider)
            {
                slider.onValueChanged.AddListener(OnChangeValue);

                var val = slider.value;
                slider.value = 0.001f;
                slider.value = val;
            }

        }

        private void OnChangeValue(float value)
        {
            if (text)
            {
                text.text = string.Format(formatString, lable, value);
            }
        }
    }
}
