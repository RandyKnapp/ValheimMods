using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ThunderKit.Common;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
using UnityEditor.UIElements;
#else
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;
#endif

namespace ThunderKit.Core.UIElements
{
    public static class TemplateHelpers
    {
        private static bool IsTemplatePath(string path) => path.Contains("Packages/com.passivepicasso.thunderkit");

        private readonly static string[] SearchFolders = new string[] { "Assets", "Packages" };

        private static readonly Dictionary<string, VisualTreeAsset> templateCache = new Dictionary<string, VisualTreeAsset>(StringComparer.Ordinal);

        public static string NicifyPackageName(string name) => ObjectNames.NicifyVariableName(name).Replace("_", " ");

        public static VisualElement GetTemplateInstance(string template, VisualElement target = null, Func<string, bool> isTemplatePath = null)
        {
            var packageTemplate = LoadTemplate(template, isTemplatePath ?? IsTemplatePath);
            var templatePath = AssetDatabase.GetAssetPath(packageTemplate);
            VisualElement instance = target;

#if UNITY_2020_1_OR_NEWER
            if (instance == null) instance = packageTemplate.Instantiate();
            else
                packageTemplate.CloneTree(instance);
#elif UNITY_2019_1_OR_NEWER
            if (instance == null) instance = packageTemplate.CloneTree();
            else
                packageTemplate.CloneTree(instance);
#elif UNITY_2018_1_OR_NEWER
            if (instance == null) instance = packageTemplate.CloneTree(null);
            else
                packageTemplate.CloneTree(instance, null);
#endif

            instance.AddToClassList("grow");

            AddSheet(instance, templatePath);
            AddSheet(instance, templatePath, "_style");

            if (EditorGUIUtility.isProSkin)
                AddSheet(instance, templatePath, "_Dark");
            else
                AddSheet(instance, templatePath, "_Light");
            return instance;
        }

        const string editorVersion =
#if UNITY_2020_1_OR_NEWER
            "2020";
#elif UNITY_2019_1_OR_NEWER
            "2019";
#elif UNITY_2018_1_OR_NEWER
            "2018";
#endif
        public static void AddSheet(VisualElement element, string templatePath, string modifier = "")
        {
            string path = templatePath.Replace(".uxml", $"{modifier}_{editorVersion}.uss");
            if (!File.Exists(path))
            {
                path = templatePath.Replace(".uxml", $"{modifier}.uss");
                if (!File.Exists(path))
                    return;
            }
            MultiVersionLoadStyleSheet(element, path);

        }

        private static void MultiVersionLoadStyleSheet(VisualElement element, string sheetPath)
        {
#if UNITY_2019_1_OR_NEWER
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(sheetPath);
            element.styleSheets.Add(styleSheet);
#elif UNITY_2018_1_OR_NEWER
            element.AddStyleSheetPath(sheetPath);
#endif
        }

        private static VisualTreeAsset LoadTemplate(string name, Func<string, bool> isTemplatePath)
        {
            if (templateCache.TryGetValue(name, out var asset) && asset != null)
                return asset;

            return templateCache[name] = CreateTemplate(name, isTemplatePath);
        }

        static VisualTreeAsset CreateTemplate(string name, Func<string, bool> isTemplatePath)
        {
            var searchResults = AssetDatabase.FindAssets(name, SearchFolders);
            var assetPaths = searchResults.Select(AssetDatabase.GUIDToAssetPath).Select(path => path.Replace("\\", "/"));
            var templatePath = assetPaths
                .Where(path => Path.GetFileNameWithoutExtension(path).Equals(name))
                .Where(path => Path.GetExtension(path).Equals(".uxml", StringComparison.CurrentCultureIgnoreCase))
                .Where(isTemplatePath)
                .FirstOrDefault();
            return AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);
        }


        public static string GetAssetDirectory(UnityEngine.Object asset)
        {
            return Path.GetDirectoryName(AssetDatabase.GetAssetPath(asset));
        }

        public static VisualElement LoadTemplateInstance(string templatePath, VisualElement instance = null)
        {
            var visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);
#if UNITY_2020_1_OR_NEWER
            if (instance == null) instance = visualTreeAsset.Instantiate();
            else
                visualTreeAsset.CloneTree(instance);
#elif UNITY_2019_1_OR_NEWER
            if (instance == null) instance = visualTreeAsset.CloneTree();
            else
                visualTreeAsset.CloneTree(instance);
#elif UNITY_2018_1_OR_NEWER
            if (instance == null) instance = visualTreeAsset.CloneTree(null);
            else
                visualTreeAsset.CloneTree(instance, null);
#endif
            return instance;
        }
    }
}