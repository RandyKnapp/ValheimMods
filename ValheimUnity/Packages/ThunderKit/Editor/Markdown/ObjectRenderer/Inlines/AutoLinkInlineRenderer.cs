using System;
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
    public class AutolinkInlineRenderer : UIElementObjectRenderer<AutolinkInline>
    {
        protected override void Write(UIElementRenderer renderer, AutolinkInline link)
        {
            var url = link.Url;
            var lowerScheme = string.Empty;
            if (link.IsEmail)
            {
                url = "mailto:" + url;
            }

            var isValidUri = Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);
            if (!isValidUri)
            {
                var match = LinkInlineRenderer.SchemeCheck.Match(url);
                if (match.Success) lowerScheme = match.Groups[1].Value.ToLower();
                else lowerScheme = "#";
            }
            else
            {
                var uri = new Uri(url);
                lowerScheme = uri.Scheme.ToLower();
            }

            var linkLabel = GetClassedElement<VisualElement>("link", lowerScheme);
            if (isValidUri)
            {
                linkLabel.RegisterCallback<MouseUpEvent>(evt =>
                {
                    if (LinkInlineRenderer.SchemeLinkHandlers.TryGetValue(lowerScheme, out var handler))
                        handler?.Invoke(url);
                });
            }
            linkLabel.tooltip = url;

            renderer.Push(linkLabel);
            renderer.WriteText(url);
            renderer.Pop();
        }
    }
}
