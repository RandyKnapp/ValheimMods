using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using EGU = UnityEditor.EditorGUIUtility;

namespace ThunderKit.Core.Editor
{
    public class PickerWindow : EditorWindow
    {
        internal enum ShowMode
        {
            // Show as a normal window with max, min & close buttons.
            NormalWindow = 0,
            // Used for a popup menu. On mac this means light shadow and no titlebar.
            PopupMenu = 1,
            // Utility window - floats above the app. Disappears when app loses focus.
            Utility = 2,
            // Window has no shadow or decorations. Used internally for dragging stuff around.
            NoShadow = 3,
            // The Unity main window. On mac, this is the same as NormalWindow, except window doesn't have a close button.
            MainWindow = 4,
            // Aux windows. The ones that close the moment you move the mouse out of them.
            AuxWindow = 5,
            // Like PopupMenu, but without keyboard focus
            Tooltip = 6,
            // Modal Utility window
            ModalUtility = 7
        }

        public Func<int, object, bool> OnItemGUI;

        MethodInfo showPopupNonFocus;
        object showValue;
        object[] showValueArray;

        public List<object> options;
        public int visibleItemCount = 8;
        public float itemHeight = EGU.singleLineHeight;
        private int offset = 0;

        private void OnEnable()
        {
            showPopupNonFocus = typeof(EditorWindow).GetMethod("ShowPopupWithMode", BindingFlags.Instance | BindingFlags.NonPublic);
            var showMode = typeof(EditorWindow).Assembly.GetType("UnityEditor.ShowMode");
            showValue = Enum.GetValues(showMode).GetValue((int)ShowMode.PopupMenu);
            showValueArray = new[] { showValue, false };
        }

        public void Show(Rect position)
        {
            showPopupNonFocus.Invoke(this, showValueArray);
            UpdatePopupPosition(position);
        }

        public void UpdatePopupPosition(Rect rect)
        {
            float newY = rect.y + rect.height + EGU.standardVerticalSpacing;

            var pos = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, newY));

            position = new Rect(pos.x, pos.y, rect.width, GetHeight());
        }

        float GetHeight() => (visibleItemCount * (itemHeight + (EGU.standardVerticalSpacing * 2))) - EGU.standardVerticalSpacing;

        public void OnGUI()
        {
            var bgc = GUI.backgroundColor;

            GUI.Box(new Rect(0, 0, position.width, GetHeight()), "");

            //var margin = GUI.skin.scrollView.margin;
            //GUI.skin.scrollView.margin = new RectOffset(1, 1, 1, 1);
            //scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            //GUI.skin.scrollView.margin = margin;
            if (Event.current.type == EventType.ScrollWheel)
            {
                var delta = Event.current.delta;
                if (delta.y > 0) offset++;
                else
                    offset--;

                offset = Mathf.Clamp(offset, 0, (options.Count - visibleItemCount) - 1);
                Repaint();
            }

            if (options == null || !options.Any()) return;
            for (int i = offset; i < Mathf.Clamp(offset + visibleItemCount, 0, options.Count); i++)
            {
                var result = options[i];
                if (OnItemGUI?.Invoke(i, result) == true) break;
            }

            //GUI.EndScrollView();

            GUI.backgroundColor = bgc;
        }

    }
}