using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    [RequireComponent(typeof(Toggle))]
    public class PlaySoundOnChecked : MonoBehaviour
    {
        public AudioSource Audio;
        public AudioClip SFX;

        private Toggle _toggle;

        public void Awake()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        public void OnDestroy()
        {
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        private void OnToggleChanged(bool _)
        {
            if (Audio != null && SFX != null && _toggle.isOn)
                Audio.PlayOneShot(SFX);
        }
    }
}
