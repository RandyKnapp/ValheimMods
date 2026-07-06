using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    [RequireComponent(typeof(Toggle))]
    public class PlaySoundOnChecked : MonoBehaviour
    {
        public AudioSource Audio;
        public AudioClip SFX;

        public delegate float AudioVolumeLevelDelegate();
        public static AudioVolumeLevelDelegate AudioVolumeLevel;

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
            {
                Audio.volume = AudioVolumeLevel();
                Audio.PlayOneShot(SFX, Audio.volume);
            }
        }
    }
}
