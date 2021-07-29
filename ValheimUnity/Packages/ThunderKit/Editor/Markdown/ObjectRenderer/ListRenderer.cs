using System.Globalization;
using Markdig.Syntax;
using System.Linq;
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
    public class ListRenderer : UIElementObjectRenderer<ListBlock>
    {
        protected override void Write(UIElementRenderer renderer, ListBlock listBlock)
        {
            renderer.Push(GetClassedElement<VisualElement>("list"));

            foreach (var item in listBlock.OfType<ListItemBlock>())
            {
                var listItem = GetClassedElement<VisualElement>("list-item");

                renderer.Push(listItem);

                var marker = listBlock.IsOrdered ? $"{item.Order}." : $"{listBlock.BulletType}";

                var classes = listBlock.IsOrdered ? "inline" : "bullet";

                renderer.WriteChildren(item);

                listItem.Children()?.FirstOrDefault()?.Insert(0, GetTextElement<Label>(marker, classes));

                renderer.Pop();
            }

            renderer.Pop();
        }
    }
}
