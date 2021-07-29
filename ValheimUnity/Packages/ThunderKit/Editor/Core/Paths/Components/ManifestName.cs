using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths.Components
{
    public class ManifestName : PathComponent
    {
        public override string GetPath(PathReference output, Pipeline pipeline) => pipeline.Manifest.Identity.Name;
    }
}
