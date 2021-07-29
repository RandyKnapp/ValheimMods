using System;
using System.Collections.Generic;
using Markdig.Helpers;
using Markdig.Syntax;
using Markdig.Renderers;
using Markdig.Syntax.Inlines;
using ThunderKit.Markdown.ObjectRenderers;
using System.Text.RegularExpressions;
#if !NET40
using System.Runtime.CompilerServices;
#endif
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif
namespace ThunderKit.Markdown
{
    using static Helpers.VisualElementFactory;
    public class UIElementRenderer : RendererBase
    {
        private static readonly Regex LiteralSplitter = new Regex(@"([\S]+\b)\S?", RegexOptions.Singleline | RegexOptions.Compiled);
        private readonly Stack<VisualElement> stack = new Stack<VisualElement>(128);

        public UIElementRenderer()
        {
        }

        public UIElementRenderer(VisualElement document)
        {
            LoadDocument(document);
        }
        public virtual void LoadDocument(VisualElement document)
        {
            Document = document;
            stack.Push(document);
            LoadRenderers();
        }
        public VisualElement Document { get; protected set; }
        /// <inheritdoc/>
        public override object Render(MarkdownObject markdownObject)
        {
            Write(markdownObject);
            return Document;
        }

        public void WriteLeafInline(LeafBlock leafBlock)
        {
            if (leafBlock == null) throw new ArgumentNullException(nameof(leafBlock));
            var inline = (Inline)leafBlock.Inline;
            while (inline != null)
            {
                Write(inline);
                inline = inline.NextSibling;
            }
        }
        public void WriteLeafRawLines(LeafBlock leafBlock)
        {
            if (leafBlock == null) throw new ArgumentNullException(nameof(leafBlock));
            if (leafBlock.Lines.Lines != null)
            {
                var lines = leafBlock.Lines;
                var slices = lines.Lines;
                for (var i = 0; i < lines.Count; i++)
                {
                    if (i != 0)
                        WriteInline(GetClassedElement<Label>("linebreak"));
                    WriteText(ref slices[i].Slice);
                }
            }
        }
        public void Push(VisualElement o)
        {
            stack.Push(o);
        }
        public void Pop()
        {
            var popped = stack.Pop();
            stack.Peek().Add(popped);
        }
        public void WriteBlock(VisualElement block)
        {
            stack.Peek().Add(block);
        }
        public void WriteInline(VisualElement inline)
        {
            AddInline(stack.Peek(), inline);
        }
        public void WriteText(ref StringSlice slice)
        {
            if (slice.IsEmpty)
                return;

            var match = LiteralSplitter.Match(slice.Text, slice.Start, slice.Length);
            while (match.Success)
            {
                string value = match.Value;
                if (!string.IsNullOrEmpty(value))
                {
                    var element = GetTextElement<Label>(value, "inline");

                    match = match.NextMatch();
                    if (match.Success == false)
                        element.AddToClassList("last-inline");

                    WriteInline(element);
                }
            }
        }

        public void WriteText(string text)
        {
            if (text == null)
                return;

            var slice = new StringSlice(text);
            WriteText(ref slice);
        }

        public void WriteText(string text, int offset, int length)
        {
            if (text == null)
                return;

            var slice = new StringSlice(text, offset, offset + length);
            WriteText(ref slice);
        }

        protected virtual void LoadRenderers()
        {
            // Default block renderers
            ObjectRenderers.Add(new CodeBlockRenderer());
            ObjectRenderers.Add(new ListRenderer());
            ObjectRenderers.Add(new HeadingRenderer());
            ObjectRenderers.Add(new ParagraphRenderer());
            ObjectRenderers.Add(new QuoteBlockRenderer());
            ObjectRenderers.Add(new ThematicBreakRenderer());
            // Default inline renderers
            ObjectRenderers.Add(new AutolinkInlineRenderer());
            ObjectRenderers.Add(new CodeInlineRenderer());
            ObjectRenderers.Add(new DelimiterInlineRenderer());
            ObjectRenderers.Add(new EmphasisInlineRenderer());
            ObjectRenderers.Add(new HtmlEntityInlineRenderer());
            ObjectRenderers.Add(new LineBreakInlineRenderer());
            ObjectRenderers.Add(new LinkInlineRenderer());
            ObjectRenderers.Add(new LiteralInlineRenderer());
            // Extension renderers
            //ObjectRenderers.Add(new TableRenderer());
            ObjectRenderers.Add(new TaskListRenderer());
        }
        private static void AddInline(VisualElement parent, VisualElement inline)
        {
            parent.Add(inline);
        }
    }
}
