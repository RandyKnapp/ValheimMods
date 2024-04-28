using BepInEx.Configuration;
using UnityEngine;

namespace Common
{
    public class ConfigPositionedElement : MonoBehaviour
    {
        public ConfigEntry<TextAnchor> AnchorConfig;
        public ConfigEntry<Vector2> PositionConfig;

        protected RectTransform _rt;
        protected TextAnchor _currentAnchor;

        public virtual void Awake()
        {
            _rt = (RectTransform)transform;
            EnsureCorrectPosition();
        }

        public virtual void Update()
        {
            EnsureCorrectPosition();
        }

        public virtual void EnsureCorrectPosition()
        {
            if (AnchorConfig == null || PositionConfig == null || (_currentAnchor == AnchorConfig.Value && _rt.anchoredPosition == PositionConfig.Value))
            {
                return;
            }

            _currentAnchor = AnchorConfig.Value;
            switch (_currentAnchor)
            {
                case TextAnchor.UpperLeft:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0, 1);
                    break;
                case TextAnchor.UpperCenter:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 1);
                    break;
                case TextAnchor.UpperRight:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(1, 1);
                    break;
                case TextAnchor.MiddleLeft:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0, 0.5f);
                    break;
                case TextAnchor.MiddleCenter:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
                case TextAnchor.MiddleRight:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(1, 0.5f);
                    break;
                case TextAnchor.LowerLeft:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0, 0);
                    break;
                case TextAnchor.LowerCenter:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0);
                    break;
                case TextAnchor.LowerRight:
                    _rt.pivot = _rt.anchorMin = _rt.anchorMax = new Vector2(1, 0);
                    break;
            }

            _rt.anchoredPosition = PositionConfig.Value;
        }
    }
}
