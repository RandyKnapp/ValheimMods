using System.Collections;
using ThunderKit.Core.Data;
using UnityEditor;
using UnityEngine;
using ThunderKit.Markdown;
using ThunderKit.Core.Windows;
#if UNITY_2019 || UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif


namespace ThunderKit.Editor.Core.Data
{
    public class SettingsWindowData : ThunderKitSetting
    {
        [InitializeOnLoadMethod]
        static void SettingsWindowSetup()
        {
            EditorApplication.wantsToQuit -= EditorApplication_wantsToQuit;
            EditorApplication.wantsToQuit += EditorApplication_wantsToQuit;

            var settings = GetOrCreateSettings<SettingsWindowData>();
            if (settings.FirstLoad && settings.ShowOnStartup)
                EditorApplication.update += ShowSettings;
        }

        private static void ShowSettings()
        {
            EditorApplication.update -= ShowSettings;
            Settings.ShowSettings();
            var settings = GetOrCreateSettings<SettingsWindowData>();
            settings.FirstLoad = false;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
        }

        private static bool EditorApplication_wantsToQuit()
        {
            var settings = GetOrCreateSettings<SettingsWindowData>();
            settings.FirstLoad = true;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return true;
        }

        [SerializeField]
        private bool FirstLoad = true;

        public bool ShowOnStartup = true;
        private SerializedObject settingsWindowDataSo;

        public override void CreateSettingsUI(VisualElement rootElement)
        {
            MarkdownElement markdown = new MarkdownElement
            {
                Data = $@"欢迎并感谢您尝试ThunderKit。请先单击下面的“定位游戏”按钮来配置您的ThunderKit项目！

如果这是你第一次使用ThunderKit, [单击此处](menulink://Tools/ThunderKit/Documentation) 启动文档",
                MarkdownDataType = MarkdownDataType.Text
            };
#if UNITY_2018
            markdown.AddStyleSheetPath("Packages/com.passivepicasso.thunderkit/Documentation/uss/markdown.uss");
            markdown.AddStyleSheetPath("Packages/com.passivepicasso.thunderkit/Documentation/uss/thunderkit_documentation.uss");
#else
            markdown.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.passivepicasso.thunderkit/Documentation/uss/markdown.uss"));
            markdown.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.passivepicasso.thunderkit/Documentation/uss/thunderkit_documentation.uss"));
#endif
            markdown.RefreshContent();
            markdown.AddToClassList("m4");

            var child = CreateStandardField(nameof(ShowOnStartup));
            child.tooltip = "Uncheck this to stop showing this window on startup";
            rootElement.Add(child);
            rootElement.Add(markdown);

            if (settingsWindowDataSo == null)
                settingsWindowDataSo = new SerializedObject(this);

            rootElement.Bind(settingsWindowDataSo);
        }
    }
}