using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ThunderKit.Common;
using ThunderKit.Core.Editor;
using ThunderKit.Core.Pipelines;
using UnityEditor;
using UnityEngine;
using static System.IO.Path;

namespace ThunderKit.Core.Paths
{
    public class PathReference : ComposableObject
    {
        [MenuItem(Constants.ThunderKitContextRoot + nameof(PathReference), false, priority = Constants.ThunderKitMenuPriority)]
        public static void Create() => ScriptableHelper.SelectNewAsset<PathReference>();

        const char opo = '<';
        const char opc = '>';
        private static readonly Regex referenceIdentifier = new Regex($"\\{opo}(.*?)\\{opc}", RegexOptions.Compiled);
        public static string ResolvePath(string input, Pipeline pipeline, UnityEngine.Object caller)
        {
            var result = input;
            var pathReferenceGuids = AssetDatabase.FindAssets($"t:{nameof(PathReference)}", Constants.AssetDatabaseFindFolders);
            var pathReferencePaths = pathReferenceGuids.Select(x => AssetDatabase.GUIDToAssetPath(x)).ToArray();
            var pathReferences = pathReferencePaths.Select(x => AssetDatabase.LoadAssetAtPath<PathReference>(x)).ToArray();
            var pathReferenceDictionary = pathReferences.ToDictionary(pr => pr.name);

            Match match = referenceIdentifier.Match(result);
            while (match != null && !string.IsNullOrEmpty(match.Value))
            {
                var matchValue = match.Value.Trim(opo, opc);
                if (!pathReferenceDictionary.ContainsKey(matchValue))
                {
                    EditorGUIUtility.PingObject(caller);
                    throw new KeyNotFoundException($"No PathReference named \"{matchValue}\" found in AssetDatabase");
                }
                var replacement = pathReferenceDictionary[matchValue].GetPath(pipeline);
                result = result.Replace(match.Value, replacement);
                match = match.NextMatch();
            }

            return result.Replace("\\", "/");
        }

        public override Type ElementType => typeof(PathComponent);

        public override bool SupportsType(Type type) => ElementType.IsAssignableFrom(type);

        public string GetPath(Pipeline pipeline)
        {
            try
            {
                return Data.OfType<PathComponent>().Select(d => d.GetPath(this, pipeline)).Aggregate(Combine);
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to resolve PathReference {this.name}", this);
                throw e;
            }
        }

        public override string ElementTemplate =>
$@"using ThunderKit.Core.Pipelines;
using ThunderKit.Core.Paths;

namespace {{0}}
{{{{
    public class {{1}} : PathComponent
    {{{{
        public override string GetPath({nameof(PathReference)} output, Pipeline pipeline)
        {{{{
            return base.GetPath(output, pipeline);
        }}}}
    }}}}
}}}}
";
    }
}