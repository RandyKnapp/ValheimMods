using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ThunderKit.Common;
using ThunderKit.Core.Data;
using ThunderKit.Core.Editor;
using ThunderKit.Core.Editor.Windows;
using UnityEditor;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace ThunderKit.Core.Windows
{
    using static ThunderKit.Core.UIElements.TemplateHelpers;
    public class Settings : TemplatedWindow
    {
        readonly static string[] searchFolders = new[] { "Assets", "Packages" };

        [MenuItem(Constants.ThunderKitMenuRoot + "Settings")]
        public static void ShowSettings() => GetWindow<Settings>();

        public override void OnEnable()
        {
            base.OnEnable();

            var settingsArea = rootVisualElement.Q("settings-area");
            var settings = AssetDatabase.FindAssets($"t:{nameof(ThunderKitSetting)}", searchFolders)
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ThunderKitSetting>)
                .ToArray();

            
            foreach(var setting in settings)
            {
                var settingSection = GetTemplateInstance("ThunderKitSettingSection");
                var title = settingSection.Q<Label>("title");
                if (title != null)
                    title.text = setting.name;
                var properties = settingSection.Q<VisualElement>("properties");
                setting.CreateSettingsUI(properties);
                settingsArea.Add(settingSection);
            }
        }
    }
}