using Markdig.Syntax.Inlines;
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
    public class EmphasisInlineRenderer : UIElementObjectRenderer<EmphasisInline>
    {
        protected override void Write(UIElementRenderer renderer, EmphasisInline obj)
        {
            string delimiterClass = null;

            switch (obj.DelimiterChar)
            {
                case '*':
                case '_':
                    delimiterClass = obj.DelimiterCount == 2 ? "bold" : "italic";
                    break;
                case '~':
                    delimiterClass = obj.DelimiterCount == 2 ? "strikethrough" : "subscript";
                    break;
                case '^':
                    delimiterClass = "superscript";
                    break;
                case '+':
                    delimiterClass = "inserted";
                    break;
                case '=':
                    delimiterClass = "marked";
                    break;
            }

            if (delimiterClass != null)
            {
                renderer.Push(GetClassedElement<VisualElement>(delimiterClass));
                renderer.WriteChildren(obj);
                renderer.Pop();
            }
            else
            {
                renderer.WriteChildren(obj);
            }
        }
    }
}
