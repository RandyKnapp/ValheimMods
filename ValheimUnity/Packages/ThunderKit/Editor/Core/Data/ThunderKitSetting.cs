using System;
using System.Collections.Generic;
using System.Linq;
using ThunderKit.Core.Editor;
using UnityEditor;
using UnityEngine;
using System.IO;
#if UNITY_2019 || UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace ThunderKit.Core.Data
{
    public class ThunderKitSetting : ScriptableObject 
    {
        public static T GetOrCreateSettings<T>() where T : ThunderKitSetting
        {
            string assetPath = $"Assets/ThunderKitSettings/{typeof(T).Name}.asset";
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            return ScriptableHelper.EnsureAsset<T>(assetPath, settings => settings.Initialize());
        }

        public virtual void Initialize() { }
        public virtual IEnumerable<string> Keywords() => Enumerable.Empty<string>();
        public virtual void CreateSettingsUI(VisualElement rootElement) { }

        protected static Type[] createSettingsUiParameterTypes = new[] { typeof(VisualElement) };

        protected static VisualElement CreateStandardField(string gamePath)
        {
            var container = new VisualElement();
            var label = ObjectNames.NicifyVariableName(gamePath);
            var field = new PropertyField { bindingPath = gamePath, label = label };
            container.Add(field);
            container.AddToClassList("thunderkit-field");
            field.AddToClassList("thunderkit-field-input");
            return container;
        }

    }
}