using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Data;
using ThunderKit.Core.Editor;
using ThunderKit.Core.Manifests.Datums;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Pipelines;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ThunderKit.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline)), RequiresManifestDatumType(typeof(AssetBundleDefinitions))]
    public class StageAssetBundles : PipelineJob
    {
        [EnumFlag]
        public BuildAssetBundleOptions AssetBundleBuildOptions = BuildAssetBundleOptions.UncompressedAssetBundle;
        public BuildTarget buildTarget = BuildTarget.StandaloneWindows;
        public bool recurseDirectories;
        public bool simulate;

        [PathReferenceResolver]
        public string BundleArtifactPath = "<AssetBundleStaging>";
        public override void Execute(Pipeline pipeline)
        {
            var excludedExtensions = new[] { ".dll", ".cs", ".meta" };

            AssetDatabase.SaveAssets();

            var assetBundleDefs = pipeline.Datums.OfType<AssetBundleDefinitions>().ToArray();
            var bundleArtifactPath = BundleArtifactPath.Resolve(pipeline, this);
            Directory.CreateDirectory(bundleArtifactPath);

            var explicitAssets = assetBundleDefs
                                .SelectMany(abd => abd.assetBundles)
                                .SelectMany(ab => ab.assets)
                                .ToArray();

            var explicitAssetPaths = new List<string>();
            PopulateWithExplicitAssets(explicitAssets, explicitAssetPaths);

            var logBuilder = new StringBuilder();
            var builds = new AssetBundleBuild[assetBundleDefs.Sum(abd => abd.assetBundles.Length)];
            logBuilder.AppendLine($"Defining {builds.Length} AssetBundleBuilds");

            var buildsIndex = 0;
            for (int defIndex = 0; defIndex < assetBundleDefs.Length; defIndex++)
            {
                var assetBundleDef = assetBundleDefs[defIndex];
                var playerAssemblies = CompilationPipeline.GetAssemblies();
                var assemblyFiles = playerAssemblies.Select(pa => pa.outputPath).ToArray();
                var sourceFiles = playerAssemblies.SelectMany(pa => pa.sourceFiles).ToArray();

                for (int i = 0; i < assetBundleDef.assetBundles.Length; i++)
                {
                    var def = assetBundleDef.assetBundles[i];


                    var build = builds[buildsIndex];

                    var assets = new List<string>();

                    logBuilder.AppendLine("--------------------------------------------------");
                    logBuilder.AppendLine($"Defining bundle: {def.assetBundleName}");
                    logBuilder.AppendLine();

                    var firstAsset = def.assets.FirstOrDefault(x => x is SceneAsset);

                    if (firstAsset != null) assets.Add(AssetDatabase.GetAssetPath(firstAsset));
                    else
                    {
                        PopulateWithExplicitAssets(def.assets, assets);

                        var dependencies = assets
                            .SelectMany(assetPath => AssetDatabase.GetDependencies(assetPath))
                            .Where(dap => !explicitAssetPaths.Contains(dap))
                            .ToArray();
                        assets.AddRange(dependencies);
                    }

                    build.assetNames = assets
                        .Select(ap => ap.Replace("\\", "/"))
                        .Where(dap => !ArrayUtility.Contains(excludedExtensions, Path.GetExtension(dap)) &&
                                      !ArrayUtility.Contains(sourceFiles, dap) &&
                                      !ArrayUtility.Contains(assemblyFiles, dap) &&
                                      !AssetDatabase.IsValidFolder(dap))
                        .Distinct()
                        .ToArray();
                    build.assetBundleName = def.assetBundleName;
                    builds[buildsIndex] = build;
                    buildsIndex++;

                    foreach (var asset in build.assetNames)
                        logBuilder.AppendLine(asset);

                    logBuilder.AppendLine("--------------------------------------------------");
                    logBuilder.AppendLine();
                }
            }
            Debug.Log(logBuilder.ToString());

            if (!simulate)
            {
                BuildPipeline.BuildAssetBundles(bundleArtifactPath, builds, AssetBundleBuildOptions, buildTarget);
                for (pipeline.ManifestIndex = 0; pipeline.ManifestIndex < pipeline.Manifests.Length; pipeline.ManifestIndex++)
                {
                    var manifest = pipeline.Manifest;
                    foreach (var assetBundleDef in manifest.Data.OfType<AssetBundleDefinitions>())
                    {
                        var bundleNames = assetBundleDef.assetBundles.Select(ab => ab.assetBundleName).ToArray();
                        foreach (var outputPath in assetBundleDef.StagingPaths.Select(path => path.Resolve(pipeline, this)))
                        {
                            foreach (string dirPath in Directory.GetDirectories(bundleArtifactPath, "*", SearchOption.AllDirectories))
                                Directory.CreateDirectory(dirPath.Replace(bundleArtifactPath, outputPath));

                            foreach (string filePath in Directory.GetFiles(bundleArtifactPath, "*", SearchOption.AllDirectories))
                            {
                                bool found = false;
                                foreach (var bundleName in bundleNames)
                                {
                                    if (filePath.IndexOf(bundleName, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (!found) continue;
                                string destFileName = filePath.Replace(bundleArtifactPath, outputPath);
                                Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
                                FileUtil.ReplaceFile(filePath, destFileName);
                            }

                            var manifestSource = Path.Combine(bundleArtifactPath, $"{Path.GetFileName(bundleArtifactPath)}.manifest");
                            var manifestDestination = Path.Combine(outputPath, $"{manifest.Identity.Name}.manifest");
                            FileUtil.ReplaceFile(manifestSource, manifestDestination);
                        }
                    }
                }
                pipeline.ManifestIndex = -1;
            }
        }

        private static void PopulateWithExplicitAssets(IEnumerable<Object> inputAssets, List<string> outputAssets)
        {
            foreach (var asset in inputAssets)
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    var files = Directory.GetFiles(assetPath, "*", SearchOption.AllDirectories);
                    var assets = files.Select(path => AssetDatabase.LoadAssetAtPath<Object>(path));
                    PopulateWithExplicitAssets(assets, outputAssets);
                }
                else if (asset is UnityPackage up)
                {
                    PopulateWithExplicitAssets(up.AssetFiles, outputAssets);
                }
                else
                {
                    outputAssets.Add(assetPath);
                }
            }
        }
    }
}
