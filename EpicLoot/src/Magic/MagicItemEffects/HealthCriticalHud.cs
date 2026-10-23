using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace EpicLoot.MagicItemEffects
{
    [HarmonyPatch(typeof(Hud), nameof(Hud.Update))]
    public static class HealthCriticalHud_Hud_Update_Patch
    {
        public static void Postfix(Hud __instance) => HealthCriticalHud.Refresh(__instance);
    }

    internal static class HealthCriticalHud
    {
        private const float MarkerWidth = 2f;
        private const float EffectCheckInterval = 0.5f;
        private const float PulseSpeed = 6f;
        private const float BorderMinAlpha = 0.45f;

        private static readonly Color MarkerColor = new Color(1f, 0.92f, 0.7f, 0.9f);
        private static readonly Color BorderColor = new Color(1f, 0.6f, 0.15f);
        private static readonly Color BackgroundColor = new Color(0.6f, 0.15f, 0f, 0.7f);

        private static Hud _hud;
        private static RectTransform _marker;
        private static Image _border;
        private static Image _background;
        private static Color _backgroundBaseColor;
        private static bool _tinted;
        private static bool _hasEffects;
        private static float _nextEffectCheck;

        internal static void Refresh(Hud hud)
        {
            if (_hud != hud)
            {
                _hud = hud;
                Build(hud);
            }

            if (_marker == null)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            if (Time.time >= _nextEffectCheck)
            {
                _nextEffectCheck = Time.time + EffectCheckInterval;
                _hasEffects = ModifyWithLowHealth.PlayerHasHealthCriticalEffects(player);
            }

            bool show = player != null && _hasEffects;
            if (_marker.gameObject.activeSelf != show)
            {
                _marker.gameObject.SetActive(show);
            }

            if (show)
            {
                float threshold = Mathf.Clamp01(ModifyWithLowHealth.GetLowHealthPercentage(player));
                if (!Mathf.Approximately(_marker.anchorMin.x, threshold))
                {
                    _marker.anchorMin = new Vector2(threshold, 0f);
                    _marker.anchorMax = new Vector2(threshold, 1f);
                }
            }

            if (show && ModifyWithLowHealth.PlayerHasLowHealth(player))
            {
                SetTint(0.5f + 0.5f * Mathf.Sin(Time.time * PulseSpeed));
                _tinted = true;
            }
            else if (_tinted)
            {
                ClearTint();
                _tinted = false;
            }
        }

        private static void SetTint(float pulse)
        {
            if (_border != null)
            {
                Color color = BorderColor;
                color.a = Mathf.Lerp(BorderMinAlpha, 1f, pulse);
                _border.color = color;
                if (!_border.gameObject.activeSelf)
                {
                    _border.gameObject.SetActive(true);
                }
            }

            if (_background != null)
            {
                _background.color = Color.Lerp(_backgroundBaseColor, BackgroundColor, pulse);
            }
        }

        private static void ClearTint()
        {
            if (_border != null)
            {
                _border.gameObject.SetActive(false);
            }

            if (_background != null)
            {
                _background.color = _backgroundBaseColor;
            }
        }

        private static void Build(Hud hud)
        {
            _marker = null;
            _border = null;
            _background = null;
            _tinted = false;
            _nextEffectCheck = 0f;

            // Auga swaps in its own health bar, which this layout was not built against.
            if (EpicLoot.HasAuga || hud.m_healthBarRoot == null)
            {
                return;
            }

            RectTransform root = hud.m_healthBarRoot;

            // "border" and "bkg" are the child names in vanilla's HUD prefab. The border is tinted through
            // a renamed copy because healthpanel's Animator (the damage flash) is bound to "Health/border"
            // and rewrites that image's colour every frame.
            Transform border = root.Find("border");
            if (border != null && border.GetComponent<Image>() != null)
            {
                GameObject borderObject = Object.Instantiate(border.gameObject, root, false);
                borderObject.name = "EL_HealthCriticalBorder";
                borderObject.transform.SetSiblingIndex(border.GetSiblingIndex() + 1);
                _border = borderObject.GetComponent<Image>();
                _border.raycastTarget = false;
                borderObject.SetActive(false);
            }

            Transform background = root.Find("bkg");
            if (background != null && background.TryGetComponent(out _background))
            {
                _backgroundBaseColor = _background.color;
            }

            // Anchored by fraction across m_healthBarRoot, which vanilla resizes to the full bar length
            // (Hud.SetHealthBarSize), and added last so it draws over both fills.
            var markerObject = new GameObject("EL_HealthCriticalMarker", typeof(RectTransform), typeof(Image));
            markerObject.layer = root.gameObject.layer;
            _marker = (RectTransform)markerObject.transform;
            _marker.SetParent(root, false);
            _marker.pivot = new Vector2(0.5f, 0.5f);
            _marker.anchorMin = new Vector2(0f, 0f);
            _marker.anchorMax = new Vector2(0f, 1f);
            _marker.anchoredPosition = Vector2.zero;
            _marker.sizeDelta = new Vector2(MarkerWidth, 0f);

            var image = markerObject.GetComponent<Image>();
            image.color = MarkerColor;
            image.raycastTarget = false;

            markerObject.SetActive(false);
        }
    }
}
