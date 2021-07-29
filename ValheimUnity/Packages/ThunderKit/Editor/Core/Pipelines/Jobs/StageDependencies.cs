using System.IO;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Paths;
using UnityEditor;

namespace ThunderKit.Core.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline))]
    public class StageDependencies : PipelineJob
    {
        [PathReferenceResolver]
        public string StagingPath;
        public Manifests.Manifest[] ExcludedManifests;

        public override void Execute(Pipeline pipeline)
        {
            for (pipeline.ManifestIndex = 0; pipeline.ManifestIndex < pipeline.Manifests.Length; pipeline.ManifestIndex++)
            {
                if (ArrayUtility.Contains(ExcludedManifests, pipeline.Manifest)) continue;
                if (AssetDatabase.GetAssetPath(pipeline.Manifest).StartsWith("Assets")) continue;

                var manifestIdentity = pipeline.Manifest.Identity;
                var dependencyPath = Path.Combine("Packages", manifestIdentity.Name);
                CopyFilesRecursively(dependencyPath, StagingPath.Resolve(pipeline, this));
            }
            pipeline.ManifestIndex = -1;
        }

        public static void CopyFilesRecursively(string source, string destination)
        {
            foreach (string dirPath in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace("Packages", destination));

            foreach (string filePath in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(filePath).Equals(".meta")) continue;

                string destFileName = filePath.Replace("Packages", destination);
                Directory.CreateDirectory(Path.GetDirectoryName(destFileName));
                File.Copy(filePath, destFileName, true);
            }
        }
    }
}
