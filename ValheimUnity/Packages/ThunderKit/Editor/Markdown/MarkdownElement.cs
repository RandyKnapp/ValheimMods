using Markdig;
using Markdig.Extensions.GenericAttributes;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Markdig.Renderers.Normalize;
using System.Linq;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace ThunderKit.Markdown
{
    using static Helpers.UnityPathUtility;

    public enum MarkdownDataType { Implicit, Source, Text }
    public class MarkdownElement : VisualElement
    {
        private static readonly UIElementRenderer renderer;
        private string data;

        static MarkdownElement()
        {
            renderer = new UIElementRenderer();
            var mpb = new MarkdownPipelineBuilder();
            mpb.Extensions.AddIfNotAlready<GenericAttributesExtension>();
            var pipeline = mpb.Build();
            pipeline.Setup(renderer);
        }
        public string Data { get => data; set => data = value; }
        private string Markdown { get; set; }
        private string NormalizedMarkdown { get; set; }
        private string Source { get; set; }
        public MarkdownDataType MarkdownDataType { get; set; }
        public bool SpaceAfterQuoteBlock { get; set; }
        public bool EmptyLineAfterCodeBlock { get; set; }
        public bool EmptyLineAfterHeading { get; set; }
        public bool EmptyLineAfterThematicBreak { get; set; }
        public string ListItemCharacter { get; set; } = "*";
        public bool ExpandAutoLinks { get; set; }
        public MarkdownElement()
        {
            EditorApplication.projectChanged -= RefreshContent;
            EditorApplication.projectChanged += RefreshContent;
        }

        string GetMarkdown()
        {
            string markdown = Data;
            switch (MarkdownDataType)
            {
                case MarkdownDataType.Implicit:
                case MarkdownDataType.Source:
                    if (!".md".Equals(Path.GetExtension(Source))) break;

                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(Source);
                    if (asset)
                        markdown = asset.text;
                    else
                        markdown = string.Empty;

                    break;
                case MarkdownDataType.Text:
                    markdown = Data;
                    break;
            }

            return string.IsNullOrEmpty(markdown) ? $"No data found: {MarkdownDataType} : {Data}" : markdown;
        }

        public void RefreshContent()
        {
            var markdown = GetMarkdown();
            if (markdown.Equals(Markdown)) return;
            Markdown = markdown;

            Clear();

            var normalizeOptions = new NormalizeOptions
            {
                EmptyLineAfterCodeBlock = EmptyLineAfterCodeBlock,
                EmptyLineAfterHeading = EmptyLineAfterHeading,
                EmptyLineAfterThematicBreak = EmptyLineAfterThematicBreak,
                ExpandAutoLinks = ExpandAutoLinks,
                ListItemCharacter = ListItemCharacter[0],
                SpaceAfterQuoteBlock = SpaceAfterQuoteBlock
            };


            NormalizedMarkdown = Markdig.Markdown.Normalize(Markdown, normalizeOptions);

            var document = Markdig.Markdown.Parse(NormalizedMarkdown);
            renderer.LoadDocument(this);
            renderer.Render(document);
        }

        public new class UxmlFactory : UxmlFactory<MarkdownElement, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            static string NormalizeName(string text) => ObjectNames.NicifyVariableName(text).ToLower();

            private readonly UxmlStringAttributeDescription m_text = new UxmlStringAttributeDescription { name = "data" };
            private readonly UxmlEnumAttributeDescription<MarkdownDataType> m_dataType = new UxmlEnumAttributeDescription<MarkdownDataType> { name = "markdown-data-type" };
            private readonly UxmlBoolAttributeDescription m_EmptyLineAfterCodeBlock = new UxmlBoolAttributeDescription { name = NormalizeName(nameof(NormalizeOptions.EmptyLineAfterCodeBlock)), defaultValue = true };
            private readonly UxmlBoolAttributeDescription m_EmptyLineAfterHeading = new UxmlBoolAttributeDescription { name = NormalizeName(nameof(NormalizeOptions.EmptyLineAfterHeading)), defaultValue = true };
            private readonly UxmlBoolAttributeDescription m_EmptyLineAfterThematicBreak = new UxmlBoolAttributeDescription { name = NormalizeName(nameof(NormalizeOptions.EmptyLineAfterThematicBreak)), defaultValue = true };
            private readonly UxmlBoolAttributeDescription m_ExpandAutoLinks = new UxmlBoolAttributeDescription { name = NormalizeName(nameof(NormalizeOptions.ExpandAutoLinks)), defaultValue = true };
            private readonly UxmlStringAttributeDescription m_ListItemCharacter = new UxmlStringAttributeDescription { name = NormalizeName(nameof(NormalizeOptions.ListItemCharacter)), defaultValue = "*" };
            private readonly UxmlBoolAttributeDescription m_SpaceAfterQuoteBlock = new UxmlBoolAttributeDescription { name = NormalizeName(nameof(NormalizeOptions.SpaceAfterQuoteBlock)), defaultValue = true };
            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var mdElement = (MarkdownElement)ve;
                mdElement.Data = m_text.GetValueFromBag(bag, cc);
                mdElement.MarkdownDataType = m_dataType.GetValueFromBag(bag, cc);
                mdElement.EmptyLineAfterCodeBlock = m_EmptyLineAfterCodeBlock.GetValueFromBag(bag, cc);
                mdElement.EmptyLineAfterHeading = m_EmptyLineAfterHeading.GetValueFromBag(bag, cc);
                mdElement.EmptyLineAfterThematicBreak = m_EmptyLineAfterThematicBreak.GetValueFromBag(bag, cc);
                mdElement.ExpandAutoLinks = m_ExpandAutoLinks.GetValueFromBag(bag, cc);
                mdElement.ListItemCharacter = m_ListItemCharacter.GetValueFromBag(bag, cc);
                mdElement.SpaceAfterQuoteBlock = m_SpaceAfterQuoteBlock.GetValueFromBag(bag, cc);

                bool configured = false;
                if (mdElement.MarkdownDataType != MarkdownDataType.Text)
                {
                    if (IsAssetDirectory(mdElement.Data))
                    {
                        mdElement.Source = mdElement.Data;
                        configured = true;
                    }
                    else if (cc.visualTreeAsset != null)
                    {
                        var treeAssetPath = AssetDatabase.GetAssetPath(cc.visualTreeAsset);
                        if (!string.IsNullOrEmpty(treeAssetPath))
                        {
                            var treeAssetDirectory = Path.GetDirectoryName(treeAssetPath);
                            var source = string.IsNullOrEmpty(mdElement.Data) ? $"{Path.GetFileNameWithoutExtension(treeAssetPath)}.md"
                                                                              : mdElement.Data;
                            var sourcePath = Path.Combine(treeAssetDirectory, source);
                            mdElement.Source = sourcePath;
                            configured = true;
                        }
                    }
                }
                else
                    configured = true;

                if (configured)
                    mdElement.RefreshContent();
            }

            public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription =>
                Enumerable.Empty<UxmlChildElementDescription>();
        }
    }
}