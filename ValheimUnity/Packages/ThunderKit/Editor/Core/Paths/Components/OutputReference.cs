using ThunderKit.Core.Pipelines;

namespace ThunderKit.Core.Paths.Components
{
    public class OutputReference : PathComponent
    {
        public PathReference reference;
        public override string GetPath(PathReference output, Pipeline pipeline) => reference.GetPath(pipeline);
    }
}
