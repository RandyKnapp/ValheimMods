using Markdig.Syntax;
using System;
using System.Text;
using UnityEditor;
using UnityEngine;
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
    public class CodeBlockRenderer : UIElementObjectRenderer<CodeBlock>
    {
        protected override void Write(UIElementRenderer renderer, CodeBlock obj)
        {
            var codeBlock = GetClassedElement<VisualElement>("code");
            codeBlock.RegisterCallback<MouseUpEvent>(CopyToClipboard);
            renderer.Push(codeBlock);
            renderer.WriteLeafRawLines(obj);
            renderer.Pop();
        }

        private void CopyToClipboard(MouseUpEvent evt)
        {
            var target = evt.currentTarget as VisualElement;
            var builder = new StringBuilder();
            var words = target.Query<Label>().Build().ToList();
            foreach (var word in words)
                builder.Append(word);

            EditorGUIUtility.systemCopyBuffer = builder.ToString();
        }
    }
}
