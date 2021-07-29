using Markdig.Syntax;
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
    public class QuoteBlockRenderer : UIElementObjectRenderer<QuoteBlock>
    {
        protected override void Write(UIElementRenderer renderer, QuoteBlock obj)
        {
            renderer.Push(GetClassedElement<VisualElement>("quote"));
            renderer.WriteChildren(obj);
            renderer.Pop();
        }
    }
}
