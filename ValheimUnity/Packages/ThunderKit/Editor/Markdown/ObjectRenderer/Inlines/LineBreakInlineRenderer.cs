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
    public class LineBreakInlineRenderer : UIElementObjectRenderer<LineBreakInline>
    {
        
        protected override void Write(UIElementRenderer renderer, LineBreakInline obj)
        {
            if (obj.IsHard)
            {
                renderer.WriteInline(GetClassedElement<Label>("linebreak"));
            }
            else
            {
                // Soft line break.
                renderer.WriteText(" ");
            }
        }
    }
}

