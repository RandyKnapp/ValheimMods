using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    // The item's effects on the Augment and Rune tabs are rows of one prefab: a fixed 236x30 box whose
    // label auto-sizes between 12 and 16 and truncates. A short effect drew at 16 beside a long one shrunk
    // to 12, the longest still lost their tail, and on the Augment tab the fixed width ran past the list's
    // padding and under its scrollbar. Here every label gets one size, and the list sizes each row: its own
    // width, and as tall as the wrapped label.
    public class EffectListRow : UIBehaviour, ILayoutElement
    {
        private const float LabelFontSize = 14f;
        private const float RowMinHeight = 30f;
        private const float RowVerticalPadding = 6f;

        private TMP_Text _label;
        private float _preferredHeight = RowMinHeight;

        // Once per list, before rows are added. Every row in the list then needs Fit, since a row with no
        // layout element would collapse to nothing.
        public static void FitRowsToList(RectTransform list)
        {
            if (list == null || !list.TryGetComponent(out VerticalLayoutGroup layout))
            {
                return;
            }

            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
        }

        public static void Fit(GameObject row, TMP_Text label)
        {
            if (label != null)
            {
                label.enableAutoSizing = false;
                label.fontSize = LabelFontSize;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.overflowMode = TextOverflowModes.Overflow;
            }

            if (!row.TryGetComponent(out EffectListRow fitter))
            {
                fitter = row.AddComponent<EffectListRow>();
            }

            fitter._label = label;
            fitter.SetDirty();
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        // The list has already set the row's width by now, so the label's rect is the width it wraps at.
        public void CalculateLayoutInputVertical()
        {
            _preferredHeight = RowMinHeight;
            if (_label != null && _label.isActiveAndEnabled)
            {
                float labelHeight = _label.GetPreferredValues(_label.rectTransform.rect.width, 0f).y;
                _preferredHeight = Mathf.Max(RowMinHeight, labelHeight + RowVerticalPadding);
            }
        }

        public float minWidth => -1f;
        public float preferredWidth => -1f;
        public float flexibleWidth => -1f;
        public float minHeight => RowMinHeight;
        public float preferredHeight => _preferredHeight;
        public float flexibleHeight => -1f;
        public int layoutPriority => 1;

        protected override void OnEnable()
        {
            base.OnEnable();
            SetDirty();
        }

        private void SetDirty()
        {
            if (IsActive())
            {
                LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
            }
        }
    }
}
