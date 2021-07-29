using Markdig.Renderers;
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
    public abstract class UIElementObjectRenderer<TObject> : MarkdownObjectRenderer<UIElementRenderer, TObject>
        where TObject : MarkdownObject
    {
    }
}
