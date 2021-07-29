using Markdig.Syntax.Inlines;
using System.Text;
using UnityEngine;
using UnityEditor;
using System;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace ThunderKit.Markdown.ObjectRenderers
{
    using static Helpers.VisualElementFactory;
    using static Helpers.UnityPathUtility;
    public class CodeInlineRenderer : UIElementObjectRenderer<CodeInline>
    {
        protected override void Write(UIElementRenderer renderer, CodeInline obj)
        {
            var codeInline = GetClassedElement<Label>( "code");
            codeInline.text = obj.Content;
            codeInline.RegisterCallback<AttachToPanelEvent>(OnAttach);

            renderer.WriteInline(codeInline);
        }
        private void OnAttach(AttachToPanelEvent evt)
        {
            var codeInline = evt.currentTarget as Label;
            codeInline.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            codeInline.RegisterCallback<MouseUpEvent>(CopyToClipboard);
        }

        private static void CopyToClipboard(MouseUpEvent evt)
        {
            var target = evt.currentTarget as VisualElement;
            var builder = new StringBuilder();
            var words = target.Query<Label>().Build().ToList();
            foreach (var word in words)
                builder.Append(word.text);

            EditorGUIUtility.systemCopyBuffer = builder.ToString();
            
        }
    }
}
