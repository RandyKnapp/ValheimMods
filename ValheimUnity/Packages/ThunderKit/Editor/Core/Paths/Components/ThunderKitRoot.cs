using ThunderKit.Core.Manifests;
using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths.Components
{
    public class ThunderKitRoot : PathComponent
    {
        public override string GetPath(PathReference output, Pipeline pipeline) => "ThunderKit";
    }
}
