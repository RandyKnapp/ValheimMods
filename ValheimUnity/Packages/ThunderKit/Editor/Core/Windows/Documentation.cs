using ThunderKit.Common;
using ThunderKit.Core.Editor.Windows;
using ThunderKit.Core.UIElements;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace ThunderKit.Core.Windows
{
    using static TemplateHelpers;

    public class Documentation : TemplatedWindow
    {
        private const string PageClass = "pagelistview__page";
        private const string PageHeaderClass = "pagelistview__page--header";
        private const string ElementClass = "pagelistview__item";
        private const string SelectedClass = "selected";
        private const string HiddenClass = "hidden";
        private const string MinimizeClass = "minimize";
        private const string DocumentationRoot = "Packages/com.passivepicasso.thunderkit/Documentation";

        [MenuItem(Constants.ThunderKitMenuRoot + "Documentation")]
        public static void ShowDocumentation() => GetWindow<Documentation>();

        public override void OnEnable()
        {
            base.OnEnable();

            var pageList = rootVisualElement.Q("page-list");
            var topicsFileGuids = AssetDatabase.FindAssets($"t:{nameof(VisualTreeAsset)}", new string[] { $"{DocumentationRoot}/topics" });
            var topicsFilePaths = topicsFileGuids.Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var uxmlTopics = topicsFilePaths.Distinct().ToArray();
            var pageFiles = uxmlTopics
                .OrderBy(dir => Path.GetDirectoryName(dir))
                .ThenBy(path => Path.GetFileNameWithoutExtension(path))
                .ToArray();
            pageList.RegisterCallback<KeyDownEvent>(OnNavigate);
            pageList.Clear();

            var pages = new Dictionary<string, PageEntry>();
            PageEntry defaultPage = null;
            foreach (var pagePath in pageFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(pagePath);
                var containingDirectory = Path.GetDirectoryName(pagePath);
                var pageNamePath = Path.Combine(containingDirectory, fileName);

                var fullParentPageName = GetPageName(containingDirectory);
                var fullPageName = GetPageName(pageNamePath);
                var parentPage = pages.TryGetValue(fullParentPageName, out var tempPage) ? tempPage : null;

                int pageDepth = 0;
                if (parentPage != null) pageDepth = parentPage.Depth + 1;

                var pageEntry = new PageEntry(fileName, fullPageName, pagePath, pageDepth);
                pageEntry.FoldOut.RegisterCallback<ChangeEvent<bool>>(OnToggle);
                pageEntry.AddManipulator(new Clickable(OnSelect));
                if (fullPageName.Equals ("topics-1st_read_me!"))
                {
                    defaultPage = pageEntry;
                }
                if (parentPage != null)
                {
                    var parentIndex = pageList.IndexOf(parentPage);
                    pageEntry.AddToClassList(HiddenClass);
                    pageEntry.AddToClassList(MinimizeClass);
                    pageList.Insert(parentIndex + parentPage.ChildPageCount + 1, pageEntry);
                    parentPage.FoldOut.RemoveFromClassList(HiddenClass);
                    parentPage.ChildPageCount++;
                }
                else
                    pageList.Add(pageEntry);
#if UNITY_2019_1_OR_NEWER
                rootVisualElement.RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
#endif
                pages.Add(fullPageName, pageEntry);
            }
            if(defaultPage != null)
                LoadSelection(defaultPage);
        }
#if UNITY_2019_1_OR_NEWER
        private void OnStyleResolved(CustomStyleResolvedEvent evt)
        {
            var root = evt.currentTarget as VisualElement;
            foreach (var pageEntry in root.Query<PageEntry>().Build().ToList())
            {
                float left = pageEntry.Depth * 12;
                pageEntry.style.paddingLeft = new StyleLength(new Length(left, LengthUnit.Pixel));
            }

        }
#endif

        public class PageEntry : VisualElement
        {
            public Foldout FoldOut;
            public Label Label;
            public string PagePath;
            public int ChildPageCount;
            public int Depth { get; private set; }
            public PageEntry(string templateName, string name, string pagePath, int depth)
            {
                this.name = name;
                this.Depth = depth;
                PagePath = pagePath;

                FoldOut = new Foldout();
                FoldOut.AddToClassList("index-toggle");
                FoldOut.AddToClassList(HiddenClass);
                FoldOut.value = false;

                Label = new Label
                {
                    text = ObjectNames.NicifyVariableName(templateName)
                };
                Label.AddToClassList(PageClass);

                Add(FoldOut);
                Add(Label);

                AddToClassList(PageHeaderClass);
                AddToClassList(ElementClass);
            }

#if UNITY_2018
            protected override void OnStyleResolved(ICustomStyle style)
            {
                base.OnStyleResolved(style);
                float left = Depth * 12;
                var paddingLeft = new StyleValue<float>(left);
                this.style.paddingLeft = paddingLeft;
            }
#endif
        }

        string GetPageName(string path)
        {
            var cleaned = path.Replace("\\", "-").Replace("/", "-").Replace(" ", "_").ToLower();
            cleaned = cleaned.Substring(cleaned.LastIndexOf("documentation-") + "documentation-".Length);
            return cleaned;

        }

        private void OnNavigate(KeyDownEvent evt)
        {
            int modifier = 0;
            switch (evt.keyCode)
            {
                case UnityEngine.KeyCode.UpArrow:
                    modifier = -1;
                    break;
                case UnityEngine.KeyCode.DownArrow:
                    modifier = 1;
                    break;
            }
            var pageList = rootVisualElement.Q("page-list");
            var selectedPage = pageList.Query<PageEntry>(className: SelectedClass).Build().First();
            var selectedIndex = pageList.IndexOf(selectedPage);
            int newSelectedIndex = selectedIndex + modifier;
            if (newSelectedIndex > -1 && newSelectedIndex < pageList.childCount)
            {
                var newSelectedPage = pageList[newSelectedIndex];
                while (newSelectedPage.ClassListContains(HiddenClass) && newSelectedIndex > -1 && newSelectedIndex < pageList.childCount)
                {
                    newSelectedIndex += modifier;
                    newSelectedPage = pageList[newSelectedIndex];
                }
                selectedPage.RemoveFromClassList(SelectedClass);
                newSelectedPage.AddToClassList(SelectedClass);
                LoadSelection(newSelectedPage as PageEntry);
            }
        }

        private void OnToggle(ChangeEvent<bool> evt)
        {
            var toggle = evt.currentTarget as VisualElement;
            var pageEntry = toggle.parent as PageEntry;
            var fullPageName = toggle.parent.name;
            var childPages = rootVisualElement.Query<PageEntry>()
                .Where(h =>
                {
                    string parentFullName = h.name.Substring(0, h.name.LastIndexOf("-"));
                    bool isChildOfEventPage = parentFullName.Equals(fullPageName);
                    return isChildOfEventPage;
                })
                .ToList();

            if (evt.newValue)
                foreach (var child in childPages)
                {
                    child.RemoveFromClassList(HiddenClass);
                    child.RemoveFromClassList(MinimizeClass);
                }
            else
                foreach (var child in childPages)
                {
                    child.AddToClassList(HiddenClass);
                    child.AddToClassList(MinimizeClass);
                    if (child.ClassListContains(SelectedClass))
                    {
                        LoadSelection(pageEntry);
                    }
                }
        }

        void OnSelect(EventBase e)
        {
            var element = e.currentTarget as PageEntry;
            LoadSelection(element);
        }

        private void LoadSelection(PageEntry element)
        {
            var pageList = rootVisualElement.Q("page-list");
            var pageView = rootVisualElement.Q("page-view");
            var selectedElement = pageList.Q(className: SelectedClass);
            selectedElement?.RemoveFromClassList(SelectedClass);
            element.AddToClassList(SelectedClass);

            pageView.Clear();
            LoadTemplateInstance(element.PagePath, pageView);
        }
    }
}