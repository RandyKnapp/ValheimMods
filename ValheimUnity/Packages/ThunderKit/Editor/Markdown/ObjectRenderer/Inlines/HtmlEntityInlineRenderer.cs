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
    public class HtmlEntityInlineRenderer : UIElementObjectRenderer<HtmlEntityInline>
    {
        protected override void Write(UIElementRenderer renderer, HtmlEntityInline obj)
        {
            var transcoded = obj.Transcoded;
            renderer.WriteText(ref transcoded);
        }
    }
}
