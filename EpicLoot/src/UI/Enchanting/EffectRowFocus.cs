using UnityEngine;
using UnityEngine.EventSystems;

namespace EpicLoot_UnityLib
{
    // The rune tab's effect rows are plain toggles that only tint their checkbox, so a focused or hovered
    // row would look like the rest. The glow is the one the item rows use.
    public class EffectRowFocus : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject Glow;

        public bool Hovered { get; private set; }

        public void SetGlow(bool on)
        {
            if (Glow != null && Glow.activeSelf != on)
            {
                Glow.SetActive(on);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Hovered = false;
        }
    }
}
