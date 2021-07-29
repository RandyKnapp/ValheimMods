using ThunderKit.Core.Pipelines;
using UnityEditor;
using UnityEngine;
using static UnityEditor.EditorGUIUtility;

namespace ThunderKit.Core.Editor.Inspectors
{
    [CustomEditor(typeof(Pipeline), true)]
    public class PipelineEditor : ComposableObjectEditor
    {
        protected override Rect OnBeforeElementHeaderGUI(Rect rect, ComposableElement element, ref string title)
        {
            var job = element as PipelineJob;
            var toggleRect = new Rect(rect.x + 2, rect.y + 1, 14, EditorGUIUtility.singleLineHeight);
            var titleContent = new GUIContent(title);
            job.Active = GUI.Toggle(toggleRect, job.Active, titleContent);
            toggleRect.x += 16;
            var size = GUI.skin.label.CalcSize(titleContent);
            toggleRect.width = size.x;
            toggleRect.height = size.y;
            GUI.Label(toggleRect, title);
            title = string.Empty;

            return rect;
        }
        protected override Rect OnAfterElementHeaderGUI(Rect rect, ComposableElement element)
        {
            var offset = 16;
            rect.x += offset;
            rect.width -= offset;
            return rect;
        }
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var size = new Vector2(250, 24);
            var rect = GUILayoutUtility.GetRect(size.x, size.y);
            rect.width = size.x;
            rect.y += standardVerticalSpacing * 2;
            rect.x = (currentViewWidth / 2) - (rect.width / 2);
            if (GUI.Button(rect, "Execute"))
            {
                var pipeline = target as Pipeline;
                if (pipeline)
                    pipeline.Execute();
            }
        }
    }
}