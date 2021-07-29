using System;
using System.IO;
using ThunderKit.Core.Attributes;
using ThunderKit.Core.Paths;
using ThunderKit.Core.Editor;
using UnityEditor;
using UnityEngine;

namespace ThunderKit.Core.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline))]
    public class Copy : FlowPipelineJob
    {
        public bool Recursive;
        [Tooltip("While enabled, will error when the source is not found")]
        public bool SourceRequired;

        [PathReferenceResolver]
        public string Source;
        [PathReferenceResolver]
        public string Destination;

        protected override void ExecuteInternal(Pipeline pipeline)
        {
            var source = string.Empty;
            try
            {
                source = Source.Resolve(pipeline, this);
            }
            catch (Exception e)
            {
                if (SourceRequired) throw e;

            }
            if (SourceRequired && string.IsNullOrEmpty(source)) throw new ArgumentException($"Required {nameof(Source)} is empty");
            if (!SourceRequired && string.IsNullOrEmpty(source)) return;

            var destination = Destination.Resolve(pipeline, this);

            bool sourceIsFile = false;

            try
            {
                sourceIsFile = !File.GetAttributes(source).HasFlag(FileAttributes.Directory);
            }
            catch (Exception e)
            {
                if (SourceRequired) throw e;
            }

            if (Recursive)
            {
                if (!Directory.Exists(source)) return;
                else if (sourceIsFile)
                    throw new ArgumentException($"Source Error: Expected Directory, Recieved File {source}");
            }

            if (Recursive) FileUtil.ReplaceDirectory(source, destination);
            else
                FileUtil.ReplaceFile(source, destination);
        }
    }
}
