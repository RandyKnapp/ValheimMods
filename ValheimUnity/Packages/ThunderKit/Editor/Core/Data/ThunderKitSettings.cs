using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditor.Compilation;
using System.IO;
using System;
using System.Linq;
using System.Reflection;
using ThunderKit.Common;
using ThunderKit.Core.Config;
using ThunderKit.Markdown;
using ThunderKit.Core.Windows;
#if UNITY_2019 || UNITY_2020
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#else
using UnityEditor.Experimental.UIElements;
using UnityEngine.Experimental.UIElements;
#endif

namespace ThunderKit.Core.Data
{
    using static ThunderKit.Common.PathExtensions;
    // Create a new type of Settings Asset.
    public class ThunderKitSettings : ThunderKitSetting
    {
        [InitializeOnLoadMethod]
        static void SetupPostCompilationAssemblyCopy()
        {
            CompilationPipeline.assemblyCompilationFinished -= LoadAllAssemblies;
            CompilationPipeline.assemblyCompilationFinished += LoadAllAssemblies;
            LoadAllAssemblies(null, null);
            GetOrCreateSettings<ThunderKitSettings>();
        }


        private static readonly string[] CopyFilePatterns = new[] { "*.dll", "*.mdb", "*.pdb" };
        static void LoadAllAssemblies(string somevalue, CompilerMessage[] message)
        {
            var targetFiles = from pattern in CopyFilePatterns
                              from file in Directory.GetFiles("Packages", pattern, SearchOption.AllDirectories)
                              select file;
            foreach (var file in targetFiles)
            {
                var fileName = Path.GetFileName(file);
                var outputPath = Combine("Library", "ScriptAssemblies", fileName);

                FileUtil.ReplaceFile(file, outputPath);
            }
        }


        private SerializedObject thunderKitSettingsSO;

        public string GameExecutable;

        public string GamePath;

        public bool Is64Bit;

        public override void Initialize() => GamePath = "";

        public override void CreateSettingsUI(VisualElement rootElement)
        {
            MarkdownElement markdown = null;
            if (string.IsNullOrEmpty(GameExecutable) || string.IsNullOrEmpty(GamePath))
            {
                markdown = new MarkdownElement
                {
                    Data =
$@"
**_Warning:_**   No game configured. Click the Locate Game button to setup your ThunderKit Project before continuing
",
                    MarkdownDataType = MarkdownDataType.Text
                };

#if UNITY_2018
                markdown.AddStyleSheetPath("Packages/com.passivepicasso.thunderkit/Documentation/uss/markdown.uss");
#else
                markdown.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.passivepicasso.thunderkit/Documentation/uss/markdown.uss"));
#endif
                markdown.AddToClassList("m4");
                markdown.RefreshContent();
                rootElement.Add(markdown);
            }

            rootElement.Add(CreateStandardField(nameof(GameExecutable)));

            rootElement.Add(CreateStandardField(nameof(GamePath)));

            var configureButton = new Button(() =>
            {
                ConfigureGame.Configure();
                if (!string.IsNullOrEmpty(GameExecutable) && !string.IsNullOrEmpty(GamePath))
                {
                    if (markdown != null)
                        markdown.RemoveFromHierarchy();
                }

            });
            configureButton.AddToClassList("configure-game-button");
            configureButton.text = "定位游戏";
            rootElement.Add(configureButton);

            if (thunderKitSettingsSO == null)
                thunderKitSettingsSO = new SerializedObject(this);
            rootElement.Bind(thunderKitSettingsSO);
        }
    }
}