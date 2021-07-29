using System.Linq;

namespace ThunderKit.Core.Pipelines
{
    public abstract class FlowPipelineJob : PipelineJob
    {
        public bool PerManifest;
        public Manifests.Manifest[] ExcludedManifests;

        public sealed override void Execute(Pipeline pipeline)
        {
            if (PerManifest)
            {
                for (pipeline.ManifestIndex = 0;
                     pipeline.ManifestIndex < pipeline.Manifests.Length;
                     pipeline.ManifestIndex++)
                {
                    if (ExcludedManifests.Contains(pipeline.Manifest)) continue;

                    ExecuteInternal(pipeline);
                }
                pipeline.ManifestIndex = -1;
            }
            else
                ExecuteInternal(pipeline);
        }

        protected abstract void ExecuteInternal(Pipeline pipeline);
    }
}