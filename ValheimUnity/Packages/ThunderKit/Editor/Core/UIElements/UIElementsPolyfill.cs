#if UNITY_2018 || UNITY_2019
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.UIElements;
using System.Linq;
#if UNITY_2019
using UnityEngine.UIElements;
#elif UNITY_2018
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;
#endif

#pragma warning disable 0414
namespace ThunderKit.Core.UIElements
{
    internal static class PolyFillConstants
    {
        public const string unityNamespace = "UnityEngine.UIElements";
    }
#if UNITY_2018
    public class UxmlVisualElementPolyFillFactory : VisualElement.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(VisualElement);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(VisualElement);
    }

    public class UxmlLabelPolyFillFactory : Label.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(Label);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(Label);
    }

    public class UxmlListViewPolyFillFactory : ListView.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(ListView);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(ListView);
    }

    public class UxmlButtonPolyFillFactory : Button.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(Button);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(Button);
    }

    public class UxmlTextFieldPolyFillFactory : TextField.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(TextField);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(TextField);
    }

    public class UxmlScrollViewPolyFillFactory : ScrollView.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(ScrollView);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(ScrollView);
    }

    public class UxmlIMGUIContainerPolyFillFactory : IMGUIContainer.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(IMGUIContainer);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(IMGUIContainer);
    }

    public class UxmlImagePolyFillFactory : Image.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(Image);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(Image);
    }

    public class UxmlFoldoutPolyFillFactory : Foldout.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(Foldout);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(Foldout);
    }

    public class UxmlBoxPolyFillFactory : Box.UxmlFactory
    {
        public override string uxmlQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;
        public override string substituteForTypeName => nameof(Box);
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + nameof(Box);
    }
#endif

    public class UxmlStylePolyFillFactory : UxmlFactory<VisualElement, UxmlStyleTraits>
    {
        private static readonly VisualElement fakeStyleElement;
        static UxmlStylePolyFillFactory()
        {
            fakeStyleElement = new VisualElement();
            fakeStyleElement.RegisterCallback<AttachToPanelEvent>(AutoRemove);
        }

        private static void AutoRemove(AttachToPanelEvent evt)
        {
            var element = evt.target as VisualElement;
            element.RemoveFromHierarchy();
        }

        public override string uxmlName => "Style";
        public override string uxmlQualifiedName => uxmlName;
        public override string substituteForTypeName => typeof(VisualElement).Name;
        public override string substituteForTypeNamespace => PolyFillConstants.unityNamespace;
        public override string substituteForTypeQualifiedName => PolyFillConstants.unityNamespace + "." + uxmlName;

#if UNITY_2018
        public override UnityEngine.Experimental.UIElements.VisualElement Create(IUxmlAttributes bag, CreationContext cc)
#else
        public override UnityEngine.UIElements.VisualElement Create(IUxmlAttributes bag, CreationContext cc)
#endif
        {
            var foundSrc = bag.TryGetAttributeValue("src", out var src);
            if (foundSrc)
            {
                if (src.StartsWith("Assets") || src.StartsWith("Packages") || src.StartsWith("/Assets") || src.StartsWith("/Packages"))
                {
#if UNITY_2018
                    cc.target.AddStyleSheetPath(src.TrimStart('/'));
#else
                    cc.target.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(src.TrimStart('/')));
#endif
                }
                else
                {
                    var vta = cc.visualTreeAsset;
                    var vtaPath = AssetDatabase.GetAssetPath(vta);
                    var vtaDirector = Path.GetDirectoryName(vtaPath);
                    string sheetPath = Path.GetFullPath(Path.Combine(vtaDirector, src)).Replace(Directory.GetCurrentDirectory(), "").TrimStart('\\', '/');
#if UNITY_2018
                    cc.target.AddStyleSheetPath(sheetPath);
#else
                    cc.target.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(sheetPath));
#endif
                }
            }

            return fakeStyleElement;
        }
    }

    public class UxmlStyleTraits : UxmlTraits
    {
#pragma warning disable IDE0052 // These are required by the unity uxml parser (i think)
        readonly UxmlStringAttributeDescription m_Name = new UxmlStringAttributeDescription { name = "name" };
        readonly UxmlStringAttributeDescription m_Path = new UxmlStringAttributeDescription { name = "path" };
        readonly UxmlStringAttributeDescription m_Src = new UxmlStringAttributeDescription { name = "src" };
#pragma warning restore IDE0052 

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription =>
            Enumerable.Empty<UxmlChildElementDescription>();
    }
}
#endif
#pragma warning restore 0414
