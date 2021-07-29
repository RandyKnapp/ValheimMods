using System.IO;
using System.Linq;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Manifests.Datum;
using ThunderKit.Core.Paths;
using UnityEditor;
using UnityEngine;

namespace ThunderKit.Core.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline)), ManifestProcessor, RequiresManifestDatumType(typeof(Files))]
    public class StageManifestFiles : PipelineJob
    {
        public override void Execute(Pipeline pipeline)
        {
            var filesDatums = pipeline.Manifest.Data.OfType<Files>().ToArray();

            foreach (var files in filesDatums)
            {
                var resolvedPaths = files.StagingPaths.Select(path => path.Resolve(pipeline, this)).ToArray();

                foreach (var outputPath in resolvedPaths)
                {
                    foreach (var file in files.files)
                    {
                        var sourcePath = AssetDatabase.GetAssetPath(file);
                        string destPath = Path.Combine(outputPath, Path.GetFileName(sourcePath)).Replace("\\", "/");
                        var isDirectory = AssetDatabase.IsValidFolder(sourcePath);
                        if (isDirectory)
                        {
                            Directory.CreateDirectory(destPath);
                        }
                        else
                        {
                            try
                            {
                                File.Delete(destPath);
                            }
                            catch { }//probably should rethrow some cases, such as access denied
                            Directory.CreateDirectory(Path.GetDirectoryName(destPath));
                        }

                        if (typeof(Texture2D).IsAssignableFrom(file.GetType()))
                        {
                            var texture = file as Texture2D;
                            FileUtil.CopyFileOrDirectory(sourcePath, destPath);
                            //File.WriteAllBytes(Path.Combine(outputPath, Path.GetFileName(textureAssetPath)), texture.EncodeToPNG());
                        }
                        else
                        {
                            FileUtil.CopyFileOrDirectory(sourcePath, destPath);
                        }
                    }
                }
            }
        }
    }
}