using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EpicLoot_UnityLib
{
    // The item's effects on the Augment and Rune tabs are rows of one prefab, in a list that sets each row's
    // width; this makes a row as tall as its wrapped label.
    public class EffectListRow : UIBehaviour, ILayoutElement
    {
        private const float RowMinHeight = 30f;
        private const float RowVerticalPadding = 6f;

        [SerializeField] private TMP_Text _label;
        private float _preferredHeight = RowMinHeight;

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
