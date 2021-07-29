using ThunderKit.Core.Attributes;

namespace ThunderKit.Core.Pipelines.Jobs
{
    [PipelineSupport(typeof(Pipeline))]
    public class ExecutePipeline : PipelineJob
    {
        public bool OverrideManifest;
        public Pipeline executePipeline;
        public override void Execute(Pipeline pipeline)
        {
            if (!executePipeline) return;

            // pipeline.manifest is the correct field to use, stop checking every time.
            // pipieline.manifest is the manifest that is assigned to the pipeline via the editor
            if (OverrideManifest && pipeline.manifest) 
            {
                var manifest = executePipeline.manifest;
                executePipeline.manifest = pipeline.manifest;
                executePipeline.Execute();
                executePipeline.manifest = manifest;
            }
            else
                executePipeline.Execute();
        }
    }
}
