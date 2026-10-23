using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace EpicLoot.MagicItemEffects
{
    // A set effect's gauge on the HUD: a clone of vanilla's adrenaline bar, recoloured, under the stamina bar, one row
    // below the adrenaline bar when that is showing. It keeps the bar's Animator, so it shows, hides and flashes as
    // vanilla's does. Rebuilt whenever the Hud instance changes (a new world, a logout). The owner calls Refresh from a
    // Hud.Update postfix every frame. Taken from KineticQuakeHud, which predates it.
    internal sealed class SetGaugeBar
    {
        private const float RowOffset = 20f;           // pixels below vanilla's adrenaline bar when that is showing
        private const float BaseY = 130f;              // vanilla's adrenaline/stamina bar position
        private const float RaisedBaseY = 320f;        // ... with the build or ship HUD up
        private const float PulseRate = 4f;            // radians per second

        private static readonly int VisibleParam = Animator.StringToHash("Visible");
        private static readonly int FlashParam = Animator.StringToHash("Flash");

        private readonly string _name;
        private readonly float _width;
        private readonly Color _fillColor;
        private readonly Color _fullColor;
        private readonly Color _slowColor;

        private Hud _hud;
        private RectTransform _root;
        private Animator _animator;
        private GuiBar _fast;
        private GuiBar _slow;
        private TMP_Text _text;
        private bool _wasFull;
        private string _shownText;

        // width: pixels (vanilla's bar is 64 per 25 max adrenaline). fullColor is pulsed towards while the gauge is full.
        internal SetGaugeBar(string name, float width, Color fillColor, Color fullColor, Color slowColor)
        {
            _name = name;
            _width = width;
            _fillColor = fillColor;
            _fullColor = fullColor;
            _slowColor = slowColor;
        }

        // value is 0-1; the bar flashes on the frame it reaches full, then pulses while it stays there.
        internal void Refresh(Hud hud, bool show, float value, string text)
        {
            if (_hud != hud)
            {
                _hud = hud;
                Build(hud);
            }

            if (_root == null)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            show &= player != null && !player.IsDead();
            if (_animator != null)
            {
                _animator.SetBool(VisibleParam, show);
            }

            if (!show)
            {
                _wasFull = false;
                return;
            }

            value = Mathf.Clamp01(value);
            bool full = value >= 0.999f;
            if (full && !_wasFull && _animator != null)
            {
                _animator.SetTrigger(FlashParam);
            }
            _wasFull = full;

            bool raised = (hud.m_buildHud != null && hud.m_buildHud.activeSelf) ||
                (hud.m_shipHudRoot != null && hud.m_shipHudRoot.activeSelf);
            float y = raised ? RaisedBaseY : BaseY;
            if (player.GetAdrenaline() > 0f)
            {
                y -= RowOffset;
            }
            _root.anchoredPosition = new Vector2(0f, y);
            _root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _width + hud.m_staminaBarBorderBuffer);

            Color color = _fillColor;
            if (full)
            {
                color = Color.Lerp(_fillColor, _fullColor, 0.5f + 0.5f * Mathf.Sin(Time.time * PulseRate));
            }

            if (_fast != null)
            {
                _fast.SetWidth(_width);
                _fast.SetValue(value);
                _fast.SetColor(color);
            }

            if (_slow != null)
            {
                _slow.SetWidth(_width);
                _slow.SetValue(value);
                _slow.SetColor(_slowColor);
            }

            if (_text != null && text != _shownText)
            {
                _shownText = text;
                _text.text = text;
            }
        }

        private void Build(Hud hud)
        {
            _root = null;
            _animator = null;
            _fast = null;
            _slow = null;
            _text = null;
            _wasFull = false;
            _shownText = null;

            RectTransform source = hud != null ? hud.m_adrenalineBarRoot : null;
            if (source == null)
            {
                return;
            }

            GameObject clone = Object.Instantiate(source.gameObject, source.parent, false);
            clone.name = _name;
            clone.SetActive(true);

            _root = (RectTransform)clone.transform;
            _animator = clone.GetComponent<Animator>();
            _fast = FindCounterpart<GuiBar>(source, _root, hud.m_adrenalineBarFast);
            _slow = FindCounterpart<GuiBar>(source, _root, hud.m_adrenalineBarSlow);
            _text = FindCounterpart<TMP_Text>(source, _root, hud.m_adrenalineText);
            if (_animator != null)
            {
                _animator.SetBool(VisibleParam, false);
            }
        }

        // The clone's copy of a component under the original root, found by its path from that root.
        private static T FindCounterpart<T>(Transform sourceRoot, Transform cloneRoot, Component source) where T : Component
        {
            if (source == null)
            {
                return null;
            }

            var path = new List<string>();
            Transform current = source.transform;
            while (current != null && current != sourceRoot)
            {
                path.Add(current.name);
                current = current.parent;
            }

            if (current == null)
            {
                return null;
            }

            Transform found = cloneRoot;
            for (int i = path.Count - 1; i >= 0 && found != null; i--)
            {
                found = found.Find(path[i]);
            }

            return found != null ? found.GetComponent<T>() : null;
        }
    }
}
