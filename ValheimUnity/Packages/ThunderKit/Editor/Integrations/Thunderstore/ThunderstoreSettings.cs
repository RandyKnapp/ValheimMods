using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Compilation;
using System.IO;
using ThunderKit.Core.Editor;
using System;
using System.Linq;
using ThunderKit.Core.Data;
#if UNITY_2019 || UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace ThunderKit.Integrations.Thunderstore
{
    // Create a new type of Settings Asset.
    public class ThunderstoreSettings : ThunderKitSetting
    {
        public string ThunderstoreUrl = "https://thunderstore.io";
        private SerializedObject thunderStoreSettingsSO;

        public class StringValueChangeArgs : EventArgs
        {
            public string newValue;
            public string previousValue;
        }

        public static event EventHandler<StringValueChangeArgs> OnThunderstoreUrlChanged;

        public override void CreateSettingsUI(VisualElement rootElement)
        {
            var settingsobject = GetOrCreateSettings<ThunderstoreSettings>();
            var container = new VisualElement();
            var label = new Label(ObjectNames.NicifyVariableName(nameof(ThunderstoreUrl)));
            var field = new TextField { bindingPath = nameof(ThunderstoreUrl) };
            field.RegisterCallback<ChangeEvent<string>>(ce =>
            {
                if (ce.newValue != ce.previousValue)
                    OnThunderstoreUrlChanged?.Invoke(field, new StringValueChangeArgs { newValue = ce.newValue, previousValue = ce.previousValue });
            });
            container.Add(label);
            container.Add(field);
            container.AddToClassList("thunderkit-field");
            field.AddToClassList("thunderkit-field-input");
            label.AddToClassList("thunderkit-field-label");
            rootElement.Add(container);

            if (thunderStoreSettingsSO == null) 
                thunderStoreSettingsSO = new SerializedObject(settingsobject);
            container.Bind(thunderStoreSettingsSO);
        }

        readonly string[] keywords = new string[] { nameof(ThunderstoreUrl) };
        public override IEnumerable<string> Keywords() => keywords;
    }
}